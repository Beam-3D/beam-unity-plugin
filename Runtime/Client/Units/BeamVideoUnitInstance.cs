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

    public override void HandleFulfillment(UnitFulfillmentResponse fulfillment)
    {
      VideoContentResponseMetadata videoMetadata = fulfillment.Metadata as VideoContentResponseMetadata;
      var unit = this.ProjectUnit.Unit;
      if (videoMetadata != null)
      {
        BeamLogger.LogInfo($"Fulfilling Video Unit {unit.Id}");

        string videoUrl = videoMetadata.Content.Video.Url;
        string billboardUrl = videoMetadata.Content.Billboard.Url;

        this.ContentUrlHighQuality = videoUrl;

        this.OnFulfillmentUpdated.Invoke(new VideoUnitFulfillmentData(UnitFulfillmentStatusCode.CompletedWithContent, unit.Id, videoMetadata.Content.Video.Url, billboardUrl));

        base.HandleFulfillment(fulfillment);
      }
      else if (fulfillment.Metadata == null)
      {
        BeamLogger.LogInfo($"Video Unit {unit.Id} was not fulfilled");
        this.OnFulfillmentUpdated.Invoke(new VideoUnitFulfillmentData(UnitFulfillmentStatusCode.CompletedEmpty, unit.Id));
      }
      else
      {
        BeamLogger.LogInfo($"Video Unit {unit.Id} was not fulfilled, metadata was incorrect type");
        this.OnFulfillmentUpdated.Invoke(new VideoUnitFulfillmentData(UnitFulfillmentStatusCode.Failed, unit.Id));
      }
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

  }
}
