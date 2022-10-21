using System.Globalization;
using System.Linq;
using Beam.Editor.Managers;
using Beam.Runtime.Client.Units;
using Beam.Runtime.Sdk.Data;
using Beam.Runtime.Sdk.Extensions;
using Beam.Runtime.Sdk.Generated.Model;
using Beam.Runtime.Sdk.Model;
using Beam.Runtime.Sdk.Utilities;
using UnityEditor;
using UnityEngine;
using ProjectUnit = Beam.Runtime.Sdk.Model.ProjectUnit;

namespace Beam.Editor.Editors
{
  [CustomEditor(typeof(BeamUnitInstance), true)]
  public class BeamUnitInstanceEditor : UnityEditor.Editor
  {
    private BeamUnitInstance scriptInstance;
    private AssetKind kind;
    private BeamData beamData;
    public int SelectedAreaIndex;
    public int SelectedUnitIndex;

    public void OnEnable()
    {
      this.beamData = Resources.Load<BeamData>(BeamAssetPaths.BEAM_EDITOR_DATA_ASSET_PATH);

      if (this.target == null)
      {
        return;
      }

      this.scriptInstance = (BeamUnitInstance)this.target;
      this.kind = this.scriptInstance.Kind;

      BeamEditorInstanceManager.UpdatePlacements();

      if (!BeamEditorInstanceManager.IsInstanceDuplicate(this.scriptInstance.UnitInstance))
      {
        return;
      }

      var nextInstance = this.GetNextUnplacedInstance();
      if (nextInstance != null)
      {
        this.scriptInstance.UnitInstance = nextInstance;
      }
      else
      {
        this.ResetSelectedUnit();
      }
      BeamEditorInstanceManager.UpdatePlacements();
    }

    private IProjectUnitInstance GetNextUnplacedInstance()
    {
      return this.scriptInstance.ProjectUnit.Unit.Instances.FirstOrDefault(ins => !BeamEditorInstanceManager.IsInstancePlaced(ins));
    }

    private void RenderInstanceSelector()
    {
      GUILayout.Label("Connect to an instance", EditorStyles.boldLabel);
      GUILayout.Label("Use the dropdowns below to select an area, unit and instance for this object.", EditorStyles.helpBox);

      System.Collections.Generic.List<string> areas = this.beamData.Scenes.Select(s => s.Name).ToList();
      areas.Insert(0, "Select an area...");

      GUILayout.Label("Area", EditorStyles.boldLabel);
      this.SelectedAreaIndex = EditorGUILayout.Popup(this.SelectedAreaIndex, areas.ToArray());
      EditorGUILayout.Space(5);

      if (this.SelectedAreaIndex != 0)
      {
        var area = this.beamData.Scenes[this.SelectedAreaIndex - 1];
        var areaUnits = this.beamData.SceneUnits.FirstOrDefault(s => s.SceneId == area.Id);
        if (areaUnits == null || areaUnits.TotalUnits == 0)
        {
          GUILayout.Label("Selected area has no units to place.", EditorStyles.boldLabel);
        }
        else
        {
          System.Collections.Generic.List<ProjectUnit> units = areaUnits.ProjectUnits.Where(pu => pu.Kind == this.kind).ToList();

          if (!units.Any())
          {
            GUILayout.Label($"Area has no {this.kind} units.", EditorStyles.boldLabel);
            return;
          }

          System.Collections.Generic.List<string> unitNames = units.Select(u => u.Unit.Name).ToList();
          GUILayout.Label("Unit", EditorStyles.boldLabel);
          unitNames.Insert(0, "Select a unit...");
          this.SelectedUnitIndex = EditorGUILayout.Popup(this.SelectedUnitIndex, unitNames.ToArray());
          EditorGUILayout.Space(5);

          if (this.SelectedUnitIndex == 0)
          {
            return;
          }
          
          var selectedUnit = units[this.SelectedUnitIndex - 1];

          var selectedProjectUnit = areaUnits.ProjectUnits.FirstOrDefault(pu => pu.Unit.Id == selectedUnit.Unit.Id);
          System.Collections.Generic.List<IProjectUnitInstance> instances = selectedProjectUnit?.Unit.Instances.Where(ins => !BeamEditorInstanceManager.IsInstancePlaced(ins)).ToList();
          if (instances?.Any() != true)
          {
            GUILayout.Label($"All of this Units instances have been placed.", EditorStyles.boldLabel);
            return;
          }

          var instance = instances.FirstOrDefault();
          this.scriptInstance.ProjectUnit = selectedProjectUnit;
          this.scriptInstance.UnitInstance = instance;

          BeamEditorInstanceManager.UpdatePlacements();
        }
      }
      else
      {
        this.SelectedUnitIndex = 0;
      }
    }

