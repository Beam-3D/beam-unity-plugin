using Beam.Runtime.Sdk.Data;
using Beam.Runtime.Sdk.Generated.Model;
using UnityEngine;

namespace Beam.Runtime.Client.Managers
{
  public class BeamSessionManager : MonoBehaviour
  {
    public Session SessionResponse;
    public bool SessionRunning;

    public void Awake()
    {
      if (BeamClient.RuntimeData.AutoStartSession)
      {
        BeamClient.StartSession();
      }
    }

    private protected void Update()
    {
      this.SessionRunning = BeamClient.CurrentSession != null;
    }
  }
}
