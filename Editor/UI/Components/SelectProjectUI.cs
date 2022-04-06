using System.Linq;
using Beam.Editor.Managers;
using Beam.Runtime.Client;
using Beam.Runtime.Sdk.Data;
using Beam.Runtime.Sdk.Generated;
using Beam.Runtime.Sdk.Generated.Model;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beam.Editor.UI.Components
{
  public class SelectProjectUI : VisualElement
  {
    public SelectProjectUI()
    {
      BeamData data = BeamClient.Data;
      this.viewDataKey = "select-project-view";
      this.AddToClassList("beam-content-grow");

      if (BeamEditorDataManager.FetchPending)
      {
        this.Add(new Loader());
        return;
      }

      var projects = data.Projects?.Items;

      if (projects == null || !projects.Any())
      {
        this.AddToClassList("beam-content-center");
        Label noProjectsLabel = new Label { text = "No projects found" };
        noProjectsLabel.AddToClassList("beam-header-center");

        Button webLink = new Button { text = "Go to web portal" };
        webLink.clicked += () => { Application.OpenURL(Endpoint.GetEndpoints().WebUrl); };
        webLink.AddToClassList("beam-centered-button");

        this.Add(noProjectsLabel);
        this.Add(webLink);

        return;
      }

      Label selectProjectLabel = new Label { text = "Select your project" };
      selectProjectLabel.AddToClassList("beam-header-center");

      this.Add(selectProjectLabel);

      ScrollView scrollView = new ScrollView(ScrollViewMode.Vertical);
      scrollView.AddToClassList("beam-project-scroller");

      for (int i = 0; i < projects.Count; i++)
      {
        Project project = projects[i];
        VisualElement projectItem = new VisualElement();
        projectItem.AddToClassList("beam-project-item");

        projectItem.AddToClassList(i % 2 == 0 ? "dark-bg" : "light-bg");

        Label label = new Label { text = project.Name };
        projectItem.Add(label);

        Button button = new Button { text = "Select" };

        button.clickable.clicked += async () =>
        {
          await BeamEditorDataManager.SelectProject(project);
        };
        projectItem.Add(button);

        scrollView.Add(projectItem);
      }
      this.Add(scrollView);
    }
  }
}
