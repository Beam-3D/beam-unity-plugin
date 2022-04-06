using Beam.Runtime.Client.Units;
using Beam.Runtime.Client.Units.Model;
using UnityEngine;

namespace Beam.Runtime.Client.Loaders.Base
{
  [RequireComponent(typeof(BeamUnitInstance))]
  public abstract class BeamLoader : MonoBehaviour
  {
    private BeamUnitInstance beamUnitInstance;
    protected BeamUnitInstance BeamUnitInstance
    {
      get
      {
        return this.beamUnitInstance ? this.beamUnitInstance : (this.beamUnitInstance = this.GetComponent<BeamUnitInstance>());
      }
    }

    protected LodStatus CurrentLodStatus { get { return this.BeamUnitInstance.LodStatus; } }

    private void OnEnable()
    {
      this.BeamUnitInstance.OnLodStatusChanged.AddListener(this.HandleLodChange);
    }

    protected abstract void HandleLodChange(LodStatus lodStatus);
  }
}
