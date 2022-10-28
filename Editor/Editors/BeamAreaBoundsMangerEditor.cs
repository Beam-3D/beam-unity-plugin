using System.Linq;
using Beam.Editor.Managers;
using Beam.Runtime.Client.Managers;
using Beam.Runtime.Sdk.Data;
using Beam.Runtime.Sdk.Generated.Model;
using UnityEditor;
using UnityEngine;

namespace Beam.Editor.Editors
{

  [CustomEditor(typeof(BeamAreaBoundsManager), true)]
  public class BeamAreaBoundsManagerEditor : UnityEditor.Editor
  {
    private BeamAreaBoundsManager scriptInstance;
    private BeamData beamData;

    private const string HelpText = "Beam Area bounds are use for analytics . \n\n" + "Resize their respective box colliders to cover the entire area where player updates should be " + "captured for a given area.";

    public void OnEnable()
    {
      this.scriptInstance = (BeamAreaBoundsManager)this.target;
      this.beamData = SerializedDataManager.Data;
    }

    public override void OnInspectorGUI()
    {
      GUILayout.Label("Beam Area Boundaries", EditorStyles.boldLabel);
      GUILayout.Label(HelpText, EditorStyles.helpBox);

      EditorGUILayout.BeginHorizontal();
      GUILayout.Label("Collider Layer");
      GUI.enabled = !Application.isPlaying;
      this.scriptInstance.BeamAreaColliderLayer = EditorGUILayout.LayerField(this.scriptInstance.BeamAreaColliderLayer);
      GUI.enabled = true;
      EditorGUILayout.EndHorizontal();

      if (this.beamData.Scenes == null || this.beamData.Scenes.Count == 0)
      {
        GUILayout.Label("No Areas in Project.", EditorStyles.boldLabel);
        return;
      }

      string[] areaIds = this.scriptInstance.AreaBoundsList.Keys.ToArray();
      foreach (string areaId in areaIds)
      {
        IScene area = this.beamData.Scenes.FirstOrDefault(scene => scene.Id == areaId);
        string areaName = area?.Name ?? "Deleted Area";
        bool anyPlacedUnitsInScene = BeamEditorInstanceManager.PlacedInstances.Any(unit => unit.ProjectUnit.Unit.SceneId == areaId);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(areaName, GUILayout.Width(100.0f));
        GUI.enabled = false;
        EditorGUILayout.ObjectField(this.scriptInstance.AreaBoundsList[areaId], typeof(BoxCollider), true);
        GUI.enabled = (!anyPlacedUnitsInScene || area == null) && !Application.isPlaying;

        if (GUILayout.Button("Remove"))
        {
          this.scriptInstance.HandleBoundsDeleted(areaId);
        }
        GUI.enabled = anyPlacedUnitsInScene && !Application.isPlaying;
        if (GUILayout.Button("Reset"))
        {
          this.scriptInstance.ResetBounds(areaId);
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
      }
    }
  }
}
