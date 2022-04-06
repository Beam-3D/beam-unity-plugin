#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Beam.Runtime.Sdk.Data;
using Beam.Runtime.Sdk.Generated.Model;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace Beam.Editor.Utilities
{
  public class GltfShaderChecker : IPreprocessBuildWithReport
  {
    public int callbackOrder { get { return 0; } }
    public void OnPreprocessBuild(BuildReport report)
    {
      BeamData data = Resources.Load<BeamData>(BeamAssetPaths.BEAM_EDITOR_DATA_ASSET_PATH);
      if (!data) return;

      bool threeDimensionalUnitInBeamData = data.SceneUnits.Any(su => su.ProjectUnits.Any(pu => pu.Kind == AssetKind.ThreeDimensional));
      if (!threeDimensionalUnitInBeamData) return;

      const string graphicsSettingsAssetPath = "ProjectSettings/GraphicsSettings.asset";
      SerializedObject graphicsManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(graphicsSettingsAssetPath)[0]);
      SerializedProperty preloadedShaders = graphicsManager.FindProperty("m_PreloadedShaders");

      List<ShaderVariantCollection> shaderVarients = new List<ShaderVariantCollection>();
      for (int i = 0; i < preloadedShaders.arraySize; i++)
        shaderVarients.Add(preloadedShaders.GetArrayElementAtIndex(i).objectReferenceValue as ShaderVariantCollection);

      if (shaderVarients.Count == 0)
      {
        bool result = EditorUtility.DisplayDialog("No shader variants provided", "GLTF Shaders are required for Three Dimensional Units to display in built projects. Continue with build?", "OK", "Cancel");
        if (!result) throw new BuildFailedException("Build cancelled due to missing shader variants.");
      }
    }
  }
}
#endif
