using Ameba;
using System;
using System.Collections.Generic;
using System.Linq;


#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;

namespace GMTK {

  public enum GridOriginSources { GameObject, Custom }

  public enum LevelGridState { Idle, Tracking, Placing }
  public class LevelGrid : MonoBehaviour {

    [Header("Grid Dimensions")]
    [Tooltip("The size in units for the gridCellPosition. Recommended is 1")]
    public float CellSize = 1f; // Matches the peg sprite spacing
    [Tooltip("The number of cells in the grid. Only positive integer numbers")]
    public Vector2Int GridSize = new(50, 34);
    [Tooltip("Whether the grid origin should be taken from the Grid's GameObject or a specific world worldPosition")]
    public GridOriginSources OriginSource;
    [Tooltip("If OriginSource is 'Custom', this field is the world worldPosition of the center of the grid")]
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
    [SerializeField] protected PlayableElementInputHandler _inputHandler;

    [Header("UnityEvents")]
    [Space]
    public UnityEvent<PlayableElement> OnElementTrackingStart = new();
    public UnityEvent<PlayableElement> OnElementTrackingEnd = new();
    public UnityEvent<PlayableElement> OnElementPlaced = new();
    
    [Space(10)]

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

    [Header("Debug Window")]
    [SerializeField] private bool enableDebugWindow = true;
    [SerializeField] private bool showDebugWindowInGame = false;
    [SerializeField] private Vector2 debugWindowPosition = new(10, 10);
    [SerializeField] private float debugWindowWidth = 400f;
    [SerializeField] private int debugWindowMaxElements = 20;
    [SerializeField] private Color debugWindowBackgroundColor = new(0.1f, 0.1f, 0.1f, 0.8f);
    [SerializeField] private Color debugWindowTextColor = Color.white;
    [SerializeField] private Color debugWindowHeaderColor = Color.cyan;

    // events channel
    protected GameEventChannel _eventsChannel;

    // element tracking
    private bool _isInitialized = false;
    protected Vector2 _gridOrigin = Vector2.zero;
    protected GridOccupancyMap _occupancyMap;
    protected PlayableElement _trackedElement;
    protected Vector2 _elementOriginalWorldPosition;
    protected Vector2Int _elementOriginalGridPosition;
    protected bool _elementWasInGrid;
    protected bool _isTrackingMovement;

    // State management
    protected LevelGridState _currentState = LevelGridState.Idle;
    protected bool _canPlaceCurrentElement = false;
    protected List<Vector2Int> _currentElementOccupiedCells = new();
    protected Vector2Int _currentElementGridPosition = Vector2Int.zero;

    // Public Getters
    public Vector2 GridOrigin => _gridOrigin;
    public LevelGridState CurrentState => _currentState;
    public bool CanPlaceCurrentElement => _canPlaceCurrentElement;

    // Debug window GUI styles
    private GUIStyle _debugWindowStyle;
    private GUIStyle _debugHeaderStyle;
    private GUIStyle _debugTextStyle;
    private bool _debugStylesInitialized = false;

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

    //private void Update() {
    //  TrackElementMovement();
    //}
    private void Update() {
      switch (_currentState) {
        case LevelGridState.Idle:
          // Check if we should start tracking
          CheckForTrackingStart();
          break;

        case LevelGridState.Tracking:
          // Update tracking logic
          UpdateElementTracking();
          break;

        case LevelGridState.Placing:
          // Placing state is handled by event handlers
          // This state is typically very short-lived
          break;
      }
    }

    private void CheckForTrackingStart() {
      // Check if input handler has an element moving
      if (_inputHandler.CurrentElement != null && _inputHandler.IsMoving) {
        var currentElement = _inputHandler.CurrentElement;

        // If this is a new element being tracked or different from current
        if (_trackedElement == null || _trackedElement != currentElement) {
          StartTrackingElement(currentElement);
          ChangeState(LevelGridState.Tracking);
        }
      }
    }

