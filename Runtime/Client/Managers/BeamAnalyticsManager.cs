using System;
using System.Collections.Generic;
using System.Linq;
using Beam.Runtime.Client.Units;
using Beam.Runtime.Sdk.Utilities;
using Beam.Runtime.Sdk;
using Beam.Runtime.Sdk.Extensions;
using Beam.Runtime.Sdk.Generated.Model;
using UnityEngine;

namespace Beam.Runtime.Client.Managers
{
  [RequireComponent(typeof(BeamAreaBoundsManager))]
  public class BeamAnalyticsManager : MonoBehaviour
  {
    public Camera mainCamera;

    private BeamSdk beamSdk;
    [SerializeReference] private float? gazePositionThreshold;
    private bool ready;

    // Player related
    private Vector3 lastPlayerPosition;
    [SerializeReference] private float? playerPositionThreshold;

    // Gaze related
    public GameObject DebugGazeMarker;
    private Guid lastGazeEventReference;

    [Tooltip(
      "When this is checked Gaze events will be triggered by any child object of a BeamThreeDimensionalUnit with a collider.")]
    public bool EnableGazeEventsFor3DUnits = true;

    private readonly GameObject gazeMarkerInstance;
    private List<GameObject> gazeMarkers;
    private GameObject lastHit;
    private Vector3 lastHitPoint;
    private ExternalCreateEvent gazeStartEvent;
    private List<ExternalCreateEvent> gazeEvents = new List<ExternalCreateEvent>();
    private ExternalCreateEvent gazeEndEvent;
    private BeamUnitInstance gazedUnitInstance;

    // Audio related
    private readonly Dictionary<UnitInstanceData, Guid> currentAudioEventReferences =
      new Dictionary<UnitInstanceData, Guid>();

    // Video Related
    private readonly Dictionary<UnitInstanceData, Guid> currentVideoEventReferences =
      new Dictionary<UnitInstanceData, Guid>();

    // Area Bounds related
    private BeamAreaBoundsManager areaBoundsManager;

    // Dict<SceneBounds, sceneId>
    private readonly Dictionary<Collider, string> areaBoundsList = new Dictionary<Collider, string>();

    public void Init()
    {
      BeamLogger.LogInfo($"Started session with ID {BeamClient.CurrentSession?.Id}");
      this.gazeMarkers = new List<GameObject>();
      this.areaBoundsManager = this.GetComponent<BeamAreaBoundsManager>();
      // Inverting the SceneBoundsList so we can look up colliders rather than sceneIds
      // Only one collider is allowed per scene so this should be fine.
      foreach (KeyValuePair<string, BoxCollider> kvp in this.areaBoundsManager.AreaBoundsList)
      {
        this.areaBoundsList.Add(kvp.Value, kvp.Key);
      }

      if (this.gazePositionThreshold == null)
      {
        this.gazePositionThreshold = 0.05f;
      }

      if (this.playerPositionThreshold == null)
      {
        this.playerPositionThreshold = 0.5f;
      }

      if (this.mainCamera == null)
      {
        this.mainCamera = Camera.main;
      }

      if (this.mainCamera == null)
      {
        BeamLogger.LogWarning("Main camera must be assigned for analytics. Please assign the main camera.");
        return;
      }

      this.ready = true;
    }

    public void Update()
    {
      if (!this.ready || string.IsNullOrWhiteSpace(BeamClient.CurrentSession?.Id))
      {
        return;
      }

      this.TrackGaze();
      this.TrackPlayer();
    }

    void OnApplicationQuit()
    {
      this.TrackSessionStop();
    }

