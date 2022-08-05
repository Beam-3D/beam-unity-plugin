using System;
using System.Collections;
using System.Linq;
using Beam.Runtime.Client.Loaders.Base;
using Beam.Runtime.Client.Loaders.Events;
using Beam.Runtime.Client.Units;
using Beam.Runtime.Client.Units.Events;
using Beam.Runtime.Client.Units.Model;
using Beam.Runtime.Client.Utilities;
using Beam.Runtime.Sdk.Utilities;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;

namespace Beam.Runtime.Client.Loaders
{
  [RequireComponent(typeof(BeamImageUnitInstance))]
  public class BeamBasicImageLoader : BeamImageLoader
  {
    public Texture2D Placeholder;
    public Renderer TargetRenderer;

    [SerializeField]
    private Material targetMaterial;
    [SerializeField]
    private string targetMaterialProperty = "";
    private Texture originalTexture;
    private Texture2D nextTexture;
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
      
      this.targetMaterial = this.TargetRenderer.material;

      if (string.IsNullOrEmpty(this.targetMaterialProperty))
      {

        int[] propertyIndices = Enumerable.Range(0, this.targetMaterial.shader.GetPropertyCount())
          .Where(v => this.targetMaterial.shader.GetPropertyType(v) == ShaderPropertyType.Texture).ToArray();

        if (!propertyIndices.Any())
        {
          this.targetMaterialProperty = "_MainTex";
        }

        this.targetMaterialProperty = this.targetMaterial.shader.GetPropertyName(propertyIndices[0]);
      }
      this.originalTexture = this.targetMaterial.GetTexture(this.targetMaterialProperty);

      if (this.Placeholder)
      {
        this.targetMaterial.SetTexture(this.targetMaterialProperty, this.Placeholder);
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
#if UNITY_2020_1_OR_NEWER
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

        this.TargetRenderer.enabled = true;

      }
    }

    private void DoSwitch()
    {
      if (!Application.isPlaying)
      {
        return;
      }

      this.targetMaterial.SetTexture(this.targetMaterialProperty, this.nextTexture);
      if (this.beamAspectRatioHandler != null)
      {
        this.beamAspectRatioHandler.HandleAspectRatio(this.nextTexture.width, this.nextTexture.height);
      }
      this.TargetRenderer.enabled = true;
      this.nextTexture = null;
    }

    public void ResetTexture()
    {
      if (this.TargetRenderer)
      {
        this.TargetRenderer.sharedMaterial.SetTexture(this.targetMaterialProperty, this.originalTexture);
      }
    }

    public override void HandleFulfillment(ImageUnitFulfillmentData fulfillmentData)
    {
      if (fulfillmentData == null)
      {
        return;
      }
      UnitFulfillmentStatusCode status = fulfillmentData.StatusCode;
      if (status == UnitFulfillmentStatusCode.Started || status == UnitFulfillmentStatusCode.CompletedWithSameContent)
      {
        return;
      }
      if (status == UnitFulfillmentStatusCode.CompletedEmpty)
      {
        if (this.EmptyFulfillmentBehaviour == EmptyFulfillmentBehaviour.Hide)
        {
          this.ResetTexture();
          this.nextTexture = null;
          this.lowQualityTexture = null;
          this.highQualityTexture = null;
          this.TargetRenderer.enabled = false;
        }
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
