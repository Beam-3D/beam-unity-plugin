using System;
using Beam.Runtime.Client.Units.Model;
using UnityEngine;
using UnityEngine.Events;

namespace Beam.Runtime.Client.Loaders.Events
{
  [Serializable]
  public class ImageLoadedEvent : UnityEvent<ImageLoadedData>
  { }

  [Serializable]
  public class ImageLoadedData
  {
    public readonly Texture2D Image;
    public readonly LodStatus ForLodStatus;
    public ImageLoadedData(Texture2D image, LodStatus forLodStatus)
    {
      this.Image = image;
      this.ForLodStatus = forLodStatus;
    }
  }
}
