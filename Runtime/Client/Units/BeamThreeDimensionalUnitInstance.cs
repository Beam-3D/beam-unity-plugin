using Beam.Runtime.Client.Units.Events;
using Beam.Runtime.Client.Units.Model;
using Beam.Runtime.Sdk.Utilities;
using Beam.Runtime.Sdk.Extensions;
using Beam.Runtime.Sdk.Generated.Model;
using UnityEngine;

namespace Beam.Runtime.Client.Units
{
  public class BeamThreeDimensionalUnitInstance : BeamUnitInstance
  {
    [HideInInspector]
    [SerializeField]
    public ThreeDimensionalUnitFulfillmentEvent OnFulfillmentUpdated;

    public override void Awake()
    {
      this.Kind = AssetKind.ThreeDimensional;

      if (Application.isPlaying)
      {
        base.Awake();
      }
    }

    public override void HandleFulfillment(IUnitFulfillmentResponse fulfillment)
    {
      IThreeDimensionalContentResponseMetadata modelMetadata = fulfillment.Metadata as IThreeDimensionalContentResponseMetadata;
      var unit = this.ProjectUnit.Unit;
      if (modelMetadata != null)
      {
        bool sameContent = this.HasSameContent(modelMetadata);

        this.ContentUrlHighQuality = modelMetadata.Content.High.Url;
        this.ContentUrlLowQuality = modelMetadata.Content.Low.Url;

        BeamLogger.LogInfo($"3D Unit {unit.Id} fulfilled with {(sameContent ? "same content" : "new content")}");
        this.OnFulfillmentUpdated.Invoke(new ThreeDimensionalUnitFulfillmentData(sameContent ? UnitFulfillmentStatusCode.CompletedWithSameContent : UnitFulfillmentStatusCode.CompletedWithContent, unit.Id, this.ContentUrlHighQuality, this.ContentUrlLowQuality));
      }
      else if (fulfillment.Metadata == null)
      {
        this.ContentUrlHighQuality = null;
        this.ContentUrlLowQuality = null;

        BeamLogger.LogInfo($"3D Unit {unit.Id} was not fulfilled (Completed empty)");
        this.OnFulfillmentUpdated.Invoke(new ThreeDimensionalUnitFulfillmentData(UnitFulfillmentStatusCode.CompletedEmpty, unit.Id));
      }
      else
      {
        this.ContentUrlHighQuality = null;
        this.ContentUrlLowQuality = null;

        BeamLogger.LogInfo($"3D Unit {unit.Id} was not fulfilled (Failed, metadata was incorrect type)");
        this.OnFulfillmentUpdated.Invoke(new ThreeDimensionalUnitFulfillmentData(UnitFulfillmentStatusCode.Failed, unit.Id));
      }
      base.HandleFulfillment(fulfillment);
    }

    private bool HasSameContent(IThreeDimensionalContentResponseMetadata modelMetadata)
    {
      string hqUrl = modelMetadata.Content.High.Url;
      string lqUrl = modelMetadata.Content.Low.Url;

      bool sameHqUrl = !string.IsNullOrWhiteSpace(this.ContentUrlHighQuality) &&
                       string.CompareOrdinal(this.ContentUrlHighQuality, hqUrl) == 0;
      bool sameLqUrl = !string.IsNullOrWhiteSpace(this.ContentUrlLowQuality) &&
                       string.CompareOrdinal(this.ContentUrlLowQuality, lqUrl) == 0;

      // Same content, don't load
      return sameHqUrl && sameLqUrl;
    }
  }
}
