using System.Collections.Generic;
using Unity.Android.Gradle;


#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;


namespace GMTK {

  public class LevelGrid : MonoBehaviour {

    [Header("Grid Dimensions")]
    [Tooltip("The size in units for the cell. Recommended is 1")]
    public float CellSize = 1f; // Matches your peg spacing
    [Tooltip("World position of the center of the grid")]
    public Vector2 GridOrigin = Vector2.zero;
    [Tooltip("The number of cells in the grid. Only positive integer numbers")]
    public Vector2Int GridSize = new(50, 34);

    [Header("Bounds")]
    public EdgeCollider2D GridTopBound;
    public EdgeCollider2D GridBottomBound;
    public EdgeCollider2D GridLeftBound;
    public EdgeCollider2D GridRightBound;

    [Header("Background Sprite")]
    [Tooltip("The Sprite to be used as background. If left empty, LevelGrid will try to find it in this GameObject")]
    [SerializeField] protected SpriteRenderer _gridSprite;
    [Tooltip("the offset of the tiled sprite to match the grid Gizmo")]
    [SerializeField] protected Vector2 _spriteOffset = Vector2.zero;

    [Header("Gizmos")]
    [SerializeField] private bool enableGizmos = true;

    [Header("Gizmo: Grid")]
    [SerializeField] private bool useGridValuesForGizmo = true;
    [SerializeField] private float gizmoCellSize = 1f;
    [SerializeField] private Vector2Int gizmoGridSize = new(50, 34);
    [SerializeField] private Color gridColor = Color.gray;

    [Header("Gizmo: Occupancy")]
    [SerializeField] private Color occupiedColor = Color.red;
    [SerializeField] private Color freeColor = Color.green;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private float gizmoSize = 0.9f;
    [SerializeField] private Vector2 occupancyOffset = new(0,0);

    protected GridOccupancyMap _occupancyMap;

    const string TOP_BOUND_TAG = "TopBound";
    const string BOTTOM_BOUND_TAG = "BottomBound";
    const string LEFT_BOUND_TAG = "LeftBound";
    const string RIGHT_BOUND_TAG = "RightBound";

    const int MIN_GRID_SIZE = 4;
    const int MAX_GRID_SIZE = 100;

    public virtual void Awake() => AddInputListeners();
    public virtual void OnDestroy() => RemoveInputListeners();
    public void Start() => Initialize();
    public void OnValidate() {
      UpdateAllEdgeColliderBoundPoints();
      GridSize.x = Mathf.Clamp(GridSize.x, MIN_GRID_SIZE, MAX_GRID_SIZE);
      GridSize.y = Mathf.Clamp(GridSize.y, MIN_GRID_SIZE, MAX_GRID_SIZE);
    }


    #region Initialization

    protected virtual void Initialize() {
      InitializeGrid();
      InitializeAllEdgeColliderBounds();
      UpdateAllEdgeColliderBoundPoints();
    }

    private void AddInputListeners() {
      SnappableInputHandler.OnElementDropped += HandleElementDropped;
      SnappableInputHandler.OnElementHovered += HandleElementHovered;
      SnappableInputHandler.OnElementUnhovered += HandleElementUnhovered;
      SnappableInputHandler.OnElementSelected += HandleElementSelected;
    }

    private void RemoveInputListeners() {
      SnappableInputHandler.OnElementDropped -= HandleElementDropped;
      SnappableInputHandler.OnElementHovered -= HandleElementHovered;
      SnappableInputHandler.OnElementUnhovered -= HandleElementUnhovered;
      SnappableInputHandler.OnElementSelected -= HandleElementSelected;
    }

    protected virtual void InitializeAllEdgeColliderBounds() {
      if (GridSize.x <= 0 || GridSize.y <= 0) {
        Debug.LogError($"GridSize must be a positive number: {GridSize}");
        return;
      }
      //the EdgeColliders are positioned to the edges of the grid
      //the tag says where they go
      GridTopBound = InitEdgeColliderBound(GridTopBound, TOP_BOUND_TAG);
      GridBottomBound = InitEdgeColliderBound(GridBottomBound, BOTTOM_BOUND_TAG);
      GridLeftBound = InitEdgeColliderBound(GridLeftBound, LEFT_BOUND_TAG);
      GridRightBound = InitEdgeColliderBound(GridRightBound, RIGHT_BOUND_TAG);
    }

    private EdgeCollider2D InitEdgeColliderBound(EdgeCollider2D boundCollider, string tag) {
      boundCollider = (boundCollider == null) ?
          gameObject.AddComponent<EdgeCollider2D>() : boundCollider;
      boundCollider.transform.parent = gameObject.transform; //make the collider a child of the grid
      boundCollider.transform.position = Vector2.zero; //center the collider
      boundCollider.gameObject.tag = tag; //assign the tag
      boundCollider.gameObject.layer = LayerMask.NameToLayer("Level"); //this layer is by default collissioned.

      return boundCollider;
    }

