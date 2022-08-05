using System.Collections.Generic;
using System.Linq;
using Beam.Runtime.Client.Loaders.Events;
using Beam.Runtime.Sdk.Extensions;
using Beam.Runtime.Sdk.Utilities.Events;
using UnityEngine;
namespace Beam.Runtime.Client.Utilities
{
  public class BeamModelResizer : MonoBehaviour
  {
    [SerializeField]
    [Tooltip("Called when the model has been resized")]
    public ResizeCompletedEvent OnResizeCompleted;
    [Tooltip("What size in Unity units should the model be")]
    public float TargetScale = 1;
    [Tooltip("Should the bottom of the model be moved to sit on the bottom of the box")]
    public bool SnapToBase;
    [Tooltip("Should the pivot point of the object also be moved to the bottom of the box")]
    public bool PivotAtBase;
    [Tooltip("Should we execute the resize code on startup. Useful for non Beam objects")]
    public bool ResizeOnStart;
    [HideInInspector]
    public Bounds Bounds;

    private float targetLossyScale = 1;
    private protected void Start()
    {

      this.targetLossyScale = this.TargetScale * this.transform.lossyScale.x;
      if (this.ResizeOnStart)
      {
        this.Resize();
      }
    }

    private protected void OnDrawGizmos()
    {
      Gizmos.color = Color.blue;
      Transform t = this.transform;
      Vector3 currentPos = t.position;
      this.targetLossyScale = this.TargetScale * t.lossyScale.x;
      Vector3 cubeCenter = this.SnapToBase ? new Vector3(currentPos.x, this.PivotAtBase ? currentPos.y + this.targetLossyScale / 2 : currentPos.y, currentPos.z) : currentPos;
      Vector3 scale = new Vector3(this.targetLossyScale, this.targetLossyScale, this.targetLossyScale);

      var oldMatrix = Gizmos.matrix;

      Gizmos.matrix = Matrix4x4.TRS(cubeCenter, this.transform.rotation, scale);
      Gizmos.DrawWireCube(Vector3.zero, new Vector3(1, 1, 1));
      Gizmos.matrix = oldMatrix;

      if (this.Bounds.size != Vector3.zero)
      {
        this.DrawBounds(this.Bounds);
      }
    }

    private void DrawBounds(Bounds bounds)
    {
      Gizmos.color = Color.red;
      Transform t = this.transform;
      if (t == null || t.GetChild(0) == null)
      {
        return;
      }
      Gizmos.matrix = Matrix4x4.TRS(t.GetChild(0).position, t.rotation, bounds.size);
      Gizmos.DrawWireCube(Vector3.zero, new Vector3(1, 1, 1));
    }

    public void Resize(ThreeDimensionalLoadedData data)
    {
      this.ResizeInstance(data.ParentTransform);
    }

    public void Resize(Transform targetTransform = null)
    {
      this.ResizeInstance(targetTransform ? targetTransform : this.transform);
    }

    private Bounds GetBoundingBox()
    {
      List<SkinnedMeshRenderer> skinnedMeshRenderers = this.GetComponentsInChildren<SkinnedMeshRenderer>(true).ToList();
      List<MeshFilter> meshFilters = this.GetComponentsInChildren<MeshFilter>(true).ToList();
      Bounds smrBoundingBox = new Bounds();
      Bounds mfBoundingBox = new Bounds();

      if (skinnedMeshRenderers.Count != 0)
      {
        smrBoundingBox = skinnedMeshRenderers.Select(f => f.bounds).Encapsulation();
      }
      if (meshFilters.Count != 0)
      {
        (Vector3 max, Vector3 min) = this.GetMaxAndMinBounds(meshFilters);
        mfBoundingBox.SetMinMax(min, max);
      }

      if (meshFilters.Count == 0)
      {
        return smrBoundingBox;
      }
      if (skinnedMeshRenderers.Count == 0)
      {
        return mfBoundingBox;
      }

      smrBoundingBox.Encapsulate(mfBoundingBox);
      return smrBoundingBox;
    }

    private void ResizeInstance(Transform instance)
    {
      Transform t = this.transform;
      var oldRotation = t.rotation;
      t.rotation = Quaternion.identity;
      instance.localPosition = Vector3.zero;

      Bounds boundingBox = this.GetBoundingBox();

      // change pivot point to actual bounds center
      GameObject pivot = new GameObject("Pivot");
      pivot.transform.position = boundingBox.center;
      pivot.transform.parent = this.transform;
      instance.parent = pivot.transform;
      pivot.transform.localPosition = Vector3.zero;

      float sv = this.CalculateScale(boundingBox.max, boundingBox.min);
      Vector3 scaleVector = new Vector3(sv, sv, sv);
      pivot.transform.localScale = scaleVector;

      if (this.SnapToBase)
      {
        Bounds finalBounds = this.GetBoundingBox();
        float extentsY = finalBounds.size.y;
        pivot.transform.position -= new Vector3(0, (this.targetLossyScale / 2) - (extentsY / 2), 0);
        if (this.PivotAtBase)
        {
          pivot.transform.position += new Vector3(0, this.targetLossyScale / 2, 0);
        }
      }

      Bounds completeBounds = this.GetBoundingBox();
      this.Bounds = completeBounds;
      this.OnResizeCompleted?.Invoke(this.Bounds);
      this.transform.rotation = oldRotation;
    }

    private float CalculateScale(Vector3 maxPoint, Vector3 minPoint)
    {
      Vector3 dims = maxPoint - minPoint;
      float[] dimsArr = { dims.x, dims.y, dims.z };
      float maxDim = dimsArr.Aggregate(0.0f, (acc, x) => Mathf.Abs(x) > acc ? x : acc);
      return this.TargetScale / maxDim;
    }

    private (Vector3, Vector3) GetMaxAndMinBounds(List<MeshFilter> filters)
    {
      Vector3 minPoint = Vector3.positiveInfinity;
      Vector3 maxPoint = Vector3.negativeInfinity;

      // Iterate over all verts to get max/min Bounds
      filters.ForEach(filter =>
      {
        filter.mesh.vertices.ToList().ForEach(vert =>
        {
          // Get point in terms of main objects local space
          // Doesn't seem to be taking rotation in to account
          Vector3 transformedVert = filter.transform.TransformPoint(vert);
          float x = transformedVert.x;
          float y = transformedVert.y;
          float z = transformedVert.z;

          if (x < minPoint.x)
          {
            minPoint.x = x;
          }

          if (y < minPoint.y)
          {
            minPoint.y = y;
          }

          if (z < minPoint.z)
          {
            minPoint.z = z;
          }

          if (x > maxPoint.x)
          {
            maxPoint.x = x;
          }

          if (y > maxPoint.y)
          {
            maxPoint.y = y;
          }

          if (z > maxPoint.z)
          {
            maxPoint.z = z;
          }
        });
      });

      return (maxPoint, minPoint);
    }
  }
}
