using Beam.Editor.Extensions;
using Beam.Editor.Utilities;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Beam.Editor.UI.Components
{
  public class UploadSceneUI : VisualElement
  {
    private static bool uploadGeometry;
    private static bool uploadUnits;
    private static bool currentlyUploading;

    private static bool sceneSaved;
    private static bool subscribed;

    readonly BeamWindow window;

    public UploadSceneUI(BeamWindow beamWindow)
    {
      this.window = beamWindow;
      if (!subscribed)
      {
        EditorSceneManager.activeSceneChangedInEditMode += this.UpdateSceneSaved;
        subscribed = true;
      }

      this.UpdateSceneSaved();

      this.Add(new Label("Upload Scene").WithClass("beam-header"));

      const string helpTextIntro = "This page allows you to upload your current Unity Scene for preview in " +
                                   "the Beam web application.";
      const string helpTextTip = "If you have only made changes to Beam Units, please leave the " +
                                 "\"Upload Scene Geometry\" box unchecked for a faster upload.";

      this.Add(new TextElement() { text = helpTextIntro }.WithClass("beam-help-text"));
      this.Add(new TextElement() { text = helpTextTip }.WithClass("beam-help-text"));

      Toggle uploadGeometryToggle = new Toggle("Upload Scene Geometry") { value = uploadGeometry };
      uploadGeometryToggle.RegisterValueChangedCallback(e =>
      {
        uploadGeometry = e.newValue;
        beamWindow.Render();
      });

      Toggle uploadUnitsToggle = new Toggle("Upload Beam Unit Locations") { value = uploadUnits };
      uploadUnitsToggle.RegisterValueChangedCallback(e =>
      {
        uploadUnits = e.newValue;
        beamWindow.Render();
      });

      uploadGeometryToggle.AddToClassList("beam-toggle");
      uploadUnitsToggle.AddToClassList("beam-toggle");

      this.Add(uploadGeometryToggle);
      this.Add(uploadUnitsToggle);

      Button uploadButton = new Button { text = "Upload" }.WithClickHandler(async () =>
      {
        currentlyUploading = true;
        beamWindow.Render();
        await SceneUploader.UploadScene(beamWindow.BeamData.GetSelectedScene()?.Id, uploadGeometry, uploadUnits);
        currentlyUploading = false;
        beamWindow.Render();
      });

      uploadButton.SetEnabled(!currentlyUploading && (uploadGeometry || uploadUnits) && sceneSaved);

      Button doneButton = new Button { text = "Done" }.WithClickHandler(() =>
      {
        beamWindow.BeamData.ShowUploadSceneUI = false;
        beamWindow.Render();
      });

      doneButton.SetEnabled(!currentlyUploading);

      this.Add(uploadButton);
      this.Add(doneButton);

      string saveSceneWarning = sceneSaved ? "" : "Please ensure all loaded scenes are saved before uploading.";
      this.Add(new TextElement() { text = saveSceneWarning }.WithClass("beam-help-text"));
    }

    ~UploadSceneUI()
    {
      if (subscribed)
      {
        SceneManager.activeSceneChanged -= this.UpdateSceneSaved;
      }
    }

    void UpdateSceneSaved()
    {
      int openSceneCount = SceneManager.sceneCount;
      sceneSaved = true;
      for (int i = 0; i < openSceneCount; i++)
      {
        if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).name))
        {
          sceneSaved = false;
        }
      }
    }

    private void UpdateSceneSaved(Scene replacedScene, Scene newScene)
    {
      this.window.Render();
      // UpdateSceneSaved();
    }

    private void UpdateSceneSaved(Scene scene)
    {
      this.window.Render();
      // UpdateSceneSaved();
    }
  }
}
