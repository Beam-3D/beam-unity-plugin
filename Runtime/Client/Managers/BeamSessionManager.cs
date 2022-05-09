using Beam.Runtime.Sdk.Generated.Model;
using UnityEngine;

namespace Beam.Runtime.Client.Managers
{
  public class BeamSessionManager : MonoBehaviour
  {
    public ISession SessionResponse;

    public bool SessionRunning
    {
      get { return !string.IsNullOrEmpty(BeamClient.CurrentSession?.Id);  }
    }

    public void Awake()
    {
      if (!BeamClient.RuntimeData.AutoStartSession) return;
      
      BeamClient.StartSession();
      
    }
  }
}
