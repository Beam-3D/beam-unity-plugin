using System;
using UnityEngine.Events;
using UnityEngine.Video;

namespace Beam.Runtime.Client.Loaders.Events
{
  [Serializable]
  public class VideoLoadedEvent : UnityEvent<VideoLoadedData>
  { }

  [Serializable]
  public class VideoLoadedData
  {
    public readonly VideoSource VideoSource;
    public VideoLoadedData(VideoSource videoSource)
    {
      this.VideoSource = videoSource;
    }
  }
}
