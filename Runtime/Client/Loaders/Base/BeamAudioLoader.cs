using Beam.Runtime.Client.Loaders.Events;
using Beam.Runtime.Client.Units;
using Beam.Runtime.Client.Units.Events;
using UnityEngine;

namespace Beam.Runtime.Client.Loaders.Base
{
  public abstract class BeamAudioLoader : BeamLoader
  {
    [SerializeField]
    public AudioLoadedEvent OnAudioLoaded;
    private BeamAudioUnitInstance beamAudioUnitInstance;
    protected BeamAudioUnitInstance BeamAudioUnitInstance
    {
      get
      {
        return this.beamAudioUnitInstance ? this.beamAudioUnitInstance : (this.beamAudioUnitInstance = this.GetComponent<BeamAudioUnitInstance>());
      }
    }

    // These calls have to be duplicated because of an issue with Unity and inheritance
    public void Awake()
    {
      this.BeamAudioUnitInstance.OnLodStatusChanged.AddListener(this.HandleLodChange);
      this.BeamAudioUnitInstance.OnFulfillmentUpdated.AddListener(this.HandleFulfillment);
    }

    public abstract void HandleFulfillment(AudioUnitFulfillmentData unitFulfillmentData);
  }
}
