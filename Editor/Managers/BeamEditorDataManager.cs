using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Beam.Editor.Utilities;
using Beam.Runtime.Client;
using Beam.Runtime.Sdk;
using Beam.Runtime.Sdk.Data;
using Beam.Runtime.Sdk.Generated.Model;
using Beam.Runtime.Sdk.Model;
using UnityEditor;
using UnityEngine;

namespace Beam.Editor.Managers
{
  public enum DataUpdateType
  {
    All,
    AuthOnly,
    FetchingStatus,
    Units,
    Areas,
    Placements,
    Projects
  }

  internal static class BeamEditorDataManager
  {
    public static LoginRequest LoginRequest;
    public static event EventHandler<DataUpdateType> DataUpdated;
    //public static event EventHandler ProjectChanged;
    public static event EventHandler FetchErrorThrown;

    public static bool FetchPending
    {
      get
      {
        return authFetchPending || baseDataFetchPending || areaFetchPending || unitFetchPending;
      }
    }

    private static bool authFetchPending;
    private static bool baseDataFetchPending;
    private static bool areaFetchPending;
    private static bool unitFetchPending;


    static BeamEditorDataManager()
    {
      BeamEditorAuthManager.LoginStatusChanged += HandleLoginEvent;
      BeamEditorInstanceManager.PlacedInstancesChanged += HandlePlacedInstancesChanged;

      Init();
    }

    private static async void HandleLoginEvent(object sender, LoginEventStatus loginEventStatus)
    {
      switch (loginEventStatus)
      {
        case LoginEventStatus.LoginStarted:
          authFetchPending = true;
          break;
        case LoginEventStatus.UserLoggedIn:
        case LoginEventStatus.LoginFinished:
        case LoginEventStatus.UserLoggedOut:
          authFetchPending = false;
          break;
        default:
          break;
      }

      DataUpdated?.Invoke(null, DataUpdateType.AuthOnly);

      if (loginEventStatus == LoginEventStatus.UserLoggedIn)
      {
        await GetBaseData();
      }
    }

    private static void HandlePlacedInstancesChanged(object sender, EventArgs args)
    {
      DataUpdated?.Invoke(null, DataUpdateType.Placements);
    }

    private static async void Init()
    {
      if (Application.isPlaying)
      {
        return;
      }

      LoginRequest = new LoginRequest { Username = "", Password = "" };

      if (await BeamEditorAuthManager.CheckAuth())
      {
        await GetBaseData();
      }
    }

    public static void ChangeProject(bool force = false)
    {
      bool confirmChange = force || EditorUtility.DisplayDialog("Are you sure?",
        "Slots associated with the current project will no longer fulfill.", "Yes", "No");
      if (!confirmChange) return;

      BeamClient.Data.ClearData(false);
      BeamClient.RuntimeData.ClearData();
      EditorUtility.SetDirty(BeamClient.Data);
      DataUpdated?.Invoke(null, DataUpdateType.All);
      DataUpdated?.Invoke(null, DataUpdateType.Units);
    }

    public static async Task GetBaseData(bool skipCache = false)
    {
      if (Application.isPlaying || !await BeamEditorAuthManager.CheckAuth())
      {
        return;
      }

      if (!skipCache && !string.IsNullOrWhiteSpace(BeamClient.Data.CacheDate))
      {
        if (DateTime.Now < DateTime.Parse(BeamClient.Data.CacheDate))
        {
          return;
        }
      }

      baseDataFetchPending = true;
      DataUpdated?.Invoke(null, DataUpdateType.FetchingStatus);

      async Task GetImageQualities() =>
        BeamClient.Data.ImageQualities = await BeamClient.Sdk.Metadata.ImageQuality.ImageQualitiesAllGetAsync();

      async Task GetVideoQualities() =>
        BeamClient.Data.VideoQualities = await BeamClient.Sdk.Metadata.VideoQuality.VideoQualitiesAllGetAsync();

      async Task GetModelQualities() => BeamClient.Data.ModelQualities =
        await BeamClient.Sdk.Metadata.ThreeDimensionalQuality.Call3dQualitiesAllGetAsync();

      async Task GetAudioQualities() =>
        BeamClient.Data.AudioQualities = await BeamClient.Sdk.Metadata.AudioQuality.AudioQualitiesAllGetAsync();

      async Task GetAspectRatios() =>
        BeamClient.Data.AspectRatios = await BeamClient.Sdk.Metadata.AspectRatio.AspectRatiosAllGetAsync();

      async Task GetLanguages() => BeamClient.Data.Languages = await BeamClient.Sdk.Metadata.Language.LanguagesAllGetAsync();
      async Task GetLocations() => BeamClient.Data.Locations = await BeamClient.Sdk.Metadata.Location.LocationsAllGetAsync();
      async Task GetDevices() => BeamClient.Data.Devices = await BeamClient.Sdk.Metadata.Device.DevicesAllGetAsync();

      // TODO: make this work for >100 tags
      TagGetBody userTagQuery = new TagGetBody
      {
        Query = new TagQuery
        {
          Page = "1",
          PageSize = 100,
          Where = new List<QueryWhereITag>
          {
            new QueryWhereITag
            {
              Key = ITagPropertyKeysEnum.Type, Operator = QueryOperator.Equals, ValueOne = "User"
            }
          }
        }
      };

      CustomMetadataKeyGetBody customMetadataKeyQuery = new CustomMetadataKeyGetBody
      {
        Query = new CustomMetadataKeyQuery { Page = "1", PageSize = 100 }
      };

      async Task GetUserTags() =>
        BeamClient.RuntimeData.UserTags = (await BeamClient.Sdk.Metadata.Tag.TagsPostAsync(userTagQuery)).Items;

      async Task GetProjects() => BeamClient.Data.Projects =
        await BeamClient.Sdk.Publishing.Project.ProjectsMyGetAsync(null, 100, new List<QueryWhereIProject>());

      async Task GetMetadataKeys() => BeamClient.RuntimeData.CustomMetadataKeys =
        (await BeamClient.Sdk.Metadata.CustomMetadataKey.CustomMetadataKeySearchPostAsync(customMetadataKeyQuery))
        .Items;

      try
      {
        await Task.WhenAll(
          GetProjects(),
          GetImageQualities(),
          GetVideoQualities(),
          GetModelQualities(),
          GetAudioQualities(),
          GetAspectRatios(),
          GetLanguages(),
          GetLocations(),
          GetDevices(),
          GetUserTags(),
          GetMetadataKeys()
        );
      }
      finally
      {
        baseDataFetchPending = false;
        DataUpdated?.Invoke(null, DataUpdateType.FetchingStatus);
      }

      BeamClassGenerator.GenerateUserTagsClass(BeamClient.RuntimeData.UserTags);
      AssetDatabase.Refresh();

      DataUpdated?.Invoke(null, DataUpdateType.All);
      BeamClient.Data.CacheDate = DateTime.Now.AddHours(BeamClient.Data.HoursToCache).ToString("o");
      EditorUtility.SetDirty(BeamClient.Data);

      if (BeamClient.Data.GetSelectedProject() == null)
      {
        BeamClient.Data.SceneUnits?.RemoveAll(su => BeamClient.Data.Scenes.All(scene => scene.Id != su.SceneId));
        DataUpdated?.Invoke(null, DataUpdateType.Units);
      }
    }

