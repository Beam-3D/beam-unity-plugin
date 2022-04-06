using System;

namespace Beam.Runtime.Client.Metadata
{
  [Serializable]
  public class CustomMetadataHandler
  {
    public string Name;
    public string Id;
    public ReceivedMetadataEvent OnMetadataReceived;

    public CustomMetadataHandler(string id, string name, ReceivedMetadataEvent onMetadataReceived)
    {
      this.Id = id;
      this.Name = name;
      this.OnMetadataReceived = onMetadataReceived;
    }
  }
}
