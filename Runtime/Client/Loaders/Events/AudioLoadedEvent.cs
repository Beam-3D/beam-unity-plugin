using System;
using UnityEngine;
using UnityEngine.Events;

namespace Beam.Runtime.Client.Loaders.Events
{
  [Serializable]
  public class AudioLoadedEvent : UnityEvent<AudioLoadedData>
  { }

  [Serializable]
  public class AudioLoadedData
  {
    public readonly AudioClip AudioClip;
    public AudioLoadedData(AudioClip audioClip)
    {
      this.AudioClip = audioClip;
    }
  }
}
