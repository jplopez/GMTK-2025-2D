using System.Collections.Generic;
using UnityEngine;

namespace GMTK {

  /// <summary>
  /// Manages a _grid system for snapping positions and calculating _grid coordinates.
  /// </summary>
  /// <remarks>This class provides functionality to align positions to a _grid and determine the _grid coordinates
  /// of a given position. The _grid is defined by a cell size and an Origin point, which can be configured using the
  /// <see cref="CellSize"/> and <see cref="Origin"/> fields.</remarks>
  public class GridManager : SnappableZoneManager {

    public EdgeCollider2D GridTopBound;
    public EdgeCollider2D GridBottomBound;
    public EdgeCollider2D GridLeftBound;
    public EdgeCollider2D GridRightBound;

    public float CellSize = 1f; // Matches your peg spacing
    public Vector2 Origin = Vector2.zero;

    private Dictionary<Vector2Int, GridSnappable> _gridElements = new();

#if UNITY_EDITOR
    protected virtual void EditorScanAndRegisterElements() => ScanZone();
#endif

    public virtual void OnEnable() {
      PlayerInputController.OnElementUnregistered += HandleRemoveRequest;
      PlayerInputController.OnElementDropped += HandleRegisterRequest;
      //PlayerInputController.OnElementSelected += HandleRemoveRequest;
      PlayerInputController.OnElementSecondary += HandleRemoveRequest;

      // This is to handle the case where an element is unregistered from the inventory and needs to be re-registered in the grid
      InventoryInputController.OnInventoryExit += HandleRegisterRequest;
    }
    public virtual void OnDisable() {
      PlayerInputController.OnElementUnregistered -= HandleRemoveRequest;
      PlayerInputController.OnElementDropped -= HandleRegisterRequest;
      //PlayerInputController.OnElementSelected -= HandleRemoveRequest;
      PlayerInputController.OnElementSecondary -= HandleRemoveRequest;

      InventoryInputController.OnInventoryExit -= HandleRegisterRequest;

    }

    #region ZoneManager overrides
    public override bool Register(GridSnappable element) {
      //attempt to register in base zone manager, which will add to _elements list
      if (base.Register(element)) {
        //place element at snapped position. If outside zone, snap to origin
        Vector2 elementPos = SnapToGrid(Vector2.zero);
        if (IsInsideZone(element)) {
          elementPos = SnapToGrid(element.transform.position);
        }
        element.transform.position = elementPos;
        RegisterAtGrid(element);
        Debug.Log($"[GridManager] Element '{element.name}' snapped to grid at {elementPos}");
      }
      else {
        Debug.LogWarning($"[GridManager] Base registration failed for element at {element.name}");
        //UnregisterFromGrid(element);
        return false;
      }
      return true;
    }

    //public override void Unregister(GridSnappable element) {
    //  try {
    //    base.Unregister(element);
    //  }
    //  catch (Exception ex) {
    //    Debug.LogWarning($"[GridManager] Exception during base Unregister: {ex.Message}");
    //    return;
    //  }

    //  try {
    //    UnregisterFromGrid(element);
    //  }
    //  catch (Exception ex) {
    //    Debug.LogWarning($"[GridManager] Exception during UnregisterFromGrid: {ex.Message}");
    //  }
    //}

    #endregion

    #region Grid element Methods

    private bool RegisterAtGrid(GridSnappable element) {
      Vector2Int coord = GetGridCoord(element.transform.position);
      if (IsOccupied(coord) && _gridElements[coord] != element) {
        Debug.LogWarning($"[GridManager] Grid position {coord} is already occupied by '{_gridElements[coord].name}'. Cannot register '{element.name}' here.");
        return false; //position occupied by another element
      }
      _gridElements[coord] = element;
      Debug.LogWarning($"[GridManager] {element.name} added to Grid position {coord}");
      return true;
    }

    //private void UnregisterFromGrid(GridSnappable element) {
    //  Vector2Int coord = GetGridCoord(element.transform.position);
    //  if (_gridElements.ContainsValue(element) && _gridElements[coord] == element) {
    //    _gridElements.Remove(coord);
    //    element.SetRegistered(false);
    //  }
    //}

    //public virtual GridSnappable GetElementAtScreenPosition(Vector2 screenPos) {
    //  Vector2 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
    //  RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
    //  //return gameObject on collider if it has a GridInteractive component
    //  if (hit.collider != null && (hit.collider.gameObject is var go)) {
    //    if (go.TryGetComponent(out GridSnappable gridElement)) {
    //      return gridElement;
    //    }
    //    else {
    //      Debug.Log($"[GridManager] GameObject found at {screenPos} (worldPos:{worldPos} does not have a GridInteractive compatible component");
    //    }
    //  }
    //  return null;
    //}
    //public virtual bool TryGetElementAtScreenPosition(Vector2 screenPos, out GridSnappable element) {
    //  element = GetElementAtScreenPosition(screenPos);
    //  return element != null;
    //}
    //public virtual bool TryGetElementCoord(GameObject element, out Vector2Int coord) {
    //  foreach (var kvp in _gridElements) {
    //    if (kvp.Value == element) {
    //      coord = kvp.Key;
    //      return true;
    //    }
    //  }
    //  coord = default;
    //  return false;
    //}

    public virtual bool IsOccupied(Vector2Int coord) => _gridElements.ContainsKey(coord);

    public virtual bool IsOccupied(Vector2 position) => IsOccupied(GetGridCoord(position));

    // The GridManager's implementation checks against the EdgeCollider2d defining the bounds of the grid
    public override bool IsInsideZone(GridSnappable element) {
      // Check if the element's position is within the bounds defined by the edge colliders
      if (element == null) return false;

      Vector2 pos = element.transform.position;
      if (GridTopBound == null || GridBottomBound == null || GridLeftBound == null || GridRightBound == null) {
        Debug.LogWarning("[GridManager] One or more grid boundary colliders are not assigned.");
        return false;
      }
      return (pos.y <= GridTopBound.bounds.max.y) &&
                    (pos.y >= GridBottomBound.bounds.min.y) &&
                    (pos.x >= GridLeftBound.bounds.min.x) &&
                    (pos.x <= GridRightBound.bounds.max.x);
    }

    #endregion


    #region Grid utilities
    public Vector2 SnapToGrid(Vector2 position) {
      float x = Mathf.Round((position.x - Origin.x) / CellSize) * CellSize + Origin.x;
      float y = Mathf.Round((position.y - Origin.y) / CellSize) * CellSize + Origin.y;
      return new Vector2(x, y);
    }

    public Vector2Int GetGridCoord(Vector2 position) {
      int x = Mathf.RoundToInt((position.x - Origin.x) / CellSize);
      int y = Mathf.RoundToInt((position.y - Origin.y) / CellSize);
      return new Vector2Int(x, y);
    }
    #endregion

  }

}