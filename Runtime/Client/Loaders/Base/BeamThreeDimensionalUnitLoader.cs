using Beam.Runtime.Client.Loaders.Events;
using Beam.Runtime.Client.Units;
using Beam.Runtime.Client.Units.Events;

namespace Beam.Runtime.Client.Loaders.Base
{
  public abstract class BeamThreeDimensionalUnitLoader : BeamLoader
  {
    public ThreeDimensionalLoadedEvent OnThreeDimensionalUnitLoaded;

    private BeamThreeDimensionalUnitInstance beamVideoUnitInstance;
    protected BeamThreeDimensionalUnitInstance BeamVideoUnitInstance
    {
      get
      {
        return this.beamVideoUnitInstance ? this.beamVideoUnitInstance : (this.beamVideoUnitInstance = this.GetComponent<BeamThreeDimensionalUnitInstance>());
      }
    }

    public void Awake()
    {
      this.BeamVideoUnitInstance.OnFulfillmentUpdated.AddListener(this.HandleFulfillment);
    }
    public abstract void HandleFulfillment(ThreeDimensionalUnitFulfillmentData unitFulfillmentData);
  }
}
