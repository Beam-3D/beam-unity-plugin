using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beam.Runtime.Client.Units;
using Beam.Runtime.Client.Units.Events;
using Beam.Runtime.Client.Units.Model;
using Beam.Runtime.Sdk.Utilities;
using Beam.Runtime.Sdk.Extensions;
using Beam.Runtime.Sdk.Generated.Model;
using Beam.Runtime.Sdk.Model;
using UnityEngine;
using BeamUnitInstance = Beam.Runtime.Client.Units.BeamUnitInstance;

namespace Beam.Runtime.Client.Managers
{
  public enum InstantFulfillmentStatus
  {
    NotStarted,
    Started,
    Finished
  }

  public class BeamFulfillmentManager : MonoBehaviour
  {
    public IFulfillmentResponse FulfillmentResponse;

    private InstantFulfillmentStatus InstantFulfillmentStatus { get; set; } =
      InstantFulfillmentStatus.NotStarted;

    private bool isFulfilling;
    private int chunksPending;
    private int chunksComplete;


    private FulfillmentRequestMetadata BuildMetadata(AssetKind kind, string minQualityId, string maxQualityId)
    {
      FulfillmentRequestMetadata metadata = null;
      switch (kind)
      {
        case AssetKind.Audio:
          metadata = new IAudioFulfillmentRequestMetadata
          {
            Kind = AssetKindAudio.Audio,
            AudioQualityId = maxQualityId
          };
          break;
        case AssetKind.Image:
          metadata = new IImageFulfillmentRequestMetadata
          {
            Kind = AssetKindImage.Image,
            LowImageQualityId = minQualityId,
            HighImageQualityId = maxQualityId
          };
          break;
        case AssetKind.Video:
          metadata = new IVideoFulfillmentRequestMetadata
          {
            Kind = AssetKindVideo.Video,
            VideoQualityId = maxQualityId
          };
          break;
        case AssetKind.ThreeDimensional:
          metadata = new IThreeDimensionalFulfillmentRequestMetadata
          {
            Kind = AssetKindThreeDimensional.ThreeDimensional,
            LowModelQualityId = minQualityId,
            HighModelQualityId = maxQualityId
          };
          break;
        case AssetKind.Data:
          metadata = new IDataFulfillmentRequestMetadata
          {
            Kind = AssetKindData.Data,
          };
          break;
      }

      return metadata;
    }

    public IEnumerator PollFulfillment(int pollingRate = 0)
    {
      if (pollingRate < 1)
      {
        BeamLogger.LogWarning("Polling rates under 1 second are not supported for performance reasons.");
        yield return null;
      }

      while (Application.isPlaying)
      {
        yield return new WaitForSeconds(pollingRate);
        if (!this.isFulfilling)
        {
          this.StartCoroutine(this.RunPolledFulfillment());
        }
      }
    }

    public IEnumerator RunInstantFulfillments()
    {
      // Coroutines can be looped more easily on the Unity game loop for future polling support
      this.InstantFulfillmentStatus = InstantFulfillmentStatus.Started;
      this.isFulfilling = true;

      var placedInstances = FindObjectsOfType<BeamUnitInstance>();

      var instantFulfillments = placedInstances
        .Where(pi => pi.ProjectUnit.Unit != null && !string.IsNullOrWhiteSpace(pi.ProjectUnit.Unit.Id))
        .Where(pi => pi.ProjectUnit.FulfillmentBehaviour == FulfillmentBehaviour.Instant)
        .ToArray();

      if (!instantFulfillments.Any())
      {
        BeamLogger.LogWarning("No units found, no instant fulfillment to run.");
        this.InstantFulfillmentStatus = InstantFulfillmentStatus.Finished;
        this.isFulfilling = false;
        yield break;
      }

      BeamLogger.LogInfo($"{instantFulfillments.Count()} units to fulfill with 'Instant' fulfillment behaviour");

      BeamUnitInstance[][] chunked = instantFulfillments.Chunk(50);
      this.chunksPending += chunked.Length;

      foreach (var batch in chunked)
      {
        this.RunManualFulfillment(batch.ToList());
      }

      while (this.chunksPending != this.chunksComplete)
      {
        yield return new WaitForSeconds(0.5f);
      }

      BeamLogger.LogInfo("Instant fulfillment complete");

      this.InstantFulfillmentStatus = InstantFulfillmentStatus.Finished;
      this.isFulfilling = false;
      this.chunksPending = 0;
      yield return null;
    }