    public async void TrackSessionStop()
    {
      if (!this.ready || string.IsNullOrEmpty(BeamClient.CurrentSession?.Id))
      {
        return;
      }

      this.SendGazeEvents();

      IEnumerable<ExternalCreateEvent> audioEndEvents = this.currentAudioEventReferences.Select(kvp =>
        new ExternalCreateEvent
        {
          SessionId = BeamClient.CurrentSession?.Id,
          Timestamp = DateTime.UtcNow,
          Metadata = new AudioEventMetadata
          {
            Kind = EventKind.Audio,
            Action = AudioEventActionKind.End,
            FulfillmentId = kvp.Key.FulfillmentId,
            InstanceId = kvp.Key.InstanceId,
            Reference = kvp.Value.ToString()
          }
        });

      IEnumerable<ExternalCreateEvent> videoEndEvents = this.currentVideoEventReferences.Select(kvp =>
        new ExternalCreateEvent
        {
          SessionId = BeamClient.CurrentSession?.Id,
          Timestamp = DateTime.UtcNow,
          Metadata = new VideoEventMetadata
          {
            Kind = EventKind.Video,
            Action = VideoEventActionKind.End,
            FulfillmentId = kvp.Key.FulfillmentId,
            InstanceId = kvp.Key.InstanceId,
            Reference = kvp.Value.ToString()
          }
        });

      IEnumerable<ExternalCreateEvent> avEvents = audioEndEvents.Concat(videoEndEvents).ToList();

      if (avEvents.Any())
      {
        LogEvents(avEvents);
      }

      await BeamClient.Sdk.Session.Session.IdStopPostAsync(BeamClient.CurrentSession?.Id);
    }

    private void TrackPlayer()
    {
      if (!this.ready || string.IsNullOrEmpty(BeamClient.CurrentSession?.Id))
      {
        return;
      }

      Transform playerTransform = this.mainCamera.transform;
      Vector3 playerPosition = playerTransform.position;
      Quaternion playerRotation = playerTransform.rotation;

      if (!(Vector3.Distance(this.lastPlayerPosition, playerPosition) > this.playerPositionThreshold))
      {
        return;
      }

      this.LogPlayerUpdate(playerPosition, playerRotation);
      this.lastPlayerPosition = playerPosition;
    }

    private void TrackGaze()
    {
      if (!this.ready || string.IsNullOrEmpty(BeamClient.CurrentSession?.Id))
      {
        return;
      }

      Transform cam = this.mainCamera.transform;
      Ray forwardRay = new Ray(cam.position, cam.forward);

      // TODO: Add this back in in future
      // if (this.UseTobiiEyeTracking && TobiiAPI.IsConnected)
      // {
      //   forwardRay = Camera.main.ScreenPointToRay(TobiiAPI.GetGazePoint().Screen);
      // }

      if (Physics.Raycast(forwardRay, out RaycastHit hit, 10))
      {
        Collider target = hit.collider;
        bool targetContainsBeamUnit = target.GetComponent<BeamUnitInstance>() != null ||
                                      (this.EnableGazeEventsFor3DUnits &&
                                       target.GetComponentInParent<BeamThreeDimensionalUnitInstance>() != null);

        if (targetContainsBeamUnit)
        {
          // Same object
          if (ReferenceEquals(this.lastHit, target.gameObject))
          {
            // Same position
            if (this.lastHitPoint == hit.point)
            {
              return;
            }

            // New position too close
            if (!(Vector3.Distance(this.lastHitPoint, hit.point) > this.gazePositionThreshold))
            {
              return;
            }

            // Moved enough to log
            this.lastHitPoint = hit.point;
            if (this.gazeStartEvent != null && this.gazeEvents != null)
            {
              this.AddGazeEvent(GazeEventActionKind.Update);
            }

            if (this.DebugGazeMarker)
            {
              this.gazeMarkers.Add(Instantiate(this.DebugGazeMarker, hit.point, new Quaternion()));
            }
          }
          // New object
          else
          {
            // Send the gaze events for the previously gazed unit.
            this.SendGazeEvents();
            
            this.lastHit = target.gameObject;
            this.gazedUnitInstance = this.lastHit.GetComponent<BeamUnitInstance>();
            if (this.gazedUnitInstance == null && this.EnableGazeEventsFor3DUnits)
            {
              // This is annoying, but as we don't know how deep the gltf scene hierarchy is
              // this is the fastest way to get the unit without manually flattening the it,
              // which could be problematic for animations or transforms.
              this.gazedUnitInstance = this.lastHit.GetComponentInParent<BeamThreeDimensionalUnitInstance>();
            }

            if (this.gazedUnitInstance == null || string.IsNullOrWhiteSpace(this.gazedUnitInstance.FulfillmentId))
            {
              // No fulfillment. Clear out the object and ignore it.
              
              return;
            }

            this.AddGazeEvent(GazeEventActionKind.Start);
          }
        }
        else
        {
          // A non beam object was hit
          this.SendGazeEvents();
        }
      }
      else
      {
        // Nothing was hit
        this.SendGazeEvents();
      }
    }

