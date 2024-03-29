﻿using Beam.Runtime.Client.Loaders.Events;
using Beam.Runtime.Client.Units;
using Beam.Runtime.Client.Units.Events;
using UnityEngine;

namespace Beam.Runtime.Client.Loaders.Base
{
  public abstract class BeamVideoLoader : BeamLoader
  {
    [SerializeField]
    public VideoLoadedEvent OnVideoLoaded;
    private BeamVideoUnitInstance beamVideoUnitInstance;
    protected BeamVideoUnitInstance BeamVideoUnitInstance
    {
      get
      {
        return this.beamVideoUnitInstance ? this.beamVideoUnitInstance : (this.beamVideoUnitInstance = this.GetComponent<BeamVideoUnitInstance>());
      }
    }

    // These calls have to be duplicated because of an issue with Unity and inheritance
    public void Awake()
    {
      this.BeamVideoUnitInstance.OnLodStatusChanged.AddListener(this.HandleLodChange);
      this.BeamVideoUnitInstance.OnFulfillmentUpdated.AddListener(this.HandleFulfillment);
    }

    public abstract void HandleFulfillment(VideoUnitFulfillmentData unitFulfillmentData);
  }
}
