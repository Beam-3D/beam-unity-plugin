using Beam.Runtime.Client.Loaders.Events;
using Beam.Runtime.Client.Units;
using Beam.Runtime.Client.Units.Events;

namespace Beam.Runtime.Client.Loaders.Base
{
  public abstract class BeamThreeDimensionalUnitLoader : BeamLoader
  {
    public ThreeDimensionalLoadedEvent OnThreeDimensionalUnitLoaded;

    private BeamThreeDimensionalUnitInstance beamThreeDimensionalUnitInstance;
    protected BeamThreeDimensionalUnitInstance BeamThreeDimensionalUnitInstance
    {
      get
      {
        return this.beamThreeDimensionalUnitInstance ? this.beamThreeDimensionalUnitInstance : (this.beamThreeDimensionalUnitInstance = this.GetComponent<BeamThreeDimensionalUnitInstance>());
      }
    }

    // These calls have to be duplicated because of an issue with Unity and inheritance
    public void Awake()
    {
      this.BeamThreeDimensionalUnitInstance.OnLodStatusChanged.AddListener(this.HandleLodChange);
      this.BeamThreeDimensionalUnitInstance.OnFulfillmentUpdated.AddListener(this.HandleFulfillment);
    }
    public abstract void HandleFulfillment(ThreeDimensionalUnitFulfillmentData unitFulfillmentData);
  }
}
