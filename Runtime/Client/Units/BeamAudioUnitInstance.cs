using Beam.Runtime.Client.Units.Events;
using Beam.Runtime.Client.Units.Model;
using Beam.Runtime.Sdk.Utilities;
using Beam.Runtime.Sdk.Generated.Model;
using UnityEngine;

namespace Beam.Runtime.Client.Units
{

  [ExecuteAlways]
  public class BeamAudioUnitInstance : BeamUnitInstance
  {
    [HideInInspector]
    [SerializeField]
    public AudioUnitFulfillmentEvent OnFulfillmentUpdated;

    public override void Awake()
    {
      base.Awake();
      this.Kind = AssetKind.Audio;
    }

    public void LogMutedEvent(bool audioMuted)
    {
      if (this.FulfillmentId == null)
      {
        return;
      }

      this.LogAudioEvent(audioMuted ? AudioEventActionKind.Muted : AudioEventActionKind.Unmuted);
    }

    public void LogPauseEvent(bool audioPaused)
    {
      this.LogAudioEvent(audioPaused ? AudioEventActionKind.Paused : AudioEventActionKind.Resumed);
    }

    public void LogStartEvent()
    {
      this.LogAudioEvent(AudioEventActionKind.Start);
    }

    public void LogEndedEvent()
    {
      this.LogAudioEvent(AudioEventActionKind.End);
    }

    public void LogStopEvent()
    {
      this.LogAudioEvent(AudioEventActionKind.Stopped);
    }
    private void LogAudioEvent(AudioEventActionKind eventActionKind)
    {
      this.analyticsManager.LogAudioEvent(this.UnitInstance.Id, this.FulfillmentId, eventActionKind);
    }


    public override void HandleFulfillment(IUnitFulfillmentResponse fulfillment)
    {
      IAudioContentResponseMetadata audioMetadata = fulfillment.Metadata as IAudioContentResponseMetadata;
      var unit = this.ProjectUnit.Unit;
      if (audioMetadata != null)
      {
        bool sameContent = this.HasSameContent(audioMetadata);

        this.ContentUrlHighQuality = audioMetadata.Content.Url;
        BeamLogger.LogInfo($"Audio Unit {unit.Id} fulfilled with {(sameContent ? "same content" : "new content")}");

        this.OnFulfillmentUpdated.Invoke(new AudioUnitFulfillmentData(sameContent ? UnitFulfillmentStatusCode.CompletedWithSameContent : UnitFulfillmentStatusCode.CompletedWithContent, unit.Id, this.ContentUrlHighQuality));
        base.HandleFulfillment(fulfillment);
      }
      else if (fulfillment.Metadata == null)
      {
        BeamLogger.LogInfo($"Audio Unit {unit.Id} was not fulfilled (Completed empty)");
        this.OnFulfillmentUpdated.Invoke(new AudioUnitFulfillmentData(UnitFulfillmentStatusCode.CompletedEmpty, unit.Id));

        this.ContentUrlHighQuality = null;
        this.ContentUrlLowQuality = null;
      }
      else
      {
        BeamLogger.LogInfo($"Audio Unit {unit.Id} was not fulfilled (Failed, metadata was incorrect type)");
        this.OnFulfillmentUpdated.Invoke(new AudioUnitFulfillmentData(UnitFulfillmentStatusCode.Failed, unit.Id));

        this.ContentUrlHighQuality = null;
        this.ContentUrlLowQuality = null;
      }
      base.HandleFulfillment(fulfillment);
    }

    // Assumes fulfillment was successful and audioMetadata is not null
    private bool HasSameContent(IAudioContentResponseMetadata audioMetadata)
    {
      return !string.IsNullOrWhiteSpace(this.ContentUrlHighQuality) && string.CompareOrdinal(this.ContentUrlHighQuality, audioMetadata.Content.Url) == 0;
    }
  }
}
