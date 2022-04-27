using System;
using Beam.Runtime.Client.Units.Model;

namespace Beam.Runtime.Client.Units.Events
{
  [Serializable]
  public class VideoUnitFulfillmentEvent : UnitFulfillmentEvent<VideoUnitFulfillmentData>
  { }
  public class VideoUnitFulfillmentData : UnitFulfillmentData
  {
    public VideoUnitFulfillmentData(UnitFulfillmentStatusCode statusCode, string unitId, string videoUrl = null, string billboardUrl = null)
    {
      this.StatusCode = statusCode;
      this.UnitId = unitId;
      this.VideoUrl = videoUrl;
      this.BillboardUrl = billboardUrl;
    }

    public VideoUnitFulfillmentData(UnitFulfillmentData data)
    {
      this.StatusCode = data.StatusCode;
      this.UnitId = data.UnitId;
      this.VideoUrl = "";
      this.BillboardUrl = "";
    }

    public readonly string VideoUrl;
    public readonly string BillboardUrl;
  }
}
