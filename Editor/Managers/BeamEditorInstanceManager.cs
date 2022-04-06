using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Beam.Runtime.Sdk.Data;
using Beam.Runtime.Sdk.Utilities;
using Beam.Runtime.Sdk.Generated.Model;
using Beam.Runtime.Client.Managers;
using Beam.Runtime.Client.Units;

using ProjectUnit = Beam.Runtime.Sdk.Model.ProjectUnit;
using AspectRatio = Beam.Runtime.Sdk.Generated.Model.AspectRatio;
using Beam.Runtime.Client;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Beam.Editor.Managers
{
  [InitializeOnLoad]
  public static class BeamEditorInstanceManager
  {

    public static List<BeamUnitInstance> PlacedInstances { get; private set; } = new List<BeamUnitInstance>();
    public static event EventHandler PlacedInstancesChanged;

    private static List<ProjectUnit> PlacedUnits { get; set; } = new List<ProjectUnit>();
    private static BeamAreaBoundsManager areaBoundsManager;

    static BeamEditorInstanceManager()
    {
      EditorSceneManager.sceneOpened += HandleSceneLoaded;
      BeamEditorDataManager.DataUpdated += CheckCurrentSceneIsValid;

      EditorApplication.delayCall += CheckCurrentSceneIsValid;
    }

    public static void UpdatePlacements()
    {
      UpdatePlacedInstances();
      UpdatePlacedUnits();
    }

    private static void UpdatePlacedInstances()
    {
      List<BeamUnitInstance> instancesInScene = Object.FindObjectsOfType<BeamUnitInstance>().Where(au => au.ProjectUnit != null).ToList();

      List<BeamUnitInstance> removedInstances = PlacedInstances
        .Where(pi => instancesInScene.All(si => si.UnitInstance.Id != pi.UnitInstance.Id)).ToList();
      removedInstances.ForEach(instance => instance.InstanceDeleted -= HandleInstanceRemoved);

      List<BeamUnitInstance> addedInstances = instancesInScene
        .Where(pi => PlacedInstances.All(si => si.UnitInstance.Id != pi.UnitInstance.Id)).ToList();
      addedInstances.ForEach(instance => instance.InstanceDeleted += HandleInstanceRemoved);

      PlacedInstances = instancesInScene;
      PlacedInstancesChanged?.Invoke(null, EventArgs.Empty);
    }

    private static void UpdatePlacedUnits()
    {
      PlacedUnits = PlacedInstances.Select(ins => ins.ProjectUnit).Distinct(new ProjectUnitEqualityComparer())
        .ToList();

      // TODO: remove the dependency on this mapping. Should be able to just load BeamData
      PlacedUnits.ForEach(unit =>
      {
        ProjectUnit matchingProjectUnit = BeamClient.Data.SceneUnits?.SelectMany(su => su.ProjectUnits
          .Where(pu => pu.Unit.Id == unit.Unit.Id)).FirstOrDefault();

        // Should already be handled when checking for missing units.
        if (matchingProjectUnit == null)
        {
          return;
        }

        unit.Unit = matchingProjectUnit.Unit;
        unit.AudioMetadata = matchingProjectUnit.AudioMetadata;
        unit.ThreeDimensionalMetadata = matchingProjectUnit.ThreeDimensionalMetadata;
        unit.ImageMetadata = matchingProjectUnit.ImageMetadata;
        unit.VideoMetadata = matchingProjectUnit.VideoMetadata;
      });
    }

    private static void CheckCurrentSceneIsValid()
    {
      EditorApplication.delayCall -= CheckCurrentSceneIsValid;

      UpdatePlacedInstances();
      UpdatePlacedUnits();
      CheckForRemovedInstances();
      CheckForRemovedUnits();
    }

    private static void HandleSceneLoaded(Scene scene, OpenSceneMode openSceneMode)
    {
      CheckCurrentSceneIsValid();
    }

    private static void CheckCurrentSceneIsValid(object sender, DataUpdateType dataUpdateType)
    {
      if (dataUpdateType == DataUpdateType.Units)
      {
        CheckCurrentSceneIsValid();
      }
    }

    public static List<BeamUnitInstance> GetInstancesByUnitId(string unitId)
    {
      return PlacedInstances == null
        ? new List<BeamUnitInstance>()
        : PlacedInstances.Where(au => au.ProjectUnit.Unit.Id == unitId).ToList();
    }

    private static void HandleInstanceRemoved(object sender, string instanceId)
    {
      PlacedInstances.Remove(sender as BeamUnitInstance);
      CheckForRemovedInstances(hideWarning: true);
      PlacedInstancesChanged?.Invoke(null, EventArgs.Empty);
    }

    public static bool IsInstancePlaced(ProjectUnitInstance unitInstance)
    {
      if (unitInstance == null || PlacedInstances == null || !PlacedInstances.Any())
      {
        return false;
      }

      return PlacedInstances.Any(au => au != null && au.UnitInstance.Id == unitInstance.Id);
    }

    public static bool IsInstanceDuplicate(ProjectUnitInstance unitInstance)
    {
      if (unitInstance == null || PlacedInstances == null || !PlacedInstances.Any())
      {
        return false;
      }

      return PlacedInstances.Count(au => au != null && au.UnitInstance.Id == unitInstance.Id) > 1;
    }


    public static void RemoveUnitFromScene(ProjectUnitInstance unitInstance)
    {
      BeamUnitInstance au = PlacedInstances.FirstOrDefault(g => g.UnitInstance.Id == unitInstance.Id);
      if (au != null)
      {
        GameObject go = au.gameObject;
        PlacedInstances.Remove(au);
        Object.DestroyImmediate(go);
        PlacedInstancesChanged?.Invoke(null, EventArgs.Empty);
      }
      else
      {
        BeamLogger.LogWarning($"Can't remove unit {unitInstance.Id} as it doesn't exist");
      }
    }

    public static void AddUnitInstanceToScene(
      AssetKind kind,
      ProjectUnit projectUnit,
      ProjectUnitInstance unitInstance
    )
    {
      ProjectUnitWithInstancesResponse unit = projectUnit.Unit;

      BeamSessionManager sessionManager = Object.FindObjectOfType<BeamSessionManager>();
      if (sessionManager == null)
      {
        BeamLogger.LogError(
          "A Beam Session manager is required in all scenes with Beam Units. Please open the Beam Window to add a Beam Manager before trying to add units.");
        return;
      }

      GameObject go = Object.Instantiate(Resources.Load<GameObject>($"Prefabs/Beam{kind.ToString()}Unit"),
        sessionManager.transform, true);
      go.name = $"[BEAM] {kind} - {unit.Name}";
      Undo.RegisterCreatedObjectUndo(go, "Create object");

      if (Selection.activeGameObject != null)
      {
        GameObject sObject = Selection.activeGameObject;
        go.transform.position = sObject.transform.position;
        go.transform.rotation *= sObject.transform.rotation;
      }

      Selection.activeGameObject = go;

      BeamUnitInstance beamUnit = go.GetComponent<BeamUnitInstance>();
      beamUnit.ProjectUnit = projectUnit;
      beamUnit.UnitInstance = unitInstance;

      BeamData beamData = Resources.Load<BeamData>(BeamAssetPaths.BEAM_EDITOR_DATA_ASSET_PATH);
      AspectRatio aspectRatio = projectUnit.GetAspectRatioId(beamData);

      // Handle aspect ratio
      if (aspectRatio != null)
      {
        float ratio = AspectRatioHelper.GetRatioMultiplier(aspectRatio);
        beamUnit.transform.localScale = new Vector3(1 * ratio, 1, 1);
      }

      if (!areaBoundsManager)
      {
        areaBoundsManager = Object.FindObjectOfType<BeamAreaBoundsManager>();
      }

      if (areaBoundsManager)
      {
        areaBoundsManager.HandleUnitAdded(beamUnit);
      }

      beamUnit.InstanceDeleted += HandleInstanceRemoved;

      PlacedInstances.Add(beamUnit);
      PlacedInstancesChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void ClearPlacements()
    {
      PlacedInstances?.ForEach(go => { RemoveUnitFromScene(go.UnitInstance); });
      PlacedInstancesChanged?.Invoke(null, EventArgs.Empty);
    }

    private static void CheckForRemovedUnits()
    {
      PlacedInstances.ForEach(placedInstance =>
      {
        if (!PlacedUnitExists(placedInstance) && !string.IsNullOrEmpty(placedInstance.ProjectUnit.Unit.Id))
        {
          BeamLogger.LogError($"{placedInstance.ProjectUnit.Unit.Name} has been deleted and will not be fulfilled, " +
                         $"please assign a new unit to gameObject {placedInstance.gameObject.name}.");
        }
      });
    }

    private static void CheckForRemovedInstances(bool hideWarning = false)
    {
      List<BeamUnitInstance> removedInstances = PlacedInstances
        .Where(placedInstance => PlacedUnitExists(placedInstance) && !PlacedInstanceExists(placedInstance)).ToList();

      if (!removedInstances.Any())
      {
        return;
      }

      List<BeamUnitInstance> irreplaceableInstances = removedInstances;

      // Replace removed instance with if an unused instance is available
      if (!Application.isPlaying)
      {
        irreplaceableInstances = removedInstances.Where(instance =>
        {
          // Select all instances of the the unit the removed instance belongs to that aren't placed.
          List<ProjectUnitInstance> availableUnits = BeamClient.Data.SceneUnits.SelectMany(sceneUnit => sceneUnit.ProjectUnits)
            .Where(unit => instance.ProjectUnit.Unit.Id == unit.Unit.Id).ToList()
            .SelectMany(projectUnit => projectUnit.Unit.Instances)
            .Where(i => !PlacedInstances.Select(pi => pi.UnitInstance).Contains(i)).ToList();

          if (!availableUnits.Any()) return true;

          instance.UnitInstance = availableUnits.First();
          UpdatePlacedInstances();
          return false;
        }).ToList();
      }

      if (hideWarning) return;

      irreplaceableInstances.ForEach(placedInstance =>
      {
        IEnumerable<BeamUnitInstance> invalidPlacedInstances = PlacedInstances
          .Where(instance => instance.UnitInstance.UnitId == placedInstance.UnitInstance.UnitId);

        string errorString =
          $"Instance of {placedInstance.ProjectUnit.Unit.Name} has been deleted and will not be fulfilled, " +
          $"please assign a new instance to one of the following gameObjects: \n";

        foreach (BeamUnitInstance bui in invalidPlacedInstances) errorString += (bui.gameObject.name + "\n");

        BeamLogger.LogError(errorString);
      });
    }

    private static bool PlacedUnitExists(BeamUnitInstance placedInstance)
    {
      return BeamClient.Data.SceneUnits?.Any(sceneUnit =>
        sceneUnit.ProjectUnits.Any(pu => pu.Unit.Id == placedInstance.ProjectUnit.Unit.Id)) ?? false;
    }

    private static bool PlacedInstanceExists(BeamUnitInstance placedInstance)
    {
      BeamData beamData = Resources.Load<BeamData>(BeamAssetPaths.BEAM_EDITOR_DATA_ASSET_PATH);
      return beamData.SceneUnits?.Any(sceneUnit => sceneUnit.ProjectUnits.Any(pu =>
        pu.Unit.Instances.Any(instance => instance.Id == placedInstance.UnitInstance.Id))) ?? false;
    }
  }
}
