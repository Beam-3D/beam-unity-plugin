using System;
using System.Collections.Generic;
using System.Linq;
using Beam.Editor.Extensions;
using Beam.Runtime.Client;
using Beam.Runtime.Sdk.Generated.Model;
using Beam.Runtime.Sdk.Utilities;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beam.Editor.UI.Components
{
  public class SettingsUI : VisualElement
  {
    private ExternalCreatableSession GetMockSession(string projectId)
    {
      return new ExternalCreatableSession
      {
        IsSandboxed = true,
        ProjectId = projectId,
        Environment = new Runtime.Sdk.Generated.Model.Environment
        {
          Engine = Engine.Unity,
          Version = Application.unityVersion
        },
        Device = new ExternalCreatableDevice
        {
          DeviceId = SystemInfo.deviceUniqueIdentifier,
          System = $"{SystemInfo.operatingSystem} {SystemInfo.operatingSystemFamily}",
          Hmd = null,
          AdvertiserId = "TBC"
        },
        Consumer = new CreateUpdateSessionConsumer
        {
          Gender = Gender.Male,
          Dob = new DateTime(1989, 02, 15),
          Language = "en",
          Location = "GB"
        },
        UserTagIds = new List<string>()
      };
    }

    public SettingsUI(BeamWindow beamWindow)
    {
      var data = BeamClient.Data;
      var runtimeData = BeamClient.RuntimeData;

      var session = data.MockSession;
      // SOURCE DATA
      List<string> engines = Enum.GetNames(typeof(Engine)).ToList();
      List<string> systems = new List<string> { "Windows 10", "Mac OS", "Oculus Quest", "Android", "iOS" };
      List<string> hmds = data.Devices.Distinct().ToList();


      this.Add(new Label("Settings").WithClass("beam-header"));



      // LOG LEVEL SELECTION
      VisualElement logLevelSection = new VisualElement().WithClass("beam-section");
      PopupField<string> logLevelSelection = new PopupField<string>("Log Level:", Enum.GetNames(typeof(LogLevel)).ToList(), data.LogLevel.ToString());
      logLevelSelection.RegisterValueChangedCallback(e =>
      {
        if (e.newValue == data.LogLevel.ToString())
        {
          return;
        }
        data.LogLevel = (LogLevel)Enum.Parse(typeof(LogLevel), e.newValue);
      });
      logLevelSection.Add(logLevelSelection);

      this.Add(logLevelSection);

      // ENDPOINT SELECTION
      VisualElement environmentSection = new VisualElement().WithClass("beam-section");
      environmentSection.Add(new SelectEnvironmentUI(true));

      this.Add(environmentSection);

      // SANDBOX
      VisualElement dataSection = new VisualElement().WithClass("beam-section");
      bool sandboxEnabled = data.SandboxEnabled;

      Toggle sandboxToggle = new Toggle("Sandbox mode") { value = sandboxEnabled };
      sandboxToggle.RegisterValueChangedCallback(e =>
      {
        data.SandboxEnabled = e.newValue;
        beamWindow.Render();
      });

      bool mockingEnabled = data.MockDataEnabled;

      Toggle mockingToggle = new Toggle("Send mock data") { value = mockingEnabled };
      mockingToggle.RegisterValueChangedCallback(e =>
      {
        data.MockDataEnabled = e.newValue;
        beamWindow.Render();
      });

      dataSection.Add(sandboxToggle);
      dataSection.Add(mockingToggle);

      this.Add(dataSection);

      // POLLING
      VisualElement pollingSection = new VisualElement().WithClass("beam-section");
      bool pollingEnabled = data.PollingEnabled;

      Toggle pollingToggle = new Toggle("Fulfillment polling") { value = pollingEnabled };


      pollingToggle.RegisterValueChangedCallback(e =>
      {
        data.PollingEnabled = e.newValue;

        if (!e.newValue)
        {
          data.PollingRate = 0;
        }
        beamWindow.Render();
      });

      int pollingRate = data.PollingRate;
      IntegerField pollingInput = new IntegerField("Polling rate (seconds)") { value = pollingRate };
      pollingInput.RegisterValueChangedCallback(e => { data.PollingRate = e.newValue; });


      pollingSection.Add(pollingToggle);

      pollingInput.SetEnabled(pollingEnabled);
      pollingSection.Add(pollingInput);

      this.Add(pollingSection);

      // MOCKING FORM
      VisualElement mockingForm = new VisualElement();
      mockingForm.SetEnabled(mockingEnabled);

      // ENVIRONMENT
      mockingForm.Add(new Label("Environment").WithClass("beam-header"));

      if (session == null || string.IsNullOrWhiteSpace(session.ProjectId))
      {
        session = this.GetMockSession(data.GetSelectedProject()?.Id);
      }

      string engine = Enum.GetName(typeof(Engine), session.Environment.Engine);

      if (string.IsNullOrWhiteSpace(engine))
      {
        engine = engines[0];
      }

      PopupField<string> engineField = new PopupField<string>("Engine", engines, engine);
      engineField.RegisterValueChangedCallback(e => session.Environment.Engine = (Engine)Enum.Parse(typeof(Engine), e.newValue));

      TextField versionField = new TextField { label = "Version", value = session.Environment.Version };
      versionField.RegisterValueChangedCallback(e => session.Environment.Version = e.newValue);

      mockingForm.Add(engineField);
      mockingForm.Add(versionField);

      // DEVICE
      string system = systems[0];
      string hmd = hmds[0];
      string deviceId = "";

      mockingForm.Add(new Label("Device").WithClass("beam-header"));

      TextField deviceIdField = new TextField { label = "Device ID", value = Guid.NewGuid().ToString() };
      deviceIdField.RegisterValueChangedCallback(e => session.Device.DeviceId = e.newValue);

      PopupField<string> systemField = new PopupField<string>("System", systems, systems[0]);
      systemField.RegisterValueChangedCallback(e => session.Device.System = e.newValue);

      PopupField<string> hmdField = new PopupField<string>("HMD", hmds, hmds[0]);
      hmdField.RegisterValueChangedCallback(e => session.Device.Hmd = e.newValue);

      mockingForm.Add(deviceIdField);
      mockingForm.Add(hmdField);
      mockingForm.Add(systemField);

      // DEMOGRAPHICS
      mockingForm.Add(new Label("Demographics").WithClass("beam-header"));
      EnumField genderField = new EnumField("Gender", data.MockGender);
      genderField.RegisterValueChangedCallback(e =>
      {
        data.MockGender = (Gender)e.newValue;
        EditorUtility.SetDirty(data);
      });

      PopupField<Language> languageField = new PopupField<Language>(
        "Language",
        data.Languages,
        data.Languages.FirstOrDefault(l => l.Name == Application.systemLanguage.ToString()),
        l => $"({l.IsoCode}) {l.Name}",
        l => $"({l.IsoCode}) {l.Name}"
      );
      languageField.RegisterValueChangedCallback(e => session.Consumer.Language = e.newValue.IsoCode);

      PopupField<Location> locationField = new PopupField<Location>(
        "Location",
        data.Locations,
        data.Locations.FirstOrDefault(l => l.IsoCode == "GB"),
        l => $"({l.IsoCode}) {l.Name}",
        l => $"({l.IsoCode}) {l.Name}"
      );
      locationField.RegisterValueChangedCallback(e => session.Consumer.Location = e.newValue.IsoCode);

      mockingForm.Add(genderField);
      mockingForm.Add(languageField);
      mockingForm.Add(locationField);

      TextField dobField = new TextField { label = "Date of birth (DD/MM/YYYY)", value = session.Consumer.Dob.ToString() };
      dobField.RegisterValueChangedCallback(e => data.MockDob = e.newValue);

      mockingForm.Add(dobField);

      this.Add(mockingForm);

      this.Add(new Button { text = "Done" }.WithClickHandler(() =>
      {
        if (!mockingEnabled)
        {
          data.MockSession = null;
        }
        else
        {
          session.Device.System = system;
          session.Device.Hmd = hmd;
          session.Device.DeviceId = deviceId;
          data.MockSession = session;
        }
        data.ShowSettingsUI = false;
        beamWindow.Render();
      }));
    }

  }
}
