using System.Collections.Generic;
using System.Linq;
using Beam.Runtime.Sdk.Utilities;
using Beam.Runtime.Sdk.Data;
using Beam.Runtime.Sdk.Generated.Model;
using UnityEngine;
using UnityEngine.Events;

namespace Beam.Runtime.Client.Metadata
{
  [ExecuteAlways]
  public class BeamCustomMetadataHandler : MonoBehaviour
  {

    [SerializeField]
    public List<CustomMetadataHandler> CustomMetadataHandlers;

    [SerializeField]
    public List<AssetCustomMetadata> ReceivedMetadata;
    private protected void OnEnable()
    {
#if UNITY_EDITOR
      if (!Application.isPlaying)
      {
        BeamDataSynchronizer.RuntimeDataRefreshed += this.UpdateMetadataHandlers;
        this.UpdateMetadataHandlers();
      }
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
      if (!Application.isPlaying)
      {
        BeamDataSynchronizer.RuntimeDataRefreshed -= this.UpdateMetadataHandlers;
      }
#endif
    }

    public void Handle(UnitFulfillmentResponse response)
    {
      if (response == null || response.CustomMetadata == null)
      {
        return;
      }
      response.CustomMetadata.ForEach(cm =>
      {
        var matchedHandler = this.CustomMetadataHandlers.FirstOrDefault(k => k.Id == cm.KeyId);
        if (matchedHandler != null)
        {
          UnityEvent<string> ev = (UnityEvent<string>)matchedHandler.OnMetadataReceived;
          MonoBehaviour target = (MonoBehaviour)ev.GetPersistentTarget(0);
          string method = ev.GetPersistentMethodName(0);
          target.SendMessage(method, cm.Value);
        }
      });
    }

    public void BasicLogger(string value)
    {
      BeamLogger.LogInfo($"The value was {value}");
    }

    public AssetCustomMetadata GetAssetCustomMetadataByKeyId(string id)
    {
      return this.ReceivedMetadata.Where(cm => cm.KeyId == id).FirstOrDefault();
    }

    public AssetCustomMetadata GetAssetCustomMetadataByName(string name)
    {
      if (BeamClient.RuntimeData.CustomMetadataKeys == null)
      {
        return null;
      }

      var key = BeamClient.RuntimeData.CustomMetadataKeys.Where(k => k.Name.Equals(name)).FirstOrDefault();
      if (key != null)
      {
        this.GetAssetCustomMetadataByKeyId(key.Id);
      }

      return null;
    }

    // TODO: Move this to the editor
    private void UpdateMetadataHandlers()
    {
      if (this.CustomMetadataHandlers == null)
      {
        this.CustomMetadataHandlers = new List<CustomMetadataHandler>();
      }

      var keys = BeamClient.RuntimeData.CustomMetadataKeys;
      List<string> existingKeys = this.CustomMetadataHandlers.Select(cmh => cmh.Id).ToList();

      existingKeys.ForEach(id =>
      {
        if (!keys.Any(k => k.Id == id))
        {
          this.CustomMetadataHandlers.Remove(this.CustomMetadataHandlers.Find(cmh => cmh.Id == id));
        }
      });

      keys.ForEach(k =>
      {
        if (!this.CustomMetadataHandlers.Any(e => e.Id == k.Id))
        {
          this.CustomMetadataHandlers.Add(new CustomMetadataHandler(k.Id, k.Name, new ReceivedMetadataEvent()));
        }
      });
      this.ReceivedMetadata = new List<AssetCustomMetadata>();
    }
  }
}
