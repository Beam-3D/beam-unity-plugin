using Beam.Runtime.Sdk.Generated.Model;
using UnityEditor;
using UnityEngine;

namespace Beam.UI
{
  public class BeamMenuItems
  {
    [MenuItem("GameObject/Beam/Image Unit", false, 10)]
    static void AddImageUnitToScene(MenuCommand command)
    {
      AddGameObjectToScene(AssetKind.Image, command);
    }
    [MenuItem("GameObject/Beam/Video Unit", false, 10)]
    static void AddVideoUnitToScene(MenuCommand command)
    {
      AddGameObjectToScene(AssetKind.Video, command);
    }
    [MenuItem("GameObject/Beam/Audio Unit", false, 10)]
    static void AddAudioUnitToScene(MenuCommand command)
    {
      AddGameObjectToScene(AssetKind.Audio, command);
    }
    [MenuItem("GameObject/Beam/3D Unit", false, 10)]
    static void AddThreeDimensionalUnitToScene(MenuCommand command)
    {
      AddGameObjectToScene(AssetKind.ThreeDimensional, command);
    }

    private static void AddGameObjectToScene(AssetKind assetKind, MenuCommand command)
    {
      GameObject go = Object.Instantiate(Resources.Load<GameObject>($"Prefabs/Beam{assetKind}Unit"));
      go.name = $"Beam {assetKind} Unit";

      GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
      // Register the creation in the undo system
      Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
      Selection.activeObject = go;
    }
  }
}