    private void SendGazeEvents()
    {
      if (string.IsNullOrEmpty(BeamClient.CurrentSession?.Id) || this.gazeStartEvent == null || this.lastHit == null || this.gazedUnitInstance == null)
      {
        return;
      }
      
      BeamLogger.LogInfo("Sending Gaze events.");

      this.AddGazeEvent(GazeEventActionKind.End);
      this.lastHit = null;
      this.gazedUnitInstance = null;

      List<ExternalCreateEvent> toSend = new List<ExternalCreateEvent> {this.gazeStartEvent};
      toSend.AddRange(this.gazeEvents);
      toSend.Add(this.gazeEndEvent);

      LogEvents(toSend);

      // Cleanup for the next gaze
      this.gazeStartEvent = null;
      this.gazeEvents = new List<ExternalCreateEvent>();
      this.gazeEndEvent = null;
      this.lastHitPoint = Vector3.zero;
      this.gazeMarkers.ForEach(Destroy);
      this.gazedUnitInstance = null;
    }

    private static async void LogEvent(ExternalCreateEvent ev)
    {
      await BeamClient.Sdk.Analytics.Analytics.CreatePostAsync(new List<ExternalCreateEvent> {ev});
    }

    private static async void LogEvents(IEnumerable<ExternalCreateEvent> evs)
    {
      await BeamClient.Sdk.Analytics.Analytics.CreatePostAsync(new List<ExternalCreateEvent>(evs));
    }

    private void LogPlayerUpdate(Vector3 position, Quaternion rotation)
    {
      if (BeamClient.CurrentSession?.Id == null || !this.ready || this.areaBoundsManager.AreaBoundsList.Count == 0)
      {
        return;
      }

      // Convert layer number in to layer mask using bit shifting.
      LayerMask colliderLayerMask = 1 << this.areaBoundsManager.BeamAreaColliderLayer;
      Collider[] overlappedAreas =
        Physics.OverlapSphere(position, 0.1f, colliderLayerMask, QueryTriggerInteraction.Collide);

      foreach (Collider col in overlappedAreas)
      {
        if (!this.areaBoundsList.ContainsKey(col) || this.areaBoundsList[col] == null)
        {
          continue;
        }

        ExternalCreateEvent ev = new ExternalCreateEvent
        {
          SessionId = BeamClient.CurrentSession?.Id,
          Timestamp = DateTime.UtcNow,
          Metadata = new PlayerUpdateEventMetadata
          {
            Kind = EventKind.PlayerUpdate,
            Action = PlayerUpdateEventActionKind.PlayerUpdate,
            PlayerPosition = position.ToCoordinates(),
            PlayerRotation = rotation.ToCoordinates(),
            SceneId = this.areaBoundsList[col]
          }
        };
        LogEvent(ev);
      }
    }