    protected virtual void UpdateAllEdgeColliderBoundPoints() {
      UpdateEdgeColliderBoundPoints(GridTopBound, TOP_BOUND_TAG);
      UpdateEdgeColliderBoundPoints(GridBottomBound, BOTTOM_BOUND_TAG);
      UpdateEdgeColliderBoundPoints(GridLeftBound, LEFT_BOUND_TAG);
      UpdateEdgeColliderBoundPoints(GridRightBound, RIGHT_BOUND_TAG);
    }

    /// <summary>
    /// This method sets the EdgeCollider Points to the grid edge specified in the 'tag' parameter.
    /// </summary>
    /// <param name="boundCollider"></param>
    /// <param name="tag"></param>
    /// <returns></returns>
    private EdgeCollider2D UpdateEdgeColliderBoundPoints(EdgeCollider2D boundCollider, string tag) {

      List<Vector2> points = new();

      int halfWidth = GridSize.x / 2;
      int halfHeight = GridSize.y / 2;

      switch (tag) {
        case TOP_BOUND_TAG:
          points.Add(new Vector2(GridOrigin.x - halfWidth, GridOrigin.y + halfHeight));
          points.Add(new Vector2(GridOrigin.x + halfWidth, GridOrigin.y + halfHeight));
          break;
        case BOTTOM_BOUND_TAG:
          points.Add(new Vector2(GridOrigin.x - halfWidth, GridOrigin.y - halfHeight));
          points.Add(new Vector2(GridOrigin.x + halfWidth, GridOrigin.y - halfHeight));
          break;
        case LEFT_BOUND_TAG:
          points.Add(new Vector2(GridOrigin.x - halfWidth, GridOrigin.y + halfHeight));
          points.Add(new Vector2(GridOrigin.x - halfWidth, GridOrigin.y - halfHeight));
          break;
        case RIGHT_BOUND_TAG:
          points.Add(new Vector2(GridOrigin.x + halfWidth, GridOrigin.y + halfHeight));
          points.Add(new Vector2(GridOrigin.x + halfWidth, GridOrigin.y - halfHeight));
          break;
        default:
          Debug.LogWarning($"EdgeCollider '{boundCollider.name}' has an invalid tag: '{tag}'");
          break;
      }
      boundCollider.SetPoints(points);
      return boundCollider;
    }

    protected virtual void InitializeGrid() {

      //TODO (optional) make maxOccupantsPerCell and mode, parameters of the GridOccupancyMap
      _occupancyMap = new GridOccupancyMap(CellSize, GridOrigin,
        maxOccupantsPerCell: 3,
        mode: CellLayeringOrder.LastToFirst);

      var allSnappables = FindObjectsByType<GridSnappable>(FindObjectsSortMode.None);
      //Snappables in the playing area at the time of initializing the grid
      //are considered non-draggable -> player cannot move them
      foreach (var snappable in allSnappables) {
        if (IsInsidePlayableArea(snappable.transform.position)) {
          snappable.transform.position = SnapToGrid(snappable.transform.position);
          snappable.Draggable=false;
          var gridOrigin = WorldToGrid(snappable.transform.position);
          _occupancyMap.Register(snappable, gridOrigin);
        }
      }
      _gridSprite = (_gridSprite == null) ? GetComponent<SpriteRenderer>() : _gridSprite;
    }


    #endregion


    #region Event Handlers


    /// <summary>
    /// If the dropped element is in the playable area, the LevelGrid snaps it
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected virtual void HandleElementDropped(object sender, GridSnappableEventArgs e) {
      var element = e.Element;
      var gridOrigin = WorldToGrid(element.transform.position);

      if (CanPlace(element, gridOrigin)) {
        Debug.Log($"Placing element {element.name} at {gridOrigin}");
        //if element is already in grid, we need to register first
        //to clean previous marked cells
        if(_occupancyMap.ContainsSnappable(element)) {
          _occupancyMap.Unregister(element, gridOrigin);
        }

        element.transform.position = SnapToGrid(gridOrigin);
        _occupancyMap.Register(element, gridOrigin);
      }
      else {
        //TODO element.ReturnToPreviousPosition();
        Debug.Log($"Can't place element {element.name} at {gridOrigin}");
      }
    }

    //TODO in the future I might apply UI controls on touch/click
    protected virtual void HandleElementSelected(object sender, GridSnappableEventArgs e) { }

    // To decouple GridSnappable behaviour from the grid, this method only notifies the GridSnappable to handle the 'unhover'
    //that way we prevent from polling the mouse position from snappables on Update
    private void HandleElementUnhovered(object sender, GridSnappableEventArgs e) {
      if (e.Element != null) e.Element.OnPointerOut();
    }

