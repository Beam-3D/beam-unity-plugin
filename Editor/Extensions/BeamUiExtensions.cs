using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beam.Editor.Extensions
{
  public static class BeamUiExtensions
  {
    public static VisualElement WithClass(this VisualElement element, string className)
    {
      element.AddToClassList(className);
      return element;
    }

    public static Button WithClickHandler(this Button button, Action callback)
    {
      button.clicked += callback;
      return button;
    }
  }
}
