using Beam.Editor.Extensions;
using Beam.Editor.Managers;
using Beam.Runtime.Client.Utilities;
using Beam.Runtime.Sdk.Generated.Model;
using Beam.Runtime.Sdk.Model;
using Beam.Runtime.Sdk.Utilities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beam.Editor.UI.Components
{
  public class LoginUI : VisualElement
  {
    //private BeamEditorDataManager dataManager;
    private readonly BeamWindow window;

    public LoginUI(BeamWindow beamWindow)
    {
      this.window = beamWindow;
      ILoginRequest request = BeamEditorDataManager.LoginRequest;
      ILoginResponse loginResponse = FileHelper.LoadLoginData();

      if (loginResponse?.User != null)
      {
        // We're just logging back in, prefill the email address
        request.Username = loginResponse.User.Username;
      }

      this.AddToClassList("beam-content-grow");

      Label emailLabel = new Label { text = "Email address" };
      TextField emailField = new TextField { value = request.Username };
      emailField.RegisterValueChangedCallback(EmailChanged);
      emailField.AddToClassList("beam-input");

      Label passwordLabel = new Label { text = "Password" };
      TextField passwordField = new TextField { isPasswordField = true, value = request.Password };
      passwordField.RegisterValueChangedCallback(PasswordChanged);
      passwordField.AddToClassList("beam-input");

      Button loginButton = new Button { text = "Log in" }
        .WithClickHandler(this.LoginClicked);

      loginButton.SetEnabled(!BeamEditorDataManager.FetchPending);

      loginButton.AddToClassList("beam-button");
      this.Add(emailLabel);
      this.Add(emailField);
      this.Add(passwordLabel);
      this.Add(passwordField);
      this.Add(loginButton);
    }

    private static void EmailChanged(ChangeEvent<string> evt)
    {
      BeamEditorDataManager.LoginRequest.Username = evt.newValue;
    }

    private static void PasswordChanged(ChangeEvent<string> evt)
    {
      BeamEditorDataManager.LoginRequest.Password = evt.newValue;
    }

    // TODO: Refactor this to Auth handler
    private async void LoginClicked()
    {
      try
      {
        await BeamEditorAuthManager.Login(BeamEditorDataManager.LoginRequest);
      }
      catch (System.Exception e)
      {
        BeamLogger.LogError(e.Message);
      }
      finally
      {
        this.window.Render();
      }
    }
  }
}
