using System.Threading.Tasks;
using Beam.Runtime.Client.Loaders.Base;
using Beam.Runtime.Client.Units;
using Beam.Runtime.Client.Units.Events;
using Beam.Runtime.Client.Units.Model;
using Beam.Runtime.Sdk.Utilities;
using Beam.Runtime.Sdk.Extensions;
using GLTFast;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityGLTF;

namespace Beam.Runtime.Client.Loaders
{
  [RequireComponent(typeof(BeamThreeDimensionalUnitInstance))]
  public class BeamUnityGltfLoader : BeamThreeDimensionalUnitLoader
  {
    public GameObject Placeholder;
    public new UnityEvent OnThreeDimensionalUnitLoaded { get; }

    private BeamThreeDimensionalUnitInstance beamThreeDimensionalUnitInstance;
    private GameObject currentPlaceholder;
    private readonly GltfAsset gltfAsset;

    private GameObject highQualityInstance;
    private GameObject lowQualityInstance;

    // Added by Ben T
    public HDRP_ImporterFactory HdrpUDPImporter;
    private GLTFComponent gltfComponent;

    private new void Awake()
    {
      base.Awake();

      this.beamThreeDimensionalUnitInstance = this.GetComponent<BeamThreeDimensionalUnitInstance>();
      this.beamThreeDimensionalUnitInstance.OnLodStatusChanged.AddListener(this.HandleLodChange);

      if (this.Placeholder != null)
      {
        Transform currentTransform = this.transform;
        this.currentPlaceholder = Instantiate(this.Placeholder, currentTransform.position, currentTransform.rotation);
        this.currentPlaceholder.transform.parent = this.transform;
        this.OnThreeDimensionalUnitLoaded.Invoke();
      }

      if (Application.platform == RuntimePlatform.WebGLPlayer)
      {
        BeamLogger.LogError("BeamUnityGltfLoader is not supported in WebGL. Please use another Loader.");
      }

      this.gltfComponent = this.gameObject.AddComponent<GLTFComponent>();
      this.gltfComponent.enabled = false;
      this.SetupForRenderPipeline();
    }

    private void SetupForRenderPipeline()
    {
      UnityRenderPipeline pipeline = UtilDetermineRenderPipeline.getRenderSettings();
      switch (pipeline)
      {
        case UnityRenderPipeline.Legacy:
          break;
        case UnityRenderPipeline.UniversalRenderPipeline:
#if UNITY_2020_1_OR_NEWER
            case UnityRenderPipeline.UniversalRenderPipeline2020:
#endif
        case UnityRenderPipeline.HighDefRenderPipeline:
          this.SetGltfComponentHdrpImporter();
          break;
        case UnityRenderPipeline.Unknown:
        default:
          BeamLogger.LogWarning("Unable to determine render pipeline");
          break;
      }
    }

    private void SetGltfComponentHdrpImporter()
    {
      if (this.HdrpUDPImporter == null)
      {
        BeamLogger.LogError("Please assign a HDRP importer for this asset");
      }
      else
      {
        this.gltfComponent.Factory = this.HdrpUDPImporter;
      }
    }


#if UNITY_EDITOR
    private void DestroyInEditMode(EditorApplication.CallbackFunction callback = null)
    {
      if (Application.isPlaying)
      {
        Destroy(this.currentPlaceholder);
        this.currentPlaceholder = null;
        this.transform.Clear();
        callback?.Invoke();
      }
      else
      {
        EditorApplication.delayCall += () =>
        {
          DestroyImmediate(this.currentPlaceholder);
          this.currentPlaceholder = null;
          this.transform.Clear();
          callback?.Invoke();
        };
      }
    }
#endif

    public override async void HandleFulfillment(ThreeDimensionalUnitFulfillmentData fulfillmentData)
    {
      if (fulfillmentData == null)
      {
        return;
      }
      this.transform.Clear();

      bool highQualityAvailable = !string.IsNullOrEmpty(fulfillmentData.HighQualityUrl);
      bool lowQualityAvailable = !string.IsNullOrEmpty(fulfillmentData.LowQualityUrl);

      // Same url.
      // TODO: Handle 0 LOD distance
      bool onlyLoadOneQuality =
        string.CompareOrdinal(fulfillmentData.HighQualityUrl, fulfillmentData.LowQualityUrl) == 0;

      if (highQualityAvailable && (onlyLoadOneQuality || !lowQualityAvailable))
      {
        await this.LoadWithUnityGltf(fulfillmentData.HighQualityUrl, LodStatus.InsideHighQualityRange);
        this.lowQualityInstance = this.highQualityInstance;
      }
      else if (lowQualityAvailable && (onlyLoadOneQuality || !highQualityAvailable))
      {

        await this.LoadWithUnityGltf(fulfillmentData.LowQualityUrl, LodStatus.OutsideHighQualityRange);
        this.highQualityInstance = this.lowQualityInstance;
      }
      // both available
      else if (highQualityAvailable)
      {
        await this.LoadWithUnityGltf(fulfillmentData.LowQualityUrl, LodStatus.OutsideHighQualityRange);
        await this.LoadWithUnityGltf(fulfillmentData.HighQualityUrl, LodStatus.InsideHighQualityRange);
      }

      this.OnThreeDimensionalUnitLoaded.Invoke();
    }

    private async Task LoadWithUnityGltf(string uri, LodStatus quality)
    {
      this.gltfComponent.enabled = true;

      if (this.gltfComponent.GLTFUri == uri)
      {
        return;
      }

      this.gltfComponent.GLTFUri = uri;

      if (this.transform.childCount > 0)
      {
        this.currentPlaceholder = quality == LodStatus.InsideHighQualityRange ? this.highQualityInstance : this.lowQualityInstance;
      }

      await this.gltfComponent.Load();

      if (this.currentPlaceholder)
      {
        DestroyImmediate(this.currentPlaceholder);
      }

      if (quality == LodStatus.InsideHighQualityRange)
      {
        this.highQualityInstance = this.gltfComponent.LastLoadedScene;
        if (this.CurrentLodStatus != quality)
        {
          this.highQualityInstance.SetActive(false);
        }
      }
      else
      {
        this.lowQualityInstance = this.gltfComponent.LastLoadedScene;
        if (this.CurrentLodStatus != quality)
        {
          this.lowQualityInstance.SetActive(false);
        }
      }
    }

    protected override void HandleLodChange(LodStatus lodStatus)
    {
      if (this.highQualityInstance == this.lowQualityInstance)
      {
        return;
      }

      if (this.CurrentLodStatus == LodStatus.InsideHighQualityRange && this.highQualityInstance != null)
      {
        this.lowQualityInstance.SetActive(false);
        this.highQualityInstance.SetActive(true);
      }
      else if (this.CurrentLodStatus == LodStatus.OutsideHighQualityRange && this.lowQualityInstance != null)
      {
        this.highQualityInstance.SetActive(false);
        this.lowQualityInstance.SetActive(true);
      }
    }

  }
}
