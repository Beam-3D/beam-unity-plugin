using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beam.Runtime.Client.Utilities
{

  public enum UnityRenderPipeline
  {
    Legacy,
    HighDefRenderPipeline,
    UniversalRenderPipeline,
    UniversalRenderPipeline2020,
    Unknown
  }

  public class DetermineRenderPipeline
  {
    public static UnityRenderPipeline GetRenderSettings()
    {
      RenderPipelineAsset renderAsset = GraphicsSettings.renderPipelineAsset;
      if (renderAsset == null)
      {
        return UnityRenderPipeline.Legacy;
      }
      else
      {
        string renderAssetName = renderAsset.GetType().Name;
        if (renderAsset.GetType().Name.Contains("HDRenderPipelineAsset"))
        {
          return UnityRenderPipeline.HighDefRenderPipeline;
        }
        if (GraphicsSettings.renderPipelineAsset.GetType().Name.Contains("UniversalRP-HighQuality"))
        {
          return UnityRenderPipeline.UniversalRenderPipeline;
        }
        if (renderAssetName.Contains("UniversalRenderPipelineAsset"))
        {
#if UNITY_2019
          return UnityRenderPipeline.UniversalRenderPipeline;
#endif

#if UNITY_2020_1_OR_NEWER
        return UnityRenderPipeline.UniversalRenderPipeline2020;
#endif
        }
      }
      Debug.LogWarning("Beam could not determine render pipeline, may cause later rendering errors");
      return UnityRenderPipeline.Unknown;
    }
  }
}
