using System;
using System.Collections.Generic;
using System.Linq;
using Beam.Editor.Extensions;
using Beam.Editor.Managers;
using Beam.Runtime.Client;
using Beam.Runtime.Client.Units;
using Beam.Runtime.Sdk.Data;
using Beam.Runtime.Sdk.Extensions;
using Beam.Runtime.Sdk.Generated;
using Beam.Runtime.Sdk.Generated.Model;
using Beam.Runtime.Sdk.Model;
using Beam.Runtime.Sdk.Utilities;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using IAspectRatio = Beam.Runtime.Sdk.Generated.Model.IAspectRatio;
using ProjectUnit = Beam.Runtime.Sdk.Model.ProjectUnit;

namespace Beam.Editor.UI.Components
{
  public class SceneUnitsUI : VisualElement
  {
    private static Texture2D tick;
    private static Texture2D cross;

    private static Button TabButton(BeamWindow beamWindow, AssetKind selectedKind, AssetKind kind)
    {
      Button button = new Button { text = $"{kind}" }.WithClickHandler(() =>
        {
          BeamEditorDataManager.SelectAssetKind(kind);
          beamWindow.Render();
        });

      button.AddToClassList("beam-tab-button");
      if (selectedKind == kind)
      {
        button.AddToClassList("beam-tab-button-selected");
      }

      return button;
    }

    private VisualElement Tag(string tagName)
    {
      Label tag = new Label(tagName.ToUpper());
      tag.AddToClassList("beam-unit-tag");

      return tag;
    }

    public SceneUnitsUI(BeamWindow beamWindow)
    {
      this.AddToClassList("beam-scene-units");
      BeamData data = BeamClient.Data;

      SceneUnits sceneUnits = data.SceneUnits?.FirstOrDefault(su => su.SceneId == data.GetSelectedScene()?.Id);

      if (sceneUnits == null)
      {
        return;
      }

      if (sceneUnits.TotalUnits == 0)
      {
        this.Add(new Label("This Area doesn't have any slots to place. Please go to the web portal to create some first.").WithClass("beam-no-data-label"));

        Button webLink = new Button { text = "Go to web portal" };
        webLink.clicked += () => { Application.OpenURL(Endpoint.GetEndpoints().WebUrl); };
        webLink.AddToClassList("beam-centered-button");
        this.Add(webLink);
        return;
      }

      VisualElement toolbar = new VisualElement();
      toolbar.AddToClassList("beam-tab-control");

      List<AssetKind> kinds = Enum.GetValues(typeof(AssetKind)).Cast<AssetKind>().ToList();

      kinds.ForEach(assetKind => toolbar.Add(TabButton(beamWindow, data.SelectedAssetKind, assetKind)));

      this.Add(toolbar);

      tick = Resources.Load<Texture2D>("Images/beam_icon_tick");
      cross = Resources.Load<Texture2D>("Images/beam_icon_cross");

      Texture2D imageIcon = Resources.Load<Texture2D>($"Images/unit_image");


      AssetKind kind = data.SelectedAssetKind;
      List<ProjectUnit> selectedUnits = sceneUnits.GetProjectUnitsByKind(kind);

      if (selectedUnits.Count == 0)
      {
        VisualElement noDataWrapper = new VisualElement();
        noDataWrapper.AddToClassList("beam-content-grow");
        noDataWrapper.AddToClassList("beam-content-center");

        Label noSlotsLabel = new Label($"This Area has no {kind} slots. Please go to the web portal to create some first.");
        noSlotsLabel.AddToClassList("beam-no-data-label");
        noDataWrapper.Add(noSlotsLabel);

        Button webLink = new Button { text = "Go to web portal" };
        webLink.clicked += () => { Application.OpenURL(Endpoint.GetEndpoints().WebUrl); };
        webLink.AddToClassList("beam-centered-button");
        noDataWrapper.Add(webLink);

        this.Add(noDataWrapper);
        return;
      }

      ScrollView scrollView = new ScrollView();
      scrollView.AddToClassList("beam-units-scroller");
      scrollView.viewDataKey = BeamClient.Data.GetSelectedScene()?.Id + BeamClient.Data.SelectedAssetKind;

      VisualElement sceneUnitsList = new VisualElement();
      sceneUnitsList.AddToClassList("beam-unit-list");

      selectedUnits.ForEach(iu =>
        sceneUnitsList.Add(RenderHeader(beamWindow, kind, iu, imageIcon)));

      scrollView.Add(sceneUnitsList);

      this.Add(scrollView);
    }

