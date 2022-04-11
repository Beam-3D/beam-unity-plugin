using System;
using System.Collections;
using Beam.Runtime.Client.Loaders.Base;
using Beam.Runtime.Client.Loaders.Events;
using Beam.Runtime.Client.Units;
using Beam.Runtime.Client.Units.Events;
using Beam.Runtime.Client.Units.Model;
using Beam.Runtime.Client.Utilities;
using Beam.Runtime.Sdk.Utilities;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;

namespace Beam.Runtime.Client.Loaders
{
  [RequireComponent(typeof(BeamVideoUnitInstance))]
  public class BeamBasicVideoLoader : BeamVideoLoader
  {
    public VideoClip Placeholder;
    [HideInInspector]
    public Renderer TargetRenderer;

    [SerializeField]
    private VideoPlayer videoPlayer;
    private AudioSource audioSource;
    private Texture2D billboard;
    private string targetMaterialProperty = "_MainTex";
    private BeamAspectRatioHandler beamAspectRatioHandler;

    private void OnEnable()
    {
      this.beamAspectRatioHandler = this.GetComponent<BeamAspectRatioHandler>();
      if (!this.videoPlayer)
      {
        this.videoPlayer = this.GetComponent<VideoPlayer>();

        if (!this.videoPlayer)
        {
          this.videoPlayer = this.gameObject.AddComponent<VideoPlayer>();
        }
      }

      this.audioSource = this.GetComponent<AudioSource>();
      this.TargetRenderer = this.GetComponent<Renderer>();
      this.SetupForRenderPipeline();
      this.videoPlayer.targetMaterialProperty = this.targetMaterialProperty;
      this.videoPlayer.prepareCompleted += this.VideoReady;
      this.videoPlayer.loopPointReached += this.VideoEnded;
      this.videoPlayer.started += this.VideoStarted;
      this.videoPlayer.SetTargetAudioSource(0, this.audioSource);

      if (!this.Placeholder)
      {
        return;
      }

      this.videoPlayer.source = VideoSource.VideoClip;
      this.videoPlayer.clip = this.Placeholder;
    }

    private void OnDisable()
    {
      if (!this.videoPlayer)
      {
        return;
      }

      this.videoPlayer.prepareCompleted -= this.VideoReady;
      this.videoPlayer.loopPointReached -= this.VideoEnded;
      this.videoPlayer.started -= this.VideoStarted;

    }

    public override void HandleFulfillment(VideoUnitFulfillmentData fulfillmentData)
    {
      if (fulfillmentData == null)
      {
        return;
      }

      string videoUrl = fulfillmentData.VideoUrl;
      string billboardUrl = fulfillmentData.BillboardUrl;

      this.videoPlayer.source = VideoSource.Url;
      this.videoPlayer.url = videoUrl;

      this.StartCoroutine(this.LoadBillboard(new Uri(billboardUrl)));
    }

    private void VideoReady(VideoPlayer player)
    {
      if (this.beamAspectRatioHandler != null)
      {
        this.beamAspectRatioHandler.HandleAspectRatio(this.videoPlayer.width, this.videoPlayer.height);
      }
      this.videoPlayer.Pause();
      this.TargetRenderer.material.mainTexture = this.billboard;
      this.TargetRenderer.enabled = true;
      this.OnVideoLoaded?.Invoke(new VideoLoadedData(this.videoPlayer.source));
      this.SetContentQuality(this.BeamUnitInstance.LodStatus);
    }

    private void VideoStarted(VideoPlayer player)
    {
      this.BeamVideoUnitInstance.LogStartedEvent();
    }

    private void VideoEnded(VideoPlayer player)
    {
      this.BeamVideoUnitInstance.LogEndedEvent();
    }

    public void ToggleMute(bool mute)
    {
      this.audioSource.mute = mute;
      this.BeamVideoUnitInstance.LogMutedEvent(mute);
    }

    public void TogglePause(bool pause)
    {
      if (pause)
      {
        this.videoPlayer.Pause();
      }
      else
      {
        this.videoPlayer.Play();
      }
      this.BeamVideoUnitInstance.LogPauseEvent(pause);
    }

    public void Stop()
    {
      this.videoPlayer.Stop();
      this.BeamVideoUnitInstance.LogStopEvent();
    }

    private void SetupForRenderPipeline()
    {
      UnityRenderPipeline pipeline = UtilDetermineRenderPipeline.GetRenderSettings();
      switch (pipeline)
      {
        case UnityRenderPipeline.Legacy:
          this.targetMaterialProperty = "_MainTex";
          break;
        case UnityRenderPipeline.UniversalRenderPipeline:
#if UNITY_2020_1_OR_NEWER
            case UnityRenderPipeline.UniversalRenderPipeline2020:
#endif
          this.targetMaterialProperty = "_BaseMap";
          break;
        case UnityRenderPipeline.HighDefRenderPipeline:
          this.targetMaterialProperty = "_BaseColorMap";
          break;
        case UnityRenderPipeline.Unknown:
        default:
          BeamLogger.LogWarning("Unable to determine render pipeline");
          break;
      }
    }

    private IEnumerator LoadBillboard(Uri uri)
    {
      UnityWebRequest www = UnityWebRequestTexture.GetTexture(uri);
      yield return www.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
    if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
#else
      if (www.isNetworkError || www.isHttpError)
#endif
      {
        BeamLogger.LogError(www.error);
      }
      else
      {
        this.billboard = ((DownloadHandlerTexture)www.downloadHandler).texture;
      }
    }

    protected override void HandleLodChange(LodStatus lodStatus)
    {
      this.SetContentQuality(lodStatus);
    }

    private void SetContentQuality(LodStatus status)
    {
      if (status == LodStatus.InsideHighQualityRange && this.videoPlayer != null)
      {
        this.TogglePause(false);
        this.videoPlayer.renderMode = VideoRenderMode.MaterialOverride;
      }
      else if (status == LodStatus.OutsideHighQualityRange && this.billboard != null)
      {
        this.TogglePause(true);
        this.videoPlayer.renderMode = VideoRenderMode.RenderTexture;
      }
    }
  }
}

