using System.Linq;
using Beam.Runtime.Client;
using Beam.Runtime.Client.Loaders;
using Beam.Runtime.Client.Units;
using Beam.Runtime.Client.Utilities;
using UnityEditor;
using UnityEngine;

namespace Beam.Editor.Utilities
{
  // ensure class initializer is called whenever scripts recompile
  [InitializeOnLoadAttribute]
  public static class PlayModeMonitor
  {
    static PlayModeMonitor()
    {
      EditorApplication.playModeStateChanged += LogPlayModeState;
    }

    private static void LogPlayModeState(PlayModeStateChange state)
    {
      if (state == PlayModeStateChange.ExitingPlayMode)
      {
        // Reset images
        System.Collections.Generic.List<BeamBasicImageLoader> images = Object.FindObjectsOfType<BeamBasicImageLoader>().ToList();
        images.ForEach(ins => ins.ResetTexture());
      }

      if (state == PlayModeStateChange.EnteredPlayMode)
      {
        var window = GetWindow();
        window?.Render();

        if (Object.FindObjectsOfType<BeamUnitInstance>().Any())
        {
          BeamManagerHandler.CheckForManagers(false);
        }
      }

      if (state != PlayModeStateChange.EnteredEditMode)
      {
        return;
      }

      {
        var window = GetWindow();
        window?.RelinkManagers();
        // Clear session data
        BeamClient.CurrentSession = null;
      }
    }

    private static IBeamWindow GetWindow()
    {
      EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
      EditorWindow window = windows?.FirstOrDefault(w => w.titleContent.text == "Beam Dashboard");

      return window != null ? window as IBeamWindow : null;
    }
  }
}
