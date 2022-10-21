using Beam.Runtime.Client.Units.Events;
using Beam.Runtime.Client.Units.Model;
using Beam.Runtime.Sdk.Generated.Model;
using Beam.Runtime.Sdk.Utilities;
using UnityEngine;

namespace Beam.Runtime.Client.Units
{
  public class BeamDataUnitInstance : BeamUnitInstance
  {
    [HideInInspector]
    [SerializeField]
    public DataUnitFulfillmentEvent OnFulfillmentUpdated;

    public override void Awake()
    {
      base.Awake();
      this.Kind = AssetKind.Data;
    }

    public override void HandleFulfillment(IUnitFulfillmentResponse fulfillment)
    {
      IDataContentResponseMetadata dataMetadata = fulfillment.Metadata as IDataContentResponseMetadata;
      var unit = this.ProjectUnit.Unit;
      if (dataMetadata != null)
      {
        bool sameContent = this.HasSameContent(dataMetadata);

        this.ContentUrlHighQuality = dataMetadata.Content.Url;

        BeamLogger.LogInfo($"Data Unit {unit.Id} fulfilled with {(sameContent ? "same content" : "new content")}");


        this.OnFulfillmentUpdated.Invoke(new DataUnitFulfillmentData(sameContent ? UnitFulfillmentStatusCode.CompletedWithSameContent : UnitFulfillmentStatusCode.CompletedWithContent, unit.Id, this.ContentUrlHighQuality));
      }
      else if (fulfillment.Metadata == null)
      {
        BeamLogger.LogInfo($"Data Unit {unit.Id} was not fulfilled (Completed empty)");
        this.OnFulfillmentUpdated.Invoke(new DataUnitFulfillmentData(UnitFulfillmentStatusCode.CompletedEmpty, unit.Id));

        this.ContentUrlHighQuality = null;
      }
      else
      {
        BeamLogger.LogInfo($"Data Unit {unit.Id} was not fulfilled (Failed, metadata was incorrect type)");
        this.OnFulfillmentUpdated.Invoke(new DataUnitFulfillmentData(UnitFulfillmentStatusCode.Failed, unit.Id));

        this.ContentUrlHighQuality = null;
        this.ContentUrlLowQuality = null;
      }
      base.HandleFulfillment(fulfillment);
    }

    // Assumes fulfilment was successful metadata is not null
    private bool HasSameContent(IDataContentResponseMetadata dataMetadata)
    {
      string url = dataMetadata.Content.Url;

      bool sameUrl = !string.IsNullOrWhiteSpace(this.ContentUrlHighQuality) && string.CompareOrdinal(this.ContentUrlHighQuality, url) == 0;

      // Same content, don't load
      return sameUrl;
    }
  }
}
