using System;
using Beam.Runtime.Client.Units.Model;

namespace Beam.Runtime.Client.Units.Events
{
  [Serializable]
  public class ImageUnitFulfillmentEvent : UnitFulfillmentEvent<ImageUnitFulfillmentData>
  { }
  public class ImageUnitFulfillmentData : UnitFulfillmentData
  {
    public ImageUnitFulfillmentData(UnitFulfillmentStatusCode statusCode, string unitId, string highQualityUrl = null, string lowQualityUrl = null)
    {
      this.StatusCode = statusCode;
      this.UnitId = unitId;
      this.HighQualityUrl = highQualityUrl;
      this.LowQualityUrl = lowQualityUrl;
    }

    public ImageUnitFulfillmentData(UnitFulfillmentData data)
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
