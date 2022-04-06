using Beam.Runtime.Client.Utilities;
using UnityEditor;
using UnityEngine;

namespace Beam.Editor.Editors
{
  [CustomEditor(typeof(BeamModelResizer))]
  [CanEditMultipleObjects]
  public class BeamModelResizerEditor: UnityEditor.Editor
  {
    private SerializedProperty targetScale;
    private SerializedProperty snapToBase;
    private SerializedProperty pivotAtBase;
    private SerializedProperty resizeOnStart;
    private SerializedProperty onResizeCompleted;
    
    private void OnEnable()
    {
      this.targetScale = this.serializedObject.FindProperty("TargetScale");      
      this.snapToBase = this.serializedObject.FindProperty("SnapToBase");       
      this.pivotAtBase = this.serializedObject.FindProperty("PivotAtBase");      
      this.resizeOnStart = this.serializedObject.FindProperty("ResizeOnStart");    
      this.onResizeCompleted = this.serializedObject.FindProperty("OnResizeCompleted");
    }

    public override void OnInspectorGUI()
    {
      this.serializedObject.Update();
      EditorGUILayout.PropertyField(this.targetScale, new GUIContent("Target Scale"));
      EditorGUILayout.PropertyField(this.snapToBase, new GUIContent("Snap To Base"));
      GUI.enabled = this.snapToBase.boolValue;
      if (!this.snapToBase.boolValue)
      {
        this.pivotAtBase.boolValue = false;
      }
      EditorGUILayout.PropertyField(this.pivotAtBase, new GUIContent("Pivot At Base"));
      GUI.enabled = true;
      EditorGUILayout.PropertyField(this.resizeOnStart, new GUIContent("Resize On Start"));
      EditorGUILayout.PropertyField(this.onResizeCompleted, new GUIContent("On Resize Completed"));
      
      this.serializedObject.ApplyModifiedProperties();
    }
  }
}
