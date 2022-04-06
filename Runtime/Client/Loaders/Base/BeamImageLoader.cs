using Beam.Runtime.Client.Loaders.Events;
using Beam.Runtime.Client.Units;
using Beam.Runtime.Client.Units.Events;
using UnityEngine;

namespace Beam.Runtime.Client.Loaders.Base
{
  public abstract class BeamImageLoader : BeamLoader
  {
    [SerializeField]
    public ImageLoadedEvent OnImageLoaded;

    private BeamImageUnitInstance beamImageUnitInstance;
    protected BeamImageUnitInstance BeamImageUnitInstance
    {
      get
      {
        return this.beamImageUnitInstance ? this.beamImageUnitInstance : (this.beamImageUnitInstance = this.GetComponent<BeamImageUnitInstance>());
      }
    }

    public void Awake()
    {
      this.BeamImageUnitInstance.OnFulfillmentUpdated.AddListener(this.HandleFulfillment);
    }

    public abstract void HandleFulfillment(ImageUnitFulfillmentData unitFulfillmentData);
  }
}
