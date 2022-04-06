using System;
using UnityEngine.Events;

namespace Beam.Runtime.Client.Metadata
{
  [Serializable]
  public class ReceivedMetadataEvent : UnityEvent<string> { }
}
