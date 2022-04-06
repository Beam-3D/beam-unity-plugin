using Beam.Runtime;
using Beam.Runtime.Client;
using Beam.Runtime.Client.Managers;
using Beam.Runtime.Sdk.Data;
using UnityEditor;
using UnityEngine;

namespace Beam.Editor.Editors
{

  [CustomEditor(typeof(BeamFulfillmentManager), true)]
  public class BeamFulfillmentManagerEditor : UnityEditor.Editor
  {
    private BeamFulfillmentManager scriptInstance;
    public void OnEnable()
    {
      this.scriptInstance = (BeamFulfillmentManager)this.target;
    }

    public override void OnInspectorGUI()
    {

      GUI.enabled = BeamClient.CurrentSession == null;
      BeamClient.RuntimeData.AutoStartFulfillment = GUILayout.Toggle(BeamClient.RuntimeData.AutoStartFulfillment, "Fulfill on session start");
      GUI.enabled = true;

      bool canStartFulfillment = Application.isPlaying && BeamClient.CurrentSession != null;

      GUI.enabled = canStartFulfillment;
      if (GUILayout.Button("Run Fulfillment"))
      {
        BeamClient.StartAutomaticFulfillment();
      }
      GUI.enabled = true;

      if (Application.isPlaying && this.scriptInstance.FulfillmentResponse != null && this.scriptInstance.FulfillmentResponse.Units != null)
      {
        var fulfillment = this.scriptInstance.FulfillmentResponse;

        GUILayout.Label("Fulfillment details", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Fulfilled Units", fulfillment.Units.Count.ToString());
      }
      this.serializedObject.ApplyModifiedProperties();
    }
  }
}
