using Beam.Runtime.Sdk.Extensions;
using UnityEngine;

namespace Beam.Runtime.Client.Utilities
{
  public enum ColliderType
  {
    Box,
    Sphere,
    Capsule
  }
  public class BeamColliderGenerator : MonoBehaviour
  {
    [Tooltip("Should we generate colliders on startup. Useful for non Beam models")]
    public bool GenerateOnStart;
    [Tooltip("Should a rigidbody also be added to the object")]
    public bool AddRigidBody;
    [Tooltip("The type of collider to generate")]
    public ColliderType ColliderType;
    private protected void Start()
    {
      if (this.GenerateOnStart)
      {
        this.GenerateColliders();
      }
    }

    public void GenerateColliders()
    {
      if (this.isActiveAndEnabled)
      {
        Bounds bounds = this.GetComponentsInChildren<Renderer>().EncapsulatedBounds();
        this.BuildColliders(bounds);
      }
    }

    public void GenerateColliders(Bounds bounds)
    {
      if (this.isActiveAndEnabled)
      {
        this.BuildColliders(bounds);
      }
    }

    private void BuildColliders(Bounds bounds)
    {
      Collider existingCollider = this.gameObject.GetComponent<Collider>();
      if (existingCollider)
      {
        Destroy(existingCollider);
      }

      if (this.ColliderType == ColliderType.Box)
      {
        BoxCollider collider = this.gameObject.AddComponent<BoxCollider>();

        collider.size = bounds.size; // * targetScale;
        collider.center = this.transform.GetChild(0).localPosition;
      }
      if (this.ColliderType == ColliderType.Sphere)
      {
        SphereCollider collider = this.gameObject.AddComponent<SphereCollider>();

        collider.radius = bounds.size.GetMaxPoint() / 2;
        collider.center = this.transform.GetChild(0).localPosition;
      }
      if (this.ColliderType == ColliderType.Capsule)
      {
        CapsuleCollider collider = this.gameObject.AddComponent<CapsuleCollider>();

        collider.radius = bounds.max.GetMaxPoint() / 4;
        collider.height = bounds.extents.y;
        collider.center = this.transform.GetChild(0).localPosition;
      }

      if (this.AddRigidBody)
      {
        this.gameObject.AddComponent<Rigidbody>();
      }
    }
  }
}

