using System.Collections.Generic;

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

    [Header("Input Handler")]
    [Tooltip("Reference to the InputHandler detecting moving elements")]
    [SerializeField] protected SnappableInputHandler _inputHandler;

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
    [SerializeField] private Vector2 occupancyOffset = new(0, 0);

    protected GridOccupancyMap _occupancyMap;

    protected GridSnappable _currentSelected;
    protected Vector2 _elementOriginalWorldPosition;
    protected Vector2Int _elementOriginalGridPosition;
    protected bool _elementWasInGrid;
    protected bool _isTrackingMovement;

    const string TOP_BOUND_TAG = "TopBound";
    const string BOTTOM_BOUND_TAG = "BottomBound";
    const string LEFT_BOUND_TAG = "LeftBound";
    const string RIGHT_BOUND_TAG = "RightBound";

    const int MIN_GRID_SIZE = 4;
    const int MAX_GRID_SIZE = 100;

    public static Vector3 ELEMENT_DEFAULT_POSITION = new(-10, 0, 0);

    #region Monobehavior Methods
    public virtual void Awake() => AddInputListeners();
    public virtual void OnDestroy() => RemoveInputListeners();
    public void Start() => Initialize();
    public void OnValidate() {
      UpdateAllEdgeColliderBoundPoints();
      GridSize.x = Mathf.Clamp(GridSize.x, MIN_GRID_SIZE, MAX_GRID_SIZE);
      GridSize.y = Mathf.Clamp(GridSize.y, MIN_GRID_SIZE, MAX_GRID_SIZE);
    }
    private void Update() {
      TrackElementMovement();
    }

    #endregion


    #region Initialization

    protected virtual void Initialize() {

      if (_inputHandler == null) {
        Debug.LogWarning($"LevelGrid: SnappableInputHandler is missing. LevelGrid will not be able to track player inputs on Elements");
        return;
      }

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
          snappable.Draggable = false;
          var gridOrigin = WorldToGrid(snappable.transform.position);
          _occupancyMap.Register(snappable, gridOrigin);
        }
      }
      _gridSprite = (_gridSprite == null) ? GetComponent<SpriteRenderer>() : _gridSprite;
    }

    #endregion


    #region Track Element Movements

    private void TrackElementMovement() {
      // Check if input handler has an element moving
      if (_inputHandler.Current != null && _inputHandler.IsMoving) {
        var currentElement = _inputHandler.Current;

        // If this is a new element being tracked, stop tracking current
        // and begin tracking new
        // otherwise, we are still tracking the same Element
        if (_currentSelected == null || _currentSelected != currentElement) {
          StopTrackingCurrentSelected();
          StartTrackingElement(currentElement);
        } 

      } else {
        //inputhandler is not moving and we are tracking -> we need to stop tracking currentSelected
        if (_isTrackingMovement) {
          StopTrackingCurrentSelected();
        }
      }
    }

    private void StartTrackingElement(GridSnappable element) {
      _currentSelected = element;
      _isTrackingMovement = true;

      // Store original position and check if it was in the grid
      var currentPosition = element.transform.position;
      _elementWasInGrid = _occupancyMap.ContainsSnappable(element);

      if (_elementWasInGrid) {
        _elementOriginalGridPosition = WorldToGrid(currentPosition);
        // Unregister from grid while moving to avoid conflicts
        _occupancyMap.Unregister(element, _elementOriginalGridPosition);
        Debug.Log($"[LevelGrid] Started tracking '{element.name}' - unregistered from ({_elementOriginalGridPosition})");
      }
      else {
        Debug.Log($"[LevelGrid] Started tracking '{element.name}' - was not in grid");
      }

      // Notify DragFeedbackComponent to start visual feedback
      if (element.TryGetComponent<DragFeedbackComponent>(out var dragFeedback)) {
        dragFeedback.StartDragFeedback();
      }
    }

    private void StopTrackingCurrentSelected() {
      // Notify DragFeedbackComponent to stop visual feedback
      if (_currentSelected != null) {
        if (_currentSelected.TryGetComponent<DragFeedbackComponent>(out var dragFeedback)) {
          dragFeedback.StopDragFeedback();
        }
      }
      _currentSelected = null;
      _isTrackingMovement = false;
      _elementWasInGrid = false;
    }

    //private void UpdateMovementFeedback(GridSnappable element) {
    //  // Optional: Add visual feedback while dragging
    //  var currentGridPos = WorldToGrid(element.transform.position);
    //  bool canPlaceHere = IsInsidePlayableArea(element.transform.position) && CanPlace(element, currentGridPos);
    //  Debug.Log($"[LevelGrid] movement feedback for {element.name}");
    //  // You could change the element's highlight color based on canPlaceHere
    //  element.SetValidDropZone(canPlaceHere);
    //}

    #endregion

    #region Event Handlers

    protected virtual void HandleElementSelected(object sender, GridSnappableEventArgs e) {
      // This now just collects element initial world and grid positions - tracking starts in Update()
      if (e.Element != null) {
        //_currentSelected = e.Element;
        _elementOriginalWorldPosition = e.Element.transform.position;
        _elementWasInGrid = _occupancyMap.ContainsSnappable(e.Element);
        if (_elementWasInGrid) {
          _elementOriginalGridPosition = WorldToGrid(_elementOriginalWorldPosition);
        }
        Debug.Log($"Element '{e.Element.name}' selected at {_elementOriginalWorldPosition}");
        if (_elementWasInGrid) Debug.Log($"Element '{e.Element.name}' at grid {_elementOriginalGridPosition}");
        e.Element.OnPointerOver();
      }
    }

    protected virtual void HandleElementDropped(object sender, GridSnappableEventArgs e) {
      var element = e.Element;
      var newGridOrigin = WorldToGrid(element.transform.position);

      // Try to place at new position
      if (IsInsidePlayableArea(element.transform.position)) {

        // Success - place at new position
        if (CanPlace(element, newGridOrigin)) {
          element.transform.position = SnapToGrid(newGridOrigin);
          _occupancyMap.Register(element, newGridOrigin);
          Debug.Log($"Placed {element.name} at {newGridOrigin}");
        }
        else {
          // Failed to place - return to original position if it was in grid
          if (_elementWasInGrid) {
            element.transform.position = SnapToGrid(_elementOriginalGridPosition);
            _occupancyMap.Register(element, _elementOriginalGridPosition);
            Debug.Log($"Returned '{element.name}' to original position {_elementOriginalGridPosition}");
          }
          // Element came from outside grid - return to inventory
          else {
            HandleElementReturnToInventory(element);
            Debug.Log($"Returned {element.name} to inventory");
          }
        }

        // Check if this was the element we were tracking
        if (element == _currentSelected) {
          Debug.Log($"Dropping tracked element '{element.name}' at {newGridOrigin}");
          // Clean up tracking
          StopTrackingCurrentSelected();
        }
        else {
          // Element wasn't being tracked (probably just clicked)
          Debug.Log($"Element '{element.name}' clicked but not moved");
        }
      }
    }

    //TODO refactor when Inventory is implemented
    private void HandleElementReturnToInventory(GridSnappable element) {
      // Find inventory zone or return to a default position
      var inventoryZone = GameObject.Find("InventoryZone");
      if (inventoryZone != null) {
        // Position randomly within inventory zone to avoid overlap
        var bounds = new Bounds(inventoryZone.transform.position, Vector3.one);
        if (inventoryZone.TryGetComponent<Collider2D>(out var collider)) {
          bounds = collider.bounds;
        }
        var randomOffset = new Vector3(
            Random.Range(-bounds.size.x * 0.4f, bounds.size.x * 0.4f),
            Random.Range(-bounds.size.y * 0.4f, bounds.size.y * 0.4f),
            0);
        element.transform.position = bounds.center + randomOffset;
      }
      else {
        // Fallback: move to a default position
        element.transform.position = ELEMENT_DEFAULT_POSITION;
      }
    }

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
    public virtual Vector2Int WorldToGrid(Vector2 position) {
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