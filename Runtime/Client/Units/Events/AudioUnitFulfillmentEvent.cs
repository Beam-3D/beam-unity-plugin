﻿using System;
using Beam.Runtime.Client.Units.Model;

namespace Beam.Runtime.Client.Units.Events
{
  [Serializable]
  public class AudioUnitFulfillmentEvent : UnitFulfillmentEvent<AudioUnitFulfillmentData>
  { }
  public class AudioUnitFulfillmentData : UnitFulfillmentData
  {
    public AudioUnitFulfillmentData(UnitFulfillmentStatusCode statusCode, string unitId, string audioUrl = null)
    {
      this.StatusCode = statusCode;
      this.UnitId = unitId;
      this.AudioUrl = audioUrl;
    }

    public readonly string AudioUrl;
  }
}
