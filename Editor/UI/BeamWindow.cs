using System;
using System.Collections.Generic;
using System.IO;
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
using Beam.Runtime.Sdk.Generated.Model;
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
    public PlayModeStateChange PlayModeState;

    [MenuItem("Beam/Dashboard")]
    public static void ShowWindow()
    {
      GetWindow<BeamWindow>(false, "Beam Dashboard", true);
    }

    private void Init()
    {
      LoadOrInitData();
      BeamManagerHandler.CheckForManagers(true);
      this.RelinkManagers();
    }

    private protected void OnFocus()
    {
      this.Render();
    }

    public void OnEnable()
    {
      this.minSize = new Vector2(700, 600);
      UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode += this.HandleSceneChange;
      BeamEditorDataManager.DataUpdated += this.HandleDataUpdated;
      BeamEditorInstanceManager.PlacedInstancesChanged += this.HandlePlacedInstancesChanged;
    }

    public void OnDisable()
    {
      UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode -= this.HandleSceneChange;
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

      if (!string.IsNullOrEmpty(BeamClient.RuntimeData.Environment))
      {
        Endpoint.Environment = BeamClient.RuntimeData.Environment;

        // Check to see if the environment is still available.
        if (string.IsNullOrEmpty(Endpoint.AvailableEnvironments.FirstOrDefault(x => x == Endpoint.Environment)))
        {
          Endpoint.Environment = EndpointManager.GetAvailableEnvironments().FirstOrDefault();
        }
      }

      this.AreaBoundsManager = FindObjectOfType<BeamAreaBoundsManager>();
      this.titleContent.text = BeamClient.Data.WindowName;

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

      ILoginResponse loginResponse = FileHelper.LoadLoginData();

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
        if (BeamClient.Data.ShowSettingsUI)
        {
          root.Add(new SettingsUI(this));
        }
        else if (BeamClient.Data.ShowUploadSceneUI)
        {
          root.Add(new UploadSceneUI(this));
        }
        else
        {
          // Check if a project is selected and that it's valid
          if (!string.IsNullOrWhiteSpace(BeamClient.RuntimeData.ProjectId) && BeamClient.Data.GetSelectedProject() != null)
          {
            // Select scene
            root.Add(new ProjectScenesUI(this));

            // Select units
            if (BeamClient.Data.Scenes != null && BeamClient.Data.Scenes.Any())
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
            BeamClient.Data.ShowSettingsUI = true;
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
              if (!string.IsNullOrWhiteSpace(BeamClient.Data.GetSelectedProject()?.Id))
              {
                await BeamEditorDataManager.GetAreas(BeamClient.Data.GetSelectedProject().Id);
              }

              if (!string.IsNullOrWhiteSpace(BeamClient.Data.GetSelectedScene()?.Id))
              {
                await BeamEditorDataManager.GetUnits(BeamClient.Data.GetSelectedScene()?.Id);
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

            EditorUtility.SetDirty(BeamClient.RuntimeData);
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
            BeamClient.Data.ShowUploadSceneUI = true;
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
    
    private static void LoadOrInitData()
    {
      BeamLogger.LogVerbose("Initializing BeamData");
      var loadedData = SerializedDataManager.Data;

      if (loadedData == null)
      {
        BeamLogger.LogInfo("BeamData not found; creating new instance of BeamData");
        Directory.CreateDirectory($"Assets/Resources/Beam");
        loadedData = ScriptableObject.CreateInstance<BeamData>();
        AssetDatabase.CreateAsset(loadedData, "Assets/Resources/Beam/BeamData.asset");
        AssetDatabase.SaveAssets();
        BeamLogger.LogLevel = loadedData.LogLevel;
      }

      BeamLogger.LogVerbose("Initializing BeamRuntimeData");
      var loadedRuntimeData = SerializedDataManager.RuntimeData;


      if (loadedRuntimeData == null)
      {
        BeamLogger.LogInfo("BeamRuntimeData not found; creating new instance of BeamRuntimeData");
        Directory.CreateDirectory($"Assets/Resources/Beam");
        loadedRuntimeData = ScriptableObject.CreateInstance<BeamRuntimeData>();
        loadedRuntimeData.ClearData();
        AssetDatabase.CreateAsset(loadedRuntimeData, "Assets/Resources/Beam/BeamRuntimeData.asset");
        AssetDatabase.SaveAssets();
      }
    }

    private void HandleSceneChange(UnityEngine.SceneManagement.Scene prev, UnityEngine.SceneManagement.Scene next)
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