    public override void OnInspectorGUI()
    {
      var projectUnit = this.scriptInstance.ProjectUnit;
      var unitInstance = this.scriptInstance.UnitInstance;

      if (projectUnit == null || string.IsNullOrWhiteSpace(projectUnit.Unit.Id) || string.IsNullOrWhiteSpace(unitInstance.Id))
      {
        this.RenderInstanceSelector();
        return;
      }

      var loginResponse = FileHelper.LoadLoginData();

      if (loginResponse == null)
      {
        GUILayout.Label("Please login via 'Beam > Dashboard' before adding scripts", EditorStyles.boldLabel);
        return;
      }

      this.RenderBaseInformation();

      if (GUILayout.Button("Change instance..."))
      {
        this.ResetSelectedUnit();
        BeamEditorInstanceManager.UpdatePlacements();
        return;
      }

      GUILayout.Space(10);
      switch (this.kind)
      {
        case AssetKind.Image:
          this.RenderImageUnitProperties();
          break;
        case AssetKind.Video:
          this.RenderVideoUnitProperties();
          break;
        case AssetKind.ThreeDimensional:
          this.RenderThreeDimensionalUnitProperties();
          break;
        case AssetKind.Audio:
          this.RenderAudioUnitProperties();
          break;
        case AssetKind.Data:
          this.RenderDataProperties();
          break;
      }

      GUILayout.Space(10);

      this.RenderFulfillmentProperties();
      this.RenderEventProperties();

      GUILayout.Space(10);

      if (GUI.changed)
      {
        EditorUtility.SetDirty(this.target);
      }

      this.DrawDefaultInspector();
      this.serializedObject.ApplyModifiedProperties();
    }

    private void RenderBaseInformation()
    {

      var projectUnit = this.scriptInstance.ProjectUnit;

      var unit = projectUnit.Unit;

      GUILayout.Label("Base information", EditorStyles.boldLabel);
      EditorGUILayout.LabelField("Name", unit.Name, EditorStyles.boldLabel);
      EditorGUILayout.LabelField("Description", unit.Description, EditorStyles.boldLabel);

      EditorGUILayout.LabelField("Unit Kind", this.kind.ToString(), EditorStyles.boldLabel);
    }

    private void RenderImageUnitProperties()
    {
      var projectUnit = this.scriptInstance.ProjectUnit;

      var qualities = this.beamData.GetQualitiesForKind(this.kind);

      string minQualityId = !string.IsNullOrWhiteSpace(projectUnit.MinQualityId) ? projectUnit.MinQualityId : qualities.LastOrDefault()?.Id;
      string maxQualityId = !string.IsNullOrWhiteSpace(projectUnit.MaxQualityId) ? projectUnit.MaxQualityId : qualities.LastOrDefault()?.Id;

      GUILayout.Label("LOD Quality levels", EditorStyles.boldLabel);
      GUILayout.Label("An asset must have a variant matching both of these quality levels for the instance to be fulfilled.", EditorStyles.helpBox);
      EditorGUILayout.LabelField("Max LOD quality", qualities.FirstOrDefault(x => x.Id == maxQualityId)?.Name ?? "", EditorStyles.boldLabel);
      EditorGUILayout.LabelField("Min LOD quality", qualities.FirstOrDefault(x => x.Id == minQualityId)?.Name ?? "", EditorStyles.boldLabel);
      this.scriptInstance.ProjectUnit.LodDistance = EditorGUILayout.FloatField("Unit LOD distance", this.scriptInstance.ProjectUnit.LodDistance);
      GUILayout.Space(10);
      GUILayout.Label($"Image specific values", EditorStyles.boldLabel);

      var aspectRatio = projectUnit.GetAspectRatioId(this.beamData);
      EditorGUILayout.LabelField("Unit aspect Ratio", $"{(aspectRatio != null ? aspectRatio.Name : "any")}", EditorStyles.boldLabel);

    }