    private IEnumerator RunPolledFulfillment()
    {
      if (this.InstantFulfillmentStatus != InstantFulfillmentStatus.Finished || this.isFulfilling)
      {
        yield break;
      }

      this.isFulfilling = true;

      // Select placed units that have already been fulfilled.
      // We may want to add a toggle to prevent re fulfillment.

      BeamUnitInstance[] placedInstances = FindObjectsOfType<BeamUnitInstance>()
        .Where(placedInstance => placedInstance.ProjectUnit.Unit != null && !string.IsNullOrWhiteSpace(placedInstance.ProjectUnit.Unit.Id))
        .Where(placedInstance => placedInstance.ProjectUnit.FulfilledSinceAwake)
        .ToArray();

      var placedUnits = placedInstances.Select(placedInstance => placedInstance.ProjectUnit);

      List<string> placedIds = placedUnits.Select(u => u.Unit.Id).ToList();

      Task<List<IUnitChecksum>> task = BeamClient.Sdk.Fulfillments.GetUnitIdChecksumsAsync(placedIds);
      yield return new WaitUntil(() => task.IsCompleted || task.IsFaulted || task.IsCanceled);

      if (task.IsFaulted)
      {
        BeamLogger.LogError("Failed getting LastUpdatedUnits");
        this.isFulfilling = false;
        if (task.Exception != null)
        {
          throw task.Exception;
        }
      }
      else if (task.IsCanceled)
      {
        this.isFulfilling = false;
        yield break;
      }

      List<IUnitChecksum> lastUpdatedUnitChecksums = task.Result;

      var unitsToFulfill = placedInstances
        .Where(placedInstance => !string.IsNullOrEmpty(placedInstance.ProjectUnit.LastFulfillmentChecksum))
        .Where(placedInstance => lastUpdatedUnitChecksums.Any(lu => lu.UnitId == placedInstance.ProjectUnit.Unit.Id))
        .Where(placedInstance => placedInstance.ProjectUnit.LastFulfillmentChecksum !=
                    lastUpdatedUnitChecksums.FirstOrDefault(lu => lu.UnitId == placedInstance.ProjectUnit.Unit.Id)?.Checksum)
        .ToArray();

      placedInstances.ToList().ForEach(placedInstance =>
      {
        if (string.IsNullOrEmpty(placedInstance.ProjectUnit.LastFulfillmentChecksum))
        {
          placedInstance.ProjectUnit.LastFulfillmentChecksum = lastUpdatedUnitChecksums.FirstOrDefault(lu => lu.UnitId == placedInstance.ProjectUnit.Unit.Id)?.Checksum;
        }
      });

      if (!unitsToFulfill.Any())
      {
        this.isFulfilling = false;
        yield break;
      }

      BeamLogger.LogInfo($"{unitsToFulfill.Count()} units polled for fulfillment.");

      BeamUnitInstance[][] chunked = unitsToFulfill.Chunk(50);
      this.chunksPending += chunked.Length;

      foreach (BeamUnitInstance[] batch in chunked)
      {
        this.RunManualFulfillment(batch.ToList());
      }

      while (this.chunksPending != this.chunksComplete)
      {
        yield return new WaitForSeconds(0.5f);
      }

      this.isFulfilling = false;
    }
    public async void RunManualFulfillment(List<BeamUnitInstance> unitInstances, List<string> dynamicTags = null)
    {
      BeamLogger.LogInfo("Running manual fulfillment");
      if (BeamClient.CurrentSession == null || string.IsNullOrWhiteSpace(BeamClient.CurrentSession.Id))
      {
        BeamLogger.LogWarning("You cannot run fulfillment without a valid session.");
        return;
      }

      HashSet<string> dynamicTagSet = new HashSet<string>(BeamClient.RuntimeData.DynamicTags);
      dynamicTagSet.UnionWith(BeamClient.ActiveDynamicTags);

      if (dynamicTags != null)
      {
        dynamicTagSet.UnionWith(dynamicTags);
      }

      IFulfillmentRequest request = new IFulfillmentRequest
      {
        SessionId = BeamClient.CurrentSession.Id,
        DynamicTags = dynamicTagSet.ToList(),
        Units = new List<IUnitFulfillmentRequest>(unitInstances.Select(unitInstance =>
        {
          var kind = unitInstance.ProjectUnit.Kind;
          var projectUnit = unitInstance.ProjectUnit;

          IUnitFulfillmentRequest instance = new IUnitFulfillmentRequest
          {
            UnitId = projectUnit.Unit.Id,
            Metadata = this.BuildMetadata(kind, projectUnit.MinQualityId, projectUnit.MaxQualityId),
          };

          return instance;
        }))
      };

      if (!string.IsNullOrWhiteSpace(BeamClient.RuntimeData.ProjectApiKey))
      {
        request.ProjectApiKey = BeamClient.RuntimeData.ProjectApiKey;
      }

      unitInstances.ForEach(this.HandleFulfillmentStartEvent);

      this.FulfillmentResponse = await BeamClient.Sdk.Fulfillments.FulfillAsync(request);
      this.chunksComplete += 1;
      this.HandleFulfillments();
    }

