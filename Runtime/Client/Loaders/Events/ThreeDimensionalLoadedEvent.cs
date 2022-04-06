using System;
using Beam.Runtime.Client.Units.Model;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

namespace Beam.Runtime.Client.Loaders.Events
{
  [Serializable]
  public class ThreeDimensionalLoadedEvent : UnityEvent<ThreeDimensionalLoadedData>
  { }

  [Serializable]
  public class ThreeDimensionalLoadedData
  {
    public readonly Transform ParentTransform;
    public readonly LodStatus ForLodStatus;
    public ThreeDimensionalLoadedData(Transform parentTransform, LodStatus forLodStatus)
    {
      this.ParentTransform = parentTransform;
      this.ForLodStatus = forLodStatus;
    }
  }
}
