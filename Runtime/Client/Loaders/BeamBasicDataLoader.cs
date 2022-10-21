using System;
using System.Collections;
using Beam.Runtime.Client.Loaders.Base;
using Beam.Runtime.Client.Loaders.Events;
using Beam.Runtime.Client.Units;
using Beam.Runtime.Client.Units.Events;
using Beam.Runtime.Client.Units.Model;
using Beam.Runtime.Sdk.Utilities;
using UnityEngine;
using UnityEngine.Networking;

namespace Beam.Runtime.Client.Loaders
{
  [RequireComponent(typeof(BeamDataUnitInstance))]
  public class BeamBasicDataLoader : BeamDataLoader
  {
    public new void Awake()
    {
      base.Awake();
    }

    public override void HandleFulfillment(DataUnitFulfillmentData fulfillmentData)
    {
      if (fulfillmentData == null)
      {
        return;
      }
      UnitFulfillmentStatusCode status = fulfillmentData.StatusCode;
      if (status == UnitFulfillmentStatusCode.Started || status == UnitFulfillmentStatusCode.CompletedWithSameContent)
      {
        return;
      }
      if (status == UnitFulfillmentStatusCode.CompletedEmpty)
      {
        return;
      }
      this.StartCoroutine(this.LoadData(new Uri(fulfillmentData.Url)));
    }

    private IEnumerator LoadData(Uri uri)
    {

      using (UnityWebRequest www = UnityWebRequest.Get(uri))
      {
        yield return www.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
      if(www.result == UnityWebRequest.Result.ConnectionError)
#else
        if (www.isNetworkError)
#endif
        {
          BeamLogger.LogError(www.error);
        }
        else
        {
          string data = www.downloadHandler.text;
          this.OnDataLoaded?.Invoke(new DataLoadedData(data, LodStatus.InsideHighQualityRange));
        }
      }
    }

    protected override void HandleLodChange(LodStatus lodStatus)
    { }
  }
}
