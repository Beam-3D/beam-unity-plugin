using Beam.Editor.Extensions;
using Beam.Editor.Managers;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beam.Editor.UI.Components
{
  public class ServerErrorUI : VisualElement
  {
    public ServerErrorUI(BeamWindow beamWindow)
    {
      this.AddToClassList("beam-error-wrapper");

      Label errorHeader = new Label("We're really sorry...");
      errorHeader.AddToClassList("beam-header");

      Label errorBody = new Label("There was a problem communicating with the Beam servers");
      errorBody.AddToClassList("beam-error-body");

      var retryButton = new Button { text = "Retry connection" }.WithClickHandler(async () =>
      {
        await BeamEditorAuthManager.CheckAuth();
      }).WithClass("beam-centered-button");
      Label resetMessage = new Label("If you're still having problems you can click the button below to reset the plugin");
      resetMessage.AddToClassList("beam-error-body");

      var resetButton = new Button { text = "Reset plugin" }.WithClickHandler(() =>
      {
        BeamEditorAuthManager.Logout();
      }).WithClass("beam-centered-button");

      this.Add(errorHeader);
      this.Add(errorBody);
      this.Add(retryButton);
      this.Add(resetButton);
    }

  }
}
