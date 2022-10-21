using System;
using Beam.Runtime.Client.Units.Model;

namespace Beam.Runtime.Client.Units.Events
{
  [Serializable]
  public class DataUnitFulfillmentEvent : UnitFulfillmentEvent<DataUnitFulfillmentData>
  { }
  public class DataUnitFulfillmentData : UnitFulfillmentData
  {
    public DataUnitFulfillmentData(UnitFulfillmentStatusCode statusCode, string unitId, string url = null)
    {
      this.StatusCode = statusCode;
      this.UnitId = unitId;
      this.Url = url;
    }

    public DataUnitFulfillmentData(UnitFulfillmentData data)
    {
      this.StatusCode = data.StatusCode;
      this.UnitId = data.UnitId;
      this.Url = "";
    }

    public readonly string Url;
  }
}
