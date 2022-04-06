using System;

namespace Beam.Editor.Utilities
{
  [Serializable]
  public class MenuOption
  {
    public string Label { get; set; }
    public delegate void Action();
    public Action Callback { get; set; }
  }
}
