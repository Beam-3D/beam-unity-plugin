using Beam.Editor.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beam.Editor.UI.Components
{
  public class Loader : VisualElement
  {
    public Loader()
    {
      this.Add(new Header(null));

      var loaderWrapper = new VisualElement().WithClass("beam-loader");
      Label loaderLabel = new Label("Fetching data...");

      loaderWrapper.Add(loaderLabel);
      this.Add(loaderWrapper);
    }

  }
}
