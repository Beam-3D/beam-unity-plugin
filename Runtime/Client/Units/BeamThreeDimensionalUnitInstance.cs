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

    public override void HandleFulfillment(UnitFulfillmentResponse fulfillment)
    {
      ThreeDimensionalContentResponseMetadata modelMetadata = fulfillment.Metadata as ThreeDimensionalContentResponseMetadata;
      var unit = this.ProjectUnit.Unit;
      if (modelMetadata != null)
      {
        if (this.HasSameContent(modelMetadata))
        {
          return;
        }

        this.transform.Clear();

        this.ContentUrlHighQuality = modelMetadata.Content.High.Url;
        this.ContentUrlLowQuality = modelMetadata.Content.Low.Url;

        this.OnFulfillmentUpdated.Invoke(new ThreeDimensionalUnitFulfillmentData(UnitFulfillmentStatusCode.CompletedWithContent, unit.Id, this.ContentUrlHighQuality, this.ContentUrlLowQuality));

        base.HandleFulfillment(fulfillment);
      }
      else if (fulfillment.Metadata == null)
      {
        BeamLogger.LogInfo($"3D Unit {unit.Id} was not fulfilled");
        this.OnFulfillmentUpdated.Invoke(new ThreeDimensionalUnitFulfillmentData(UnitFulfillmentStatusCode.CompletedEmpty, unit.Id));
      }
      else
      {
        BeamLogger.LogInfo($"3D Unit {unit.Id} was not fulfilled, metadata was incorrect type");
        this.OnFulfillmentUpdated.Invoke(new ThreeDimensionalUnitFulfillmentData(UnitFulfillmentStatusCode.Failed, unit.Id));
      }
    }

    private bool HasSameContent(ThreeDimensionalContentResponseMetadata modelMetadata)
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