    public void LogConversionEvent(string instanceId, string fulfillmentId)
    {
      if (BeamClient.CurrentSession?.Id == null || this.ready || this.areaBoundsManager.AreaBoundsList.Count == 0)
      {
        return;
      }

      if (string.IsNullOrEmpty(instanceId))
      {
        BeamLogger.LogInfo("No instanceId assigned. Aborting conversion event creation.");
        return;
      }

      if (string.IsNullOrEmpty(fulfillmentId))
      {
        BeamLogger.LogInfo($"Instance {instanceId} has not been fulfilled. Aborting conversion event creation.");
        return;
      }

      ExternalCreateEvent ev = new ExternalCreateEvent
      {
        SessionId = BeamClient.CurrentSession?.Id,
        Timestamp = DateTime.UtcNow,
        Metadata = new ConvertedEventMetadata
        {
          Kind = EventKind.Converted,
          Action = ConvertedEventActionKind.Converted,
          InstanceId = instanceId,
          FulfillmentId = fulfillmentId
        }
      };
      LogEvent(ev);
    }

    public void LogAudioEvent(string instanceId, string fulfillmentId, AudioEventActionKind actionKind)
    {
      if (BeamClient.CurrentSession?.Id == null || this.ready || this.areaBoundsManager.AreaBoundsList.Count == 0)
      {
        return;
      }

      if (string.IsNullOrEmpty(instanceId))
      {
        BeamLogger.LogInfo("No instanceId assigned. Aborting audio event creation.");
        return;
      }

      if (string.IsNullOrEmpty(fulfillmentId))
      {
        BeamLogger.LogInfo($"Instance {instanceId} has not been fulfilled. Aborting audio event creation.");
        return;
      }
      
      UnitInstanceData key = new UnitInstanceData(instanceId, fulfillmentId); 

      this.currentAudioEventReferences.TryGetValue(key, out Guid currentReference);


      if (actionKind == AudioEventActionKind.Start)
      {
        if (currentReference != Guid.Empty)
        {
          BeamLogger.LogInfo("Video already started, skipping video start event.");
          return;
        }
        
        currentReference = Guid.NewGuid();
        this.currentAudioEventReferences.Add(new UnitInstanceData(instanceId, fulfillmentId), currentReference);
      }

      
      if (currentReference == Guid.Empty)
      {
        BeamLogger.LogInfo($"Audio start event for instance {instanceId} has not been sent, event will not be logged");
        return;
      }

      ExternalCreateEvent ev = new ExternalCreateEvent
      {
        SessionId = BeamClient.CurrentSession?.Id,
        Timestamp = DateTime.UtcNow,
        Metadata = new AudioEventMetadata
        {
          Kind = EventKind.Audio,
          Action = actionKind,
          FulfillmentId = fulfillmentId,
          InstanceId = instanceId,
          Reference = currentReference.ToString()
        }
      };

      if (actionKind == AudioEventActionKind.End)
      {
        this.currentAudioEventReferences.Remove(key);
      }

      LogEvent(ev);
    }

    public void LogVideoEvent(string instanceId, string fulfillmentId, VideoEventActionKind actionKind)
    {
      if (BeamClient.CurrentSession?.Id == null || this.ready || this.areaBoundsManager.AreaBoundsList.Count == 0)
      {
        return;
      }

      if (fulfillmentId == null)
      {
        return;
      }

      UnitInstanceData key = new UnitInstanceData(instanceId, fulfillmentId); 

      this.currentVideoEventReferences.TryGetValue(key, out Guid currentReference);

      if (actionKind == VideoEventActionKind.Start)
      {
        if (currentReference != Guid.Empty)
        {
          BeamLogger.LogInfo("Video already started, skipping video start event.");
          return;
        }
        
        currentReference = Guid.NewGuid();
        this.currentVideoEventReferences.Add(new UnitInstanceData(instanceId, fulfillmentId), currentReference);
      }

      if (currentReference == Guid.Empty)
      {
        BeamLogger.LogInfo($"Video start event for instance {instanceId} has not been sent, event will not be logged");
        return;
      }

      ExternalCreateEvent ev = new ExternalCreateEvent
      {
        SessionId = BeamClient.CurrentSession?.Id,
        Timestamp = DateTime.UtcNow,
        Metadata = new VideoEventMetadata
        {
          Kind = EventKind.Video,
          Action = actionKind,
          FulfillmentId = fulfillmentId,
          InstanceId = instanceId,
          Reference = currentReference.ToString()
        }
      };
      
      if (actionKind == VideoEventActionKind.End)
      {
        this.currentVideoEventReferences.Remove(key);
      }
      
      LogEvent(ev);
    }

