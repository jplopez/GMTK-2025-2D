using System;
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

    [Header("Grid Bounds")]
    public EdgeCollider2D GridTopBound;
    public EdgeCollider2D GridBottomBound;
    public EdgeCollider2D GridLeftBound;
    public EdgeCollider2D GridRightBound;

    [Header("Cell Settings")]
    public float CellSize = 1f; // Matches your peg spacing
    public Vector2 Origin = Vector2.zero;

    private Dictionary<Vector2Int, GridSnappable> _gridElements = new();

    //Gizmos
    [Header("Gizmo Settings")]
    [SerializeField] private bool enableGizmo = true;
    [SerializeField] private bool useCellSizeForGizmo = true;
    [SerializeField] private float gizmoCellSize = 1f;
    [SerializeField] private int gridWidth = 10;
    [SerializeField] private int gridHeight = 10;
    [SerializeField] private Color gridColor = Color.gray;

#if UNITY_EDITOR
    protected virtual void EditorScanAndRegisterElements() => ScanZone();
#endif

    public virtual void OnEnable() {
      SnappableInputHandler.OnElementDropped += HandleElementDropped;
      SnappableInputHandler.OnElementHovered += HandleElementHovered;
      SnappableInputHandler.OnElementUnhovered += HandleElementUnhovered;
      SnappableInputHandler.OnElementSelected += HandleElementSelected;
    }


    public virtual void OnDisable() {
      SnappableInputHandler.OnElementDropped -= HandleElementDropped;
      SnappableInputHandler.OnElementHovered -= HandleElementHovered;
      SnappableInputHandler.OnElementUnhovered -= HandleElementUnhovered;
      SnappableInputHandler.OnElementSelected -= HandleElementSelected;
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
    #endregion

    #region Grid element Methods
    private bool RegisterAtGrid(GridSnappable element) {
      Vector2Int coord = GetGridCoord(element.transform.position);
      if (IsOccupied(coord) && _gridElements[coord] != element) {
        Debug.LogWarning($"[GridManager] Grid position {coord} is already occupied by '{_gridElements[coord].name}'. Cannot register '{element.name}' here.");
        return false; //position occupied by another element
      }
      _gridElements[coord] = element;
      Debug.Log($"[GridManager] {element.name} added to Grid position {coord}");
      return true;
    }

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

    #region Event Handlers


    private void HandleElementDropped(object sender, GridSnappableEventArgs e) {
      var element = e.Element;
      if (element == null) return;
      try {
        element.SetGlow(false);
        if (IsInsideZone(element)) {
          HandleRegisterRequest(element);
        }else {
          HandleRemoveRequest(element);
        }
      } catch(Exception ex) {
        Debug.LogError($"[GridManager] Failed to handle element after ElementDropped event : {ex.Message}");
#if UNITY_EDITOR
        throw ex;
#endif
      }
    }
    
    //For now, selecting an element only enables the glow from Hovered.
    private void HandleElementSelected(object sender, GridSnappableEventArgs e) => HandleElementHovered(sender, e);
    private void HandleElementUnhovered(object sender, GridSnappableEventArgs e) {
      var element = e.Element;
      if (element == null) return;
      if (element.Draggable) {
        element.SetGlow(false);
      }
    }

    private void HandleElementHovered(object sender, GridSnappableEventArgs e) {
      var element = e.Element;
      if (element == null) return;
      if(element.Draggable) {
        element.SetGlow(true);
      }
    }
    #endregion

    #region Grid utilities
    //public Vector2 SnapToGrid(Vector2 position) {
    //  float x = Mathf.Round((position.x - Origin.x) / CellSize) * CellSize + Origin.x;
    //  float y = Mathf.Round((position.y - Origin.y) / CellSize) * CellSize + Origin.y;
    //  Debug.Log($"SnapToGrid {position} => {x},{y}");
    //  return new Vector2(x, y);
    //}

    public Vector2 SnapToGrid(Vector2 position) {
      Vector2Int index = GetGridIndex(position);
      float x = index.x * CellSize + Origin.x;
      float y = index.y * CellSize + Origin.y;
      return new Vector2(x, y);
    }

    public Vector2Int GetGridCoord(Vector2 position) {
      return GetGridIndex(position);
    }

    //public Vector2Int GetGridCoord(Vector2 position) {
    //  int x = Mathf.RoundToInt((position.x - Origin.x) / CellSize);
    //  int y = Mathf.RoundToInt((position.y - Origin.y) / CellSize);
    //  Debug.Log($"GetGridCoord {position} => {x},{y}");
    //  return new Vector2Int(x, y);
    //}

    private Vector2Int GetGridIndex(Vector2 position) {
      int x = Mathf.RoundToInt((position.x - Origin.x) / CellSize);
      int y = Mathf.RoundToInt((position.y - Origin.y) / CellSize);
      return new Vector2Int(x, y);
    }

    private float ToGridXFloat(float xPos) {
      return Mathf.Round((xPos - Origin.x) / CellSize) * CellSize;
    }

    private int ToGridXInt(float xPos) {
      return Mathf.RoundToInt(ToGridXFloat(xPos));
    }
    #endregion

    private void OnDrawGizmos() {
      if (!enableGizmo) return;
      if (useCellSizeForGizmo) gizmoCellSize = CellSize;
      Gizmos.color = gridColor;

      int halfWidth = gridWidth / 2;
      int halfHeight = gridHeight / 2;

      for (int x = -halfWidth; x <= halfWidth; x++) {
        Vector3 start = new Vector3(Origin.x + x * gizmoCellSize, Origin.y - halfHeight * gizmoCellSize, 0f);
        Vector3 end = new Vector3(Origin.x + x * gizmoCellSize, Origin.y + halfHeight * gizmoCellSize, 0f);
        Gizmos.DrawLine(start, end);
      }

      for (int y = -halfHeight; y <= halfHeight; y++) {
        Vector3 start = new Vector3(Origin.x - halfWidth * gizmoCellSize, Origin.y + y * gizmoCellSize, 0f);
        Vector3 end = new Vector3(Origin.x + halfWidth * gizmoCellSize, Origin.y + y * gizmoCellSize, 0f);
        Gizmos.DrawLine(start, end);
      }
    }


  }
}