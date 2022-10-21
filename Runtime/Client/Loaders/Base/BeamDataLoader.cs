using Beam.Runtime.Client.Loaders.Events;
using Beam.Runtime.Client.Units;
using Beam.Runtime.Client.Units.Events;
using UnityEngine;

namespace Beam.Runtime.Client.Loaders.Base
{
  [RequireComponent(typeof(BeamDataUnitInstance))]
  public abstract class BeamDataLoader : BeamLoader
  {
    [SerializeField]
    public DataLoadedEvent OnDataLoaded;
    private BeamDataUnitInstance beamDataUnitInstance;
    protected BeamDataUnitInstance BeamDataUnitInstance
    {
      get
      {
        return this.beamDataUnitInstance ? this.beamDataUnitInstance : (this.beamDataUnitInstance = this.GetComponent<BeamDataUnitInstance>());
      }
    }

    // These calls have to be duplicated because of an issue with Unity and inheritance
    public void Awake()
    {
      this.BeamDataUnitInstance.OnFulfillmentUpdated.AddListener(this.HandleFulfillment);
    }

    public abstract void HandleFulfillment(DataUnitFulfillmentData unitFulfillmentData);
  }
}
