using System;
using System.Collections;
using Beam.Runtime.Client.Loaders.Base;
using Beam.Runtime.Client.Loaders.Events;
using Beam.Runtime.Client.Units;
using Beam.Runtime.Client.Units.Events;
using Beam.Runtime.Client.Units.Model;
using Beam.Runtime.Sdk.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Beam.Runtime.Client.Loaders
{
  [RequireComponent(typeof(BeamAudioUnitInstance))]
  public class BeamBasicAudioLoader : BeamAudioLoader
  {
    public AudioClip Placeholder;
    [SerializeField]
    public AudioSource AudioSource;

    private AudioClip nextClip;
    private AudioClip loadedClip;

    public new void Awake()
    {
      base.Awake();

      if (this.AudioSource == null)
      {
        this.AudioSource = this.GetComponent<AudioSource>();
      }

      if (!this.Placeholder)
      {
        return;
      }

      this.AudioSource.clip = this.Placeholder;
    }

    public override void HandleFulfillment(AudioUnitFulfillmentData fulfillmentData)
    {
      if (fulfillmentData == null)
      {
        return;
      }
      this.StartCoroutine(this.LoadAudio(new Uri(fulfillmentData.AudioUrl)));
    }

    private IEnumerator LoadAudio(Uri uri)
    {
      AudioType audioType = AudioType.OGGVORBIS;

      // Checking audio type, as MPEG won't stream in the editor and .ogg won't stream in WebGL.
      using (UnityWebRequest www = UnityWebRequest.Head(uri))
      {
        yield return www.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
      if(www.result == UnityWebRequest.Result.ConnectionError)
#else
        if (www.isNetworkError)
#endif
        {
          BeamLogger.LogError(www.error);
        }
        else
        {
          if (www.GetResponseHeader("Content-Type").ToLower().Equals("audio/mpeg"))
            audioType = AudioType.MPEG;
        }
      }

#if UNITY_EDITOR
      if (Application.isEditor)
      {
        if (audioType == AudioType.MPEG && EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
          BeamLogger.LogWarning("Mpeg audio will only stream in a WebGL build");
        else if (audioType == AudioType.OGGVORBIS && EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL)
          BeamLogger.LogWarning("Ogg Vorbis audio will not stream in a WebGL build.");

        if (audioType == AudioType.MPEG)
          BeamLogger.LogInfo("Mpeg audio detected, skipping streaming in editor.");
      }
#endif

      // Skipping streaming if audio stream is incompatible
      if ((Application.platform != RuntimePlatform.WebGLPlayer || audioType != AudioType.MPEG) &&
          audioType == AudioType.MPEG)
      {
        yield break;
      }

      {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, audioType))
        {
          yield return www.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        if(www.result == UnityWebRequest.Result.ConnectionError)
#else
          if (www.isNetworkError)
#endif
          {
            BeamLogger.LogError(www.error);
          }
          else
          {
            this.nextClip = DownloadHandlerAudioClip.GetContent(www);
            this.OnAudioLoaded?.Invoke(new AudioLoadedData(this.nextClip));
          }
        }
      }
    }

    public void Update()
    {
      if (this.nextClip != null)
      {
        this.SwitchAudio();
      }
    }

    private void SwitchAudio()
    {
      this.StartCoroutine(this.DoSwitch());
    }

    private IEnumerator DoSwitch()
    {
      this.loadedClip = this.nextClip;
      this.nextClip = null;
      this.AudioSource.clip = this.loadedClip;
      this.AudioSource.enabled = true;
      this.AudioSource.Play();

      // TODO: Need to tidy this up, what if someone stops the clip? 
      this.BeamAudioUnitInstance.LogStartEvent();
      yield return new WaitForSeconds(this.loadedClip.length);
      this.BeamAudioUnitInstance.LogEndedEvent();
    }

    public void ToggleMute(bool mute)
    {
      this.AudioSource.mute = mute;
      this.BeamAudioUnitInstance.LogMutedEvent(mute);
    }

    public void TogglePause(bool pause)
    {
      if (pause)
      {
        this.AudioSource.Pause();
      }
      else
      {
        // Restarts the audio if finished, unpauses if paused.
        // May need to track if the audio has finished playing in case looping isn't desired.
        if (!this.AudioSource.isPlaying)
        {
          if (this.AudioSource.time == 0)
          {
            this.AudioSource.Play();
          }
          else
          {
            this.AudioSource.UnPause();
          }
        }
      }
      this.BeamAudioUnitInstance.LogPauseEvent(pause);
    }

    public void Stop()
    {
      this.AudioSource.Stop();
      this.BeamAudioUnitInstance.LogStopEvent();
    }

    protected override void HandleLodChange(LodStatus lodStatus)
    {
      bool inLodHqRange = lodStatus == LodStatus.InsideHighQualityRange;
      this.TogglePause(!inLodHqRange);
    }
  }
}
