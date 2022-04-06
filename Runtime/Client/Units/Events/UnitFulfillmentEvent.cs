using System;
using Beam.Runtime.Client.Units.Model;
using UnityEngine.Events;

namespace Beam.Runtime.Client.Units.Events
{
  [Serializable]
  public class UnitFulfillmentEvent<UnitFulfillmentData> : UnityEvent<UnitFulfillmentData>
  { }

  public class UnitFulfillmentData
  {
    public UnitFulfillmentData() { }
    public UnitFulfillmentData(UnitFulfillmentStatusCode statusCode, string unitId)
    {
      this.StatusCode = statusCode;
      this.UnitId = unitId;
    }
    public UnitFulfillmentStatusCode StatusCode;
    public string UnitId;

  }
}
