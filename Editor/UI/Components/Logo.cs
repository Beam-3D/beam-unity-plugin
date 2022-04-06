using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beam.Editor.UI.Components
{
  public class Logo : VisualElement
  {
    public Logo()
    {
      Texture2D logo = Resources.Load<Texture2D>("Images/beam_logo_square_white");
      this.AddToClassList("beam-logo-wrapper");

      Image image = new Image();
      image.AddToClassList("beam-logo");
      image.image = logo;

      this.Add(image);
    }

  }
}
