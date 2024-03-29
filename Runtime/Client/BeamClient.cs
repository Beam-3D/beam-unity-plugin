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
using Beam.Runtime.Client.Managers;

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
    public bool CommenceAnalyticsOnSessionStart = false;
    public string[] ExternalIds;
  }

  public static class BeamClient
  {
    private static readonly string[] disallowedHeaders = new string[] { "Content-Type", "Accept" };
    public static BeamSdk Sdk
    {
      get
      {
        return beamSdk ?? (beamSdk = new BeamSdk());
      }
    }
    public static BeamData Data
    {
      get
      {
        return SerializedDataManager.Data;
      }
    }

    public static BeamRuntimeData RuntimeData
    {
      get
      {
        return SerializedDataManager.RuntimeData;
      }
    }
    public static readonly List<string> ActiveDynamicTags = new List<string>();

    private static BeamSdk beamSdk;

    public static ISession CurrentSession
    {
      get { return RuntimeData ? RuntimeData.CurrentSession : null; }
      set
      {
        if (RuntimeData)
        {
          RuntimeData.CurrentSession = value;
        }
      }
    }

    /// <summary>
    /// Starts the current session. Optionally provide a parameters object to specify gender and date of birth.
    /// </summary>
    public static async void StartSession(SessionParameters parameters = null)
    {
      bool hasApiKeys = RuntimeData.ProjectApiKeys.Any() && !string.IsNullOrEmpty(RuntimeData.ProjectApiKeys[0].ApiKey);

      if (!RuntimeData || (string.IsNullOrWhiteSpace(RuntimeData.ProjectId) && !hasApiKeys))
      {
        BeamLogger.LogWarning("A ProjectId or ProjectApiKey are usually required to start a session.");
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
          Location = "GB",
          ExternalIds = new List<string>()
        },
        UserTagIds = new List<string>()
      };

#if UNITY_EDITOR
      // Mocking is only supported in editor
      if (Application.isEditor)
      {
        if (Data && Data.MockSession != null && Data.MockDataEnabled)
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
        if (parameters.ExternalIds != null)
        {
          sessionRequest.Consumer.ExternalIds = parameters.ExternalIds.ToList();
        }
      }

      if (hasApiKeys)
      {
        sessionRequest.ProjectApiKey = RuntimeData.ProjectApiKeys[0].ApiKey;
      }

      CurrentSession = await Sdk.Session.StartSessionAsync(sessionRequest);

      BeamLogger.LogInfo($"Session Started with ID {CurrentSession.Id}");

      if (parameters != null && parameters.CommenceAnalyticsOnSessionStart)
      {
        BeamLogger.LogInfo($"Analytics set to start on session start.");
        StartAnalytics();
      }

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

      BeamLogger.LogInfo($"Stopping session with ID {CurrentSession.Id}");
      BeamManagerHandler.GetAnalyticsManager().TrackSessionStop();
      await Sdk.Session.StopSessionAsync(CurrentSession.Id);
      CurrentSession = null;
      if (RuntimeData)
      {
        RuntimeData.CurrentSession = null;
      }
    }

    /// <summary>
    /// Adds a tag to the existing session. Cached server data in RuntimeData is checked
    /// </summary>
    public static async Task AddSessionUserTagByName(string tagName)
    {
      if (CurrentSession == null || string.IsNullOrWhiteSpace(CurrentSession.Id))
      {
        BeamLogger.LogWarning($"Cannot add session tag '{tagName}' before the session has started.");
        return;
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
        return;
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
    public static async Task RemoveSessionUserTagById(string tagId)
    {
      if (CurrentSession == null || string.IsNullOrWhiteSpace(CurrentSession?.Id))
      {
        BeamLogger.LogWarning($"Cannot remove tag before the session has started.");
        return;
      }

      await Sdk.Session.DeleteTagAsync(CurrentSession?.Id, tagId);
      BeamLogger.LogInfo($"User tag with ID '{tagId}' removed from current session");
    }

    /// <summary>
    /// Adds a dynamic tag to future fulfillment requests
    /// </summary>
    /// <param name="tag"></param>
    public static void AddDynamicTag(string tag)
    {
      if (!RuntimeData.DynamicTags.Contains(tag) && !ActiveDynamicTags.Contains(tag))
      {
        BeamLogger.LogInfo($"Adding dynamic tag \"{tag}\" to future fulfillment requests");
        ActiveDynamicTags.Add(tag);
        return;
      }
      BeamLogger.LogInfo($"Failed to add dynamic tag \"{tag}\"; tag already exists");
    }

    /// <summary>
    /// Removes a dynamic tag from future fulfillment requests
    /// </summary>
    /// <param name="tag"></param>
    public static void RemoveDynamicTag(string tag)
    {
      if (ActiveDynamicTags.Contains(tag))
      {
        ActiveDynamicTags.Remove(tag);
        BeamLogger.LogInfo($"Dynamic tag \"{tag}\" successfully removed");
        return;
      }
      if (RuntimeData.DynamicTags.Contains(tag))
      {
        BeamLogger.LogInfo($"Failed to remove dynamic tag \"{tag}\"; Tag included in BeamRuntimeData");
        return;
      }
      BeamLogger.LogInfo($"Failed to remove dynamic tag \"{tag}\"; tag not currently in use");
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
    /// <param name="beamUnitInstance">The instance to fulfill</param>
    /// <param name="dynamicTags">Optional dynamic tags to apply for fulfillment</param>
    public static void StartManualFulfillment(BeamUnitInstance beamUnitInstance, List<string> dynamicTags = null)
    {
      var fulfillmentManager = BeamManagerHandler.GetFulfillmentManager();
      fulfillmentManager.RunManualFulfillment(new List<BeamUnitInstance> { beamUnitInstance }, dynamicTags);
    }

    /// <summary>
    /// Runs fulfillment for the specified Unit(s) by id
    /// </summary>
    /// <param name="beamUnitInstances">The instances to fulfill</param>
    /// <param name="dynamicTags">Optional dynamic tags to apply for fulfillment</param>
    public static void StartManualFulfillment(List<BeamUnitInstance> beamUnitInstances, List<string> dynamicTags = null)
    {
      var fulfillmentManager = BeamManagerHandler.GetFulfillmentManager();
      fulfillmentManager.RunManualFulfillment(beamUnitInstances, dynamicTags);
    }

    /// <summary>
    /// Starts the collection of analytics data.
    /// </summary>
    /// <remarks>
    /// A Session must have been started in order to collect analytics.
    /// 
    /// User consent should be obtained before collecting analytics data.
    /// </remarks>
    public static void StartAnalytics()
    {
      if (CurrentSession == null || string.IsNullOrWhiteSpace(CurrentSession.Id))
      {
        BeamLogger.LogError("Session must be started for analytics to run.");
        return;
      }

      BeamAnalyticsManager analyticsManager = BeamManagerHandler.GetAnalyticsManager();
      analyticsManager.Init();
    }

    public static void StopAnalytics()
    {
      if (CurrentSession == null || string.IsNullOrWhiteSpace(CurrentSession.Id))
      {
        BeamLogger.LogError("No current session, analytics are not running.");
        return;
      }

      BeamAnalyticsManager analyticsManager = BeamManagerHandler.GetAnalyticsManager();
      analyticsManager.StopAnalytics();
    }

    public static void AddCustomHeader(string key, string value)
    {
      if (RuntimeData.CustomRuntimeHeaders.Any(ch => ch.Key == key))
      {
        BeamLogger.LogError($"A header with key {key} already exists.");
        return;
      }

      if (disallowedHeaders.Contains(key))
      {
        BeamLogger.LogError($"Overriding the {key} header is restricted as it will cause issues with Beam API commuincation");
        return;
      }

      RuntimeData.CustomRuntimeHeaders.Add(new Model.CustomHeader { Key = key, Value = value });
    }

    public static void RemoveCustomHeader(string key)
    {
      if (!RuntimeData.CustomRuntimeHeaders.Any(ch => ch.Key == key))
      {
        BeamLogger.LogError($"No header with key {key} exists.");
        return;
      }

      RuntimeData.CustomRuntimeHeaders.Remove(RuntimeData.CustomRuntimeHeaders.FirstOrDefault(ch => ch.Key == key));
    }
  }
}