    public static async Task GetAreas(string projectId)
    {
      BeamData beamData = BeamClient.Data;
      BeamRuntimeData beamRuntimeData = BeamClient.RuntimeData;

      try
      {
        areaFetchPending = true;
        DataUpdated?.Invoke(null, DataUpdateType.FetchingStatus);
        SearchResponseIScene result =
          await BeamClient.Sdk.Publishing.Scene.ProjectsIdScenesPostAsync(projectId, new ScenesQueryOmitProjectId());
        beamData.Scenes = result.Items;
      }
      finally
      {
        areaFetchPending = false;
        DataUpdated?.Invoke(null, DataUpdateType.FetchingStatus);
      }

      DataUpdated?.Invoke(null, DataUpdateType.Areas);

      if (beamData.Scenes.Any())
      {
        if (beamData.GetSelectedScene() == null)
        {
          IScene selectedArea = beamData.Scenes.FirstOrDefault();
          await SelectArea(selectedArea);
        }

      }

      beamData.SceneUnits.RemoveAll(su => beamData.Scenes.All(scene => scene.Id != su.SceneId));

      if (!beamData.Scenes.Any())
      {
        DataUpdated?.Invoke(null, DataUpdateType.Units);
      }


      EditorUtility.SetDirty(beamData);
      EditorUtility.SetDirty(beamRuntimeData);
    }

    public static async Task GetUnits(string areaId)
    {
      BeamData beamData = BeamClient.Data;
      BeamRuntimeData beamRuntimeData = BeamClient.RuntimeData;
      try
      {
        unitFetchPending = true;
        DataUpdated?.Invoke(null, DataUpdateType.FetchingStatus);
        // TODO: Make this work for more than 100 units.
        SearchResponseIProjectUnitWithInstancesResponse response =
          await BeamClient.Sdk.Publishing.ProjectUnit.ProjectsScenesSceneIdUnitsGetAsync(
            areaId,
            null,
            100,
            new List<QueryWhereIProjectUnit>()
          );


        if (response != null)
        {
          if (beamData.SceneUnits == null)
          {
            beamData.SceneUnits = new List<SceneUnits>();
          }

          SceneUnits existingAreaUnits = beamData.SceneUnits.FirstOrDefault(su => su.SceneId == areaId);
          SceneUnits newAreaUnits = new SceneUnits(areaId, response.Items);


          if (existingAreaUnits != null)
          {
            // Replace existing scene units with new scene units
            int index = beamData.SceneUnits.IndexOf(existingAreaUnits);
            beamData.SceneUnits[index] = newAreaUnits;
          }
          else
          {
            // New scene units, add now
            beamData.SceneUnits.Add(newAreaUnits);
          }
        }
      }
      finally
      {
        unitFetchPending = false;
        DataUpdated?.Invoke(null, DataUpdateType.FetchingStatus);
      }

      DataUpdated?.Invoke(null, DataUpdateType.Units);
    }

    public static async Task SelectProject(Project project)
    {
      BeamClient.Data.SetSelectedProject(project.Id);
      BeamClient.RuntimeData.ProjectId = project.Id;
      try
      {
        await GetAreas(project.Id);
      }
      catch
      {
        FetchErrorThrown?.Invoke(null, EventArgs.Empty);
      }
    }

    public static async Task SelectArea(IScene scene)
    {
      BeamClient.Data.SetSelectedScene(scene.Id);
      try
      {
        await GetUnits(scene.Id);
      }
      catch
      {
        FetchErrorThrown?.Invoke(null, EventArgs.Empty);
      }
    }

    public static void SelectAssetKind(AssetKind kind)
    {
      BeamClient.Data.SelectedAssetKind = kind;
      EditorUtility.SetDirty(BeamClient.Data);
    }
  }
}
