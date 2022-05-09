using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beam.Runtime.Client.Units;
using Beam.Runtime.Sdk.Utilities;
using Beam.Runtime.Sdk;
using Beam.Runtime.Sdk.Data;
using Beam.Runtime.Sdk.Generated.Model;
using UnityEngine;
using Beam.Runtime.Client.Utilities;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Beam.Runtime.Client
{
  public class SessionParameters
  {
    public Gender? Gender;
    public string DateOfBirth;
    public string[] UserTagIds;
  }
  public static class BeamClient
  {
    public static readonly BeamSdk Sdk;
    public static readonly BeamData Data;
    public static readonly BeamRuntimeData RuntimeData;
    public static ISession CurrentSession { 
      get { return RuntimeData.CurrentSession; }
      set { RuntimeData.CurrentSession = value; }
    }

    static BeamClient()
    {
      Sdk = new BeamSdk();
      Data = LoadOrInitData();
      RuntimeData = LoadOrInitRuntimeData();
    }

    private static BeamData LoadOrInitData()
    {
      var data = Resources.Load<BeamData>(BeamAssetPaths.BEAM_EDITOR_DATA_ASSET_PATH);

#if UNITY_EDITOR
      if (data == null)
      {
        Directory.CreateDirectory($"Assets/Resources/Beam");
        data = ScriptableObject.CreateInstance<BeamData>();
        AssetDatabase.CreateAsset(data, "Assets/Resources/Beam/BeamData.asset");
        AssetDatabase.SaveAssets();
      }
#endif

      return data;
    }

    private static BeamRuntimeData LoadOrInitRuntimeData()
    {
      var runtimeData = Resources.Load<BeamRuntimeData>(BeamAssetPaths.BEAM_RUNTIME_DATA_ASSET_PATH);

#if UNITY_EDITOR
      if (runtimeData == null)
      {
        Directory.CreateDirectory($"Assets/Resources/Beam");
        runtimeData = ScriptableObject.CreateInstance<BeamRuntimeData>();
        runtimeData.ClearData();
        AssetDatabase.CreateAsset(runtimeData, "Assets/Resources/Beam/BeamRuntimeData.asset");
        AssetDatabase.SaveAssets();
      }
#endif

      return runtimeData;
    }

    /// <summary>
    /// Starts the current session. Optionally provide a parameters object to specify gender and date of birth.
    /// </summary>
    public static async void StartSession(SessionParameters parameters = null)
    {
      if (string.IsNullOrWhiteSpace(RuntimeData.ProjectId))
      {
        BeamLogger.LogWarning("You must select a project before starting a session");
      }

      // Session already exists
      if (!string.IsNullOrEmpty(CurrentSession?.Id))
      {
        if (RuntimeData.AutoStartFulfillment)
        {
          StartAutomaticFulfillment();
        }

        return;
      }

      ICreatableSession sessionRequest = new ICreatableSession
      {
        ProjectId = RuntimeData.ProjectId,
        Environment = new IEnvironment
        {
          Engine = Engine.Unity,
          Version = Application.unityVersion
        },
        Device = new ICreatableDevice
        {
          DeviceId = SystemInfo.deviceUniqueIdentifier,
          System = $"{SystemInfo.operatingSystem} {SystemInfo.operatingSystemFamily}",
          Hmd = HmdHandler.GetHMD(),
          AdvertiserId = "TBC"
        },
        Consumer = new ICreatableConsumer
        {
          Language = Application.systemLanguage.ToString().Substring(0, 2).ToLower(),
          Location = "GB"
        },
        UserTagIds = new List<string>()
      };

#if UNITY_EDITOR
      // Mocking is only supported in editor
      if (Application.isEditor)
      {
        if (Data.MockSession != null && Data.MockDataEnabled)
        {
          sessionRequest = Data.GetSerializedMockData();
          sessionRequest.ProjectId = RuntimeData.ProjectId;
          BeamLogger.LogInfo("Session running with mocked data");
        }
      }
#endif

      // Add DOB and gender if specified
      if (parameters != null)
      {
        if (parameters.Gender != null)
        {
          sessionRequest.Consumer.Gender = parameters.Gender;
        }
        if (parameters.DateOfBirth != null)
        {
          sessionRequest.Consumer.Dob = parameters.DateOfBirth;
        }
        if (parameters.UserTagIds != null)
        {
          sessionRequest.UserTagIds = parameters.UserTagIds.ToList();
        }
      }

      CurrentSession = await Sdk.Session.StartSessionAsync(sessionRequest);

      BeamManagerHandler.GetAnalyticsManager().Init();

      if (RuntimeData.AutoStartFulfillment)
      {
        StartAutomaticFulfillment();
      }
    }

    /// <summary>
    /// Stops the current session if one is running
    /// </summary>
    public static async void StopSession()
    {
      if (CurrentSession == null || string.IsNullOrWhiteSpace(CurrentSession.Id))
      {
        BeamLogger.LogError("There is no current running session so it cannot be stopped");
        return;
      }
      await Sdk.Session.StopSessionAsync(CurrentSession.Id);
      BeamManagerHandler.GetAnalyticsManager().TrackSessionStop();
    }

    /// <summary>
    /// Adds a tag to the existing session. Cached server data in RuntimeData is checked
    /// </summary>
    public static async Task AddSessionUserTagByName(string tagName)
    {
      if (CurrentSession == null || string.IsNullOrWhiteSpace(CurrentSession.Id))
      {
        BeamLogger.LogWarning($"Cannot add session tag '{tagName}' before the session has started.");
      }

      ITag userTag = RuntimeData.UserTags.FirstOrDefault(t => t.Name == tagName);
      if (userTag == null)
      {
        BeamLogger.LogWarning($"User tag '{tagName}' does not match any from the server so it will be ignored.");
        return;
      }

      await Sdk.Session.AddTagAsync(CurrentSession?.Id, userTag.Id);
      BeamLogger.LogInfo($"User tag '{tagName}' added to current session");
    }


    /// <summary>
    /// Adds a tag to the existing session by its ID. Cached server data in RuntimeData is not checked
    /// </summary>
    public static async Task AddSessionUserTagById(string tagId)
    {
      if (CurrentSession == null || string.IsNullOrWhiteSpace(CurrentSession.Id))
      {
        BeamLogger.LogWarning($"Cannot add tag before the session has started.");
      }

      await Sdk.Session.AddTagAsync(CurrentSession?.Id, tagId);
      BeamLogger.LogInfo($"User tag with ID '{tagId}' added to current session");
    }

    /// <summary>
    /// Removes a tag from the existing session. Cached server data in RuntimeData is checked
    /// </summary>
    public static async Task RemoveSessionUserTagByName(string tagName)
    {
      if (CurrentSession == null || string.IsNullOrWhiteSpace(CurrentSession.Id))
      {
        BeamLogger.LogWarning($"Cannot remove tag '{tagName}' before the session has started.");
      }

      ITag userTag = RuntimeData.UserTags.FirstOrDefault(t => t.Name == tagName);
      if (userTag == null)
      {
        BeamLogger.LogWarning($"User tag '{tagName}' does not match any from the server so it will be ignored.");
        return;
      }

      await Sdk.Session.DeleteTagAsync(CurrentSession?.Id, userTag.Id);
      BeamLogger.LogInfo($"User tag '{tagName}' removed from current session");
    }


    /// <summary>
    /// Removes a tag from the existing session by its ID. Cached server data in RuntimeData is not checked
    /// </summary>
    public static async Task RemovesSessionUserTagById(string tagId)
    {
      if (CurrentSession == null || string.IsNullOrWhiteSpace(CurrentSession?.Id))
      {
        BeamLogger.LogWarning($"Cannot remove tag before the session has started.");
      }

      await Sdk.Session.DeleteTagAsync(CurrentSession?.Id, tagId);
      BeamLogger.LogInfo($"User tag with ID '{tagId}' removed from current session");
    }


    /// <summary>
    /// Runs fulfillment (and starts polling if enabled) for all 'Instant' fulfillments
    /// </summary>
    public static void StartAutomaticFulfillment()
    {
      var fulfillmentManager = BeamManagerHandler.GetFulfillmentManager();
      fulfillmentManager.StartCoroutine(fulfillmentManager.RunInstantFulfillments());

      if (Data.PollingEnabled)
      {
        fulfillmentManager.StartCoroutine(fulfillmentManager.PollFulfillment(Data.PollingRate));
      }
    }

    /// <summary>
    /// Runs fulfillment for the specified Unit(s) by id
    /// </summary>
    public static void StartManualFulfillment(BeamUnitInstance beamUnitInstance)
    {
      var fulfillmentManager = BeamManagerHandler.GetFulfillmentManager();
      fulfillmentManager.RunManualFulfillment(new List<BeamUnitInstance> { beamUnitInstance });
    }

    /// <summary>
    /// Runs fulfillment for the specified Unit(s) by id
    /// </summary>
    public static void StartManualFulfillment(List<BeamUnitInstance> beamUnitInstances)
    {
      var fulfillmentManager = BeamManagerHandler.GetFulfillmentManager();
      fulfillmentManager.RunManualFulfillment(beamUnitInstances);
    }
  }
}
