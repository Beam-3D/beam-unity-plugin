using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beam.Editor.UI.Components
{
  public class Header : VisualElement
  {
    public Header([CanBeNull] VisualElement content)
    {
      VisualElement header = new VisualElement();
      header.AddToClassList("beam-project-header-wrapper");

      Texture2D logo = Resources.Load<Texture2D>("Images/beam_logo_placeholder_small");
      Image image = new Image();
      image.AddToClassList("beam-logo");
      image.image = logo;
      header.Add(image);

      if (content != null)
      {
        header.Add(content);
      }
      this.Add(header);
    }
  }
}
