using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beam.Runtime.Client.Units;
using Beam.Runtime.Client.Utilities;
using Beam.Runtime.Sdk.Data;
using Beam.Runtime.Sdk.Generated;
using Beam.Runtime.Sdk.Generated.Api;
using Beam.Runtime.Sdk.Generated.Client;
using Beam.Runtime.Sdk.Generated.Model;
using Beam.Runtime.Sdk.Utilities;
using UnityEditor;
using UnityEngine;
using GLTFast.Export;
using Object = UnityEngine.Object;
using System.IO;

namespace Beam.Editor.Utilities
{
  public class SceneUploader
  {
    static readonly BeamData beamData;
    static readonly string bucketName = "public_assets";
    public static async Task UploadScene(string sceneId, bool uploadGeometry, bool uploadUnitLocations)
    {
      var loginResponse = FileHelper.LoadLoginData();
      if (loginResponse == null)
      {
        BeamLogger.LogInfo("Please log in to upload a scene.");
        return;
      }

      try
      {
        string modelUploadTicket = null;
        RepresentationMetadata representationMetadata = null;

        if (uploadGeometry)
        {
          byte[] exportedScene = await GetActiveSceneAsGlb();

          if (exportedScene.Length != 0)
          {
            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            string uploadSceneName = currentSceneName + ".glb";
            modelUploadTicket = await UploadModelToStorage(currentSceneName, exportedScene);
          }
          else
          {
            BeamLogger.LogWarning("No scene geometry found.");
          }
        }

        if (uploadUnitLocations)
        {
          representationMetadata = GetRepresentationMetadataForScene(sceneId);
        }

        IScene sceneUpdateResponse = await UploadSceneRepresentation(sceneId, modelUploadTicket, representationMetadata);
        BeamLogger.LogInfo($"Updated scene representation for scene: {sceneUpdateResponse.Name} at {sceneUpdateResponse.UpdatedOn}.");
      }
      catch (Exception e)
      {
        BeamLogger.LogError(e.Message);
        return;
      }
    }

    private static async Task<FileCreationResult> IniateUploadModelToStorage(string fileName, int contentLength, string contentType, UploadApi uploadApi)
    {
      OrbitUploadMetadataAny uploadMetadata = new OrbitUploadMetadataAny(name: fileName, size: contentLength, contentType: "model/gltf-binary", additionalMetadata: new System.Object());
      FileCreationResult result = await uploadApi.UploadBucketNamePostAsync(bucketName, uploadMetadata);

      return result;
    }
    private static async Task<string> UploadModelToStorage(string filename, byte[] model)
    {
      ApiClient client = new ApiClient(Endpoint.GetEndpoints().StorageUrl, new TokenManager());
      UploadApi uploadApi = new UploadApi(client, client, new Configuration());
      FileCreationResult postResponse = await IniateUploadModelToStorage(filename, model.Length, "model/gltf-binary", uploadApi);
      await client.UploadByteArrayToStorage(model, bucketName, postResponse.Ticket);
      return postResponse.Ticket;
    }

    private static async Task<IScene> UploadSceneRepresentation(string sceneId, string storageTicket, RepresentationMetadata metadata)
    {
      ApiClient publishingApiClient = new ApiClient(Endpoint.GetEndpoints().PublishingUrl, new TokenManager());
      SceneApi sceneApi = new SceneApi(publishingApiClient, publishingApiClient, new Configuration());

      var scenes = await sceneApi.ProjectsScenesIdsGetAsync(new List<string>() { sceneId });
      var scene = scenes.FirstOrDefault();
      if (scene == null)
      {
        throw new Exception($"Failed to retrieve scene '{sceneId}'");
      }

      UpdatableSceneRepresentationOmitId sceneRepresentation = new UpdatableSceneRepresentationOmitId(storageTicket, metadata, scene.UpdatedOn);
      SceneUpdateSceneRepresentationBody sceneUpdateBody = new SceneUpdateSceneRepresentationBody(sceneRepresentation);

      return await sceneApi.ProjectsScenesIdPatchAsync(sceneId, sceneUpdateBody);
    }

