using System;
using Beam.Runtime.Client.Units.Model;

namespace Beam.Runtime.Client.Units.Events
{
  [Serializable]
  public class ThreeDimensionalUnitFulfillmentEvent : UnitFulfillmentEvent<ThreeDimensionalUnitFulfillmentData>
  { }
  public class ThreeDimensionalUnitFulfillmentData : UnitFulfillmentData
  {
    public ThreeDimensionalUnitFulfillmentData(UnitFulfillmentStatusCode statusCode, string unitId, string highQualityUrl = null, string lowQualityUrl = null)
    {
      this.StatusCode = statusCode;
      this.UnitId = unitId;
      this.HighQualityUrl = highQualityUrl;
      this.LowQualityUrl = lowQualityUrl;
    }

    public ThreeDimensionalUnitFulfillmentData(UnitFulfillmentData data)
    {
      this.StatusCode = data.StatusCode;
      this.UnitId = data.UnitId;
      this.HighQualityUrl = "";
      this.LowQualityUrl = "";
    }

    public readonly string HighQualityUrl;
    public readonly string LowQualityUrl;
  }
}
