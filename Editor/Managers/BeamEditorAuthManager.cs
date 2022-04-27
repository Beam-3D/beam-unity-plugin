using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Beam.Runtime.Client;
using Beam.Runtime.Client.Utilities;
using Beam.Runtime.Sdk;
using Beam.Runtime.Sdk.Generated.Client;
using Beam.Runtime.Sdk.Generated.Model;
using Beam.Runtime.Sdk.Model;
using Beam.Runtime.Sdk.Utilities;
using UnityEditor;
using UnityEngine;

namespace Beam.Editor.Managers
{
  public enum LoginEventStatus
  {
    LoginStarted,
    LoginFinished,
    UserLoggedIn,
    UserLoggedOut
  }

  public static class BeamEditorAuthManager
  {
    public static bool ServerError { get; private set; } = false;

    public static event EventHandler<LoginEventStatus> LoginStatusChanged;

    public static async Task<bool> CheckAuth()
    {
      ILoginResponse loginResponse = FileHelper.LoadLoginData();
      ServerError = false;
      // Check for missing token
      if (loginResponse == null || string.IsNullOrWhiteSpace(loginResponse.Token))
      {
        Logout(force: true);
        LoginStatusChanged?.Invoke(null, LoginEventStatus.UserLoggedOut);
        return false;
      }

      try
      {
        // This probably isn't ideal but not sure how best to check the token is valid?
        await BeamClient.Sdk.Projects.GetMyProjectsAsync(new IProjectsQuery(null, 1, new List<IQueryWhereIProject>()));
        ServerError = false;
        return true;
      }
      catch (ApiException e)
      {
        ServerError = false;
        switch (e.ErrorCode)
        {
          case 403:
          case 401:
            // Authentication error or bad token. Clear token and request a new login
            loginResponse.Token = null;
            FileHelper.SaveLoginData(loginResponse);
            break;
          case 400:
          case 503:
            // Bad request. API might be down?
            //EditorUtility.DisplayDialog("[BEAM] Problem communicating with server", "We couldn't reach our servers. Try again later", "OK");
            BeamLogger.LogError("Problem Communicating with Beam Server");
            // Setting this.ServerError to true before shutting unity causes layout issues on logging back in, not sure why.
            ServerError = true;
            //LoginFailed?.Invoke(null, EventArgs.Empty);
            break;
        }
      }
      finally
      {
        LoginStatusChanged?.Invoke(null, LoginEventStatus.LoginFinished);
      }

      return false;
    }

    public static async Task Login(ILoginRequest request)
    {
      try
      {
        request.Username = request.Username.ToLower();
        LoginStatusChanged?.Invoke(null, LoginEventStatus.LoginStarted);
        ILoginResponse loginResponse = await BeamClient.Sdk.Login(request);
        FileHelper.SaveLoginData(loginResponse);
        LoginStatusChanged?.Invoke(null, LoginEventStatus.UserLoggedIn);

      }
      catch (Exception)
      {
        EditorUtility.DisplayDialog("[BEAM] Problem logging in", $"Check your credentials and try again", "OK");
      }
      finally
      {
        LoginStatusChanged?.Invoke(null, LoginEventStatus.LoginFinished);
      }
    }

    public static void Logout(bool force = false)
    {
      // LinkManagers(this.beamWindow);
      // var confirm = force || EditorUtility.DisplayDialog("Are you sure?", "Logging out will remove all unit placements", "Yes", "No");
      bool confirm = force ||
                     EditorUtility.DisplayDialog("Are you sure?", "Are you sure you want to log out?", "Yes", "No");
      if (!confirm) return;
      FileHelper.DeleteLoginData();
      ServerError = false;
      LoginStatusChanged?.Invoke(null, LoginEventStatus.UserLoggedOut);
      BeamClient.RuntimeData.ClearData();
      BeamClient.Data.ClearData();
    }
  }
}
