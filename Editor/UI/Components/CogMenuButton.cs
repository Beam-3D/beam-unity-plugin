using System.Collections.Generic;
using System.Linq;
using Beam.Editor.Utilities;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beam.Editor.UI.Components
{
  public class CogMenuButton : VisualElement
  {
    public CogMenuButton(List<MenuOption> options)
    {
      var cog = Resources.Load<Texture2D>("Images/beam_icon_cog");

      List<string> labels = options.Select(op => op.Label).ToList();
      labels.Insert(0, "");

      PopupField<string> popup = new PopupField<string>(labels, 0);
      popup.RegisterValueChangedCallback(e => { ExecuteAction(labels.IndexOf(e.newValue) - 1, options); });

      this.Add(popup);
      this.AddToClassList("beam-settings-button");
    }

    private static void ExecuteAction(int index, List<MenuOption> options)
    {
      options[index].Callback();
    }

  }
}