    private void RenderVideoUnitProperties()
    {
      var projectUnit = this.scriptInstance.ProjectUnit;
      var qualities = this.beamData.GetQualitiesForKind(this.kind);

      string maxQualityId = !string.IsNullOrWhiteSpace(projectUnit.MaxQualityId) ? projectUnit.MaxQualityId : qualities.LastOrDefault()?.Id;

      GUILayout.Label("LOD Quality level", EditorStyles.boldLabel);
      GUILayout.Label("An asset must have a variant this quality level for the instance to be fulfilled.", EditorStyles.helpBox);
      EditorGUILayout.LabelField("LOD quality", qualities.FirstOrDefault(x => x.Id == maxQualityId)?.Name ?? "", EditorStyles.boldLabel);
      this.scriptInstance.ProjectUnit.LodDistance = EditorGUILayout.FloatField("Unit LOD distance", this.scriptInstance.ProjectUnit.LodDistance);
      GUILayout.Label("Video will be paused beyond this distance.", EditorStyles.helpBox);
      GUILayout.Space(10);
      GUILayout.Label($"Video specific values", EditorStyles.boldLabel);

      var aspectRatio = projectUnit.GetAspectRatioId(this.beamData);
      EditorGUILayout.LabelField("Unit aspect Ratio", $"{(aspectRatio != null ? aspectRatio.Name : "any")}", EditorStyles.boldLabel);
      EditorGUILayout.LabelField("Can be muted", projectUnit.VideoMetadata.Mutable ? "Yes" : "No", EditorStyles.boldLabel);
      EditorGUILayout.LabelField("Max length", $"{projectUnit.VideoMetadata.MaxLengthInSeconds} seconds", EditorStyles.boldLabel);
    }

    private void RenderThreeDimensionalUnitProperties()
    {
      var projectUnit = this.scriptInstance.ProjectUnit;
      var qualities = this.beamData.GetQualitiesForKind(this.kind);

      string minQualityId = !string.IsNullOrWhiteSpace(projectUnit.MinQualityId) ? projectUnit.MinQualityId : qualities.LastOrDefault()?.Id;
      string maxQualityId = !string.IsNullOrWhiteSpace(projectUnit.MaxQualityId) ? projectUnit.MaxQualityId : qualities.LastOrDefault()?.Id;

      GUILayout.Label("LOD Quality levels", EditorStyles.boldLabel);
      GUILayout.Label("An asset must have a variant matching both of these quality levels for the instance to be fulfilled.", EditorStyles.helpBox);
      EditorGUILayout.LabelField("Max LOD quality", qualities.FirstOrDefault(x => x.Id == maxQualityId)?.Name ?? "", EditorStyles.boldLabel);
      EditorGUILayout.LabelField("Min LOD quality", qualities.FirstOrDefault(x => x.Id == minQualityId)?.Name ?? "", EditorStyles.boldLabel);
      this.scriptInstance.ProjectUnit.LodDistance = EditorGUILayout.FloatField("Unit LOD distance", this.scriptInstance.ProjectUnit.LodDistance);
      GUILayout.Space(10);
      GUILayout.Label($"Three Dimensional specific values", EditorStyles.boldLabel);
      EditorGUILayout.LabelField("Can be moved", projectUnit.ThreeDimensionalMetadata.Movable ? "Yes" : "No", EditorStyles.boldLabel);
      GUILayout.Space(10);
      GUILayout.Label("'Can be moved' currently has no direct functionality so you will need to add your own script to implement behaviour if desired.", EditorStyles.helpBox);

      GUI.enabled = true;
    }

