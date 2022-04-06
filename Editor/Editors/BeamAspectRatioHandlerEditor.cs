using System;
using System.Linq;
using Beam.Runtime.Client.Units;
using Beam.Runtime.Client.Utilities;
using Beam.Runtime.Sdk.Data;
using Beam.Runtime.Sdk.Extensions;
using Beam.Runtime.Sdk.Generated.Model;
using Beam.Runtime.Sdk.Model;
using Beam.Runtime.Sdk.Utilities;
using UnityEditor;
using UnityEngine;

namespace Beam.Editor.Editors
{
  [CustomEditor(typeof(BeamAspectRatioHandler), true)]
  public class BeamAspectRatioHandlerEditor : UnityEditor.Editor
  {
    private BeamAspectRatioHandler scriptInstance;
    private BeamUnitInstance beamUnitInstance;
    private BeamData beamData;
    private int selectedRescaleIndex = 0;
    private readonly string[] rescaleDirectionOptions = { "x", "y" };
    public void OnEnable()
    {
      if (this.scriptInstance == null)
      {
        this.scriptInstance = (BeamAspectRatioHandler)this.target;
      }
      if (this.beamUnitInstance == null)
      {
        this.beamUnitInstance = this.scriptInstance.BeamUnitInstance;
      }
      if (this.beamData == null)
      {
        this.beamData = Resources.Load<BeamData>(BeamAssetPaths.BEAM_EDITOR_DATA_ASSET_PATH);
      }
    }
    public override void OnInspectorGUI()
    {
      var kind = this.beamUnitInstance.ProjectUnit.Kind;

      var aspectRatio = this.beamUnitInstance.ProjectUnit.GetAspectRatioId(this.beamData);

      Vector3 currentLocalScale = this.scriptInstance.transform.localScale;
      float actualAspectMultiplier = currentLocalScale.x / currentLocalScale.y;

      if (aspectRatio != null)
      {
        float aspectMultiplier = AspectRatioHelper.GetRatioMultiplier(aspectRatio);

        bool isCorrectAspectRatio = Math.Abs(aspectMultiplier - actualAspectMultiplier) < 0.0001f;

        EditorGUILayout.LabelField("Unit aspect Ratio", $"{aspectRatio.Name}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Transform aspect Ratio", $"{AspectRatioHelper.GetSimplestAspectRatio(currentLocalScale.x, currentLocalScale.y)}", EditorStyles.boldLabel);

        EditorGUI.BeginDisabledGroup(isCorrectAspectRatio);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Reset aspect ratio from X"))
        {
          this.scriptInstance.transform.MaintainAspectRatio(aspectRatio, "x");
        }

        if (GUILayout.Button("Reset aspect ratio from Y"))
        {
          this.scriptInstance.transform.MaintainAspectRatio(aspectRatio, "y");
        }

        EditorGUILayout.EndHorizontal();
        EditorGUI.BeginDisabledGroup(false);
      }
      else
      {
        EditorGUILayout.LabelField("Unit aspect Ratio", "Any", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Transform aspect Ratio", $"{AspectRatioHelper.GetSimplestAspectRatio(currentLocalScale.x, currentLocalScale.y)}", EditorStyles.boldLabel);
        GUILayout.Space(10);

        var aspectRatioChangeBehaviour = this.scriptInstance.AspectRatioChangeBehaviour;

        switch (kind)
        {
          case AssetKind.Image:
            aspectRatioChangeBehaviour = (AspectRatioChangeBehaviour)EditorGUILayout.EnumPopup("Resize Behaviour", aspectRatioChangeBehaviour);
            EditorGUILayout.LabelField(AspectRatioChangeBehaviourDescription.GetDescription(aspectRatioChangeBehaviour), EditorStyles.helpBox);
            break;
          case AssetKind.Video:
            aspectRatioChangeBehaviour = (AspectRatioChangeBehaviour)EditorGUILayout.EnumPopup("Resize Behaviour", aspectRatioChangeBehaviour);
            EditorGUILayout.LabelField(AspectRatioChangeBehaviourDescription.GetDescription(aspectRatioChangeBehaviour), EditorStyles.helpBox);
            break;
          default:
            return;
        }

        this.scriptInstance.AspectRatioChangeBehaviour = aspectRatioChangeBehaviour;

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Resize helpers", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"These controls will resize the transform in Edit mode. The do not affect runtime fulfillment resizing.", EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Reset Aspect Ratio From: ", EditorStyles.boldLabel);
        this.selectedRescaleIndex = EditorGUILayout.Popup(this.selectedRescaleIndex, this.rescaleDirectionOptions);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField("Landscape");
        EditorGUILayout.BeginHorizontal();
        this.beamData.AspectRatios.Where(ar => ar.HeightRatio < ar.WidthRatio).ToList().ForEach(ar =>
        {
          if (GUILayout.Button($"{ar.Name}"))
          {
            this.scriptInstance.transform.MaintainAspectRatio(ar, this.rescaleDirectionOptions[this.selectedRescaleIndex]);
          }
        });
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField("Portrait");
        EditorGUILayout.BeginHorizontal();
        this.beamData.AspectRatios.Where(ar => ar.HeightRatio > ar.WidthRatio).ToList().ForEach(ar =>
        {
          if (GUILayout.Button($"{ar.Name}"))
          {
            this.scriptInstance.transform.MaintainAspectRatio(ar, this.rescaleDirectionOptions[this.selectedRescaleIndex]);
          }
        });
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField("Square");
        EditorGUILayout.BeginHorizontal();
        this.beamData.AspectRatios.Where(ar => ar.HeightRatio == ar.WidthRatio).ToList().ForEach(ar =>
        {
          if (GUILayout.Button($"{ar.Name}"))
          {
            this.scriptInstance.transform.MaintainAspectRatio(ar, this.rescaleDirectionOptions[this.selectedRescaleIndex]);
          }
        });
        EditorGUILayout.EndHorizontal();
      }

    }
  }
}
