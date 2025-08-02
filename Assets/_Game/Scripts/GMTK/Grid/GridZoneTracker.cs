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

    // Events for when elements are registered or unregistered
    public event Action<GridSnappable> OnElementRegistered;
    public event Action<GridSnappable> OnElementUnregistered;

    public virtual void Start() => ScanZone();
    public virtual void OnEnable() => GridSnappableUIController.OnRemoveRequested += HandleRemoveRequest;
    public virtual void OnDisable() => GridSnappableUIController.OnRemoveRequested -= HandleRemoveRequest;


    protected virtual void HandleRemoveRequest(GridSnappable snappable) => Unregister(snappable);

    protected virtual void ScanZone() {
      _elements.Clear();
      int regesteredCount = 0;
      var foundElements = FindObjectsByType<GridSnappable>(FindObjectsSortMode.None);
      foreach (var snappable in foundElements) {
        if (zoneCollider.bounds.Contains(snappable.GetPosition())) {
          if (!Register(snappable)) {
            Debug.LogWarning($"[ZoneManager] Failed to register element at {snappable.transform.position}");
          }
          regesteredCount++;
        }
      }
      Debug.Log($"[ZoneManager] Scan registered {regesteredCount} of {foundElements.Length} elements inside zone '{name}'");
    }

    public virtual bool Register(GridSnappable element) {
      if(!_elements.Contains(element)) {
        _elements.Add(element);
        return true;
      }
      OnElementRegistered?.Invoke(element);

#if UNITY_EDITOR
      _editorView.Add(new GridElementView { Element = element });
#endif
      return true;
    }

    public virtual void Unregister(GridSnappable element) {
      if (element == null || !_elements.Contains(element)) return;
      _elements.Remove(element);
      OnElementUnregistered?.Invoke(element);
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