    // This method only delegates to the GridSnappable to handle the 'hover'
    //that way we prevent from polling the mouse position from snappables on Update
    protected virtual void HandleElementHovered(object sender, GridSnappableEventArgs e) {
      if (e.Element != null) e.Element.OnPointerOver();
    }
    #endregion

    #region Grid Methods

    public virtual bool IsInsidePlayableArea(Vector2 position) {

      if (GridTopBound == null || GridBottomBound == null || GridLeftBound == null || GridRightBound == null) {
        Debug.LogWarning("[GridManager] One or more grid boundary colliders are not assigned.");
        return false;
      }
      return (position.y <= GridTopBound.bounds.max.y) &&
              (position.y >= GridBottomBound.bounds.min.y) &&
              (position.x >= GridLeftBound.bounds.min.x) &&
              (position.x <= GridRightBound.bounds.max.x);
    }
    public bool IsOccupied(Vector2 position) => _occupancyMap.HasAnyOccupants(position);

    public virtual bool CanPlace(GridSnappable snappable, Vector2Int gridOrigin) {
      foreach (var cell in snappable.GetWorldOccupiedCells(gridOrigin)) {
        if (_occupancyMap.HasAnyOccupants(cell)) return false;
      }
      return true;
    }

    /// <summary>
    /// Returns the Grid coordinates that correspond to the world position 'position' and returns them as a new Vector2 
    /// </summary>
    /// <param name="position"></param>
    /// <returns>Vector2 with the world coordinates of the Grid coordinates assigned to 'position'</returns>
    protected virtual Vector2 SnapToGrid(Vector2 position) {
      Vector2Int index = GetGridIndex(position);
      float x = index.x * CellSize + GridOrigin.x;
      float y = index.y * CellSize + GridOrigin.y;
      return new Vector2(x, y);
    }

    /// <summary>
    /// Returns the Grid position that corresponds to the world position specified in 'position'
    /// </summary>
    /// <param name="position"></param>
    /// <returns>Vector2Int with the Grid coordinates that correspons</returns>
    protected virtual Vector2Int WorldToGrid(Vector2 position) {
      return GetGridIndex(position);
    }

    private Vector2 GridToWorld(Vector2Int cell) {
      float x = cell.x * CellSize + GridOrigin.x;
      float y = cell.y * CellSize + GridOrigin.y;
      return new Vector2(x, y);
    }

    /// <summary>
    /// Common World Coordinates to Grid Coordinates method
    /// </summary>
    private Vector2Int GetGridIndex(Vector2 position) {
      int x = Mathf.RoundToInt((position.x - GridOrigin.x) / CellSize);
      int y = Mathf.RoundToInt((position.y - GridOrigin.y) / CellSize);
      return new Vector2Int(x, y);
    }
    #endregion

    #region Gizmos

    private void OnDrawGizmos() {
      if (!enableGizmos) return;
      DrawGridGizmos();
      DrawOccupancyGizmos();
    }

    private void DrawGridGizmos() {
      if (useGridValuesForGizmo) {
        gizmoCellSize = CellSize;
        gizmoGridSize = GridSize;
      }
      Gizmos.color = gridColor;
      InitializeGrid();

      int halfWidth = gizmoGridSize.x / 2;
      int halfHeight = gizmoGridSize.y / 2;

      for (int x = -halfWidth; x <= halfWidth; x++) {
        Vector3 start = new(GridOrigin.x + x * gizmoCellSize, GridOrigin.y - halfHeight * gizmoCellSize, 0f);
        Vector3 end = new(GridOrigin.x + x * gizmoCellSize, GridOrigin.y + halfHeight * gizmoCellSize, 0f);
        Gizmos.DrawLine(gameObject.transform.position + start, gameObject.transform.position + end);
      }

      for (int y = -halfHeight; y <= halfHeight; y++) {
        Vector3 start = new(GridOrigin.x - halfWidth * gizmoCellSize, GridOrigin.y + y * gizmoCellSize, 0f);
        Vector3 end = new(GridOrigin.x + halfWidth * gizmoCellSize, GridOrigin.y + y * gizmoCellSize, 0f);
        Gizmos.DrawLine(gameObject.transform.position + start, gameObject.transform.position + end);
      }
    }

    private void DrawOccupancyGizmos() {
      if (_occupancyMap == null) return;

      foreach (var kvp in _occupancyMap.GetAllCells()) {
        var cell = kvp.Key;
        var occupants = kvp.Value;

        Vector3 worldPos = GridToWorld(cell) + occupancyOffset;
        Gizmos.color = occupants.HasAnyOccupant ? occupiedColor : freeColor;
        Gizmos.DrawCube(worldPos, Vector3.one * gizmoSize);

#if UNITY_EDITOR
        Handles.color = textColor;
        Handles.Label(worldPos + Vector3.up * 0.2f, $"{occupants.Count}");
#endif
      }
    }




    #endregion
  }
}