    private static VisualElement RenderHeader(BeamWindow window, AssetKind kind,
      ProjectUnit projectUnit, Texture2D icon)
    {
      BeamData data = BeamClient.Data;
      // UNIT CONTAINER
      VisualElement unitWrapper = new VisualElement();
      unitWrapper.AddToClassList("beam-unit");

      // UNIT HEADER WRAPPER
      VisualElement unitHeader = new VisualElement();
      unitHeader.AddToClassList("beam-unit-header");


      VisualElement imageWrapper = new VisualElement();
      Image image = new Image();
      image.AddToClassList("beam-unit-kind-image");
      image.image = icon;
      imageWrapper.Add(image);

      VisualElement unitInfo = new VisualElement();
      unitInfo.AddToClassList("beam-unit-info");

      IProjectUnitWithInstances unit = projectUnit.Unit;

      List<BeamUnitInstance> placed = BeamEditorInstanceManager.GetInstancesByUnitId(unit.Id);

      // If we've placed any instances of the unit, use that units values instead (quality, fulfillment behaviour etc)
      if (placed.Any())
      {
        projectUnit = placed[0].ProjectUnit;
      }

      Label unitName = new Label($"{unit.Name} [{unit.Instances.Count()}]");
      VisualElement unitDescription = new Label(unit.Description).WithClass("beam-small-text");

      unitInfo.Add(unitName);
      unitInfo.Add(unitDescription);

      if (AspectRatioHelper.HasAspectRatio(kind))
      {
        IAspectRatio ratio = projectUnit.GetAspectRatioId(data);
        imageWrapper.Add(new Label(ratio?.Name ?? "Any").WithClass("beam-unit-header-ratio-label"));
      }

      unitHeader.Add(imageWrapper);

      unitHeader.Add(unitInfo);

      // UNIT QUALITIES
      List<SimpleQuality> qualities = data.GetQualitiesForKind(kind);
      if (kind != AssetKind.Audio)
      {
        VisualElement qualityControls = new VisualElement().WithClass("beam-quality-controls");
        qualityControls.Add(new Label("LOD quality levels"));

        VisualElement qualityDropdowns = new VisualElement().WithClass("beam-quality-dropdowns");

        // This is by design. Default to High/High until we have LOD support working
        string defaultMaxQualityId = !string.IsNullOrWhiteSpace(projectUnit.MaxQualityId)
          ? projectUnit.MaxQualityId
          : qualities.LastOrDefault()?.Id;
        string defaultMinQualityId = !string.IsNullOrWhiteSpace(projectUnit.MinQualityId)
          ? projectUnit.MinQualityId
          : qualities.LastOrDefault()?.Id;

        VisualElement maxQualityDropdownWrapper = new VisualElement().WithClass("quality-dd-wrapper");
        maxQualityDropdownWrapper.Add(new Label("Max"));
        PopupField<SimpleQuality> maxQuality =
          new PopupField<SimpleQuality>(qualities, qualities.FirstOrDefault(x => x.Id == defaultMaxQualityId));
        projectUnit.MaxQualityId = defaultMaxQualityId;
        maxQuality.RegisterValueChangedCallback(e =>
        {
          projectUnit.MaxQualityId = e.newValue.Id;

          placed.ForEach(p => p.ProjectUnit.MaxQualityId = e.newValue.Id);
        });
        maxQualityDropdownWrapper.Add(maxQuality);
        qualityDropdowns.Add(maxQualityDropdownWrapper);
        qualityControls.Add(qualityDropdowns);

        if (kind == AssetKind.Image || kind == AssetKind.ThreeDimensional)
        {
          VisualElement minQualityDropdownWrapper = new VisualElement().WithClass("quality-dd-wrapper");
          minQualityDropdownWrapper.Add(new Label("Min"));
          PopupField<SimpleQuality> minQualityId =
            new PopupField<SimpleQuality>(qualities, qualities.FirstOrDefault(x => x.Id == defaultMinQualityId));
          projectUnit.MinQualityId = defaultMinQualityId;
          minQualityId.RegisterValueChangedCallback(e =>
          {
            projectUnit.MinQualityId = e.newValue.Id;

            placed.ForEach(p => p.ProjectUnit.MinQualityId = e.newValue.Id);
          });
          minQualityDropdownWrapper.Add(minQualityId);
          qualityDropdowns.Add(minQualityDropdownWrapper);
          qualityControls.Add(qualityDropdowns);
        }

        unitHeader.Add(qualityControls);
      }
      else
      {
        // Only one audio quality atm, so default to the first one
        projectUnit.MaxQualityId = qualities.FirstOrDefault()?.Id;
        projectUnit.MinQualityId = projectUnit.MaxQualityId;
      }


      // FULFILLMENT BEHAVIOUR
      VisualElement fulfillmentMode = new VisualElement().WithClass("beam-fulfillment-mode");
      fulfillmentMode.Add(new Label("Fulfillment"));
      EnumField fulfillmentModeDropdown = new EnumField("Mode", projectUnit.FulfillmentBehaviour);
      fulfillmentModeDropdown.RegisterValueChangedCallback(e =>
      {
        projectUnit.FulfillmentBehaviour = (FulfillmentBehaviour)e.newValue;

        placed.ForEach(p => p.ProjectUnit.FulfillmentBehaviour = (FulfillmentBehaviour)e.newValue);


        window.Render();
      });
      fulfillmentMode.Add(fulfillmentModeDropdown);

      if (projectUnit.FulfillmentBehaviour == FulfillmentBehaviour.Range)
      {
        FloatField rangeField = new FloatField("Range") { value = projectUnit.FulfillmentRange };
        rangeField.RegisterValueChangedCallback(e =>
        {
          projectUnit.FulfillmentRange = e.newValue;


          placed.ForEach(p => p.ProjectUnit.FulfillmentRange = e.newValue);
        });
        fulfillmentMode.Add(rangeField);
      }

      unitHeader.Add(fulfillmentMode);

      unitWrapper.Add(unitHeader);
      unitWrapper.Add(RenderInstances(kind, projectUnit));

      return unitWrapper;
    }

