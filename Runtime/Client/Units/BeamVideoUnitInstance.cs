using Beam.Runtime.Client.Units.Events;
using Beam.Runtime.Client.Units.Model;
using Beam.Runtime.Sdk.Utilities;
using Beam.Runtime.Sdk.Generated.Model;
using UnityEngine;

namespace Beam.Runtime.Client.Units
{
  [ExecuteAlways]
  public class BeamVideoUnitInstance : BeamUnitInstance
  {
    [HideInInspector]
    [SerializeField]
    public VideoUnitFulfillmentEvent OnFulfillmentUpdated;

    public override void Awake()
    {
      base.Awake();

      this.Kind = AssetKind.Video;
    }

    public override void HandleFulfillment(IUnitFulfillmentResponse fulfillment)
    {
      IVideoContentResponseMetadata videoMetadata = fulfillment.Metadata as IVideoContentResponseMetadata;
      var unit = this.ProjectUnit.Unit;
      if (videoMetadata != null)
      {
        bool sameContent = this.HasSameContent(videoMetadata);
        BeamLogger.LogInfo($"Video Unit {unit.Id} fulfilled with {(sameContent ? "same content" : "new content")}");

        string videoUrl = videoMetadata.Content.Video.Url;
        string billboardUrl = videoMetadata.Content.Billboard.Url;

        this.ContentUrlHighQuality = videoUrl;

        UnitFulfillmentStatusCode statusCode = sameContent ? UnitFulfillmentStatusCode.CompletedWithSameContent : UnitFulfillmentStatusCode.CompletedWithContent;

        this.OnFulfillmentUpdated.Invoke(new VideoUnitFulfillmentData(statusCode, unit.Id, videoMetadata.Content.Video.Url, billboardUrl));
      }
      else if (fulfillment.Metadata == null)
      {
        BeamLogger.LogInfo($"Video Unit {unit.Id} was not fulfilled");
        this.OnFulfillmentUpdated.Invoke(new VideoUnitFulfillmentData(UnitFulfillmentStatusCode.CompletedEmpty, unit.Id));

        this.ContentUrlHighQuality = null;
        this.ContentUrlLowQuality = null;
      }
      else
      {
        BeamLogger.LogInfo($"Video Unit {unit.Id} was not fulfilled, metadata was incorrect type");
        this.OnFulfillmentUpdated.Invoke(new VideoUnitFulfillmentData(UnitFulfillmentStatusCode.Failed, unit.Id));

        this.ContentUrlHighQuality = null;
        this.ContentUrlLowQuality = null;
      }
      base.HandleFulfillment(fulfillment);
    }

    public void LogMutedEvent(bool videoMuted)
    {
      if (this.FulfillmentId == null)
      {
        return;
      }

      this.LogVideoEvent(videoMuted ? VideoEventActionKind.Muted : VideoEventActionKind.Unmuted);
    }

    public void LogPauseEvent(bool videoPaused)
    {
      this.LogVideoEvent(videoPaused ? VideoEventActionKind.Paused : VideoEventActionKind.Resumed);
    }

    public void LogStartedEvent()
    {
      this.LogVideoEvent(VideoEventActionKind.Start);
    }

    public void LogEndedEvent()
    {
      this.LogVideoEvent(VideoEventActionKind.End);
    }

    public void LogStopEvent()
    {
      this.LogVideoEvent(VideoEventActionKind.Stopped);
    }

    private void LogVideoEvent(VideoEventActionKind eventActionKind)
    {
      this.analyticsManager.LogVideoEvent(this.UnitInstance.Id, this.FulfillmentId, eventActionKind);
    }
    private bool HasSameContent(IVideoContentResponseMetadata videoMetadata)
    {
      return !string.IsNullOrWhiteSpace(this.ContentUrlHighQuality) && string.CompareOrdinal(this.ContentUrlHighQuality, videoMetadata.Content.Video.Url) == 0;
    }
  }
}
