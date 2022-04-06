using Beam.Runtime.Client.Managers;
using Beam.Runtime.Sdk.Data;
using Beam.Runtime.Sdk.Utilities;
using UnityEngine;

namespace Beam.Runtime.Client.Utilities
{
  public class BeamManagerHandler
  {
    public static BeamFulfillmentManager GetFulfillmentManager()
    {
      var fulfillmentManager = UnityEngine.Object.FindObjectOfType<BeamFulfillmentManager>();
      if (fulfillmentManager != null)
      {
        return fulfillmentManager;
      }

      throw new System.Exception("[BEAM] Fulfillment manager not found.");
    }

    public static BeamAnalyticsManager GetAnalyticsManager()
    {
      var analyticsManager = UnityEngine.Object.FindObjectOfType<BeamAnalyticsManager>();
      if (analyticsManager != null)
      {
        return analyticsManager;
      }

      throw new System.Exception("[BEAM] Analytics manager not found.");
    }

    public static void CheckForManagers(bool addIfNotPresent)
    {
      bool sessionManagerPresent = Object.FindObjectOfType<BeamSessionManager>() != null;
      bool fulfillmentManagerPresent = Object.FindObjectOfType<BeamFulfillmentManager>() != null;
      bool analyticsManagerPresent = Object.FindObjectOfType<BeamAnalyticsManager>() != null;

      if (addIfNotPresent)
      {
        GameObject manager = GameObject.Find(BeamClient.RuntimeData.ManagerName);
        if (!manager)
        {
          manager = new GameObject(BeamClient.RuntimeData.ManagerName);
        }

        if (!sessionManagerPresent)
        {
          manager.AddComponent<BeamSessionManager>();
        }

        if (!fulfillmentManagerPresent)
        {
          manager.AddComponent<BeamFulfillmentManager>();
        }

        if (!analyticsManagerPresent)
        {
          manager.AddComponent<BeamAnalyticsManager>();
        }
      }
      else
      {
        if (!sessionManagerPresent)
        {
          BeamLogger.LogWarning("No BeamSessionManager component in scene. Beam Units will not be fulfilled.");
        }

        if (!fulfillmentManagerPresent)
        {
          BeamLogger.LogWarning("No BeamFulfillmentManager component in scene. Beam Units will not be fulfilled.");
        }
        if (!analyticsManagerPresent)
        {
          BeamLogger.LogWarning("No BeamAnalyticsManager component in scene. Analytics data will not be sent.");
        }
      }
    }

  }
}