    private void RenderAudioUnitProperties()
    {
      var projectUnit = this.scriptInstance.ProjectUnit;

      GUILayout.Label($"Audio specific values", EditorStyles.boldLabel);
      EditorGUILayout.LabelField("Max length", $"{projectUnit.AudioMetadata.MaxLengthInSeconds} seconds", EditorStyles.boldLabel);
      this.scriptInstance.ProjectUnit.LodDistance = EditorGUILayout.FloatField("Unit LOD distance", this.scriptInstance.ProjectUnit.LodDistance);
      GUILayout.Label("Audio will be paused beyond this distance.", EditorStyles.helpBox);
    }

    private void RenderDataProperties()
    {
      var projectUnit = this.scriptInstance.ProjectUnit;
      
      GUILayout.Label($"Data specific values", EditorStyles.boldLabel);

      IDataSchema schema = this.beamData.GetDataSchemaById(projectUnit.DataMetadata.DataSchemaId);
      EditorGUILayout.LabelField("Schema Name", $"{schema?.Name ?? ""}", EditorStyles.boldLabel);
    }


    private void RenderFulfillmentProperties()
    {
      var projectUnit = this.scriptInstance.ProjectUnit;

      GUILayout.Label($"Fulfillment", EditorStyles.boldLabel);
      EditorGUILayout.LabelField("Behaviour", projectUnit.FulfillmentBehaviour.ToString(), EditorStyles.boldLabel);
      if (projectUnit.FulfillmentBehaviour == FulfillmentBehaviour.Range)
      {
        EditorGUILayout.LabelField("Fulfillment range", projectUnit.FulfillmentRange.ToString(CultureInfo.InvariantCulture), EditorStyles.boldLabel);
      }

      if (projectUnit.FulfillmentBehaviour == FulfillmentBehaviour.Manual)
      {
        GUILayout.Label("To fulfill this unit, from your code do GetComponent<BeamUnitInstance>().CallFulfill()", EditorStyles.helpBox);
      }

      if (!Application.isPlaying)
      {
        return;
      }

      EditorGUILayout.LabelField("High Quality Content url", this.scriptInstance.ContentUrlHighQuality, EditorStyles.boldLabel);
      EditorGUILayout.LabelField("Low Quality Content url", this.scriptInstance.ContentUrlLowQuality, EditorStyles.boldLabel);
    }

    private bool showEvents;
    private void RenderEventProperties()
    {
      this.showEvents = EditorGUILayout.Foldout(this.showEvents, "Events");

      if (!this.showEvents)
      {
        return;
      }
      
      EditorGUILayout.PropertyField(this.serializedObject.FindProperty("OnFulfillmentUpdated"));
      EditorGUILayout.PropertyField(this.serializedObject.FindProperty("OnLodStatusChanged"));

      // Get specific unit type properties

      // Image
      var onImageUnitFulfilled = this.serializedObject.FindProperty("OnImageUnitFulfilled");
      if (onImageUnitFulfilled != null)
      {
        EditorGUILayout.PropertyField(this.serializedObject.FindProperty("OnImageUnitFulfilled"));
      }

      // ThreeDimensional
      var onThreeDimensionalUnitFulfilled = this.serializedObject.FindProperty("OnThreeDimensionalUnitFulfilled");
      if (onThreeDimensionalUnitFulfilled != null)
      {
        EditorGUILayout.PropertyField(this.serializedObject.FindProperty("OnThreeDimensionalUnitFulfilled"));
      }

      // Audio
      var onAudioUnitFulfilled = this.serializedObject.FindProperty("OnAudioUnitFulfilled");
      if (onAudioUnitFulfilled != null)
      {
        EditorGUILayout.PropertyField(this.serializedObject.FindProperty("OnAudioUnitFulfilled"));
      }

      // Video
      var onVideoUnitFulfilled = this.serializedObject.FindProperty("OnVideoUnitFulfilled");
      if (onVideoUnitFulfilled != null)
      {
        EditorGUILayout.PropertyField(this.serializedObject.FindProperty("OnVideoUnitFulfilled"));
      }

      this.serializedObject.ApplyModifiedProperties();
    }

    private void ResetSelectedUnit()
    {
      this.SelectedUnitIndex = 0;
      this.scriptInstance.ProjectUnit.Unit = null;
      this.scriptInstance.ProjectUnit = null;
      this.scriptInstance.UnitInstance = null;
    }
  }
}
