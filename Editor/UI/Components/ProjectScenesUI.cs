using System;
using System.Collections.Generic;
using System.Linq;
using Beam.Editor.Managers;
using Beam.Runtime.Client;
using Beam.Runtime.Sdk.Data;
using Beam.Runtime.Sdk.Generated;
using Beam.Runtime.Sdk.Generated.Model;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beam.Editor.UI.Components
{
  public class ProjectScenesUI : VisualElement
  {
    public ProjectScenesUI(IBeamWindow beamWindow)
    {
      BeamData data = BeamClient.Data;
      IProject project = data.GetSelectedProject();

      VisualElement projectHeaderContent = new VisualElement();
      projectHeaderContent.AddToClassList("beam-project-header-content");

      Label nameLabel = new Label { text = project.Name };
      nameLabel.AddToClassList("beam-header");
      Label descriptionLabel = new Label { text = project.Description };

      projectHeaderContent.Add(nameLabel);
      projectHeaderContent.Add(descriptionLabel);

      VisualElement projectHeader = new Header(projectHeaderContent);

      VisualElement areaSelectorWrapper = new VisualElement();
      areaSelectorWrapper.AddToClassList("beam-scene-selector-wrapper");
      areaSelectorWrapper.AddToClassList("beam-header");

      if (data.Scenes == null || !data.Scenes.Any())
      {
        VisualElement noDataWrapper = new VisualElement();
        noDataWrapper.AddToClassList("beam-content-grow");
        noDataWrapper.AddToClassList("beam-content-center");

        this.Add(projectHeader);
        Label noAreasLabel = new Label("This project has no areas. Please go to the web portal to create some first.");
        noAreasLabel.AddToClassList("beam-no-data-label");
        noDataWrapper.Add(noAreasLabel);

        Button webLink = new Button { text = "Go to web portal" };
        webLink.clicked += () => { Application.OpenURL(Endpoint.GetEndpoints().WebUrl); };
        webLink.AddToClassList("beam-centered-button");
        noDataWrapper.Add(webLink);

        this.Add(noDataWrapper);
      }

      var currentSelectedArea = data.GetSelectedScene();
      if (currentSelectedArea == null)
      {
        return;
      }

      int currentSelectedAreaIndex = data.Scenes.IndexOf(currentSelectedArea);

      Label placementsLabel = new Label { text = $"'{currentSelectedArea.Name}' Area unit instances" };

      List<string> areas = data.Scenes.Select(s => s.Name).ToList();
      PopupField<string> popup = new PopupField<string>(areas, Math.Max(currentSelectedAreaIndex, 0));
      popup.RegisterValueChangedCallback(async e =>
      {
        await BeamEditorDataManager.SelectArea(data.Scenes.FirstOrDefault(s => s.Name == e.newValue));
        EditorUtility.SetDirty(data);
        beamWindow.Render();
      });

      this.Add(projectHeader);
      areaSelectorWrapper.Add(placementsLabel);
      areaSelectorWrapper.Add(popup);

      this.Add(areaSelectorWrapper);
    }

  }
}