    private void AddGazeEvent(GazeEventActionKind gazeEventActionKind)
    {
      if (BeamClient.CurrentSession?.Id == null || !this.ready || this.areaBoundsManager.AreaBoundsList.Count == 0)
      {
        return;
      }

      if (string.IsNullOrEmpty(this.gazedUnitInstance.UnitInstance.Id))
      {
        BeamLogger.LogInfo(
          $"GameObject {this.gazedUnitInstance.gameObject.name} has no assigned instance. Aborting gaze event creation.");
      }

      if (string.IsNullOrEmpty(this.gazedUnitInstance.FulfillmentId))
      {
        BeamLogger.LogInfo(
          $"Instance {this.gazedUnitInstance.UnitInstance.Id} has not been fulfilled. Aborting gaze event creation.");
      }

      Transform mainCamTransform = this.mainCamera.transform;

      if (gazeEventActionKind == GazeEventActionKind.Start)
      {
        this.lastGazeEventReference = Guid.NewGuid();
      }

      if (this.lastGazeEventReference == Guid.Empty)
      {
        BeamLogger.LogInfo(
          $"Gaze start event not found, aborting gaze event creation for instance {this.gazedUnitInstance.UnitInstance.Id}.");
        return;
      }
      
      BeamLogger.LogVerbose( $"Creating {gazeEventActionKind.ToString()} event for instance {this.gazedUnitInstance.UnitInstance.Id}");

      ExternalCreateEvent gazeEvent = new ExternalCreateEvent
      {
        SessionId = BeamClient.CurrentSession?.Id,
        Timestamp = DateTime.UtcNow,
        Metadata = new GazeEventMetadata
        {
          Kind = EventKind.Gaze,
          Action = gazeEventActionKind,
          InstanceId = this.gazedUnitInstance.UnitInstance.Id,
          FulfillmentId = this.gazedUnitInstance.FulfillmentId,
          InstanceHitPosition = this.lastHitPoint.ToCoordinates(),
          InstancePosition = this.lastHit.transform.position.ToCoordinates(),
          InstanceRotation = this.lastHit.transform.rotation.ToCoordinates(),
          PlayerPosition = mainCamTransform.position.ToCoordinates(),
          PlayerRotation = mainCamTransform.rotation.ToCoordinates(),
          Reference = this.lastGazeEventReference.ToString()
        }
      };

      switch (gazeEventActionKind)
      {
        case GazeEventActionKind.Start:
          this.gazeStartEvent = gazeEvent;
          this.gazeEvents = new List<ExternalCreateEvent>();
          break;
        case GazeEventActionKind.Update:
          this.gazeEvents?.Add(gazeEvent);
          break;
        case GazeEventActionKind.End:
          this.gazeEndEvent = gazeEvent;
          this.lastGazeEventReference = Guid.Empty;
          break;
        default:
          break;
      }
    }
  }

  internal readonly struct UnitInstanceData : IEquatable<UnitInstanceData>
  {
    public readonly string InstanceId;
    public readonly string FulfillmentId;

    public UnitInstanceData(string instanceId, string fulfillmentId)
    {
      this.InstanceId = instanceId;
      this.FulfillmentId = fulfillmentId;
    }

    public bool Equals(UnitInstanceData other)
    {
      return this.InstanceId == other.InstanceId && this.FulfillmentId == other.FulfillmentId;
    }

    public override bool Equals(object obj)
    {
      return obj is UnitInstanceData other && this.Equals(other);
    }

    public override int GetHashCode()
    {
      unchecked
      {
        return (this.InstanceId.GetHashCode() * 397) ^ this.FulfillmentId.GetHashCode();
      }
    }
  }
}
