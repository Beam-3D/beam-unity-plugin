using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Beam.Runtime.Sdk.Data;
using Beam.Runtime.Client.Units;
using Beam.Runtime.Sdk.Generated.Model;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Beam.Runtime.Client.Managers
{
  [ExecuteAlways]
  public class BeamAreaBoundsManager : MonoBehaviour, ISerializationCallbackReceiver
  {
    // Dict<AreaId SceneBounds>
    public Dictionary<string, BoxCollider> AreaBoundsList = new Dictionary<string, BoxCollider>();

    // We have to use these because Unity won't serialize dictionaries by default
    [SerializeField] private List<string> keys;
    [SerializeField] private List<BoxCollider> values;

    public int BeamAreaColliderLayer;

#if UNITY_EDITOR
    private IEnumerable<string> selectedAreaIds;
    private BeamData beamData;
    private readonly Color32 beamBlue = new Color32(0, 164, 250, 255);

    private bool subscribedToSelectionChange = false;

    private void OnEnable()
    {
      this.beamData = SerializedDataManager.Data;
      this.subscribedToSelectionChange = false;
    }

    void Update()
    {
      if (!this.subscribedToSelectionChange)
      {
        Selection.selectionChanged += this.HandleSelectionChanged;
        this.subscribedToSelectionChange = true;
      }

      if (!Application.isPlaying)
      {
        this.UpdateBounds();
      }
    }

    void HandleSelectionChanged()
    {
      List<string> removedAreaBounds = this.AreaBoundsList.Where(kvp => kvp.Value == null).Select(kvp => kvp.Key).ToList();

      foreach (string removedId in removedAreaBounds)
      {
        this.AreaBoundsList.Remove(removedId);
      }

      BeamUnitInstance[] selectedUnits =
        Selection.GetFiltered<BeamUnitInstance>(SelectionMode.ExcludePrefab | SelectionMode.Deep);
      this.selectedAreaIds = selectedUnits
        .GroupBy(unit => unit.ProjectUnit.Unit.SceneId)
        .Select(grp => grp.First().ProjectUnit.Unit.SceneId);
    }

    void OnDrawGizmosSelected()
    {
      foreach (KeyValuePair<string, BoxCollider> areaBounds in this.AreaBoundsList)
      {
        if (areaBounds.Value != null)
        {
          Gizmos.color = this.beamBlue;
          Gizmos.matrix = areaBounds.Value.transform.localToWorldMatrix;
          Gizmos.DrawWireCube(areaBounds.Value.center, areaBounds.Value.size);
        }
      }
    }

    void OnDrawGizmos()
    {
      if (this.selectedAreaIds != null)
      {
        foreach (string areaId in this.selectedAreaIds)
        {
          if (string.IsNullOrEmpty(areaId))
          {
            continue;
          }

          if (!this.AreaBoundsList.ContainsKey(areaId) || this.AreaBoundsList[areaId] == null) continue;
          BoxCollider bc = this.AreaBoundsList[areaId];
          Gizmos.color = this.beamBlue;
          Gizmos.matrix = bc.transform.localToWorldMatrix;
          Gizmos.DrawWireCube(bc.center, bc.size);
        }
      }
    }

    private BoxCollider CreateAreaBounds(string areaId, Vector3 position)
    {
      IScene area = this.beamData.Scenes.FirstOrDefault(scene => scene.Id == areaId);
      string areaName = area?.Name ?? "Deleted Area";
      GameObject boundsHolder = new GameObject(areaName + " Area Boundary");
      boundsHolder.transform.SetPositionAndRotation(position, Quaternion.identity);
      BoxCollider newAreaBounds = boundsHolder.AddComponent<BoxCollider>();
      newAreaBounds.isTrigger = true;
      newAreaBounds.gameObject.layer = this.BeamAreaColliderLayer;
      return newAreaBounds;
    }

    void UpdateBounds()
    {
      BeamUnitInstance[] placedInstances = FindObjectsOfType<BeamUnitInstance>();
      foreach (BeamUnitInstance unit in placedInstances)
      {
        this.EncapsulateUnit(unit);
      }

      foreach (KeyValuePair<string, BoxCollider> areaBounds in this.AreaBoundsList)
      {
        if (areaBounds.Value != null)
        {
          if (areaBounds.Value.transform.parent == null)
          {
            areaBounds.Value.transform.localScale = Vector3.one;
          }

          // Locking the global scale of the unit to as close to 1 as possible so that the boundary transforms work.
          Transform areaTransform = areaBounds.Value.transform;
          Vector3 currentScale = areaTransform.lossyScale;
          Vector3 newLocalScale = new Vector3(1.0f / currentScale.x, 1.0f / currentScale.y, 1.0f / currentScale.z);
          areaTransform.localScale = newLocalScale;

          // These are needed for analytics.
          areaBounds.Value.isTrigger = true;
          areaBounds.Value.gameObject.layer = this.BeamAreaColliderLayer;
        }
      }
    }

    public void DeleteAllBounds()
    {
      List<string> keys = this.AreaBoundsList.Keys.ToList();
      keys.ForEach(this.HandleBoundsDeleted);
    }

    public void HandleUnitAdded(BeamUnitInstance unit)
    {
      if (this.beamData == null)
      {
        this.beamData = SerializedDataManager.Data;
      }

      string areaId = unit.ProjectUnit.Unit.SceneId;

      if (!this.AreaBoundsList.ContainsKey(areaId))
      {
        this.AreaBoundsList.Add(areaId, null);
      }

      if (this.AreaBoundsList[areaId] == null)
      {
        this.AreaBoundsList[areaId] = this.CreateAreaBounds(areaId, unit.transform.position);
      }

      this.EncapsulateUnit(unit);
    }

    public void HandleBoundsDeleted(string areaId)
    {
      if (this.AreaBoundsList[areaId] != null)
      {
        DestroyImmediate(this.AreaBoundsList[areaId].gameObject);
      }

      this.AreaBoundsList.Remove(areaId);
    }

    public void ResetBounds(string areaId)
    {
      BoxCollider bc = this.AreaBoundsList[areaId];

      if (bc == null) return;

      IEnumerable<BeamUnitInstance> units = FindObjectsOfType<BeamUnitInstance>()
        .Where(instance => instance.ProjectUnit.Unit.SceneId == areaId).ToList();

      Vector3 center = Vector3.zero;

      if (units.Any())
      {
        center = this.AverageCenter(units.Select(unit => unit.transform.position));
      }

      bc.center = bc.transform.InverseTransformPoint(center);
      bc.size = Vector3.one;
      foreach (BeamUnitInstance unit in units)
      {
        this.EncapsulateUnit(unit);
      }
    }

    public void Reset()
    {
      List<string> areaIds = this.AreaBoundsList.Keys.ToList();
      areaIds.ForEach(this.HandleBoundsDeleted);
    }

    private void EncapsulateUnit(BeamUnitInstance unit)
    {
      string areaId = unit.ProjectUnit.Unit.SceneId;
      IScene area = this.beamData.Scenes?.FirstOrDefault(s => s.Id == areaId);

      if (string.IsNullOrEmpty(areaId) || area == null)
      {
        return;
      }

      if (!this.AreaBoundsList.ContainsKey(areaId))
      {
        this.AreaBoundsList.Add(areaId, null);
      }

      if (this.AreaBoundsList[areaId] == null)
      {
        this.AreaBoundsList[areaId] = this.CreateAreaBounds(areaId, unit.transform.position);
      }

      BoxCollider boxCollider = this.AreaBoundsList[areaId];

      // We have to do this to account for rotation on the gameObject, otherwise
      // the bounds are locked to the main axes and everything breaks;
      Bounds b = new Bounds(boxCollider.center, boxCollider.size);

      Vector3 pointInBoxSpace = boxCollider.transform.InverseTransformPoint(unit.transform.position);
      b.Encapsulate(pointInBoxSpace);

      boxCollider.size = b.size;
      boxCollider.center = b.center;
      this.AreaBoundsList[areaId] = boxCollider;
    }

    private Vector3 AverageCenter(IEnumerable<Vector3> vectors)
    {
      IEnumerable<Vector3> enumeratedVectors = vectors as Vector3[] ?? vectors.ToArray();
      int count = enumeratedVectors.Count();

      if (count == 1)
      {
        return enumeratedVectors.First();
      }

      Vector3 total = Vector3.zero;

      foreach (Vector3 v in enumeratedVectors)
      {
        total += v;
      }

      return total /= count;
    }

    private void OnDestroy()
    {
      if (!this.subscribedToSelectionChange)
      {
        return;
      }

      Selection.selectionChanged -= this.HandleSelectionChanged;
      this.subscribedToSelectionChange = false;
    }
#endif

    public void OnBeforeSerialize()
    {
      if (this.keys == null)
      {
        this.keys = new List<string>();
      }

      if (this.values == null)
      {
        this.values = new List<BoxCollider>();
      }

      this.keys.Clear();
      this.values.Clear();

      foreach (var kvp in this.AreaBoundsList)
      {
        this.keys.Add(kvp.Key);
        this.values.Add(kvp.Value);
      }
    }

    public void OnAfterDeserialize()
    {
      this.AreaBoundsList = new Dictionary<string, BoxCollider>();

      for (int i = 0; i != Math.Min(this.keys.Count, this.values.Count); i++) this.AreaBoundsList.Add(this.keys[i], this.values[i]);
    }
  }
}
