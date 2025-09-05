using Ameba;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace GMTK {

  public enum GridOriginSources { GameObject, Custom }
  public class LevelGrid : MonoBehaviour {

    [Header("Grid Dimensions")]
    [Tooltip("The size in units for the cell. Recommended is 1")]
    public float CellSize = 1f; // Matches the peg sprite spacing
    [Tooltip("The number of cells in the grid. Only positive integer numbers")]
    public Vector2Int GridSize = new(50, 34);
    [Tooltip("Whether the grid origin should be taken from the Grid's GameObject or a specific world position")]
    public GridOriginSources OriginSource;
    [Tooltip("If OriginSource is 'Custom', this field is the world position of the center of the grid")]
    public Vector2 CustomGridOrigin = Vector2.zero;

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

    public Vector2 GridOrigin => _gridOrigin;

    protected Vector2 _gridOrigin = Vector2.zero;
    protected GridOccupancyMap _occupancyMap;
    protected GridSnappable _currentSelected;
    protected Vector2 _elementOriginalWorldPosition;
    protected Vector2Int _elementOriginalGridPosition;
    protected bool _elementWasInGrid;
    protected bool _isTrackingMovement;

    protected GameEventChannel _eventsChannel;

    const string TOP_BOUND_TAG = "TopBound";
    const string BOTTOM_BOUND_TAG = "BottomBound";
    const string LEFT_BOUND_TAG = "LeftBound";
    const string RIGHT_BOUND_TAG = "RightBound";
    const int MIN_GRID_SIZE = 4;
    const int MAX_GRID_SIZE = 100;

    public static Vector3 ELEMENT_DEFAULT_POSITION = new(-10, 0, 0);

    #region Monobehavior Methods

    private void Awake() {
     
      if (_eventsChannel == null) {
        _eventsChannel = ServiceLocator.Get<GameEventChannel>();
      }
      AddInputListeners();
    }
    public virtual void OnDestroy() => RemoveInputListeners();
    public void Start() => Initialize();
    public void OnValidate() {
      GridSize.x = Mathf.Clamp(GridSize.x, MIN_GRID_SIZE, MAX_GRID_SIZE);
      GridSize.y = Mathf.Clamp(GridSize.y, MIN_GRID_SIZE, MAX_GRID_SIZE);
      //InitializeAllEdgeColliderBounds();
      UpdateAllEdgeColliderBoundPoints();
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

      if (OriginSource == GridOriginSources.GameObject) {
        _gridOrigin = gameObject.transform.position;
      }
      else if (OriginSource == GridOriginSources.Custom) {
        _gridOrigin = CustomGridOrigin;
      }
      else {
        //safety measure
        _gridOrigin = Vector2.zero;
      }

      InitializeGrid();
      InitializeAllEdgeColliderBounds();
      UpdateAllEdgeColliderBoundPoints();
    }
    protected virtual void InitializeGrid() {

      //TODO (optional) make maxOccupantsPerCell and mode, parameters of the GridOccupancyMap
      _occupancyMap = new GridOccupancyMap(CellSize, _gridOrigin,
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
      //boundCollider.gameObject.tag = tag; //assign the tag
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
      float xPos = gameObject.transform.position.x;
      float yPos = gameObject.transform.position.y;
      switch (tag) {
        case TOP_BOUND_TAG:
          points.Add(new Vector2(xPos - halfWidth, yPos + halfHeight));
          points.Add(new Vector2(xPos + halfWidth, yPos + halfHeight));
          break;
        case BOTTOM_BOUND_TAG:
          points.Add(new Vector2(xPos - halfWidth, yPos - halfHeight));
          points.Add(new Vector2(xPos + halfWidth, yPos - halfHeight));
          break;
        case LEFT_BOUND_TAG:
          points.Add(new Vector2(xPos - halfWidth, yPos + halfHeight));
          points.Add(new Vector2(xPos - halfWidth, yPos - halfHeight));
          break;
        case RIGHT_BOUND_TAG:
          points.Add(new Vector2(xPos + halfWidth, yPos + halfHeight));
          points.Add(new Vector2(xPos + halfWidth, yPos - halfHeight));
          break;
        default:
          Debug.LogWarning($"EdgeCollider '{boundCollider.name}' has an invalid tag: '{tag}'");
          break;
      }
      boundCollider.SetPoints(points);
      return boundCollider;
    }

    #endregion

    #region Event Listeners

    private void AddInputListeners() {
      if (_eventsChannel == null) return;
      
      // Change from EventArgs to specific types
      _eventsChannel.AddListener<GridSnappableEventArgs>(GameEventType.ElementSelected, HandleElementSelected);
      _eventsChannel.AddListener<GridSnappableEventArgs>(GameEventType.ElementDropped, HandleElementDropped);
      _eventsChannel.AddListener<GridSnappableEventArgs>(GameEventType.ElementHovered, HandleElementHovered);
      _eventsChannel.AddListener<GridSnappableEventArgs>(GameEventType.ElementUnhovered, HandleElementUnhovered);

      // Change from EventArgs to InventoryEventData
      _eventsChannel.AddListener<InventoryEventData>(GameEventType.InventoryElementAdded, HandleInventoryElementAddedWrapper);
      _eventsChannel.AddListener<InventoryEventData>(GameEventType.InventoryElementRetrieved, HandleInventoryElementRetrievedWrapper);
      _eventsChannel.AddListener<InventoryEventData>(GameEventType.InventoryOperationFailed, HandleInventoryOperationFailedWrapper);
      _eventsChannel.AddListener<InventoryEventData>(GameEventType.InventoryUpdated, HandleInventoryUpdatedWrapper);
    }

    private void RemoveInputListeners() {
      if (_eventsChannel == null) return;
      
      _eventsChannel.RemoveListener<GridSnappableEventArgs>(GameEventType.ElementSelected, HandleElementSelected);
      _eventsChannel.RemoveListener<GridSnappableEventArgs>(GameEventType.ElementDropped, HandleElementDropped);
      _eventsChannel.RemoveListener<GridSnappableEventArgs>(GameEventType.ElementHovered, HandleElementHovered);
      _eventsChannel.RemoveListener<GridSnappableEventArgs>(GameEventType.ElementUnhovered, HandleElementUnhovered);

      _eventsChannel.RemoveListener<InventoryEventData>(GameEventType.InventoryElementAdded, HandleInventoryElementAddedWrapper);
      _eventsChannel.RemoveListener<InventoryEventData>(GameEventType.InventoryElementRetrieved, HandleInventoryElementRetrievedWrapper);
      _eventsChannel.RemoveListener<InventoryEventData>(GameEventType.InventoryOperationFailed, HandleInventoryOperationFailedWrapper);
      _eventsChannel.RemoveListener<InventoryEventData>(GameEventType.InventoryUpdated, HandleInventoryUpdatedWrapper);
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

      }
      else {
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

    #endregion

    #region Event Handler Wrappers (EventArgs -> InventoryEventData)

    private void HandleInventoryElementAddedWrapper(EventArgs args) {
      if (args is InventoryEventData inventoryData) {
        HandleInventoryElementAdded(inventoryData);
      }
    }

    private void HandleInventoryElementRetrievedWrapper(EventArgs args) {
      if (args is InventoryEventData inventoryData) {
        HandleInventoryElementRetrieved(inventoryData);
      }
    }

    private void HandleInventoryOperationFailedWrapper(EventArgs args) {
      if (args is InventoryEventData inventoryData) {
        HandleInventoryOperationFailed(inventoryData);
      }
    }

    private void HandleInventoryUpdatedWrapper(EventArgs args) {
      if (args is InventoryEventData inventoryData) {
        HandleInventoryUpdated(inventoryData);
      }
    }

    #endregion

    #region Inventory Event Handlers

    // Enhanced HandleElementReturnToInventory with richer context
    protected virtual void HandleElementReturnToInventory(GridSnappable element) {

      // Create rich event data with context
      var eventData = InventoryEventData.CreateAddRequest(element, "LevelGrid")
          .WithContext(
              wasInGrid: _occupancyMap.ContainsSnappable(element),
              wasInInventory: false  // We know it's coming from grid
          );

      // Request inventory to add this element
      _eventsChannel.Raise(GameEventType.InventoryAddRequest, eventData);
      Debug.Log($"[LevelGrid] Requested inventory to add {element.name} with context");

      //else {
      //  // Fallback to old positioning system
      //  HandleElementReturnToInventoryFallback(element);
      //}
    }

    private void HandleElementReturnToInventoryFallback(GridSnappable element) {
      var inventoryZone = GameObject.Find("InventoryZone");
      if (inventoryZone != null) {
        var bounds = new Bounds(inventoryZone.transform.position, Vector3.one);
        if (inventoryZone.TryGetComponent<Collider2D>(out var collider)) {
          bounds = collider.bounds;
        }
        var randomOffset = new Vector3(
            UnityEngine.Random.Range(-bounds.size.x * 0.4f, bounds.size.x * 0.4f),
            UnityEngine.Random.Range(-bounds.size.y * 0.4f, bounds.size.y * 0.4f),
            0);
        element.transform.position = bounds.center + randomOffset;
        Debug.Log($"[LevelGrid] Used fallback positioning for {element.name}");
      }
      else {
        element.transform.position = ELEMENT_DEFAULT_POSITION;
      }
    }

    protected virtual void HandleInventoryElementAdded(InventoryEventData data) {
      if (data.Success) {
        Debug.Log($"[LevelGrid] Element successfully added to inventory: {data.ElementName} " +
                 $"(Available: {data.AvailableQuantity}/{data.TotalQuantity}) from {data.SourceSystem}");

        // Element was successfully added to inventory and destroyed by LevelInventory
        // We can use the rich context for additional logic
        if (data.WasInGrid) {
          Debug.Log($"[LevelGrid] Element was moved from grid to inventory");
        }
      }
      else {
        Debug.LogWarning($"[LevelGrid] Failed to add element to inventory: {data.Message}");
        // Fallback to old positioning if inventory add failed
        if (data.Element != null) {
          HandleElementReturnToInventoryFallback(data.Element);
        }
      }
    }

    protected virtual void HandleInventoryElementRetrieved(InventoryEventData data) {
      if (data.Success && data.Element != null) {
        Debug.Log($"[LevelGrid] Element retrieved from inventory: {data.ElementName} " +
                 $"(Remaining: {data.AvailableQuantity}/{data.TotalQuantity}) by {data.SourceSystem}");

        // Position the retrieved element based on context
        PositionRetrievedElement(data.Element, data);
      }
      else {
        Debug.LogWarning($"[LevelGrid] Failed to retrieve element from inventory: {data.Message}");
      }
    }

    protected virtual void HandleInventoryOperationFailed(InventoryEventData data) {
      Debug.LogWarning($"[LevelGrid] Inventory operation failed: {data.Operation} | {data.Message} | Source: {data.SourceSystem}");

      // Handle specific failure cases
      switch (data.Operation) {
        case InventoryOperation.Add:
          if (data.Element != null) {
            HandleElementReturnToInventoryFallback(data.Element);
          }
          break;
        case InventoryOperation.Retrieve:
          // Maybe show UI feedback that element is not available
          break;
      }
    }

    protected virtual void HandleInventoryUpdated(InventoryEventData data) {
      Debug.Log($"[LevelGrid] Inventory updated: {data.Message} | Source: {data.SourceSystem}");
      // Could trigger UI updates or other systems that care about inventory state
    }

    protected virtual void PositionRetrievedElement(GridSnappable element, InventoryEventData context) {
      // Use context to make intelligent positioning decisions
      Vector3 targetPosition;

      if (context.WorldPosition != Vector3.zero) {
        // Use the original world position if available
        targetPosition = context.WorldPosition;
      }
      else {
        // Fallback to inventory zone position
        var inventoryZone = GameObject.Find("InventoryZone");
        targetPosition = inventoryZone != null ? inventoryZone.transform.position : ELEMENT_DEFAULT_POSITION;
      }

      element.transform.position = targetPosition;
      element.Draggable = true;

      Debug.Log($"[LevelGrid] Positioned retrieved element {element.name} at {targetPosition} " +
               $"(Category: {context.CategoryId}, Source: {context.SourceSystem})");
    }
    #endregion

    #region Element Movement Event Handlers

    protected virtual void HandleElementSelected(GridSnappableEventArgs args) {
      // This now just collects element initial world and grid positions - tracking starts in Update()
      if (args is GridSnappableEventArgs e && e.Element != null) {
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
    protected virtual void HandleElementDropped(GridSnappableEventArgs args) {

      if (args is GridSnappableEventArgs e && e.Element != null) {


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
    }

    // To decouple GridSnappable behaviour from the grid, this method only notifies the GridSnappable to handle the 'unhover'
    //that way we prevent from polling the mouse position from snappables on Update
    private void HandleElementUnhovered(EventArgs args) {
      if (args is GridSnappableEventArgs e && e.Element != null)
        e.Element.OnPointerOut();
    }

    // This method only delegates to the GridSnappable to handle the 'hover'
    //that way we prevent from polling the mouse position from snappables on Update
    protected virtual void HandleElementHovered(EventArgs args) {
      if (args is GridSnappableEventArgs e && e.Element != null)
        e.Element.OnPointerOver();
    }

    #endregion

    #region Public API for Grid and Position 

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
    public virtual Vector2 SnapToGrid(Vector2 position) {
      Vector2Int index = GetGridIndex(position);
      float x = index.x * CellSize + _gridOrigin.x;
      float y = index.y * CellSize + _gridOrigin.y;
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

    public Vector2 GridToWorld(Vector2Int cell) {
      float x = cell.x * CellSize + _gridOrigin.x;
      float y = cell.y * CellSize + _gridOrigin.y;
      return new Vector2(x, y);
    }

    /// <summary>
    /// Common World Coordinates to Grid Coordinates method
    /// </summary>
    public Vector2Int GetGridIndex(Vector2 position) {
      int x = Mathf.RoundToInt((position.x - _gridOrigin.x) / CellSize);
      int y = Mathf.RoundToInt((position.y - _gridOrigin.y) / CellSize);
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
        Vector3 start = new(_gridOrigin.x + x * gizmoCellSize, _gridOrigin.y - halfHeight * gizmoCellSize, 0f);
        Vector3 end = new(_gridOrigin.x + x * gizmoCellSize, _gridOrigin.y + halfHeight * gizmoCellSize, 0f);
        Gizmos.DrawLine(gameObject.transform.position + start, gameObject.transform.position + end);
      }

      for (int y = -halfHeight; y <= halfHeight; y++) {
        Vector3 start = new(_gridOrigin.x - halfWidth * gizmoCellSize, _gridOrigin.y + y * gizmoCellSize, 0f);
        Vector3 end = new(_gridOrigin.x + halfWidth * gizmoCellSize, _gridOrigin.y + y * gizmoCellSize, 0f);
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