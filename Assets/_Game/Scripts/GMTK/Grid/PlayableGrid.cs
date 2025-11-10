using Ameba;
using MoreMountains.Feedbacks;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace GMTK {

  /// <summary>
  /// MonoBehaviour that manages a playable grid for placing and manipulating PlayableElements.
  /// Listens to element events from GameEventsChannel and provides feedback through Unity events and Feel Feedbacks.
  /// </summary>
  [AddComponentMenu("GMTK/Playable Grid")]
  public class PlayableGrid : MonoBehaviour {

    [Header("Grid Configuration")]
    [Tooltip("Number of rows in the grid")]
    [SerializeField] private int _rows = 10;
    
    [Tooltip("Number of columns in the grid")]
    [SerializeField] private int _columns = 10;
    
    [Tooltip("Size of each tile in world units")]
    [SerializeField] private float _tileSize = 1.0f;

    [Tooltip("World position offset for the grid origin")]
    [SerializeField] private Vector2 _gridOrigin = Vector2.zero;

    [Header("Events")]
    [Tooltip("Invoked when an element is successfully added to the grid")]
    public UnityEvent<PlayableElement> OnElementAdded = new();
    
    [Tooltip("Invoked when an element is removed from the grid")]
    public UnityEvent<PlayableElement> OnElementRemoved = new();

    [Header("Feedbacks")]
    [Tooltip("Feedback played when an element moves over the grid")]
    public MMF_Player OnElementOverGridFeedback;
    
    [Tooltip("Feedback played when an element is picked from the grid")]
    public MMF_Player OnElementPickedFeedback;
    
    [Tooltip("Feedback played when an element is successfully dropped on the grid")]
    public MMF_Player OnElementDroppedSuccessFeedback;
    
    [Tooltip("Feedback played when an element drop is invalid")]
    public MMF_Player OnElementDroppedInvalidFeedback;

    // Private fields
    private AmebaGrid _grid;
    private GameEventChannel _eventsChannel;
    private PlayableElement _trackedElement;
    private Vector2Int? _trackedElementOriginalPosition;
    private bool _canPlaceTrackedElement;

    #region MonoBehaviour Methods

    private void Awake() {
      InitializeGrid();
      _eventsChannel = ServiceLocator.Get<GameEventChannel>();
      
      if (_eventsChannel != null) {
        AddEventListeners();
      }
    }

    private void OnDestroy() {
      if (_eventsChannel != null) {
        RemoveEventListeners();
      }
    }

    private void OnValidate() {
      // Ensure valid grid dimensions
      _rows = Mathf.Max(1, _rows);
      _columns = Mathf.Max(1, _columns);
      _tileSize = Mathf.Max(0.1f, _tileSize);
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the internal grid structure.
    /// </summary>
    private void InitializeGrid() {
      try {
        _grid = new AmebaGrid(_rows, _columns, _tileSize);
        this.Log($"PlayableGrid initialized with {_rows}x{_columns} grid, tile size {_tileSize}");
      }
      catch (Exception ex) {
        this.LogError($"Failed to initialize grid: {ex.Message}");
      }
    }

    #endregion

    #region Event Listeners

    private void AddEventListeners() {
      _eventsChannel.AddListener<PlayableElementEventArgs>(GameEventType.ElementSelected, OnElementSelected);
      _eventsChannel.AddListener<PlayableElementEventArgs>(GameEventType.ElementDragging, OnElementDragging);
      _eventsChannel.AddListener<PlayableElementEventArgs>(GameEventType.ElementDropped, OnElementDropped);
      _eventsChannel.AddListener<PlayableElementEventArgs>(GameEventType.ElementHovered, OnElementHovered);
    }

    private void RemoveEventListeners() {
      _eventsChannel.RemoveListener<PlayableElementEventArgs>(GameEventType.ElementSelected, OnElementSelected);
      _eventsChannel.RemoveListener<PlayableElementEventArgs>(GameEventType.ElementDragging, OnElementDragging);
      _eventsChannel.RemoveListener<PlayableElementEventArgs>(GameEventType.ElementDropped, OnElementDropped);
      _eventsChannel.RemoveListener<PlayableElementEventArgs>(GameEventType.ElementHovered, OnElementHovered);
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles element selection events.
    /// </summary>
    private void OnElementSelected(PlayableElementEventArgs args) {
      if (args.Element == null) return;
      
      // Check if element is in grid and store its position
      var worldPos = args.Element.GetPosition();
      var gridPos = WorldToGrid(worldPos);
      
      if (TryGetAs<PlayableElement>(gridPos.x, gridPos.y, out PlayableElement element) && element == args.Element) {
        _trackedElement = args.Element;
        _trackedElementOriginalPosition = gridPos;
        
        // Remove from grid while being dragged
        Remove(gridPos.x, gridPos.y);
        
        PlayFeedback(OnElementPickedFeedback);
        this.LogDebug($"Element '{args.Element.name}' picked from grid at {gridPos}");
      }
    }

    /// <summary>
    /// Handles element dragging events to validate placement.
    /// </summary>
    private void OnElementDragging(PlayableElementEventArgs args) {
      if (args.Element == null || _trackedElement != args.Element) return;
      
      var worldPos = args.WorldPosition;
      _canPlaceTrackedElement = CanPlaceElement(args.Element, worldPos);
      
      if (_canPlaceTrackedElement) {
        PlayFeedback(OnElementOverGridFeedback);
      }
    }

    /// <summary>
    /// Handles element drop events.
    /// </summary>
    private void OnElementDropped(PlayableElementEventArgs args) {
      if (args.Element == null) return;
      
      var worldPos = args.Element.GetPosition();
      var gridPos = WorldToGrid(worldPos);
      
      if (CanPlaceElement(args.Element, worldPos)) {
        // Snap to grid and add
        var snappedPos = GridToWorld(gridPos);
        args.Element.UpdatePosition(snappedPos);
        
        Add(gridPos.x, gridPos.y, args.Element);
        
        PlayFeedback(OnElementDroppedSuccessFeedback);
        OnElementAdded?.Invoke(args.Element);
        
        this.LogDebug($"Element '{args.Element.name}' placed at grid position {gridPos}");
      }
      else {
        // Invalid placement - return to original position if it was in grid
        if (_trackedElement == args.Element && _trackedElementOriginalPosition.HasValue) {
          var originalPos = _trackedElementOriginalPosition.Value;
          var snappedPos = GridToWorld(originalPos);
          args.Element.UpdatePosition(snappedPos);
          
          Add(originalPos.x, originalPos.y, args.Element);
          
          this.LogDebug($"Element '{args.Element.name}' returned to original position {originalPos}");
        }
        
        PlayFeedback(OnElementDroppedInvalidFeedback);
      }
      
      // Clear tracking
      _trackedElement = null;
      _trackedElementOriginalPosition = null;
      _canPlaceTrackedElement = false;
    }

    /// <summary>
    /// Handles element hover events.
    /// </summary>
    private void OnElementHovered(PlayableElementEventArgs args) {
      // Can be extended for additional hover feedback
    }

    #endregion

    #region Public API - Grid Management

    /// <summary>
    /// Adds a PlayableElement to the grid at the specified position.
    /// </summary>
    /// <param name="x">The X coordinate in the grid.</param>
    /// <param name="y">The Y coordinate in the grid.</param>
    /// <param name="element">The PlayableElement to add.</param>
    /// <returns>The existing element at that position, or null if empty.</returns>
    public PlayableElement Add(int x, int y, PlayableElement element) {
      if (_grid == null) {
        this.LogWarning("Grid not initialized");
        return null;
      }

      var previous = _grid.Add(x, y, element) as PlayableElement;
      
      if (previous != null && previous != element) {
        OnElementRemoved?.Invoke(previous);
      }
      
      if (element != null) {
        OnElementAdded?.Invoke(element);
      }
      
      return previous;
    }

    /// <summary>
    /// Gets the PlayableElement at the specified grid position.
    /// </summary>
    /// <param name="x">The X coordinate in the grid.</param>
    /// <param name="y">The Y coordinate in the grid.</param>
    /// <returns>The PlayableElement at the position, or null if empty.</returns>
    public PlayableElement Get(int x, int y) {
      if (_grid == null) {
        this.LogWarning("Grid not initialized");
        return null;
      }

      return _grid.GetAs<PlayableElement>(x, y);
    }

    /// <summary>
    /// Attempts to get the PlayableElement at the specified grid position.
    /// </summary>
    /// <param name="x">The X coordinate in the grid.</param>
    /// <param name="y">The Y coordinate in the grid.</param>
    /// <param name="element">The PlayableElement if found.</param>
    /// <returns>True if an element exists at the position; otherwise, false.</returns>
    public bool TryGet(int x, int y, out PlayableElement element) {
      element = null;
      
      if (_grid == null) {
        this.LogWarning("Grid not initialized");
        return false;
      }

      return _grid.TryGetAs<PlayableElement>(x, y, out element);
    }

    /// <summary>
    /// Attempts to get an object at the specified grid position as the specified type.
    /// </summary>
    /// <typeparam name="T">The type to cast to.</typeparam>
    /// <param name="x">The X coordinate in the grid.</param>
    /// <param name="y">The Y coordinate in the grid.</param>
    /// <param name="obj">The object if found and castable.</param>
    /// <returns>True if an object exists and can be cast to T; otherwise, false.</returns>
    public bool TryGetAs<T>(int x, int y, out T obj) where T : class {
      obj = null;
      
      if (_grid == null) {
        this.LogWarning("Grid not initialized");
        return false;
      }

      return _grid.TryGetAs<T>(x, y, out obj);
    }

    /// <summary>
    /// Removes the PlayableElement at the specified grid position.
    /// </summary>
    /// <param name="x">The X coordinate in the grid.</param>
    /// <param name="y">The Y coordinate in the grid.</param>
    /// <returns>The removed PlayableElement, or null if empty.</returns>
    public PlayableElement Remove(int x, int y) {
      if (_grid == null) {
        this.LogWarning("Grid not initialized");
        return null;
      }

      var element = _grid.Remove(x, y) as PlayableElement;
      
      if (element != null) {
        OnElementRemoved?.Invoke(element);
      }
      
      return element;
    }

    /// <summary>
    /// Attempts to remove the PlayableElement at the specified grid position.
    /// </summary>
    /// <param name="x">The X coordinate in the grid.</param>
    /// <param name="y">The Y coordinate in the grid.</param>
    /// <param name="element">The removed PlayableElement if found.</param>
    /// <returns>True if an element was removed; otherwise, false.</returns>
    public bool TryRemove(int x, int y, out PlayableElement element) {
      element = null;
      
      if (_grid == null) {
        this.LogWarning("Grid not initialized");
        return false;
      }

      if (_grid.TryRemove(x, y, out object obj)) {
        element = obj as PlayableElement;
        if (element != null) {
          OnElementRemoved?.Invoke(element);
          return true;
        }
      }
      
      return false;
    }

    /// <summary>
    /// Checks if the specified grid position is empty.
    /// </summary>
    /// <param name="x">The X coordinate in the grid.</param>
    /// <param name="y">The Y coordinate in the grid.</param>
    /// <returns>True if the position is empty; otherwise, false.</returns>
    public bool IsEmpty(int x, int y) {
      if (_grid == null) {
        this.LogWarning("Grid not initialized");
        return true;
      }

      return _grid.IsEmpty(x, y);
    }

    /// <summary>
    /// Checks if a PlayableElement can be placed at the given world position.
    /// </summary>
    /// <param name="element">The PlayableElement to check.</param>
    /// <param name="worldPosition">The world position to check.</param>
    /// <returns>True if the element can be placed; otherwise, false.</returns>
    public bool CanPlaceElement(PlayableElement element, Vector2 worldPosition) {
      if (_grid == null || element == null) {
        return false;
      }

      var gridPos = WorldToGrid(worldPosition);
      
      // Check if position is within grid bounds
      if (gridPos.x < 0 || gridPos.x >= _columns || gridPos.y < 0 || gridPos.y >= _rows) {
        return false;
      }

      // Check if the tile is empty
      return IsEmpty(gridPos.x, gridPos.y);
    }

    #endregion

    #region Coordinate Conversion

    /// <summary>
    /// Converts world coordinates to grid coordinates.
    /// </summary>
    /// <param name="worldPosition">The world position.</param>
    /// <returns>The corresponding grid coordinates.</returns>
    public Vector2Int WorldToGrid(Vector2 worldPosition) {
      var localPos = worldPosition - _gridOrigin;
      int x = Mathf.RoundToInt(localPos.x / _tileSize);
      int y = Mathf.RoundToInt(localPos.y / _tileSize);
      return new Vector2Int(x, y);
    }

    /// <summary>
    /// Converts grid coordinates to world coordinates (bottom-left corner of tile).
    /// </summary>
    /// <param name="gridPosition">The grid coordinates.</param>
    /// <returns>The corresponding world position.</returns>
    public Vector2 GridToWorld(Vector2Int gridPosition) {
      float x = gridPosition.x * _tileSize + _gridOrigin.x;
      float y = gridPosition.y * _tileSize + _gridOrigin.y;
      return new Vector2(x, y);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Plays a Feel Feedback if it's assigned.
    /// </summary>
    private void PlayFeedback(MMF_Player feedback) {
      if (feedback != null) {
        feedback.PlayFeedbacks();
      }
    }

    #endregion
  }
}
