using System;
using System.Collections;
using Beam.Runtime.Client.Loaders.Base;
using Beam.Runtime.Client.Loaders.Events;
using Beam.Runtime.Client.Units;
using Beam.Runtime.Client.Units.Events;
using Beam.Runtime.Client.Units.Model;
using Beam.Runtime.Client.Utilities;
using Beam.Runtime.Sdk.Utilities;
using UnityEngine;
using UnityEngine.Networking;

namespace Beam.Runtime.Client.Loaders
{
  [RequireComponent(typeof(BeamImageUnitInstance))]
  public class BeamBasicImageLoader : BeamImageLoader
  {
    public Texture2D Placeholder;
    [HideInInspector]
    public Texture OriginalTexture;
    public Renderer TargetRenderer;

    private Texture2D nextTexture;
    private string targetMaterialProperty = "_MainTex";
    private Texture2D highQualityTexture;
    private Texture2D lowQualityTexture;
    private BeamAspectRatioHandler beamAspectRatioHandler;

    private new void Awake()
    {
      if (!Application.isPlaying)
      {
        return;
      }

      this.beamAspectRatioHandler = this.GetComponent<BeamAspectRatioHandler>();

      base.Awake();

      if (!this.TargetRenderer)
      {
        Renderer fetchedRenderer = this.GetComponent<Renderer>();

        if (fetchedRenderer is null)
        {
          BeamLogger.LogWarning("Script has no target renderer. It will not be fulfilled.", this.gameObject);
          return;
        }

        this.TargetRenderer = fetchedRenderer;
      }

      this.SetupForRenderPipeline();

      this.OriginalTexture = this.TargetRenderer.material.GetTexture(this.targetMaterialProperty);
      if (this.Placeholder)
      {
        this.TargetRenderer.material.SetTexture(this.targetMaterialProperty, this.Placeholder);
      }
    }

    public void Update()
    {
      if (this.nextTexture != null)
      {
        this.DoSwitch();
      }
    }

    private IEnumerator LoadTexture(Uri uri, LodStatus forLodStatus)
    {
      BeamLogger.LogInfo("Loading texture: " + uri);

      UnityWebRequest www = UnityWebRequestTexture.GetTexture(uri);
      yield return www.SendWebRequest();
#if UNITY20201ORNEWER
    if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
#else
      if (www.isNetworkError || www.isHttpError)
#endif
      {
        BeamLogger.LogError(www.error);
      }
      else
      {
        var downloadedTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;

        if (forLodStatus == LodStatus.InsideHighQualityRange)
        {
          this.highQualityTexture = downloadedTexture;
        }
        else if (forLodStatus == LodStatus.OutsideHighQualityRange)
        {
          this.lowQualityTexture = downloadedTexture;
        }

        this.OnImageLoaded?.Invoke(new ImageLoadedData(downloadedTexture, forLodStatus));

        if (this.CurrentLodStatus == forLodStatus)
        {
          this.nextTexture = downloadedTexture;
        }

      }
    }

    private void DoSwitch()
    {
      if (!Application.isPlaying)
      {
        return;
      }

      this.TargetRenderer.material.SetTexture(this.targetMaterialProperty, this.nextTexture);
      if (this.beamAspectRatioHandler != null)
      {
        this.beamAspectRatioHandler.HandleAspectRatio(this.nextTexture.width, this.nextTexture.height);
      }
      this.TargetRenderer.enabled = true;
      this.nextTexture = null;
    }
    private void SetupForRenderPipeline()
    {
      UnityRenderPipeline pipeline = UtilDetermineRenderPipeline.getRenderSettings();
      switch (pipeline)
      {
        case UnityRenderPipeline.Legacy:
          this.targetMaterialProperty = "_MainTex";
          break;
        case UnityRenderPipeline.UniversalRenderPipeline:
#if UNITY_2020_1_OR_NEWER
            case UnityRenderPipeline.UniversalRenderPipeline2020:
#endif
          this.targetMaterialProperty = "_BaseMap";
          break;
        case UnityRenderPipeline.HighDefRenderPipeline:
          this.targetMaterialProperty = "_BaseColorMap";
          break;
        case UnityRenderPipeline.Unknown:
        default:
          BeamLogger.LogWarning("Unable to determine render pipeline");
          break;
      }
    }

    public void ResetTexture()
    {
      if (this.TargetRenderer)
      {
        this.TargetRenderer.sharedMaterial.SetTexture(this.targetMaterialProperty, this.OriginalTexture);
      }
    }



    public override void HandleFulfillment(ImageUnitFulfillmentData fulfillmentData)
    {
      if (fulfillmentData == null)
      {
        return;
      }

      bool highQualityAvailable = !string.IsNullOrEmpty(fulfillmentData.HighQualityUrl);
      bool lowQualityAvailable = !string.IsNullOrEmpty(fulfillmentData.LowQualityUrl);

      // TODO: handle lod distance of 0;
      bool onlyLoadOneQuality = string.CompareOrdinal(fulfillmentData.HighQualityUrl, fulfillmentData.LowQualityUrl) == 0; // || this.projectUnit.LodDistance == 0;

      // Same url or only HQ available, set both textures to same content
      if (highQualityAvailable && (onlyLoadOneQuality || !lowQualityAvailable))
      {
        this.StartCoroutine(this.LoadTexture(new Uri(fulfillmentData.HighQualityUrl), LodStatus.InsideHighQualityRange));
        this.lowQualityTexture = this.highQualityTexture;
      }
      else if (lowQualityAvailable && (onlyLoadOneQuality || !highQualityAvailable))
      {
        this.StartCoroutine(this.LoadTexture(new Uri(fulfillmentData.LowQualityUrl), LodStatus.OutsideHighQualityRange));
        this.highQualityTexture = this.lowQualityTexture;
      }
      else if (highQualityAvailable && lowQualityAvailable)
      {
        this.StartCoroutine(this.LoadTexture(new Uri(fulfillmentData.HighQualityUrl), LodStatus.InsideHighQualityRange));
        this.StartCoroutine(this.LoadTexture(new Uri(fulfillmentData.LowQualityUrl), LodStatus.OutsideHighQualityRange));
      }
    }

    protected override void HandleLodChange(LodStatus lodStatus)
    {
      if (lodStatus == LodStatus.InsideHighQualityRange && this.highQualityTexture != null)
      {
        this.nextTexture = this.highQualityTexture;
      }
      else if (lodStatus == LodStatus.OutsideHighQualityRange && this.lowQualityTexture != null)
      {
        this.nextTexture = this.lowQualityTexture;
      }
    }
  }
}
