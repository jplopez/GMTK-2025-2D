using System;
using System.Collections.Generic;
using UnityEngine;

namespace GMTK {


  /// <summary>
  /// Manages a defined zone where <see cref="GridSnappable"/> elements can be registered, unregistered, and tracked.
  /// </summary>
  /// <remarks>This abstract class provides functionality for managing a collection of <see
  /// cref="GridSnappable"/> elements within a specified zone. The zone is defined by a <see cref="Collider2D"/> and
  /// supports operations such as scanning for elements within the zone, registering and unregistering elements, and
  /// checking whether an element is inside the zone.  Events are provided to notify when elements are registered or
  /// unregistered.</remarks>
  public abstract class SnappableZoneManager : MonoBehaviour {

    // List of all registered elements in this zone
    [Tooltip("Collider defining the zone area")]
    [SerializeField] protected Collider2D zoneCollider;

    protected readonly List<GridSnappable> _elements = new();

    // Public read-only access to registered elements
    public IReadOnlyList<GridSnappable> Elements => _elements.AsReadOnly();

#if UNITY_EDITOR
    // Editor view of registered elements
    public IReadOnlyList<GridElementView> EditorView => _editorView;
#endif
    // Serialized list for editor visualization, hidden in inspector
    [SerializeField, HideInInspector]
    private List<GridElementView> _editorView = new();

    public virtual void Start() => ScanZone();

    protected virtual void HandleRemoveRequest(GridSnappable snappable) => Unregister(snappable);

    protected virtual void HandleRegisterRequest(GridSnappable snappable) {
      if (snappable == null) return;
      if (IsInsideZone(snappable)) {
        Register(snappable);
      }
    }

    protected virtual void ScanZone() {
      _elements.Clear();
      int regesteredCount = 0;
      var foundElements = FindObjectsByType<GridSnappable>(FindObjectsSortMode.None);
      foreach (var snappable in foundElements) {
        if (IsInsideZone(snappable)) {
          if (!Register(snappable)) {
            Debug.LogWarning($"[ZoneManager] Failed to register element at {snappable.transform.position}");
          }
          regesteredCount++;
        }
      }
      Debug.Log($"[ZoneManager] Scan registered {regesteredCount} of {foundElements.Length} elements inside zone '{name}'");
    }

    public virtual bool Register(GridSnappable element) {
      if(element != null && !_elements.Contains(element) ) {
        Debug.Log($"[ZoneManager] Registering element {element.name} in zone '{name}'");
        _elements.Add(element);
        return true;
      }
      element.SetRegistered();
      //OnElementRegistered?.Invoke(element);

#if UNITY_EDITOR
      _editorView.Add(new GridElementView { Element = element });
#endif
      return true;
    }

    public virtual void Unregister(GridSnappable element) {
      if (element == null || !_elements.Contains(element)) return;
      Debug.Log($"[ZoneManager] Unregistering element {element.name} from zone '{name}'");
      _elements.Remove(element);
      element.SetRegistered(false);
#if UNITY_EDITOR
      _editorView.Remove(new GridElementView { Element = element });
#endif
    }

    public virtual bool IsInsideZone(GridSnappable element) => zoneCollider.bounds.Contains(element.GetPosition());

    public virtual bool Contains(GridSnappable element) => _elements.Contains(element);
  }

  [Serializable]
  public class GridElementView {
    public Vector2Int Coord;
    public GridSnappable Element;
  }

}