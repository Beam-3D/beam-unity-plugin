using System;
using Beam.Runtime.Client.Units.Model;
using UnityEngine;
using UnityEngine.Events;

namespace Beam.Runtime.Client.Loaders.Events
{
  [Serializable]
  public class DataLoadedEvent : UnityEvent<DataLoadedData>
  { }

  [Serializable]
  public class DataLoadedData
  {
    public readonly string Data;
    public readonly LodStatus ForLodStatus;
    public DataLoadedData(string data, LodStatus forLodStatus)
    {
      this.Data = data;
      this.ForLodStatus = forLodStatus;
    }
  }
}
