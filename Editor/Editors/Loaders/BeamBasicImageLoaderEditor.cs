using System;
using System.Collections.Generic;
using System.Linq;
using Beam.Runtime.Client.Loaders;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beam.Editor.Editors.Loaders
{
  [CustomEditor(typeof(BeamBasicImageLoader))]
  [CanEditMultipleObjects]
  public class BeamBasicImageLoaderEditor : UnityEditor.Editor
  {
    private SerializedProperty placeholder;
    private SerializedProperty targetRenderer;
    private SerializedProperty targetMaterial;
    private SerializedProperty targetMaterialProperty;

    private int targetMaterialPropertyIndex;

    private SerializedProperty onImageLoaded;
    private SerializedProperty emptyFulfillmentBehaviour;

    private protected void OnEnable()
    {
      if (this.serializedObject == null)
      {
        return;
      }

      this.placeholder = this.serializedObject.FindProperty("Placeholder");
      this.targetRenderer = this.serializedObject.FindProperty("TargetRenderer");
      this.targetMaterial = this.serializedObject.FindProperty("targetMaterial");
      this.targetMaterialProperty = this.serializedObject.FindProperty("targetMaterialProperty");

      this.SetCurrentTargetTextureIndex();

      this.onImageLoaded = this.serializedObject.FindProperty("OnImageLoaded");
      this.emptyFulfillmentBehaviour = this.serializedObject.FindProperty("EmptyFulfillmentBehaviour");
    }

    public override void OnInspectorGUI()
    {
      this.serializedObject.Update();
      EditorGUILayout.PropertyField(this.placeholder,
        new GUIContent("Placeholder", tooltip: "This texture will be applied on start."));
      EditorGUILayout.PropertyField(this.targetRenderer,
        new GUIContent("Target Renderer",
          "The renderer the loaded texture will be applied to. Note: The texture will only be applied to the first instantiated material assigned to the renderer."));

      this.DrawTextureSlotDropdown();

      EditorGUILayout.PropertyField(this.onImageLoaded, new GUIContent("On Image Loaded"));
      EditorGUILayout.PropertyField(this.emptyFulfillmentBehaviour, new GUIContent("Empty Fulfillment Behaviour"));

      this.serializedObject.ApplyModifiedProperties();
    }

    private void SetCurrentTargetTextureIndex()
    {
      if (this.targetRenderer.objectReferenceValue == null || this.targetRenderer.hasMultipleDifferentValues)
      {
        return;
      }

      this.targetMaterialPropertyIndex =
        Array.IndexOf(this.GetTargetTextureProperties(), this.targetMaterialProperty.stringValue);

      if (this.targetMaterialPropertyIndex < 0)
      {
        this.targetMaterialPropertyIndex = 0;
      }
    }

    private void DrawTextureSlotDropdown()
    {
      if (this.targetRenderer.objectReferenceValue == null)
      {
        // This will cause the slot to be evaluated at runtime, resulting 
        this.targetMaterialProperty.stringValue = "";
        return;
      }

      this.targetMaterial.objectReferenceValue = ((Renderer)this.targetRenderer.objectReferenceValue).sharedMaterial;
      if (!this.targetMaterial.hasMultipleDifferentValues)
      {
        this.SetCurrentTargetTextureIndex();

        string[] textureProperties = this.GetTargetTextureProperties();
        

        this.targetMaterialPropertyIndex = EditorGUILayout.Popup(new GUIContent(
            "Target Texture Slot", "The texture slot the image will be applied to."), this.targetMaterialPropertyIndex,
          textureProperties);

        this.targetMaterialProperty.stringValue = textureProperties[this.targetMaterialPropertyIndex];
        EditorGUILayout.HelpBox("It is recommended to assign a placeholder texture to " +
                                "texture slots that will be replaced with a loaded image to ensure the target " +
                                "property Keyword is not disabled by Unity.", MessageType.Info);
      }
      else
      {
        EditorGUILayout.HelpBox("Texture slot assignment is not supported across multiple renderers.",
          MessageType.Warning);
      }
    }

    private string[] GetTargetTextureProperties()
    {
      Renderer renderer = ((Renderer)this.targetRenderer.objectReferenceValue);

      if (renderer == null) return Array.Empty<string>();

      Material mat = renderer.sharedMaterial;
      IEnumerable<int> propertyIndices = Enumerable.Range(0, mat.shader.GetPropertyCount())
        .Where(v => mat.shader.GetPropertyType(v) == ShaderPropertyType.Texture);

      return propertyIndices.Select(v => mat.shader.GetPropertyName(v)).ToArray();
    }
  }
}
