using Beam.Runtime.Client.Units.Events;
using Beam.Runtime.Client.Units.Model;
using Beam.Runtime.Sdk.Generated.Model;
using Beam.Runtime.Sdk.Utilities;
using UnityEngine;

namespace Beam.Runtime.Client.Units
{
  [ExecuteAlways]
  public class BeamImageUnitInstance : BeamUnitInstance
  {
    [HideInInspector]
    [SerializeField]
    public ImageUnitFulfillmentEvent OnFulfillmentUpdated;

    public override void Awake()
    {
      base.Awake();
      this.Kind = AssetKind.Image;
    }

    public override void HandleFulfillment(IUnitFulfillmentResponse fulfillment)
    {
      IImageContentResponseMetadata imageMetadata = fulfillment.Metadata as IImageContentResponseMetadata;
      var unit = this.ProjectUnit.Unit;
      if (imageMetadata != null)
      {
        bool sameContent = this.HasSameContent(imageMetadata);

        this.ContentUrlHighQuality = imageMetadata.Content.High.Url;
        this.ContentUrlLowQuality = imageMetadata.Content.Low.Url;

        BeamLogger.LogInfo($"Image Unit {unit.Id} fulfilled with {(sameContent ? "same content" : "new content")}");


        this.OnFulfillmentUpdated.Invoke(new ImageUnitFulfillmentData(sameContent ? UnitFulfillmentStatusCode.CompletedWithSameContent : UnitFulfillmentStatusCode.CompletedWithContent, unit.Id, this.ContentUrlHighQuality, this.ContentUrlLowQuality));
      }
      else if (fulfillment.Metadata == null)
      {
        BeamLogger.LogInfo($"Image Unit {unit.Id} was not fulfilled (Completed empty)");
        this.OnFulfillmentUpdated.Invoke(new ImageUnitFulfillmentData(UnitFulfillmentStatusCode.CompletedEmpty, unit.Id));

        this.ContentUrlHighQuality = null;
        this.ContentUrlLowQuality = null;
      }
      else
      {
        BeamLogger.LogInfo($"Image Unit {unit.Id} was not fulfilled (Failed, metadata was incorrect type)");
        this.OnFulfillmentUpdated.Invoke(new ImageUnitFulfillmentData(UnitFulfillmentStatusCode.Failed, unit.Id));

        this.ContentUrlHighQuality = null;
        this.ContentUrlLowQuality = null;
      }
      base.HandleFulfillment(fulfillment);
    }

    // Assumes fulfilment was successful metadata is not null
    private bool HasSameContent(IImageContentResponseMetadata imageMetadata)
    {
      string hqUrl = imageMetadata.Content.High.Url;
      string lqUrl = imageMetadata.Content.Low.Url;

      bool sameHqUrl = !string.IsNullOrWhiteSpace(this.ContentUrlHighQuality) && string.CompareOrdinal(this.ContentUrlHighQuality, hqUrl) == 0;
      bool sameLqUrl = !string.IsNullOrWhiteSpace(this.ContentUrlLowQuality) && string.CompareOrdinal(this.ContentUrlLowQuality, lqUrl) == 0;

      // Same content, don't load
      return sameHqUrl && sameLqUrl;
    }
  }
}