    private static VisualElement RenderInstances(AssetKind kind, ProjectUnit projectUnit)
    {
      VisualElement unitInstanceList = new VisualElement();
      unitInstanceList.AddToClassList("beam-unit-instance-list");

      IProjectUnitWithInstances unit = projectUnit.Unit;

      for (int i = 0; i < unit.Instances.Count(); i++)
      {
        IProjectUnitInstance instance = unit.Instances[i];

        bool isPlaced = BeamEditorInstanceManager.IsInstancePlaced(instance);
        VisualElement instanceWrapper = new VisualElement();
        instanceWrapper.AddToClassList("beam-unit-instance");

        instanceWrapper.AddToClassList(i % 2 == 0 ? "dark-bg" : "light-bg");

        VisualElement instanceName = new Label($"Instance {i}").WithClass("beam-unit-instance-label");

        Image tickCross = new Image();
        tickCross.AddToClassList("beam-unit-placed-image");
        tickCross.image = isPlaced ? tick : cross;

        instanceWrapper.Add(tickCross);
        instanceWrapper.Add(instanceName);

        if (!isPlaced)
        {
          instanceWrapper.Add(new Button(() =>
            {
              BeamEditorInstanceManager.AddUnitInstanceToScene(kind, projectUnit, instance);
            })
          { text = "Add" });
        }
        else
        {
          instanceWrapper.Add(new Button(() => { BeamEditorInstanceManager.RemoveUnitFromScene(instance); })
          { text = "Remove" });
        }

        unitInstanceList.Add(instanceWrapper);
      }

      return unitInstanceList;
    }
  }
}
