using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beam.Runtime.Client.Utilities
{
  public class HmdHandler
  {
    public static string GetHMD()
    {
      string hmdName = null;
#if !UNITY_WEBGL

      List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();
      UnityEngine.XR.InputDevices.GetDevices(devices);
      hmdName = devices.Any() ? devices[0].name : null;
#endif
      return hmdName;
    }

  }
}
