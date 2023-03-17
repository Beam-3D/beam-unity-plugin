// Below define is necessary for adding colliders to GLTFast units
// as referenced here: https://github.com/atteneder/glTFast/blob/main/Documentation~/glTFast.md
// and here: https://docs.unity3d.com/ScriptReference/Mesh-isReadable.html
#define GLTFAST_KEEP_MESH_DATA
using System.Threading.Tasks;
using Beam.Runtime.Client.Loaders.Base;
using Beam.Runtime.Client.Loaders.Events;
using Beam.Runtime.Client.Units;
using Beam.Runtime.Client.Units.Events;
using Beam.Runtime.Client.Units.Model;
using Beam.Runtime.Client.Utilities;
using Beam.Runtime.Sdk.Extensions;
using Beam.Runtime.Sdk.Utilities;
using GLTFast;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Beam.Runtime.Client.Loaders
{
  [RequireComponent(typeof(BeamThreeDimensionalUnitInstance))]
  // ReSharper disable once InconsistentNaming
  public class BeamGLTFastLoader : BeamThreeDimensionalUnitLoader
  {
    public GameObject Placeholder;
    private GameObject currentPlaceholder;
    private readonly GltfAsset gLtfAsset;
    private string lastLowQualityUrl;
    private string lastHighQualityUrl;
    private GameObject highQualityInstance;
    private GameObject lowQualityInstance;

    private new void Awake()
    {
      base.Awake();

      this.BeamThreeDimensionalUnitInstance.OnLodStatusChanged.AddListener(this.HandleLodChange);

      if (this.Placeholder == null)
      {
        return;
      }

      Transform currentTransform = this.transform;
      this.currentPlaceholder = Instantiate(this.Placeholder, currentTransform.position, currentTransform.rotation);
      this.currentPlaceholder.transform.parent = this.transform;
    }

#if UNITYEDITOR
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

    public override async void HandleFulfillment(ThreeDimensionalUnitFulfillmentData unitFulfillmentData)
    {
      if (unitFulfillmentData == null)
      {
        return;
      }
      UnitFulfillmentStatusCode status = unitFulfillmentData.StatusCode;
      if (status == UnitFulfillmentStatusCode.Started || status == UnitFulfillmentStatusCode.CompletedWithSameContent)
      {
        return;
      }
      if (status == UnitFulfillmentStatusCode.CompletedEmpty)
      {
        if (this.EmptyFulfillmentBehaviour == EmptyFulfillmentBehaviour.Hide)
        {
          this.transform.Clear();
          this.lastLowQualityUrl = null;
          this.lastHighQualityUrl = null;
        }
        return;
      }

      this.transform.Clear();

      bool highQualityAvailable = !string.IsNullOrEmpty(unitFulfillmentData.HighQualityUrl);
      bool lowQualityAvailable = !string.IsNullOrEmpty(unitFulfillmentData.LowQualityUrl);

      // Same url.
      // TODO: Handle 0 LOD distance
      bool onlyLoadOneQuality =
        string.CompareOrdinal(unitFulfillmentData.HighQualityUrl, unitFulfillmentData.LowQualityUrl) == 0;

      if (highQualityAvailable && (onlyLoadOneQuality || !lowQualityAvailable))
      {
        await this.LoadWithGltfFast(unitFulfillmentData.HighQualityUrl, LodStatus.InsideHighQualityRange);
        this.lowQualityInstance = this.highQualityInstance;
      }
      else if (lowQualityAvailable && (onlyLoadOneQuality || !highQualityAvailable))
      {

        await this.LoadWithGltfFast(unitFulfillmentData.LowQualityUrl, LodStatus.OutsideHighQualityRange);
        this.highQualityInstance = this.lowQualityInstance;
      }
      // both available
      else if (highQualityAvailable)
      {
        await this.LoadWithGltfFast(unitFulfillmentData.LowQualityUrl, LodStatus.OutsideHighQualityRange);
        await this.LoadWithGltfFast(unitFulfillmentData.HighQualityUrl, LodStatus.InsideHighQualityRange);
      }

      this.lastHighQualityUrl = unitFulfillmentData.HighQualityUrl;
      this.lastLowQualityUrl = unitFulfillmentData.LowQualityUrl;
    }

    private async Task LoadWithGltfFast(string uri, LodStatus quality)
    {
      GltfAsset gltfAsset;

      if (this.transform.childCount > 0)
      {
        this.currentPlaceholder = quality == LodStatus.InsideHighQualityRange ? this.highQualityInstance : this.lowQualityInstance;
        this.transform.Clear(true);
      }

      GameObject go = new GameObject();
      var currentTransform = this.transform;
      go.transform.SetPositionAndRotation(currentTransform.position, currentTransform.rotation);
      go.transform.SetParent(this.transform);

      if (quality == LodStatus.InsideHighQualityRange)
      {
        go.name = "highQualityScene";
        this.highQualityInstance = go;
        gltfAsset = this.highQualityInstance.AddComponent<GltfAsset>();
      }
      else
      {
        go.name = "lowQualityScene";
        this.lowQualityInstance = go;
        gltfAsset = this.lowQualityInstance.AddComponent<GltfAsset>();
      }

      gltfAsset.LoadOnStartup = false;
      bool success = await gltfAsset.Load($"{uri}?filename=model.glb");

      if (success)
      {
        this.OnThreeDimensionalUnitLoaded?.Invoke(new ThreeDimensionalLoadedData(quality == LodStatus.InsideHighQualityRange ? this.highQualityInstance.transform : this.lowQualityInstance.transform, quality));
        if (this.currentPlaceholder)
        {
          DestroyImmediate(this.currentPlaceholder);
        }

        if (gltfAsset.gameObject == this.highQualityInstance && this.CurrentLodStatus != LodStatus.InsideHighQualityRange)
        {
          gltfAsset.gameObject.SetActive(false);
        }
        else if (gltfAsset.gameObject == this.lowQualityInstance && this.CurrentLodStatus != LodStatus.OutsideHighQualityRange)
        {
          gltfAsset.gameObject.SetActive(false);
        }
      }
      else
      {
        BeamLogger.LogError("Loading GLTF via GLTF FAST library failed!");
      }
    }

    private static void CorrectGltfFastReflection(Transform input)
    {
      foreach (Transform t in input)
        t.localScale = Vector3.Scale(t.localScale, new Vector3(-1, 1, -1));
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
