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

    public void Awake()
    {
      this.BeamAudioUnitInstance.OnFulfillmentUpdated.AddListener(this.HandleFulfillment);
    }

    public abstract void HandleFulfillment(AudioUnitFulfillmentData unitFulfillmentData);
  }
}
