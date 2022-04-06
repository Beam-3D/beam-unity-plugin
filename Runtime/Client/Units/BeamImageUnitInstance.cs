using System;
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

    public override void HandleFulfillment(UnitFulfillmentResponse fulfillment)
    {
      base.HandleFulfillment(fulfillment);
      ImageContentResponseMetadata imageMetadata = fulfillment.Metadata as ImageContentResponseMetadata;
      var unit = this.ProjectUnit.Unit;
      if (imageMetadata != null)
      {
        if (this.HasSameContent(imageMetadata))
        {
          return;
        }

        this.ContentUrlHighQuality = imageMetadata.Content.High.Url;
        this.ContentUrlLowQuality = imageMetadata.Content.Low.Url;

        BeamLogger.LogInfo($"Fulfilling Image Unit (HQ) {unit.Id} with {this.ContentUrlHighQuality}");
        BeamLogger.LogInfo($"Fulfilling Image Unit (LQ) {unit.Id} with {this.ContentUrlLowQuality}");

        this.OnFulfillmentUpdated.Invoke(new ImageUnitFulfillmentData(UnitFulfillmentStatusCode.CompletedWithContent, unit.Id, this.ContentUrlHighQuality, this.ContentUrlLowQuality));
      }
      else if (fulfillment.Metadata == null)
      {
        BeamLogger.LogInfo($"Image Unit {unit.Id} was not fulfilled");
        this.OnFulfillmentUpdated.Invoke(new ImageUnitFulfillmentData(UnitFulfillmentStatusCode.CompletedEmpty, unit.Id));
      }
      else
      {
        BeamLogger.LogInfo($"Image Unit {unit.Id} was not fulfilled, metadata was incorrect type");
        this.OnFulfillmentUpdated.Invoke(new ImageUnitFulfillmentData(UnitFulfillmentStatusCode.Failed, unit.Id));
      }
    }

    // Assumes fulfilment was successful metadata is not null
    private bool HasSameContent(ImageContentResponseMetadata imageMetadata)
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
