using System.Collections.Generic;
using System.Linq;
using Beam.Runtime.Sdk.Utilities;
using Beam.Runtime.Sdk.Data;
using Beam.Runtime.Sdk.Generated.Model;
using UnityEngine;
using UnityEngine.Events;
using System;

namespace Beam.Runtime.Client.Metadata
{
  [ExecuteAlways]
  public class BeamCustomMetadataHandler : MonoBehaviour
  {

    [SerializeField]
    public List<CustomMetadataHandler> CustomMetadataHandlers;

    [SerializeField]
    public List<IAssetCustomMetadata> ReceivedMetadata;
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

    public void Handle(IUnitFulfillmentResponse response)
    {
      if (response == null || response.CustomMetadata == null)
      {
        return;
      }
      response.CustomMetadata.ForEach(cm =>
      {
        CustomMetadataHandler matchedHandler = this.CustomMetadataHandlers.FirstOrDefault(k => k.Id == cm.KeyId);
        if (matchedHandler != null)
        {
          UnityEvent<string> ev = (UnityEvent<string>)matchedHandler.OnMetadataReceived;
          int count = ev.GetPersistentEventCount();
          for (int i = 0; i < count; i++)
          {
            try
            {
              UnityEngine.Object target = ev.GetPersistentTarget(i);
              if (!(target is MonoBehaviour))
              {
                continue;
              }
              if (target is null)
              {
                continue;
              }
              string method = ev.GetPersistentMethodName(i);
              (target as MonoBehaviour).SendMessage(method, cm.Value);
            }
            catch (Exception e)
            {
              Debug.LogError($"Error triggering custom metadata handler {matchedHandler.Name} on gameobject {this.gameObject.name}.");
              Debug.LogException(e);
            }
          }
        }
      });
    }

    public void BasicLogger(string value)
    {
      BeamLogger.LogInfo($"The value was {value}");
    }

    public IAssetCustomMetadata GetAssetCustomMetadataByKeyId(string id)
    {
      return this.ReceivedMetadata.Where(cm => cm.KeyId == id).FirstOrDefault();
    }

    public IAssetCustomMetadata GetAssetCustomMetadataByName(string name)
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
      this.ReceivedMetadata = new List<IAssetCustomMetadata>();
    }
  }
}