    private void HandleFulfillmentStartEvent(BeamUnitInstance unitInstance)
    {
      UnitFulfillmentData fulfillmentData = new UnitFulfillmentData(UnitFulfillmentStatusCode.Started, unitInstance.ProjectUnit.Unit.Id);
      switch (unitInstance.Kind)
      {
        case AssetKind.Image:
          {
            BeamImageUnitInstance imageUnitInstance = (BeamImageUnitInstance)unitInstance;
            imageUnitInstance.OnFulfillmentUpdated?.Invoke(new ImageUnitFulfillmentData(fulfillmentData));
            break;
          }
        case AssetKind.Audio:
          {
            BeamAudioUnitInstance audioInstance = (BeamAudioUnitInstance)unitInstance;
            audioInstance.OnFulfillmentUpdated?.Invoke(new AudioUnitFulfillmentData(fulfillmentData));
            break;
          }
        case AssetKind.Video:
          {
            BeamVideoUnitInstance videoUnitInstance = (BeamVideoUnitInstance)unitInstance;
            videoUnitInstance.OnFulfillmentUpdated?.Invoke(new VideoUnitFulfillmentData(fulfillmentData));
            break;
          }
        case AssetKind.ThreeDimensional:
          {
            BeamThreeDimensionalUnitInstance threeDUnitInstance = (BeamThreeDimensionalUnitInstance)unitInstance;
            threeDUnitInstance.OnFulfillmentUpdated?.Invoke(new ThreeDimensionalUnitFulfillmentData(fulfillmentData));
            break;
          }
      }
    }

    private async void HandleFulfillments()
    {
      BeamUnitInstance[] unitInstances = FindObjectsOfType<BeamUnitInstance>();
      List<ICreateEvent> fulfillmentEvents = new List<ICreateEvent>();
      this.FulfillmentResponse.Units.ForEach(fr =>
      {
        List<BeamUnitInstance> matchedUnits = unitInstances.Where(ui => ui.ProjectUnit.Unit.Id == fr.UnitId).ToList();
        if (!matchedUnits.Any()) return;
        foreach (BeamUnitInstance unit in matchedUnits)
        {

          unit.HandleFulfillment(fr);
        }
      });

      if (fulfillmentEvents.Any())
      {
        await BeamClient.Sdk.Events.SendAsync(fulfillmentEvents);
      }
    }
  }
}
