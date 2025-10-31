#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GMTK {
  [CustomEditor(typeof(PlayableElement))]
  public class PlayableElementEditor : UnityEditor.Editor {

    private PlayableElement _element;

    [SerializeField] private CommonComponentSettings _commonSettings = new();

    // Gizmo visualization settings
    [SerializeField] private float _gizmoCellSize = 1.0f;
    [SerializeField] private float _gizmoTolerance = 0.4f;
    [SerializeField] private Color _gizmoColor = Color.yellow;
    //[SerializeField] private bool _gizmoFoldoutOpen = false;
    //[SerializeField] private bool _showOccupiedCells = true;

    [SerializeField] private bool _occupancyFoldoutOpen = false;
    [SerializeField] private bool _pointerFoldoutOpen = false;
    [SerializeField] private bool _movementFoldoutOpen = false;
    [SerializeField] private bool _eventsFoldoutOpen = false;

    protected static Dictionary<PlayableElement, bool> _showOccupiedCells = new();

    // Public properties for gizmo drawer access
    //public bool ShowOccupiedCells => _showOccupiedCells;
    public float GizmoCellSize => _gizmoCellSize;
    public float GizmoTolerance => _gizmoTolerance;
    public Color GizmoColor => _gizmoColor;

    public static bool ShowOccupiedCells(PlayableElement element) => _showOccupiedCells.TryGetValue(element, out var value) && value;

    //public properties
    private SerializedProperty _snapTransformProp;
    private SerializedProperty _modelProp;
    private SerializedProperty _draggableProp;
    private SerializedProperty _canRotateProp;
    private SerializedProperty _canSelectProp;
    private SerializedProperty _flippableProp;
    //input properties for ElementPointerComponent
    private SerializedProperty _selectionTriggerProp;
    private SerializedProperty _accuracyProp;
    private SerializedProperty _maxOffsetProp;
    private SerializedProperty _hoverThresholdProp;
    //pointer state props
    private SerializedProperty _isSelectedProp;
    private SerializedProperty _isHoveredProp;
    private SerializedProperty _canHoverProp;
    private SerializedProperty _isDraggingProp;

    //non-public properties
    private SerializedProperty _occupiedCells;

    public const int BUTTON_WIDTH = 150;

    // Cached GUI content for help icons
    private static GUIContent _helpIconContent;
    private static GUIContent _draggingHelpIcon;
    private static GUIContent _physicsHelpIcon;
    private static GUIStyle HelpButtonStyle {
      get {
        GUIStyle style = new(GUI.skin.button) {
          padding = new RectOffset(1, 1, 1, 1),
          margin = new RectOffset(0, 0, 2, 1),
          fixedWidth = 20,
          fixedHeight = EditorGUIUtility.singleLineHeight
        };
        return style;
      }
    }

    private static GUIStyle FoldoutHeaderStyle {
      get {
        GUIStyle style = new(EditorStyles.foldout) {
          fontStyle = FontStyle.Bold,
          alignment = TextAnchor.MiddleLeft,
          padding = new RectOffset(15, 6, 4, 4),
          border = new RectOffset(2, 0, 0, 0),
          fixedHeight = EditorGUIUtility.singleLineHeight,
          stretchWidth = true,       
        };
        return style;
      }
    }

    private void OnEnable() {
      _element = (PlayableElement)target;

      //model properties
      _snapTransformProp = serializedObject.FindProperty("SnapTransform");
      _modelProp = serializedObject.FindProperty("Model");
      //input actions
      _flippableProp = serializedObject.FindProperty("Flippable");
      _canRotateProp = serializedObject.FindProperty("CanRotate");
      _draggableProp = serializedObject.FindProperty("Draggable");
      _isDraggingProp = serializedObject.FindProperty("_isDragging");
      //occupancy properties
      _occupiedCells = serializedObject.FindProperty("OccupiedCells");
      //pointer state props
      _canSelectProp = serializedObject.FindProperty("_canSelect");
      _isSelectedProp = serializedObject.FindProperty("_isSelected");
      _canHoverProp = serializedObject.FindProperty("_canHover");
      _isHoveredProp = serializedObject.FindProperty("_isHovered");
      //selection properties
      _selectionTriggerProp = serializedObject.FindProperty("SelectionTriggers");
      _accuracyProp = serializedObject.FindProperty("Accuracy");
      _maxOffsetProp = serializedObject.FindProperty("MaxOffset");
      //hovering properties
      _hoverThresholdProp = serializedObject.FindProperty("HoverThreshold");

      // Initialize help icons
      InitializeHelpIcons();

      // Register gizmo settings with the drawer
      RegisterGizmoSettings();
    }

    private void OnDisable() {
      // Unregister gizmo settings when editor is disabled
      if (_element != null) {
        PlayableElementOccupancyGizmo.UnregisterGizmoSettings(_element);
      }
    }

    private static void InitializeHelpIcons() {
      if (_helpIconContent == null) {
        // Try to get Unity's built-in help icon
        _helpIconContent = EditorGUIUtility.IconContent("_Help");
        if (_helpIconContent?.image == null) {
          // Fallback to question mark text if icon is not available
          _helpIconContent = new GUIContent("?", "Help");
        }
      }

      if (_draggingHelpIcon == null) {
        _draggingHelpIcon = new GUIContent(_helpIconContent.image,
          "ElementDraggingComponent provides drag behavior settings such as:\n" +
          "• Drag threshold distance\n" +
          "• Drag constraints (horizontal/vertical only)\n" +
          "• Visual feedback during dragging\n" +
          "• Snap-to-grid behavior\n" +
          "• Event callbacks for drag start/end");
      }

      if (_physicsHelpIcon == null) {
        _physicsHelpIcon = new GUIContent(_helpIconContent.image,
          "ElementPhysicsComponent provides physics-based rotation settings such as:\n" +
          "• Custom rotation behavior overrides\n" +
          "• Physics-based rotation constraints\n" +
          "• Rotation damping and limits\n" +
          "• Integration with Unity's physics system\n" +
          "• Advanced rotation event handling");
      }
    }

    private void RegisterGizmoSettings() {
      if (_element == null) return;

      var settings = new PlayableElementOccupancyGizmo.GizmoSettings {
        showGizmos = _showOccupiedCells.TryGetValue(_element, out var value) && value,
        showAdditionalGizmos = false,
        showWhenNotSelected = false,
        cellSize = _gizmoCellSize,
        tolerance = _gizmoTolerance,
        color = _gizmoColor
      };
      PlayableElementOccupancyGizmo.RegisterGizmoSettings(_element, settings);
    }

    public override void OnInspectorGUI() {
      serializedObject.Update();

      /*
       * TODO
       * Model
       * Occupancy
       * Selection
       * Dragging
       * Movement (rotate, flip)
       * Events
      */
      DrawCurrentStatesProperties();

      DrawModelAndOccupancyProperties();

      DrawPointerProperties();

      DrawMovementProperties();

      DrawUnityEvents();

      serializedObject.ApplyModifiedProperties();
    }

    private void DrawCurrentStatesProperties() {
      EditorGUILayout.LabelField("Current State", EditorStyles.boldLabel);
      if (Application.isPlaying) {
        using (new EditorGUI.DisabledGroupScope(true)) {
          EditorGUILayout.PropertyField(_isSelectedProp);
          EditorGUILayout.PropertyField(_isHoveredProp);
          EditorGUILayout.PropertyField(_isDraggingProp);
        }
      }
      EditorGUILayout.Space();
    }

    private void DrawPointerProperties() {
      //selection
      _pointerFoldoutOpen = EditorGUILayout.Foldout(_pointerFoldoutOpen, "Pointer Settings", true, FoldoutHeaderStyle);
      if (!_pointerFoldoutOpen) return;
      EditorGUILayout.Space();
      using (new EditorGUI.IndentLevelScope()) {
        EditorGUILayout.PropertyField(_canSelectProp);
        EditorGUILayout.PropertyField(_selectionTriggerProp);
        EditorGUILayout.PropertyField(_accuracyProp);
        EditorGUILayout.PropertyField(_maxOffsetProp);
        EditorGUILayout.Space();
        using (new EditorGUI.DisabledGroupScope(true)) {
          EditorGUILayout.PropertyField(_canHoverProp);
        }
        EditorGUILayout.PropertyField(_hoverThresholdProp);
      }
      EditorGUILayout.Space();
    }

    private void DrawUnityEvents() {
      _eventsFoldoutOpen = EditorGUILayout.Foldout(_eventsFoldoutOpen, "Unity Events", true, FoldoutHeaderStyle);
      if (!_eventsFoldoutOpen) return;

      string[] eventProps = new string[] {
        "BeforeInitialize",
        "AfterInitialize",
        "OnSelect",
        "OnDeselected",
        "OnHovered",
        "OnUnhovered",
        "BeforeDragStart",
        "DragStart",
        "OnDragging",
        "BeforeDragEnd",
        "DragEnd",
        "BeforeInput",
        "PlayerInput",
      };

      using (new EditorGUI.IndentLevelScope()) {
        foreach (string eventPropName in eventProps) {
          if (serializedObject.FindProperty(eventPropName) is SerializedProperty eventProp) {
            EditorGUILayout.PropertyField(eventProp);
            if (eventPropName == "OnDragging") {
              if (serializedObject.FindProperty("DragMinDistance") is var dragMinDistanceProp)
                EditorGUILayout.PropertyField(dragMinDistanceProp);
              if (serializedObject.FindProperty("DragCooldown") is var dragCooldownProp)
                EditorGUILayout.PropertyField(dragCooldownProp);
            }
            EditorGUILayout.Space();
          }
        }
      }

    }

    private void DrawModelAndOccupancyProperties() {

      EditorGUILayout.LabelField("Model", EditorStyles.boldLabel);
      using (new EditorGUI.IndentLevelScope()) {
        EditorGUILayout.PropertyField(_snapTransformProp);
        EditorGUILayout.PropertyField(_modelProp);
      }
      EditorGUILayout.Space();

      //EditorGUILayout.LabelField("Occupancy", EditorStyles.boldLabel);
      _occupancyFoldoutOpen = EditorGUILayout.Foldout(_occupancyFoldoutOpen, "Occupancy", true, FoldoutHeaderStyle);
      if (!_occupancyFoldoutOpen) return;

      using (new EditorGUILayout.HorizontalScope(GUILayout.Width(200))) {
        //button to auto-fill it using integrated helper methods
        if (GUILayout.Button("Auto-Fill Occupied Cells", EditorStyles.miniButtonLeft)) {
          AutoFillOccupiedCells(_element, _gizmoCellSize, _gizmoTolerance);
        }
      }
      EditorGUILayout.Space();

      using (new EditorGUI.IndentLevelScope()) {
        EditorGUILayout.PropertyField(_occupiedCells);
        EditorGUILayout.Space();
        DrawOccupancyGizmoSettings();
        EditorGUILayout.Space();
      }
    }

    private void DrawMovementProperties() {
      int maxWidth = 200;
      //EditorGUILayout.LabelField("Movement", EditorStyles.boldLabel);
      _movementFoldoutOpen = EditorGUILayout.Foldout(_movementFoldoutOpen, "Movement", true, FoldoutHeaderStyle);
      if (!_movementFoldoutOpen) return;

      using (new EditorGUI.IndentLevelScope()) {
        using (new EditorGUILayout.VerticalScope()) {

          EditorGUILayout.PropertyField(_flippableProp);
          EditorGUILayout.Space();

          using (new EditorGUILayout.HorizontalScope(GUILayout.MaxWidth(maxWidth))) {
            EditorGUILayout.PropertyField(_draggableProp);
            if (_draggableProp.boolValue) DrawDraggableProperties();
          }

          using (new EditorGUILayout.HorizontalScope(GUILayout.MaxWidth(maxWidth))) {
            EditorGUILayout.PropertyField(_canRotateProp);
            CheckRotationOverride();
          }
        }
      }
      EditorGUILayout.Space();
    }

    private void DrawDraggableProperties() {
      const int buttonWidth = 150;
      const int helpButtonWidth = 20;

      if (_element.TryGetComponent(out ElementDraggingComponent draggingComponent)) {
        //link to component
        if (GUILayout.Button("Dragging Component", GUILayout.Width(buttonWidth))) {
          Selection.activeGameObject = draggingComponent.gameObject;
        }
        if (GUILayout.Button(_draggingHelpIcon, HelpButtonStyle, GUILayout.Width(helpButtonWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight))) {
          EditorUtility.DisplayDialog("Dragging Component Help", _draggingHelpIcon.tooltip, "OK");
        }
      }
      else {
        //EditorGUILayout.HelpBox("No ElementDraggingComponent component found. Add one to configure dragging settings.", MessageType.Warning);
        if (GUILayout.Button("Add Dragging Component", GUILayout.Width(buttonWidth))) {
          draggingComponent = _element.gameObject.AddComponent<ElementDraggingComponent>();
          Debug.Log($"Added ElementDraggingComponent ({draggingComponent.name}) to {_element.name}");
        }
        if (GUILayout.Button(_draggingHelpIcon, HelpButtonStyle, GUILayout.Width(helpButtonWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight))) {
          EditorUtility.DisplayDialog("Add Dragging Component Help", _draggingHelpIcon.tooltip, "OK");
        }
      }
      EditorGUILayout.Space();
    }

    private void CheckRotationOverride() {
      const int buttonWidth = 150;

      if (_element.TryGetComponent(out ElementPhysicsComponent physicsComponent)) {
        if (GUILayout.Button("Physics Component", GUILayout.Width(buttonWidth))) {
          Selection.activeGameObject = physicsComponent.gameObject;
        }
        if (GUILayout.Button(_physicsHelpIcon, HelpButtonStyle, GUILayout.Height(EditorGUIUtility.singleLineHeight))) {
          EditorUtility.DisplayDialog("Physics Component Help", _physicsHelpIcon.tooltip, "OK");
        }
      }
      else {
        if (GUILayout.Button("Add Physics Component", GUILayout.Width(buttonWidth))) {
          physicsComponent = _element.gameObject.AddComponent<ElementPhysicsComponent>();
          Debug.Log($"Added ElementPhysicsComponent ({physicsComponent.name}) to {_element.name}");
        }
        if (GUILayout.Button(_physicsHelpIcon, HelpButtonStyle, GUILayout.Height(EditorGUIUtility.singleLineHeight))) {
          EditorUtility.DisplayDialog("Add Physics Component Help", _physicsHelpIcon.tooltip, "OK");
        }
      }
    }

    private void DrawOccupancyGizmoSettings() {


      EditorGUILayout.LabelField("Occupancy Gizmo", EditorStyles.boldLabel);
      using (new EditorGUILayout.HorizontalScope(GUILayout.Width(200))) {
        //reset all gizmos settings button
        if (GUILayout.Button("Clear All Gizmos", GUILayout.Width(BUTTON_WIDTH))) {
          PlayableElementOccupancyGizmo.ClearAllRegisteredSettings();
          _showOccupiedCells.Clear();
        }
      }
      EditorGUILayout.Space();
      using (new EditorGUI.IndentLevelScope()) {
        //obtain flag for the element
        bool elementShowOccupiedCells = _showOccupiedCells.TryGetValue(_element, out var value) && value;

        bool previousShowOccupiedCells = elementShowOccupiedCells;
        //Debug.Log($"Pre previousShowOccupiedCells: {previousShowOccupiedCells}, _showOccupiedCells[_element]: {elementShowOccupiedCells}");
        _showOccupiedCells[_element] = EditorGUILayout.Toggle("Show Gizmo", previousShowOccupiedCells);
        //Debug.Log($"Pos previousShowOccupiedCells: {previousShowOccupiedCells}, _showOccupiedCells[_element]: {_showOccupiedCells[_element]}");
        if (_showOccupiedCells[_element]) {
          using (new EditorGUI.IndentLevelScope()) {
            float previousCellSize = _gizmoCellSize;
            float previousTolerance = _gizmoTolerance;
            Color previousColor = _gizmoColor;

            _gizmoCellSize = EditorGUILayout.FloatField("Cell Size", _gizmoCellSize);
            _gizmoTolerance = EditorGUILayout.Slider("Tolerance", _gizmoTolerance, 0f, 1f);
            _gizmoColor = EditorGUILayout.ColorField("Gizmo Color", _gizmoColor);

            // Re-register settings if any gizmo setting changed
            if (previousShowOccupiedCells != _showOccupiedCells[_element] ||
                !Mathf.Approximately(previousCellSize, _gizmoCellSize) ||
                !Mathf.Approximately(previousTolerance, _gizmoTolerance) ||
                previousColor != _gizmoColor) {
              RegisterGizmoSettings();
            }

            // Show tolerance explanation
            string toleranceText = _gizmoTolerance switch {
              <= 0.1f => "Pixel perfect (any overlap)",
              <= 0.3f => "Minimal coverage required",
              <= 0.7f => "Moderate coverage required",
              _ => "High coverage required"
            };
            EditorGUILayout.HelpBox($"Tolerance: {toleranceText}", MessageType.Info);

            // Show rotation info if element is rotated
            Transform snapTransform = _element.SnapTransform != null ? _element.SnapTransform : _element.transform;
            float currentRotation = snapTransform.rotation.eulerAngles.z;
            if (Mathf.Abs(currentRotation) > 0.1f) {
              EditorGUILayout.HelpBox($"Element rotated: {currentRotation:F1}°", MessageType.Info);
            }

            // Force scene view refresh when gizmo settings change
            if (GUI.changed) {
              SceneView.RepaintAll();
            }
          }
        }
        else if (previousShowOccupiedCells != _showOccupiedCells[_element]) {
          // Re-register to update the showGizmos flag
          RegisterGizmoSettings();
        }

      }
      EditorGUILayout.Space();
    }

    /// <summary>
    /// Auto-fills the occupied cells of a PlayableElement based on its sprite renderer bounds.
    /// Uses SnapTransform as the source of truth for position, scale, and rotation.
    /// </summary>
    private void AutoFillOccupiedCells(PlayableElement element, float cellSize = 1.0f, float tolerance = 0.1f) {
      if (element == null) {
        Debug.LogWarning("[PlayableElementEditor] Element is null");
        return;
      }

      // Clamp tolerance to valid range
      tolerance = Mathf.Clamp01(tolerance);

      // Get the sprite renderer from the element using SnapTransform logic
      SpriteRenderer spriteRenderer = GetSpriteRenderer(element);
      if (spriteRenderer == null || spriteRenderer.sprite == null) {
        Debug.LogWarning($"[PlayableElementEditor] No valid SpriteRenderer or Sprite found on {element.name}");
        return;
      }

      // Use SnapTransform as the source of truth
      Transform snapTransform = element.SnapTransform != null ? element.SnapTransform : element.transform;

      // Calculate occupied cells based on sprite bounds and SnapTransform scale
      var occupiedCells = CalculateOccupiedCells(spriteRenderer, snapTransform, cellSize, tolerance);

      // Update the element's occupied cells
      element.OccupiedCells.Clear();
      element.OccupiedCells.AddRange(occupiedCells);

      // Mark the element as dirty for editor updates
      EditorUtility.SetDirty(element);

      Debug.Log($"[PlayableElementEditor] Updated {element.name} with {occupiedCells.Count} occupied cells " +
                $"(tolerance: {tolerance:F2}): [{string.Join(", ", occupiedCells)}]");
    }

    /// <summary>
    /// Calculates occupied cells using SnapTransform as the source of truth.
    /// </summary>
    private List<Vector2Int> CalculateOccupiedCells(SpriteRenderer spriteRenderer, Transform snapTransform, float cellSize, float tolerance) {
      var occupiedCells = new List<Vector2Int>();
      Sprite sprite = spriteRenderer.sprite;

      // Get sprite bounds in local space
      Bounds spriteBounds = sprite.bounds;

      // Get the SnapTransform's scale - this affects the actual world size
      Vector3 scale = snapTransform.lossyScale;

      // Calculate scaled sprite bounds
      Vector2 scaledSpriteSize = new Vector2(
        spriteBounds.size.x * Mathf.Abs(scale.x),
        spriteBounds.size.y * Mathf.Abs(scale.y)
      );

      // Use SnapTransform position as the pivot
      Vector2 pivotWorldPosition = snapTransform.position;

      // Get the sprite's bounds in world coordinates relative to the pivot, accounting for scale
      Vector2 scaledBoundsMin = new Vector2(
        spriteBounds.min.x * scale.x,
        spriteBounds.min.y * scale.y
      );
      Vector2 scaledBoundsMax = new Vector2(
        spriteBounds.max.x * scale.x,
        spriteBounds.max.y * scale.y
      );

      Vector2 spriteBottomLeft = pivotWorldPosition + scaledBoundsMin;
      Vector2 spriteTopRight = pivotWorldPosition + scaledBoundsMax;

      // Calculate the range of cells the sprite potentially covers
      int minCellX = Mathf.FloorToInt((spriteBottomLeft.x - pivotWorldPosition.x) / cellSize);
      int maxCellX = Mathf.FloorToInt((spriteTopRight.x - pivotWorldPosition.x) / cellSize);
      int minCellY = Mathf.FloorToInt((spriteBottomLeft.y - pivotWorldPosition.y) / cellSize);
      int maxCellY = Mathf.FloorToInt((spriteTopRight.y - pivotWorldPosition.y) / cellSize);

      // Calculate cell coverage for each potential cell
      for (int cellX = minCellX; cellX <= maxCellX; cellX++) {
        for (int cellY = minCellY; cellY <= maxCellY; cellY++) {
          float coveragePercentage = CalculateCellCoverage(cellX, cellY, cellSize, pivotWorldPosition, spriteBottomLeft, spriteTopRight);

          if (coveragePercentage >= tolerance) {
            occupiedCells.Add(new Vector2Int(cellX, cellY));
          }
        }
      }

      // Ensure we have at least one cell (fallback for very small sprites)
      if (occupiedCells.Count == 0) {
        occupiedCells.Add(Vector2Int.zero);
        Debug.LogWarning("[PlayableElementEditor] No cells met tolerance threshold, adding (0,0) as fallback");
      }

      return occupiedCells;
    }

    private float CalculateCellCoverage(int cellX, int cellY, float cellSize, Vector2 pivotWorldPosition, Vector2 spriteBottomLeft, Vector2 spriteTopRight) {
      // Calculate the bounds of the grid cell
      Vector2 cellBottomLeft = pivotWorldPosition + new Vector2(cellX * cellSize, cellY * cellSize);
      Vector2 cellTopRight = cellBottomLeft + new Vector2(cellSize, cellSize);

      // Calculate the intersection rectangle between sprite and cell
      Vector2 intersectionBottomLeft = new Vector2(
        Mathf.Max(cellBottomLeft.x, spriteBottomLeft.x),
        Mathf.Max(cellBottomLeft.y, spriteBottomLeft.y)
      );

      Vector2 intersectionTopRight = new Vector2(
        Mathf.Min(cellTopRight.x, spriteTopRight.x),
        Mathf.Min(cellTopRight.y, spriteTopRight.y)
      );

      // Check if there's actually an intersection
      if (intersectionBottomLeft.x >= intersectionTopRight.x || intersectionBottomLeft.y >= intersectionTopRight.y) {
        return 0f; // No intersection
      }

      // Calculate intersection area
      float intersectionWidth = intersectionTopRight.x - intersectionBottomLeft.x;
      float intersectionHeight = intersectionTopRight.y - intersectionBottomLeft.y;
      float intersectionArea = intersectionWidth * intersectionHeight;

      // Calculate cell area
      float cellArea = cellSize * cellSize;

      // Return coverage percentage
      float coverage = intersectionArea / cellArea;

      return Mathf.Clamp01(coverage);
    }

    private SpriteRenderer GetSpriteRenderer(PlayableElement element) {
      // First check the Model transform if specified
      if (element.Model != null) {
        var renderer = element.Model.GetComponent<SpriteRenderer>();
        if (renderer != null) return renderer;

        // Also check children of Model
        renderer = element.Model.GetComponentInChildren<SpriteRenderer>();
        if (renderer != null) return renderer;
      }

      // Fallback to checking the element's own GameObject
      var elementRenderer = element.GetComponent<SpriteRenderer>();
      if (elementRenderer != null) return elementRenderer;

      // Final fallback - check children
      return element.GetComponentInChildren<SpriteRenderer>();
    }

  }
}
#endif