using System.Collections.Generic;
using System.Linq;
using Beam.Runtime.Client;
using Beam.Runtime.Client.Managers;
using Beam.Runtime.Sdk.Data;
using Beam.Runtime.Sdk.Generated.Model;
using UnityEditor;
using UnityEngine;

namespace Beam.Editor.Editors
{
  [CustomEditor(typeof(BeamSessionManager), true)]
  public class BeamSessionManagerEditor : UnityEditor.Editor
  {
    private BeamSessionManager scriptInstance;
    public void OnEnable()
    {
      this.scriptInstance = (BeamSessionManager)this.target;
    }

    public override void OnInspectorGUI()
    {
      if (BeamClient.CurrentSession != null && !string.IsNullOrWhiteSpace(BeamClient.CurrentSession.Id))
      {
        var session = BeamClient.CurrentSession;
        GUILayout.Label("Session details", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Session ID", session.Id);
        EditorGUILayout.LabelField("User tags", BeamClient.RuntimeData.ActiveUserTags != null ? string.Join(",", BeamClient.RuntimeData.ActiveUserTags.Select(t => t.Name)) : "None");
      }
      else
      {
        GUI.enabled = !Application.isPlaying;
        BeamClient.RuntimeData.AutoStartSession = GUILayout.Toggle(BeamClient.RuntimeData.AutoStartSession, "Auto start session");
        GUI.enabled = Application.isPlaying;

        bool canStartSession = Application.isPlaying && !this.scriptInstance.SessionRunning && !BeamClient.RuntimeData.AutoStartSession;
        GUI.enabled = !Application.isPlaying || canStartSession;

        GUILayout.Label("User tags", EditorStyles.boldLabel);
        GUILayout.Label("The User Tags you enable here will be sent up with the session request to help you with debugging and testing User Tag based fulfilments.", EditorStyles.helpBox);

        List<Tag> activeUserTags = new List<Tag>();

        BeamClient.RuntimeData.UserTags.ForEach(tag =>
        {
          bool active = BeamClient.RuntimeData.ActiveUserTags.Contains(tag);
          bool toggled = GUILayout.Toggle(active, tag.Name);
          if (toggled)
          {
            activeUserTags.Add(tag);
          }
        });

        BeamClient.RuntimeData.ActiveUserTags = activeUserTags;

        GUI.enabled = canStartSession;
        if (GUILayout.Button("Start session"))
        {
          BeamClient.StartSession();
        }


        this.serializedObject.ApplyModifiedProperties();

      }
    }
  }
}
