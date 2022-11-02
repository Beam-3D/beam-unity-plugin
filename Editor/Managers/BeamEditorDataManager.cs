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
    public static ILoginRequest LoginRequest = new ILoginRequest { Username = "", Password = "" };
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

    public static async void Init()
    {
      if (Application.isPlaying)
      {
        return;
      }

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
        BeamClient.Data.ImageQualities = await BeamClient.Sdk.Metadata.GetAllImageQualitiesAsync();

      async Task GetVideoQualities() =>
        BeamClient.Data.VideoQualities = await BeamClient.Sdk.Metadata.GetAllVideoQualitiesAsync();

      async Task GetModelQualities() => BeamClient.Data.ModelQualities =
        await BeamClient.Sdk.Metadata.GetAllThreeDimensionalQualitiesAsync();

      async Task GetAudioQualities() =>
        BeamClient.Data.AudioQualities = await BeamClient.Sdk.Metadata.GetAllAudioQualitiesAsync();

      async Task GetAspectRatios() =>
        BeamClient.Data.AspectRatios = await BeamClient.Sdk.Metadata.GetAllAspectRatiosAsync();

      async Task GetLanguages() => BeamClient.Data.Languages = await BeamClient.Sdk.Metadata.GetAllLanguagesAsync();
      async Task GetLocations() => BeamClient.Data.Locations = await BeamClient.Sdk.Metadata.GetAllLocationsAsync();
      async Task GetDevices() => BeamClient.Data.Devices = new List<string>((await BeamClient.Sdk.Metadata.GetAllDevicesAsync()).Select(x => x.Name));

      
      // TODO: make this work for >100 tags
      ITagQuery userTagQuery = new ITagQuery
      {
        Page = "1",
        PageSize = 100,
        Where = new List<IQueryWhereITag>
        {
          new IQueryWhereITag
          {
            Key = "type", Operator = QueryOperator.Equals, ValueOne = "User"
          }
        }
      };

      ICustomMetadataKeyQuery customMetadataKeyQuery = new ICustomMetadataKeyQuery
      {
        Page = "1",
        PageSize = 100
      };
      
      IDataSchemaQuery dataSchemaQuery = new IDataSchemaQuery
      {
        Page = "1",
        PageSize = 100
      };

      async Task GetUserTags() =>
        BeamClient.RuntimeData.UserTags = (await BeamClient.Sdk.Metadata.SearchTagsAsync(userTagQuery)).Items;

      async Task GetProjects() => BeamClient.Data.Projects =
        await BeamClient.Sdk.Projects.GetMyProjectsAsync(new IProjectsQuery(null, 100, new List<IQueryWhereIProject>()));

      async Task GetMetadataKeys() => BeamClient.RuntimeData.CustomMetadataKeys =
        (await BeamClient.Sdk.Metadata.SearchCustomMetadataKeysAsync(customMetadataKeyQuery))
        .Items;

      async Task GetDataSchemas() => BeamClient.Data.DataSchemas =
        (await BeamClient.Sdk.Metadata.SearchDataSchemasAsync(dataSchemaQuery)).Items;

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
          GetMetadataKeys(),
          GetDataSchemas()
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
      BeamClient.Data.CacheDate = DateTime.Now.AddHours(BeamClient.Data.HoursToCache).ToString("O");
      EditorUtility.SetDirty(BeamClient.Data);

      BeamClient.Data.SetSelectedProject(BeamClient.RuntimeData.ProjectId);

      if (BeamClient.Data.GetSelectedProject() == null)
      {
        BeamClient.Data.SceneUnits?.RemoveAll(su => BeamClient.Data.Scenes.All(scene => scene.Id != su.SceneId));
        DataUpdated?.Invoke(null, DataUpdateType.Units);
      }
      else
      {
        // TODO: Update this so that unit error warnings don't show if a scene hasn't been selected yet.
        await GetAreas(BeamClient.Data.GetSelectedProject().Id);
        await GetProjectApiKeys(BeamClient.Data.GetSelectedProject().Id);

      
        DataUpdated?.Invoke(null, DataUpdateType.Areas);
      }
    }

    public static async Task GetProjectApiKeys(string projectId)
    {
      BeamClient.RuntimeData.ProjectApiKeys = await BeamClient.Sdk.Projects.GetPublicProjectApiKeysAsync(projectId);
    }

    public static async Task GetAreas(string projectId)
    {
      BeamData beamData = BeamClient.Data;
      BeamRuntimeData beamRuntimeData = BeamClient.RuntimeData;

      try
      {
        areaFetchPending = true;
        DataUpdated?.Invoke(null, DataUpdateType.FetchingStatus);
        ISearchResponseIScene result =
          await BeamClient.Sdk.ProjectScenes.GetScenesByProjectIdAsync(projectId, new IScenesQuery(null, 100, null, null, projectId));
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
        ISearchResponseIProjectUnitWithInstances response =
          await BeamClient.Sdk.ProjectUnits.GetProjectUnitsBySceneIdAsync(
            areaId,
            new IProjectUnitsQuery
            (
              null,
              100,
              new List<IQueryWhereICoreProjectUnit>(),
              null,
              areaId
            )
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

    public static async Task SelectProject(IProject project)
    {
      BeamClient.Data.SetSelectedProject(project.Id);
      BeamClient.RuntimeData.ProjectId = project.Id;
      try
      {
        await GetAreas(project.Id);
        await GetProjectApiKeys(project.Id);
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
