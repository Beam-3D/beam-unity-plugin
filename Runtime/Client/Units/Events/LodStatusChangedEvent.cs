using System;
using Beam.Runtime.Client.Units.Model;
using UnityEngine.Events;

namespace Beam.Runtime.Client.Units.Events
{
  [Serializable]
  public class LodStatusChangedEvent : UnityEvent<LodStatus>
  { }
}
