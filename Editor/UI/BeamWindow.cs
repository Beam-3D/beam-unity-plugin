using System;
using System.Collections.Generic;
using System.Linq;
using Beam.Editor.Managers;
using Beam.Editor.UI.Components;
using Beam.Editor.Utilities;
using Beam.Runtime.Client;
using Beam.Runtime.Client.Managers;
using Beam.Runtime.Client.Utilities;
using Beam.Runtime.Sdk;
using Beam.Runtime.Sdk.Data;
using Beam.Runtime.Sdk.Generated;
using Beam.Runtime.Sdk.Generated.Client;
using Beam.Runtime.Sdk.Model;
using Beam.Runtime.Sdk.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beam.Editor.UI
{

  public class BeamWindow : EditorWindowSingleton<BeamWindow>, IBeamWindow
  {
    public BeamAreaBoundsManager AreaBoundsManager;
    public BeamData BeamData;
    public BeamRuntimeData BeamRuntimeData;
    public PlayModeStateChange PlayModeState;

    [MenuItem("Beam/Dashboard")]
    public static void ShowWindow()
    {
      GetWindow<BeamWindow>(false, "Beam Dashboard", true);
    }

    private void Init()
    {
      BeamManagerHandler.CheckForManagers(true);
      this.BeamData = BeamClient.Data;
      this.BeamRuntimeData = BeamClient.RuntimeData;
      this.RelinkManagers();
    }

    private protected void OnFocus()
    {
      this.Render();
    }

    public void OnEnable()
    {
      this.minSize = new Vector2(700, 600);
      UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode += this.HandleAreaChange;
      BeamEditorDataManager.DataUpdated += this.HandleDataUpdated;
      BeamEditorInstanceManager.PlacedInstancesChanged += this.HandlePlacedInstancesChanged;
    }

    public void OnDisable()
    {
      UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode -= this.HandleAreaChange;
      BeamEditorDataManager.DataUpdated -= this.HandleDataUpdated;
      BeamEditorInstanceManager.PlacedInstancesChanged -= this.HandlePlacedInstancesChanged;
    }

    public void Awake()
    {
      this.Init();
    }


    public void RelinkManagers()
    {
      if (this.PlayModeState == PlayModeStateChange.EnteredPlayMode ||
          this.PlayModeState == PlayModeStateChange.ExitingEditMode)
      {
        this.Render();
        return;
      }

      if (!string.IsNullOrEmpty(this.BeamRuntimeData.Environment))
      {
        Endpoint.Environment = this.BeamRuntimeData.Environment;

        // Check to see if the environment is still available.
        if (string.IsNullOrEmpty(Endpoint.AvailableEnvironments.FirstOrDefault(x => x == Endpoint.Environment)))
        {
          Endpoint.Environment = Endpoint.AvailableEnvironments.FirstOrDefault();
        }
      }

      this.AreaBoundsManager = FindObjectOfType<BeamAreaBoundsManager>();
      this.titleContent.text = this.BeamData.WindowName;

      this.Render();
    }

    public void Render()
    {
      VisualElement root = this.rootVisualElement;
      root.Clear();
      root.AddToClassList("beam-main-view");
      var ss = Resources.Load<StyleSheet>("UI/BeamTheme");

      root.styleSheets.Add(ss);
      if (Application.isPlaying)
      {
        root.Add(new Label("Not available whilst project is running."));
        return;
      }

      if (this.AreaBoundsManager == null)
      {
        BeamManagerHandler.CheckForManagers(true);
      }

      if (BeamEditorAuthManager.ServerError)
      {
        root.Add(new Logo());
        root.Add(new ServerErrorUI(this));
        SelectEnvironmentUI environmentSelector = new SelectEnvironmentUI(this);
        root.Add(environmentSelector);
        this.Repaint();
        return;
      }

      LoginResponse loginResponse = FileHelper.LoadLoginData();

      if (loginResponse == null || string.IsNullOrWhiteSpace(loginResponse.Token))
      {
        // Render login UI
        root.Add(new Logo());
        root.Add(new LoginUI(this));

        SelectEnvironmentUI environmentSelector = new SelectEnvironmentUI(this);
        root.Add(environmentSelector);
      }
      else if (BeamEditorDataManager.FetchPending)
      {
        root.Add(new Loader());
        return;
      }
      else
      {
        // Render authenticated UI
        if (this.BeamData.ShowSettingsUI)
        {
          root.Add(new SettingsUI(this));
        }
        else if (this.BeamData.ShowUploadSceneUI)
        {
          root.Add(new UploadSceneUI(this));
        }
        else
        {
          // Check if a project is selected and that it's valid
          if (!string.IsNullOrWhiteSpace(this.BeamRuntimeData.ProjectId) && this.BeamData.GetSelectedProject() != null)
          {
            // Select scene
            root.Add(new ProjectScenesUI(this));

            // Select units
            if (this.BeamData.Scenes != null && this.BeamData.Scenes.Any())
            {
              root.Add(new SceneUnitsUI(this));
            }
          }
          else
          {
            // Select project
            root.Add(new Logo());
            root.Add(new SelectProjectUI());
          }

          this.RenderCogMenu(root);
        }
      }

      this.Repaint();
    }

    // TODO: Make these into events? 
    private void RenderCogMenu(VisualElement root)
    {
      List<MenuOption> menuOptions = new List<MenuOption>
      {
        new MenuOption
        {
          Label = "Settings",
          Callback = () =>
          {
            // Should this be in BeamData?
            this.BeamData.ShowSettingsUI = true;
            this.Render();
          }
        },
        new MenuOption
        {
          Label = "Refresh data",
          Callback = async () =>
          {
            try
            {
              // TODO: Move to Editor Data Manager

              await BeamEditorDataManager.GetBaseData(true);
              if (!string.IsNullOrWhiteSpace(this.BeamData.GetSelectedProject()?.Id))
              {
                await BeamEditorDataManager.GetAreas(this.BeamData.GetSelectedProject().Id);
              }

              if (!string.IsNullOrWhiteSpace(this.BeamData.GetSelectedScene()?.Id))
              {
                await BeamEditorDataManager.GetUnits(this.BeamData.GetSelectedScene()?.Id);
              }

              // TODO: Move out of BeamClient
              BeamDataSynchronizer.OnRuntimeDataRefreshed();
            }
            catch (ApiException e)
            {
              BeamLogger.LogError("Error refreshing data.");
              Debug.LogException(e);
            }
            catch (Exception e)
            {
              Debug.LogException(e);
            }

            EditorUtility.SetDirty(this.BeamRuntimeData);
            this.Render();
          }
        },
        new MenuOption
        {
          Label = "Change project",
          Callback = () =>
          {
            BeamEditorDataManager.ChangeProject(
              BeamEditorInstanceManager.PlacedInstances == null || !BeamEditorInstanceManager.PlacedInstances.Any());
            this.Render();
          }
        },
        new MenuOption
        {
          Label = "Upload scene to web",
          Callback = () =>
          {
            this.BeamData.ShowUploadSceneUI = true;
            this.Render();
          }
        },
        new MenuOption
        {
          Label = "Log out",
          Callback = () =>
          {
            BeamEditorAuthManager.Logout();
            this.Render();
          }
        }
      };
      root.Add(new CogMenuButton(menuOptions));
    }

    public void Logout()
    {
      BeamEditorAuthManager.Logout(true);
    }

    private void HandleAreaChange(UnityEngine.SceneManagement.Scene prev, UnityEngine.SceneManagement.Scene next)
    {
      this.Init();
      this.Repaint();
    }

    private void HandleDataUpdated(object sender, DataUpdateType updateType)
    {
      this.Render();
    }

    private void HandlePlacedInstancesChanged(object sender, EventArgs e)
    {
      this.Render();
    }
  }
}
