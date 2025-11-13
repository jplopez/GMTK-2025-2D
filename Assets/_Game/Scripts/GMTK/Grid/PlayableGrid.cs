using Ameba;
using MoreMountains.Feedbacks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEssentials;

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

    [Tooltip("The world position for the Grid (0,0) tile")]
    public Vector2 GridOrigin;

    [Header("Elements")]
    [Tooltip("Whether the grid allows objects to be placed on occupied cells, in which case the previous occupant will be removed")]
    public bool AllowReplaceItems = false;
    [Tooltip("If true, the grid will be populated with initial elements on start")]
    public bool PopulateOnStart = false;
    public PlayableElement[] InitialElements;
    [Space(10)]

    [Foldout("Bounds", true)]
    [Header("Bounds")]
    [Tooltip("Collider to define the top boundary of the grid")]
    public EdgeCollider2D TopBound;
    [Tooltip("Collider to define the bottom boundary of the grid")]
    public EdgeCollider2D BottomBound;
    [Tooltip("Collider to define the left boundary of the grid")]
    public EdgeCollider2D LeftBound;
    [Tooltip("Collider to define the right boundary of the grid")]
    public EdgeCollider2D RightBound;
    [EndFoldout]

    [Space]
    [Foldout("Events", true)]
    [Header("Events")]
    [Tooltip("Invoked when an element is successfully added to the grid")]
    public UnityEvent<PlayableElement> OnElementAdded = new();
    [Tooltip("Invoked when an element is removed from the grid")]
    public UnityEvent<PlayableElement> OnElementRemoved = new();
    [EndFoldout]

    [Space]
    [Header("Feedbacks")]
    [Foldout("Feedbacks", true)]
    [Tooltip("Feedback played when an element moves over the grid")]
    public MMF_Player OnElementOverGridFeedback;
    [Tooltip("Feedback played when an element is picked from the grid")]
    public MMF_Player OnElementPickedFeedback;
    [Tooltip("Feedback played when an element is successfully dropped on the grid")]
    public MMF_Player OnElementDroppedSuccessFeedback;
    [Tooltip("Feedback played when an element drop is invalid")]
    public MMF_Player OnElementDroppedInvalidFeedback;
    [EndFoldout]

    [Space]
    [Header("Gizmos")]
    public bool ShowGizmos = true;

    [Foldout("Gizmos", true)]
    [MMFCondition("ShowGizmos", true)]
    [Tooltip("Enable/disable grid gizmo visualization")]
    [SerializeField] private bool _showGridGizmo = true;
    [MMFCondition("ShowGizmos", true)]
    [Help("Enabling tile position labels may impact editor performance on large grids.", MessageType.Warning)]
    [Tooltip("Show tile position text in each cell")]
    [SerializeField] private bool _showTilePositions = true;
    [MMFCondition("ShowGizmos", true)]
    [Tooltip("Highlight occupied cells with red color")]
    [SerializeField] private bool _colorOccupiedCells = true;
    [EndFoldout]

    public AmebaGrid Grid => _grid;

    // Private fields
    protected AmebaGrid _grid;

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
      _grid.Clear();
    }

    private void OnValidate() {
      // Ensure valid grid dimensions
      _rows = Mathf.Max(1, _rows);
      _columns = Mathf.Max(1, _columns);
      _tileSize = Mathf.Max(0.1f, _tileSize);
    }

    private void OnDisable() {
      _grid.Clear();
      _trackedElement = null;
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

        GridOrigin = GridOrigin == null ? transform.position : GridOrigin;

        PopulateGrid(PopulateOnStart);
      }
      catch (Exception ex) {
        this.LogError($"Failed to initialize grid: {ex.Message}");
      }
    }

    private void PopulateGrid(bool populate) {
      if (populate) {
        foreach (var element in InitialElements) {
          if (element != null) {

            var gridPos = WorldToGrid((Vector2)element.GetPosition());
            AddElementToGrid(element, gridPos);
          }
        }
      }

    }

    #endregion

    #region Add/Remove Elements

    /// <summary>
    /// Adds a PlayableElement to the grid at the specified grid position, considering all the footprint the element needs.
    /// </summary>
    /// <remarks>
    /// This methods uses PlayableElement.OccupiedCells to determine which grid tiles the element will occupy. Checks are performed to ensure all required tiles are free or that 'AllowReplaceItems' is true, and all cells are within bounds.<br/>
    /// When 'AllowReplaceItems' is false, this methods returns an empty list. If true, it returns a list of any PlayableElements that were replaced during the addition.
    /// </remarks>
    /// <param name="element"></param>
    /// <param name="gridPos"></param>
    /// <returns>list of any PlayableElements that were replaced during the addition</returns>
    protected virtual List<PlayableElement> AddElementToGrid(PlayableElement element, Vector2Int gridPos) {
      // list of Grid coordinates the element will occupy
      List<Vector2Int> tilesToOccupy = new();
      foreach (var requiredTile in element.OccupiedCells) {
        var requiredGridPos = new Vector2Int(gridPos.x + requiredTile.x, gridPos.y + requiredTile.y);
        this.LogDebug($"{element.name} requiredTile {requiredTile} => {requiredGridPos}");

        if (CanPlaceAtGridPosition(requiredGridPos)) {
          tilesToOccupy.Add(requiredGridPos);
        }
        else {
          this.LogError($"'{element.name}' requested position {requiredGridPos} is invalid for placement. Element can't be added");
          return new List<PlayableElement>();
        }
      }

      // list to capture replaced elements
      List<PlayableElement> replacedElements = new();

      foreach (var tilePos in tilesToOccupy) {
        if (_grid.TryAdd(tilePos.x, tilePos.y, element, out var replacedElement)) {
          if (replacedElement is PlayableElement) {
            if (AllowReplaceItems) {
              replacedElements.Add(replacedElement as PlayableElement);
            }
          }
          this.LogDebug($"'{element.name}' has occupied {tilePos}");
        }
        else {
          this.LogError($"'{element.name}' can't occupy {tilePos}. Rolling back");
          // Rollback previous additions
          RollbackTiles(tilesToOccupy, element);
          return new List<PlayableElement>();
        }
      }
      var snappedPos = SnapToGrid(gridPos);
      element.UpdatePosition(snappedPos);

      return replacedElements;
    }

    private void RollbackTiles(List<Vector2Int> tilesToRollback, PlayableElement elementToRemove) {
      foreach (var rollbackPos in tilesToRollback) {
        PlayableElement element = _grid.GetTile(rollbackPos.x, rollbackPos.y).GetObjectAs<PlayableElement>();
        if (elementToRemove.Equals(element)) {
          _grid.Remove(rollbackPos.x, rollbackPos.y);
        }
      }
    }

    protected virtual void RemoveElementFromGrid(PlayableElement element, Vector2Int gridPos) {
      int removedTiles = 0;
      int expectedRemovals = element.OccupiedCells.Count + 1; // +1 for origin tile
      foreach (var requiredTile in element.OccupiedCells) {
        var requiredGridPos = new Vector2Int(gridPos.x + requiredTile.x, gridPos.y + requiredTile.y);
        this.LogDebug($"REMOVE {element.name} requiredTile {requiredTile} => {requiredGridPos}");
        if(_grid.TryGetAs(requiredGridPos.x, requiredGridPos.y, out PlayableElement occupyingElement) && occupyingElement.Equals(element)) {
          _grid.Remove(requiredGridPos.x, requiredGridPos.y);
          removedTiles++;
        }
      }
      if (removedTiles != expectedRemovals) {
        this.LogWarning($"Removed {removedTiles} tiles for '{element.name}' but expected to remove {expectedRemovals}. There may be inconsistencies in the grid state.");
        // Additional depth search. This should remove any remaining tiles occupied by the element, but is more expensive
        RemoveElementFromGrid(element);
      }
    }

    protected virtual void RemoveElementFromGrid(PlayableElement element) {
      // Find all tiles occupied by the element and remove them
      List<Vector2Int> tilesToRemove = _grid.GetTiles().ToList().Where(t => t.GetObjectAs<PlayableElement>() == element).Select(t => new Vector2Int(t.X, t.Y)).ToList();
      foreach (var tilePos in tilesToRemove) {
        _grid.Remove(tilePos.x, tilePos.y);
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

      if (_grid.TryGetAs(gridPos.x, gridPos.y, out PlayableElement element) && element == args.Element) {
        _trackedElement = args.Element;
        _trackedElementOriginalPosition = gridPos;

        // Remove from grid while being dragged
        RemoveElementFromGrid(args.Element, gridPos);
        OnElementRemoved?.Invoke(args.Element);

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

        RemoveElementFromGrid(args.Element); // Ensure element is not already in grid

        List<PlayableElement> replacedElements = AddElementToGrid(args.Element, gridPos);

        // Drop success feedback and event
        PlayFeedback(OnElementDroppedSuccessFeedback);
        OnElementAdded?.Invoke(args.Element);

        // If replacing, invoke removal event
        if (AllowReplaceItems) {
          foreach (var prevElement in replacedElements) {
            OnElementRemoved?.Invoke(prevElement);
          }
        }

        this.LogDebug($"Element '{args.Element.name}' placed at grid position {gridPos}");
      }
      else {
        // Invalid placement - return to original position if it was in grid
        if (_trackedElement == args.Element && _trackedElementOriginalPosition.HasValue) {

          var originalPos = _trackedElementOriginalPosition.Value;
          //var snappedPos = SnapToGrid(GridToWorld(originalPos));
          //args.Element.UpdatePosition(snappedPos);
          //_grid.Add(originalPos.x, originalPos.y, args.Element);
          AddElementToGrid(args.Element, originalPos);

          PlayFeedback(OnElementDroppedInvalidFeedback);
          this.LogDebug($"Element '{args.Element.name}' returned to original position {originalPos}");
        }
        else {
          this.LogDebug($"Element '{args.Element.name}' dropped outside grid and not returned (was not originally in grid)");
        }
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

    /// <summary>
    /// Checks if a PlayableElement can be placed at the given world position.
    /// </summary>
    /// <param name="element">The PlayableElement to check.</param>
    /// <param name="worldPosition">The world position to check.</param>
    /// <returns>True if the element can be placed; otherwise, false.</returns>
    public bool CanPlaceElement(PlayableElement element, Vector2 worldPosition) {
      // input parameters validations
      if (_grid == null || element == null) return false;

      var gridPos = WorldToGrid(worldPosition);
      // Check if position is within grid bounds and tile is empty
      this.LogDebug($"CanPlaceElement: element={element.name}, worldPosition={worldPosition}, gridPos={gridPos}, AllowReplaceItems={AllowReplaceItems}");
      return CanPlaceAtGridPosition(gridPos);
    }

    public bool CanPlaceAtGridPosition(Vector2Int gridPos) {
      return _grid.IsValidCoordinate(gridPos.x, gridPos.y)
            && (AllowReplaceItems || _grid.IsEmpty(gridPos.x, gridPos.y));
    }

    #region Coordinate Conversion

    /// <summary>
    /// Converts world coordinates to grid coordinates.
    /// </summary>
    /// <param name="worldPosition">The world position.</param>
    /// <returns>The corresponding grid coordinates.</returns>
    public Vector2Int WorldToGrid(Vector2 worldPosition) {
      //Vector2 bottomLeft = new(GridOrigin.x - (_columns / 2f * _tileSize),
      //  GridOrigin.y - (_rows / 2f * _tileSize));
      var localPos = worldPosition - GridOrigin;
      int x = Mathf.RoundToInt(localPos.x / _tileSize);
      int y = Mathf.RoundToInt(localPos.y / _tileSize);
      this.LogDebug($"WorldToGrid: worldPosition={worldPosition}, localPos={localPos}, gridPos={new Vector2Int(x, y)}");
      return new Vector2Int(x, y);
    }

    /// <summary>
    /// Converts grid coordinates to world coordinates (bottom-left corner of tile).
    /// </summary>
    /// <param name="gridPosition">The grid coordinates.</param>
    /// <returns>The corresponding world position.</returns>
    public Vector2 GridToWorld(Vector2Int gridPosition) {
      Vector2 bottomLeft = new(GridOrigin.x - (_columns / 2f * _tileSize),
        GridOrigin.y - (_rows / 2f * _tileSize));
      float x = gridPosition.x * _tileSize + bottomLeft.x;
      float y = gridPosition.y * _tileSize + bottomLeft.y;
      return new Vector2(x, y);
    }

    /// <summary>
    /// Snaps a world position to the nearest grid point.
    /// </summary>
    /// <remarks>This method converts the given world position to grid coordinates, determines the nearest
    /// grid point,  and then converts it back to world coordinates. It is useful for aligning objects to a grid-based
    /// layout.</remarks>
    /// <param name="worldPosition">The position in world coordinates to be snapped to the grid.</param>
    /// <returns>A <see cref="Vector2"/> representing the world coordinates of the nearest grid point.</returns>
    public Vector2 SnapToGrid(Vector2 worldPosition) {
      var gridPos = WorldToGrid(worldPosition);
      return GridToWorld(gridPos);
    }

    #endregion


    /// <summary>
    /// Plays a Feel Feedback if it's assigned.
    /// </summary>
    private void PlayFeedback(MMF_Player feedback, bool reverse = false) {
      if (feedback != null && feedback.gameObject.activeInHierarchy) {
        if (reverse) feedback.PlayFeedbacksInReverse();
        else feedback.PlayFeedbacks();
      }
    }


#if UNITY_EDITOR

    #region Gizmos
    private void OnDrawGizmos() {
      // Ensure grid is initialized for gizmo drawing
      _grid ??= new AmebaGrid(_rows, _columns, _tileSize);

      // Draw grid lines in bright yellow
      Gizmos.color = Color.blueViolet;
      // Set the boundaries' positions and sizes
      int halfWidth = Mathf.RoundToInt(_columns / 2);
      int halfHeight = Mathf.RoundToInt(_rows / 2);

      DrawGizmoGrid(0, _columns, 0, _rows);
      DrawGizmoTiles(); //occupancy and labels

    }

    private void DrawGizmoGrid(int x1, int x2, int y1, int y2) {
      if (!_showGridGizmo) return;
      // Draw vertical lines
      for (int x = x1; x <= x2; x++) {
        Vector3 start = new((x * _tileSize) + GridOrigin.x,
          (GridOrigin.y + y1) * _tileSize, 0);
        Vector3 end = new((x * _tileSize) + GridOrigin.x,
          (GridOrigin.y + y2) * _tileSize, 0);
        Gizmos.DrawLine(start, end);
      }

      // Draw horizontal lines
      for (int y = y1; y <= y2; y++) {
        Vector3 start = new(GridOrigin.x + (x1 * _tileSize),
          y * _tileSize + GridOrigin.y, 0);

        Vector3 end = new(GridOrigin.x + (x2 * _tileSize),
          y * _tileSize + GridOrigin.y, 0);
        Gizmos.DrawLine(start, end);
      }
    }

    private void DrawGizmoTiles() {

      foreach (var tile in _grid.GetTiles()) {

        if (_colorOccupiedCells && tile.GetObject() != null) {
          Gizmos.color = new Color(1f, 0f, 0f, 0.75f); // Red with 75% transparency
          float gizmoSize = _tileSize * 0.8f;
          Vector3 worldPos = tile.Center + GridOrigin;
          Gizmos.DrawCube(worldPos, Vector3.one * gizmoSize);
        }

        if (_showTilePositions) {
          DrawTilePositionLabel(tile.TopLeft + GridOrigin, tile.X, tile.Y);
        }

      }

    }

    /// <summary>
    /// Draws the tile position label at the specified position
    /// </summary>
    private void DrawTilePositionLabel(Vector3 position, int x, int y) {
      // Use UnityEditor.Handles for text drawing
      //Handles.color = Color.darkB;
      GUIStyle style = new() {
        normal = { textColor = Color.darkBlue },
        alignment = TextAnchor.UpperLeft
      };
      string label = $"({x},{y})";
      Handles.Label(position, label, style);
    }

    #endregion

    #region ContextMenu

    [ContextMenu("Create Bounds")]
    public void CreateBounds() {
      // Remove existing bounds if any
      if (TopBound != null) DestroyImmediate(TopBound);
      if (BottomBound != null) DestroyImmediate(BottomBound);
      if (LeftBound != null) DestroyImmediate(LeftBound);
      if (RightBound != null) DestroyImmediate(RightBound);

      TopBound = gameObject.AddComponent<EdgeCollider2D>();
      TopBound.transform.parent = transform;
      BottomBound = gameObject.AddComponent<EdgeCollider2D>();
      BottomBound.transform.parent = transform;
      LeftBound = gameObject.AddComponent<EdgeCollider2D>();
      LeftBound.transform.parent = transform;
      RightBound = gameObject.AddComponent<EdgeCollider2D>();
      RightBound.transform.parent = transform;

      // Set the boundaries' positions and sizes
      float x1 = -_columns / 2f * _tileSize;
      float x2 = _columns / 2f * _tileSize;
      float y1 = -_rows / 2f * _tileSize;
      float y2 = _rows / 2f * _tileSize;
      SetBoundary(TopBound, new Vector2(x1, y1), new Vector2(_columns, 0));
      SetBoundary(BottomBound, new Vector2(x1, y2), new Vector2(_columns, 0));
      SetBoundary(LeftBound, new Vector2(x1, y1), new Vector2(0, _rows));
      SetBoundary(RightBound, new Vector2(x2, y1), new Vector2(0, _rows));
    }

    private void SetBoundary(EdgeCollider2D bound, Vector2 offset, Vector2 size) {
      var points = new Vector2[2];
      points[0] = offset;
      points[1] = offset + size;
      bound.points = points;
    }

    [ContextMenu("Populate Grid")]
    public void Populate() => PopulateGrid(true);

    [ContextMenu("Empty Grid")]
    public void Empty() => _grid.Clear();

    [ContextMenu("Restart Grid (Empty, Create Bounds, Populate")]
    public void ResetGrid() {
      Empty();
      CreateBounds();
      PopulateGrid(true);
    }


    #endregion
#endif

  }
}
