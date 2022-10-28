using System.Linq;
using Beam.Editor.Managers;
using Beam.Runtime.Client;
using Beam.Runtime.Sdk.Data;
using Beam.Runtime.Sdk.Generated;
using Beam.Runtime.Sdk.Utilities;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beam.Editor.UI.Components
{
  public class SelectEnvironmentUI : VisualElement
  {
    public static readonly string BeamEnvironmentKey = "Beam-Environment";
    public SelectEnvironmentUI(bool forceLogout = false)
    {
      System.Collections.Generic.List<string> endpoints = EndpointManager.GetAvailableEnvironments().ToList();
      if (endpoints.Count <= 1)
      {
        return;
      }

      if (string.IsNullOrEmpty(BeamClient.RuntimeData.Environment))
      {
        BeamClient.RuntimeData.Environment = Endpoint.Environment;
      }

      Endpoint.Environment = BeamClient.RuntimeData.Environment;

      PopupField<string> environmentSelection = new PopupField<string>("Environment:", endpoints, Endpoint.Environment);
      environmentSelection.labelElement.AddToClassList("beam-environment-label");
      environmentSelection.RegisterValueChangedCallback(e =>
      {
        if (Endpoint.Environment == e.newValue)
        {
          return;
        }

        string logoutWarningTitle = "Are you sure?";
        string logoutWarningMessage = "Changing the endpoint will log you out and remove all unit placements.";

        if (forceLogout && !EditorUtility.DisplayDialog(logoutWarningTitle, logoutWarningMessage, "Yes", "No"))
        {
          environmentSelection.value = Endpoint.Environment;
          return;
        }

        Endpoint.Environment = e.newValue;
        BeamClient.RuntimeData.Environment = Endpoint.Environment;
        if (forceLogout)
        {
          BeamEditorAuthManager.Logout(force: true);
        }
        BeamClient.Sdk.UpdateEndpoints();
      });

      this.Add(environmentSelection);
    }
  }
}
