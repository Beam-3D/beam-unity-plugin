using Beam.Runtime.Client.Units;
using Beam.Runtime.Sdk.Model;
using UnityEngine;

namespace Beam.Runtime.Client.Utilities
{
  [ExecuteInEditMode]
  [RequireComponent(typeof(BeamUnitInstance))]
  public class BeamAspectRatioHandler : MonoBehaviour
  {
    [HideInInspector]
    public BeamUnitInstance BeamUnitInstance;
    [HideInInspector]
    [SerializeField]
    public AspectRatioChangeBehaviour AspectRatioChangeBehaviour;
    private protected void Awake()
    {
      this.BeamUnitInstance = this.GetComponent<BeamUnitInstance>();
    }
    public void HandleAspectRatio(float width, float height)
    {
      Vector3 currentScale = this.transform.localScale;
      float aspectRatio = width / height;
      switch (this.AspectRatioChangeBehaviour)
      {
        case AspectRatioChangeBehaviour.MaintainHeight:
          this.transform.localScale = new Vector3((currentScale.y * aspectRatio), currentScale.y, currentScale.z);
          break;
        case AspectRatioChangeBehaviour.MaintainWidth:
          this.transform.localScale = new Vector3(currentScale.x, (currentScale.x / aspectRatio), currentScale.z);
          break;
        case AspectRatioChangeBehaviour.Stretch:
        default:
          break;
      }
    }
  }
}
