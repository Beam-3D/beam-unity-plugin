using System;
using System.Collections.Generic;
using Beam.Runtime.Client.Managers;
using Beam.Runtime.Client.Metadata;
using Beam.Runtime.Client.Units.Events;
using Beam.Runtime.Client.Units.Model;
using Beam.Runtime.Client.Utilities;
using Beam.Runtime.Sdk.Generated.Model;
using Beam.Runtime.Sdk.Model;
using UnityEngine;
using ProjectUnit = Beam.Runtime.Sdk.Model.ProjectUnit;

namespace Beam.Runtime.Client.Units
{
  [ExecuteAlways]
  public abstract class BeamUnitInstance : MonoBehaviour
  {
    [HideInInspector]
    public ProjectUnit ProjectUnit;
    [HideInInspector]
    public ProjectUnitInstance UnitInstance;
    [HideInInspector]
    [SerializeField]
    public LodStatusChangedEvent OnLodStatusChanged;

    public event EventHandler<string> InstanceDeleted;
    public AssetKind Kind { get; protected set; }
    public string ContentUrlHighQuality { get; protected set; }
    public string ContentUrlLowQuality { get; protected set; }
    public LodStatus LodStatus { get; private set; }
    public string FulfillmentId { get; private set; }

    protected BeamAnalyticsManager analyticsManager;

    private bool needsFulfillment;
    private bool previouslyWithinLodRange;
    private GameObject manager;
    private BeamFulfillmentManager fulfillmentManager;
    private BeamSessionManager sessionManager;
    private BeamCustomMetadataHandler customMetadataHandler;

    public virtual void HandleFulfillment(UnitFulfillmentResponse fulfillment)
    {
      this.ProjectUnit.FulfilledSinceAwake = true;
      this.FulfillmentId = fulfillment.FulfillmentId;

      if (this.customMetadataHandler != null)
      {
        this.customMetadataHandler.Handle(fulfillment);
      }
      this.needsFulfillment = false;
    }

    public virtual void Awake()
    {
      this.ContentUrlHighQuality = null;
      this.ContentUrlLowQuality = null;
    }

    public void OnEnable()
    {
      this.SetInitialQuality();
      BeamManagerHandler.CheckForManagers(true);

      this.manager = GameObject.Find(BeamClient.RuntimeData.ManagerName);

      this.analyticsManager = this.manager.GetComponent<BeamAnalyticsManager>();
      this.fulfillmentManager = this.manager.GetComponent<BeamFulfillmentManager>();
      this.sessionManager = this.manager.GetComponent<BeamSessionManager>();
      this.customMetadataHandler = this.GetComponent<BeamCustomMetadataHandler>();
      this.needsFulfillment = true;

      // This is set to prevent an item being fulfilled by polling if it hasn't been
      // manually or instant fulfilled yet.
      this.ProjectUnit.FulfilledSinceAwake = false;
      this.ProjectUnit.LastFulfillmentChecksum = "";
    }

    public void CallFulfill()
    {
      if (this.ProjectUnit == null || string.IsNullOrWhiteSpace(this.ProjectUnit.Unit.Id))
      {
        return;
      }
      this.fulfillmentManager.RunManualFulfillment(new List<BeamUnitInstance> { this });
    }

    public virtual void OnDestroy()
    {
#if UNITY_EDITOR
      if (!Application.isPlaying) this.InstanceDeleted?.Invoke(this, this.UnitInstance.Id);
#endif
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
      if (this.ProjectUnit == null || this.ProjectUnit.FulfillmentBehaviour != FulfillmentBehaviour.Range)
      {
        return;
      }

      Gizmos.color = Color.yellow;
      Gizmos.DrawWireSphere(this.transform.position, this.ProjectUnit.FulfillmentRange);
    }
#endif

    public virtual void Update()
    {
      if (!Application.isPlaying)
      {
        return;
      }

      if (this.ProjectUnit == null) return;

      bool readyToFulfill = this.needsFulfillment && this.sessionManager.SessionRunning && this.ProjectUnit != null;

      if (this.ProjectUnit.FulfillmentBehaviour == FulfillmentBehaviour.Range && readyToFulfill)
      {
        if (!CheckIfWithinDistance(this.ProjectUnit.FulfillmentRange, this.transform.position)) return;
        this.needsFulfillment = false;
        this.CallFulfill();
      }
      else if (!this.needsFulfillment)
      {
        this.CheckForLodChange();
      }
    }

    public void LogConversionEvent()
    {
      if (this.analyticsManager == null || string.IsNullOrEmpty(this.FulfillmentId))
      {
        return;
      }
      this.analyticsManager.LogConversionEvent(this.UnitInstance.Id, this.FulfillmentId);
    }

    private void SetInitialQuality()
    {
      if (this.CheckIfWithinLodDistance() || this.ProjectUnit.LodDistance == 0.0f)
      {
        this.LodStatus = LodStatus.InsideHighQualityRange;
        this.previouslyWithinLodRange = true;
      }
      else
      {
        this.LodStatus = LodStatus.OutsideHighQualityRange;
        this.previouslyWithinLodRange = false;
      }
    }
    private void CheckForLodChange()
    {
      if (this.ProjectUnit.MaxQualityId == this.ProjectUnit.MinQualityId)
      {
        return;
      }

      bool inLodHighQualityRange = this.CheckIfWithinLodDistance();
      if (!inLodHighQualityRange && this.previouslyWithinLodRange)
      {
        this.previouslyWithinLodRange = false;
        this.LodStatus = LodStatus.OutsideHighQualityRange;
        this.OnLodStatusChanged?.Invoke(this.LodStatus);
      }
      else if (inLodHighQualityRange && !this.previouslyWithinLodRange)
      {
        this.previouslyWithinLodRange = true;
        this.LodStatus = LodStatus.InsideHighQualityRange;
        this.OnLodStatusChanged?.Invoke(this.LodStatus);
      }

    }

    private bool CheckIfWithinLodDistance()
    {
      return CheckIfWithinDistance(this.ProjectUnit.LodDistance, this.transform.position) || this.ProjectUnit.LodDistance == 0.0f;
    }

    private static bool CheckIfWithinDistance(float maxDistance, Vector3 position)
    {
      if (Camera.main == null)
      {
        throw new Exception("Main Camera tag must be assigned.");
      }
      float distance = Vector3.Distance(position, Camera.main.transform.position);
      return distance <= maxDistance;
    }
  }
}
