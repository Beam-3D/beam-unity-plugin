using System;

namespace Beam.Runtime.Client
{
  public static class BeamDataSynchronizer
  {
#if UNITY_EDITOR
    public static event Action RuntimeDataRefreshed;
#endif

#if UNITY_EDITOR
    public static void OnRuntimeDataRefreshed()
    {
      Action handler = RuntimeDataRefreshed;
      handler?.Invoke();
    }
#endif
  }
}