    private void UpdateElementTracking() {
      // Verify we're still tracking
      if (_inputHandler.CurrentElement == null || !_inputHandler.IsMoving) {
        // No longer tracking - return to idle
        ChangeState(LevelGridState.Idle);
        return;
      }

      // Continue tracking current element
      if (_trackedElement != null) {
        UpdateElementPlacementValidation();
      }
    }

    #endregion

    #region Initialization

    protected virtual void Initialize() {
      _isInitialized = false;
      if (_inputHandler == null) {
        this.LogWarning($"PlayableElementInputHandler is missing. LevelGrid will not be able to track player inputs on Elements");
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
      _isInitialized = true;
    }

    protected virtual void InitializeGrid() {

      //TODO (optional) make maxOccupantsPerCell and mode, parameters of the GridOccupancyMap
      _occupancyMap = new GridOccupancyMap(CellSize, _gridOrigin,
        maxOccupantsPerCell: 3,
        mode: CellLayeringOrder.LastToFirst);

      var allOccupants = FindObjectsByType<PlayableElement>(FindObjectsSortMode.None);
      //Elements in the playing area at the time of initializing the grid
      //are considered non-draggable -> player cannot move them
      this.Log($"Initializing Grid for {allOccupants.Length} occupants");
      foreach (var occupant in allOccupants) {
        if (IsInsidePlayableArea(occupant.transform.position)) {
          occupant.SnapTransform.position = SnapToGrid(occupant.SnapTransform.position);
          occupant.Draggable = false;
          var gridOrigin = WorldToGrid(occupant.SnapTransform.position);
          _occupancyMap.Register(occupant, gridOrigin);
        }
      }
      this.Log($"Grid Initialized with {_occupancyMap.OccupantsCount} occupants");
      _gridSprite = (_gridSprite == null) ? GetComponent<SpriteRenderer>() : _gridSprite;
    }

    #endregion

    #region Grid Bounds
    protected virtual void InitializeAllEdgeColliderBounds() {
      if (GridSize.x <= 0 || GridSize.y <= 0) {
        this.LogError($"GridSize must be a positive number: {GridSize}");
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
      if(boundCollider == null) {
        this.LogWarning($"EdgeCollider is null for tag '{tag}'");
        return null;
      }
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
      _eventsChannel.AddListener<PlayableElementEventArgs>(GameEventType.ElementDragging, HandleElementDragging);
      _eventsChannel.AddListener<PlayableElementEventArgs>(GameEventType.ElementSelected, HandleElementSelected);
      _eventsChannel.AddListener<PlayableElementEventArgs>(GameEventType.ElementDropped, HandleElementDropped);
      //_eventsChannel.AddListener<PlayableElementEventArgs>(GameEventType.ElementHovered, HandleElementHovered);
      //_eventsChannel.AddListener<PlayableElementEventArgs>(GameEventType.ElementUnhovered, HandleElementUnhovered);
    }

    private void RemoveInputListeners() {
      if (_eventsChannel == null) return;
      _eventsChannel.RemoveListener<PlayableElementEventArgs>(GameEventType.ElementDragging, HandleElementDragging);
      _eventsChannel.RemoveListener<PlayableElementEventArgs>(GameEventType.ElementSelected, HandleElementSelected);
      _eventsChannel.RemoveListener<PlayableElementEventArgs>(GameEventType.ElementDropped, HandleElementDropped);
      //_eventsChannel.RemoveListener<PlayableElementEventArgs>(GameEventType.ElementHovered, HandleElementHovered);
      //_eventsChannel.RemoveListener<PlayableElementEventArgs>(GameEventType.ElementUnhovered, HandleElementUnhovered);
    }

    #endregion

    #region Track Element Movements

    //private void TrackElementMovement() {
    //  // Check if input handler has an element moving
    //  if (_inputHandler.CurrentElement != null && _inputHandler.IsMoving) {
    //    var currentElement = _inputHandler.CurrentElement;

    //    // If this is a new element being tracked, stop tracking current
    //    // and begin tracking new
    //    // otherwise, we are still tracking the same Element
    //    if (_trackedElement == null || _trackedElement != currentElement) {
    //      StopTrackingCurrentSelected();
    //      StartTrackingElement(currentElement);
    //    }

    //  }
    //  else {
    //    //inputhandler is not moving and we are tracking -> we need to stop tracking currentSelected
    //    if (_isTrackingMovement) {
    //      StopTrackingCurrentSelected();
    //    }
    //  }
    //}

    private void StartTrackingElement(PlayableElement element) {
      _trackedElement = element;
      _isTrackingMovement = true;

      // Store original worldPosition and check if it was in the grid
      var currentPosition = element.SnapTransform.position;
      _elementWasInGrid = _occupancyMap.ContainsElement(element);

      if (_elementWasInGrid) {
        _elementOriginalGridPosition = WorldToGrid(currentPosition);
        // Unregister from grid while moving to avoid conflicts
        _occupancyMap.Unregister(element, _elementOriginalGridPosition);
        this.Log($"Started tracking '{element.name}' - unregistered from ({_elementOriginalGridPosition})");
      }
      else {
        this.Log($"Started tracking '{element.name}' - was not in grid");
      }

      //// Notify _dragFeedbackComponent to start visual feedback
      //if (element.TryGetComponent<DragFeedbackComponent>(out var dragFeedback)) {
      //  dragFeedback.StartDragFeedback();
      //}
    }

    //private void StopTrackingCurrentSelected() {
    //  // Notify _dragFeedbackComponent to stop visual feedback
    //  //if (_trackedElement != null) {
    //  //  if (_trackedElement.TryGetComponent<DragFeedbackComponent>(out var dragFeedback)) {
    //  //    dragFeedback.StopDragFeedback();
    //  //  }
    //  //}
    //  _trackedElement = null;
    //  _isTrackingMovement = false;
    //  _elementWasInGrid = false;
    //}

    #endregion

    #region Element Movement Event Handlers

    protected virtual void HandleElementDragging(PlayableElementEventArgs args) {
      // Currently handled in Update() method
      if (_currentState == LevelGridState.Tracking) UpdateElementTracking();
    }

    protected virtual void HandleElementSelected(PlayableElementEventArgs args) {
      if (args.Element is PlayableElement element) {
        // Store initial data for tracking
        _elementOriginalWorldPosition = element.SnapTransform.position;
        _elementWasInGrid = _occupancyMap.ContainsElement(element);

        if (_elementWasInGrid) {
          _elementOriginalGridPosition = WorldToGrid(_elementOriginalWorldPosition);
        }

        this.Log($"Element '{element.name}' selected at {_elementOriginalWorldPosition}");
        element.OnSelect();

        // State remains Idle until dragging begins
      }
    }

    protected virtual void HandleElementDropped(PlayableElementEventArgs args) {
      if (args.Element is PlayableElement element) {
        var elemModel = element.Model.transform;
        var newGridOrigin = WorldToGrid(elemModel.position);
        // Change to Placing state
        ChangeState(LevelGridState.Placing);
        // Try to place at new worldPosition
        if (IsInsidePlayableArea(elemModel.position)) {
          if (CanPlace(element, newGridOrigin)) {
            // Success - place at new worldPosition
            elemModel.position = GridToWorld(newGridOrigin);
            _occupancyMap.Register(element, newGridOrigin);
            this.Log($"Placed {element.name} at {newGridOrigin}");
            OnElementPlaced?.Invoke(element);
          }
          else {
            // Failed to place - return to original worldPosition
            ReturnElementToOriginalPosition(element);
          }
        }
        else {
          // Outside playable area - return to original worldPosition
          ReturnElementToOriginalPosition(element);
        }

        // Clean up tracking and return to Idle
        CleanupTracking();
        ChangeState(LevelGridState.Idle);
      }
    }
    //protected virtual void HandleElementSelected(PlayableElementEventArgs args) {
    //  // This now just collects element initial world and grid positions - tracking starts in Update()
    //  if (args.Element is PlayableElement element) {
    //    //var element = args.Element;
    //    //_trackedElement = e.Element;
    //    _elementOriginalWorldPosition = element.transform.worldPosition;
    //    _elementWasInGrid = _occupancyMap.ContainsElement(element);
    //    if (_elementWasInGrid) {
    //      _elementOriginalGridPosition = WorldToGrid(_elementOriginalWorldPosition);
    //    }
    //    this.Log($"Element '{element.name}' selected at {_elementOriginalWorldPosition}");
    //    if (_elementWasInGrid) this.Log($"Element '{element.name}' at grid {_elementOriginalGridPosition}");
    //    element.OnSelect();
    //  }
    //}
    //protected virtual void HandleElementDropped(PlayableElementEventArgs args) {

    //  if (args.Element is PlayableElement element) {
    //    //var element = args.Element;
    //    var newGridOrigin = WorldToGrid(element.transform.worldPosition);

    //    // Try to place at new worldPosition
    //    if (IsInsidePlayableArea(element.transform.worldPosition)) {

    //      // Success - place at new worldPosition
    //      if (CanPlace(element, newGridOrigin)) {
    //        element.transform.worldPosition = SnapToGrid(newGridOrigin);
    //        _occupancyMap.Register(element, newGridOrigin);
    //        this.Log($"Placed {element.name} at {newGridOrigin}");
    //      }
    //      else {
    //        // Failed to place - return to original worldPosition if it was in grid
    //        if (_occupancyMap.ContainsElement(element)) {

    //          element.transform.worldPosition = SnapToGrid(_elementOriginalGridPosition);
    //          _occupancyMap.Register(element, _elementOriginalGridPosition);
    //          this.Log($"Returned '{element.name}' to original worldPosition {_elementOriginalGridPosition}");
    //        }
    //        // Element came from outside grid - return to inventory
    //        else {
    //          //HandleElementReturnToInventory(element);
    //          this.LogDebug($"Returned {element.name} to inventory");
    //          element.transform.worldPosition = SnapToGrid(_elementOriginalGridPosition);
    //        }
    //      }

    //      // Check if this was the element we were tracking
    //      if (element == _trackedElement) {
    //        this.Log($"Dropping tracked element '{element.name}' at {newGridOrigin}");
    //        // Clean up tracking
    //        StopTrackingCurrentSelected();
    //      }
    //      else {
    //        // Element wasn't being tracked (probably just clicked)
    //        this.Log($"Element '{element.name}' clicked but not moved");
    //      }
    //    }
    //  }
    //}

    //private void HandleElementUnhovered(PlayableElementEventArgs args) {
    //  if (args.Element is PlayableElement element) element.OnUnhover();//.OnUnhovered();
    //}

    //protected virtual void HandleElementHovered(PlayableElementEventArgs args) {
    //  if (args.Element is PlayableElement element) element.OnHover(); //.OnHovered();
    //}

    #endregion

    #region State Management

    private void ChangeState(LevelGridState newState) {
      if (_currentState == newState) return;

      this.Log($"LevelGrid state changing from {_currentState} to {newState}");
      _currentState = newState;

      // Handle state-specific setup
      switch (newState) {
        case LevelGridState.Idle:
          OnEnterIdleState();
          break;
        case LevelGridState.Tracking:
          OnEnterTrackingState();
          break;
        case LevelGridState.Placing:
          OnEnterPlacingState();
          break;
      }
    }

    private void OnEnterIdleState() {
      // Clean up any tracking data
      _canPlaceCurrentElement = false;
      _currentElementOccupiedCells.Clear();
    }

    private void OnEnterTrackingState() {
      // Initialize tracking validation
      if (_trackedElement != null) {
        UpdateElementPlacementValidation();
        OnElementTrackingStart?.Invoke(_trackedElement);
      }
    }

    private void OnEnterPlacingState() {
      // Placing state setup (if needed)
      // This state is typically handled by the drop event handler
    }

    #endregion

    #region Element Placement

    private void UpdateElementPlacementValidation() {
      if (_trackedElement == null) {
        _canPlaceCurrentElement = false;
        _currentElementOccupiedCells.Clear();
        return;
      }

      // Calculate current grid worldPosition
      _currentElementGridPosition = WorldToGrid(_trackedElement.SnapTransform.position);

      // Get the cells this element would occupy
      _currentElementOccupiedCells.Clear();
      foreach (var cell in _trackedElement.GetWorldOccupiedCells(_currentElementGridPosition)) {
        _currentElementOccupiedCells.Add(cell);
      }

      // Check if we can place at current worldPosition
      _canPlaceCurrentElement = IsInsidePlayableArea(_trackedElement.SnapTransform.position) &&
                               CanPlace(_trackedElement, _currentElementGridPosition);

      this.LogDebug($"Element '{_trackedElement.name}' at grid {_currentElementGridPosition} - Can place: {_canPlaceCurrentElement}");
    }

    private void ReturnElementToOriginalPosition(PlayableElement element) {
      if (_elementWasInGrid) {
        element.SnapTransform.position = SnapToGrid(_elementOriginalGridPosition);
        _occupancyMap.Register(element, _elementOriginalGridPosition);
        this.Log($"Returned '{element.name}' to original worldPosition {_elementOriginalGridPosition}");
      }
      else {
        // Element came from outside grid - return to original world worldPosition
        element.transform.position = _elementOriginalWorldPosition;
        this.Log($"Returned '{element.name}' to default worldPosition");
      }
    }
    private void CleanupTracking() {

      OnElementTrackingEnd?.Invoke(_trackedElement);

      _trackedElement = null;
      _isTrackingMovement = false;
      _elementWasInGrid = false;
      _canPlaceCurrentElement = false;
      _currentElementOccupiedCells.Clear();
    }

    #endregion

    #region Public API for Grid and Position 

    public virtual bool IsInsidePlayableArea(Vector2 position) {

      if (GridTopBound == null || GridBottomBound == null || GridLeftBound == null || GridRightBound == null) {
        this.LogWarning("One or more grid boundary colliders are not assigned.");
        return false;
      }
      return (position.y <= GridTopBound.bounds.max.y) &&
              (position.y >= GridBottomBound.bounds.min.y) &&
              (position.x >= GridLeftBound.bounds.min.x) &&
              (position.x <= GridRightBound.bounds.max.x);
    }
    public bool IsOccupied(Vector2 position) => _occupancyMap.HasAnyOccupantsInWorldPosition(position);

    public virtual bool CanPlace(PlayableElement element, Vector2Int gridOrigin) {
      if (!_isInitialized) return false;
      foreach (var cell in element.GetWorldOccupiedCells(gridOrigin)) {
        if (_occupancyMap.HasAnyOccupantsInWorldPosition(cell)) {
          this.LogDebug($"CanPlace gridCellPosition '{cell}' has occupants");
          return false;
        }
      }
      this.LogDebug($"CanPlace {element.name} can be placed at {gridOrigin}");
      return true;
    }

    /// <summary>
    /// Returns a world position based on 'position' parameter, adjusted to the nearest Grid position.<br/>
    /// This method returns a new Vector2 instance, making
    /// usage of <see cref="GridToWorld(Vector2Int)"/> to resolve the returned value.
    /// </summary>
    /// <param name="position">Vector2 with the initial world position</param>
    /// <returns>Vector2 with the world position of 'position' adjusted to the nearest Grid coordinates</returns>
    public virtual Vector2 SnapToGrid(Vector2 position) => GridToWorld(GetGridIndex(position));
    //  {
    //  Vector2Int index = GetGridIndex(position);
    //  float x = index.x * CellSize + _gridOrigin.x;
    //  float y = index.y * CellSize + _gridOrigin.y;
    //  return new Vector2(x, y);
    //}

    /// <summary>
    /// Returns the Grid position that corresponds to the world position specified in 'worldPosition'.<br/>
    /// This method is similar to <see cref="GetGridIndex(Vector2)"/> in the sense that return nearest Grid coordinates.
    /// </summary>
    /// <param name="worldPosition"></param>
    /// <returns>Vector2Int with the nearest Grid coordinates to 'worldPosition'</returns>
    public virtual Vector2Int WorldToGrid(Vector2 worldPosition) {
      return GetGridIndex(worldPosition);
    }

    /// <summary>
    /// Returns the world position that would correspond to the Grid position specified in 'gridCellPosition'.<br/>
    /// This method is guaranteed to return a world position that matches the grid position.<br/>
    /// The conversion uses the fields <see cref="GridSize"/> and <see cref="_gridOrigin"/>.
    /// </summary>
    /// <param name="gridCellPosition"></param>
    /// <returns>New Vector2 with the world position of 'gridCellPosition'</returns>
    public Vector2 GridToWorld(Vector2Int gridCellPosition) {
      float x = gridCellPosition.x * CellSize + _gridOrigin.x;
      float y = gridCellPosition.y * CellSize + _gridOrigin.y;
      return new Vector2(x, y);
    }

    /// <summary>
    /// Returns the Grid coordinates that would correspond to the World Coordinates in 'worldPosition'.<br/>
    /// This method will look for the nearest Grid coordinate, meaning the result is not guaranteed to match the original values of 'worldPosition'.<br/>
    /// The conversion uses the fields <see cref="GridSize"/> and <see cref="_gridOrigin"/>.
    /// </summary>
    private Vector2Int GetGridIndex(Vector2 worldPosition) {
      int x = Mathf.RoundToInt((worldPosition.x - _gridOrigin.x) / CellSize);
      int y = Mathf.RoundToInt((worldPosition.y - _gridOrigin.y) / CellSize);
      return new Vector2Int(x, y);
    }
    #endregion

    #region Debug Window

#if UNITY_EDITOR
    private void OnGUI() {
      if (enableDebugWindow && (Application.isEditor || showDebugWindowInGame)) {
        InitializeDebugWindowStyles();
        DrawDebugWindow();
      }
    }
#endif

#if UNITY_EDITOR
    private void InitializeDebugWindowStyles() {
      if (_debugStylesInitialized) return;

      _debugWindowStyle = new GUIStyle(GUI.skin.box) {
        normal = { background = MakeTex(2, 2, debugWindowBackgroundColor) },
        padding = new RectOffset(10, 10, 10, 10)
      };

      _debugHeaderStyle = new GUIStyle(GUI.skin.label) {
        fontStyle = FontStyle.Bold,
        fontSize = 14,
        normal = { textColor = debugWindowHeaderColor },
        alignment = TextAnchor.MiddleLeft
      };

      _debugTextStyle = new GUIStyle(GUI.skin.label) {
        fontSize = 12,
        normal = { textColor = debugWindowTextColor },
        alignment = TextAnchor.MiddleLeft,
        wordWrap = false
      };

      _debugStylesInitialized = true;
    }

    private void DrawDebugWindow() {
      if (!_isInitialized || _occupancyMap == null) return;

      // Calculate window height based on content
      var registeredElements = GetRegisteredElements();
      int displayCount = Mathf.Min(registeredElements.Count, debugWindowMaxElements);
      float windowHeight = 120f + (displayCount * 20f) + (registeredElements.Count > debugWindowMaxElements ? 20f : 0f);

      Rect windowRect = new(
        debugWindowPosition.x,
        debugWindowPosition.y,
        debugWindowWidth,
        windowHeight
      );

      GUI.Box(windowRect, "", _debugWindowStyle);

      GUILayout.BeginArea(new Rect(windowRect.x + 10, windowRect.y + 10, windowRect.width - 20, windowRect.height - 20));

      // Header
      GUILayout.Label("LevelGrid Debug Info", _debugHeaderStyle);
      GUILayout.Space(5);

      // Grid info
      GUILayout.Label($"Grid Size: {GridSize.x} × {GridSize.y}", _debugTextStyle);
      GUILayout.Label($"Cell Size: {CellSize:F1}", _debugTextStyle);
      GUILayout.Label($"Grid Origin: ({_gridOrigin.x:F1}, {_gridOrigin.y:F1})", _debugTextStyle);
      GUILayout.Label($"Total Registered Elements: {_occupancyMap.OccupantsCount}", _debugTextStyle);

      // Add state information
      GUILayout.Label($"Current State: {_currentState}", _debugHeaderStyle);

      if (_trackedElement != null) {
        GUILayout.Label($"Tracking: {_trackedElement.name}", _debugTextStyle);
        GUILayout.Label($"Can Place: {_canPlaceCurrentElement}", _debugTextStyle);
        GUILayout.Label($"Grid Position: ({_currentElementGridPosition.x}, {_currentElementGridPosition.y})", _debugTextStyle);

        if (_currentElementOccupiedCells.Count > 0) {
          string cellsStr = string.Join(", ", _currentElementOccupiedCells.Select(c => $"({c.x},{c.y})"));
          GUILayout.Label($"Occupied Cells: {cellsStr}", _debugTextStyle);
        }
      }

      GUILayout.Space(10);
      GUILayout.Label("Registered PlayableElements:", _debugHeaderStyle);
      GUILayout.Space(5);

      // Show registered elements
      if (registeredElements.Count == 0) {
        GUILayout.Label("No elements registered", _debugTextStyle);
      }
      else {
        for (int i = 0; i < displayCount; i++) {
          var kvp = registeredElements[i];
          Vector2Int cell = kvp.Key;
          var elements = kvp.Value;

          string elementNames = string.Join(", ", elements.Select(e => e.name));
          Vector2 worldPos = GridToWorld(cell);

          GUILayout.Label($"Cell ({cell.x:+00;-00}, {cell.y:+00;-00}) → World ({worldPos.x:F1}, {worldPos.y:F1}): {elementNames}", _debugTextStyle);
        }

        if (registeredElements.Count > debugWindowMaxElements) {
          GUILayout.Label($"... and {registeredElements.Count - debugWindowMaxElements} more elements", _debugTextStyle);
        }
      }

      GUILayout.EndArea();
    }

    private List<KeyValuePair<Vector2Int, List<PlayableElement>>> GetRegisteredElements() {
      var result = new List<KeyValuePair<Vector2Int, List<PlayableElement>>>();

      if (_occupancyMap?.GetAllCells() == null) return result;

      foreach (var kvp in _occupancyMap.GetAllCells()) {
        var cell = kvp.Key;
        var occupancyCell = kvp.Value;

        if (occupancyCell.HasAnyOccupant) {
          var elements = occupancyCell.GetOccupants().ToList();
          result.Add(new KeyValuePair<Vector2Int, List<PlayableElement>>(cell, elements));
        }
      }

      // Sort by grid coordinates for consistent display
      result.Sort((a, b) => {
        int yCompare = b.Key.y.CompareTo(a.Key.y); // Sort by Y descending (top to bottom)
        return yCompare != 0 ? yCompare : a.Key.x.CompareTo(b.Key.x); // Then by X ascending (left to right)
      });

      return result;
    }

    private Texture2D MakeTex(int width, int height, Color col) {
      Color[] pix = new Color[width * height];
      for (int i = 0; i < pix.Length; i++) {
        pix[i] = col;
      }

      Texture2D result = new(width, height);
      result.SetPixels(pix);
      result.Apply();
      return result;
    }
#endif

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