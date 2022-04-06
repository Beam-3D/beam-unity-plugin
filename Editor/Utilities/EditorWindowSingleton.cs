#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Beam.Editor.Utilities
{
  public class EditorWindowSingleton<TSelfType> : EditorWindow where TSelfType : EditorWindow
  {
    private static TSelfType instance = null;
    public static TSelfType FindFirstInstance()
    {
      TSelfType[] windows = (TSelfType[])Resources.FindObjectsOfTypeAll(typeof(TSelfType));
      if (windows.Length == 0)
        return null;
      return windows[0];
    }

    public static TSelfType Instance
    {
      get
      {
        if (instance == null)
        {
          instance = FindFirstInstance();
          if (instance == null)
            instance = GetWindow<TSelfType>();
        }
        return instance;
      }
    }
  }
}
#endif