    public static async Task<byte[]> GetActiveSceneAsGlb()
    {
      UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
      GameObject[] rootGameObjects = scene.GetRootGameObjects();

      List<GameObject> disabledObjects = new List<GameObject>();

      foreach (GameObject go in rootGameObjects)
      {
        foreach (Transform t in go.transform)
        {
          BeamUnitInstance beamUnitInstance = t.gameObject.GetComponent<BeamUnitInstance>();
          if (beamUnitInstance && t.gameObject.activeInHierarchy)
          {
            disabledObjects.Add(beamUnitInstance.gameObject);
            beamUnitInstance.gameObject.SetActive(false);
          }
        }
      }

      List<ReflectionProbe> probes = Object.FindObjectsOfType<ReflectionProbe>().ToList();
      probes.ForEach(p => p.gameObject.SetActive(false));

      GameObject[] exportableTransforms = rootGameObjects
      .Where(go => go.activeInHierarchy)
      .Select(go => go.gameObject).ToArray();

      GameObjectExport export = new GameObjectExport(new ExportSettings
      {
        format = GltfFormat.Binary,
        fileConflictResolution = FileConflictResolution.Overwrite
      });
      export.AddScene(exportableTransforms);

      using (MemoryStream ms = new MemoryStream())
      {
        await export.SaveToStreamAndDispose(ms);
        disabledObjects.ForEach(go => go.SetActive(true));
        probes.ForEach(p => p.gameObject.SetActive(true));
        return ms.ToArray();
      }
    }

    private static string RetrieveTexturePath(Texture texture)
    {
      return AssetDatabase.GetAssetPath(texture);
    }

    private static RepresentationMetadata GetRepresentationMetadataForScene(string sceneId)
    {
      List<RepresentationUnitInstanceLocation> unitInstanceLocations = Object.FindObjectsOfType<BeamUnitInstance>()
      .Where(instance => instance.ProjectUnit.Unit.SceneId == sceneId)
      .Select(instance =>
      {
        Vector3 instancePos = Vector3.zero;
        Vector3 instanceRot = instance.transform.rotation.eulerAngles;
        Vector3 instanceScale = Vector3.one;

        if (instance.GetType() == typeof(BeamThreeDimensionalUnitInstance))
        {
          BeamThreeDimensionalUnitInstance threeDInstance = instance as BeamThreeDimensionalUnitInstance;
          BeamModelResizer resizer = instance.GetComponent<BeamModelResizer>();

          if (resizer != null)
          {
            float targetScale = resizer.TargetScale;

            Vector3 offset = Vector3.zero;
            if (resizer.SnapToBase && resizer.PivotAtBase)
            {
              float offsetDistance = targetScale / 2;
              offset = Vector3.Scale(instance.transform.up, new Vector3(offsetDistance, offsetDistance, offsetDistance));
            }
            instancePos = instance.transform.position + offset;
            instanceScale = new Vector3(targetScale, targetScale, targetScale);

          }
        }
        else
        {
          instancePos = instance.transform.position;
          instanceScale = instance.transform.lossyScale;
        }

        Coordinates coordinatesPos = new Coordinates(instancePos.x, instancePos.y, instancePos.z);
        Coordinates coordinatesRot = new Coordinates(instanceRot.x, instanceRot.y, instanceRot.z);
        Coordinates coordinatesScale = new Coordinates(instanceScale.x, instanceScale.y, instanceScale.z);
        return new RepresentationUnitInstanceLocation(instance.ProjectUnit.Unit.Id, instance.UnitInstance.Id, instance.ProjectUnit.Kind, coordinatesPos, coordinatesRot, coordinatesScale);
      }).ToList<RepresentationUnitInstanceLocation>();

      return new RepresentationMetadata(CoordinateSystem.LeftHanded, unitInstanceLocations);
    }
  }
}
