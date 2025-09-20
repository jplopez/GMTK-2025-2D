#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GMTK {
  [CustomEditor(typeof(PlayableElement))]
  public class PlayableElementEditor : Editor {

    private PlayableElement _element;

    // Gizmo visualization settings
    [SerializeField] private float _gizmoCellSize = 1.0f;
    [SerializeField] private float _gizmoTolerance = 0.4f;
    [SerializeField] private Color _gizmoColor = Color.yellow;
    [SerializeField] private bool _gizmoFoldoutOpen = false;
    //[SerializeField] private bool _showOccupiedCells = true;

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

    //non-public properties
    private SerializedProperty _occupiedCells;

    public const int BUTTON_WIDTH = 150;

    private void OnEnable() {
      _element = (PlayableElement)target;

      //public properties
      _snapTransformProp = serializedObject.FindProperty("SnapTransform");
      _modelProp = serializedObject.FindProperty("Model");
      _draggableProp = serializedObject.FindProperty("Draggable");
      _canRotateProp = serializedObject.FindProperty("CanRotate");
      _canSelectProp = serializedObject.FindProperty("_canSelect");
      _flippableProp = serializedObject.FindProperty("Flippable");

      //non-public properties
      _occupiedCells = serializedObject.FindProperty("OccupiedCells");

      // Register gizmo settings with the drawer
      RegisterGizmoSettings();
    }

    private void OnDisable() {
      // Unregister gizmo settings when editor is disabled
      if (_element != null) {
        PlayableElementOccupancyGizmo.UnregisterGizmoSettings(_element);
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
      //Debug.Log($"RegisterGizmoSettings for {_element.name}: showGizmos={settings.showGizmos}, cellSize={settings.cellSize}, tolerance={settings.tolerance}, color={settings.color}");
      PlayableElementOccupancyGizmo.RegisterGizmoSettings(_element, settings);
    }

    public override void OnInspectorGUI() {
      serializedObject.Update();

      DrawModelAndOccupancyProperties();
      DrawMovementProperties();

      serializedObject.ApplyModifiedProperties();
    }

    private void DrawModelAndOccupancyProperties() {

      EditorGUILayout.LabelField("Model", EditorStyles.boldLabel);
      using (new EditorGUI.IndentLevelScope()) {
        EditorGUILayout.PropertyField(_snapTransformProp);
        EditorGUILayout.PropertyField(_modelProp);
      }
      EditorGUILayout.Space();

      EditorGUILayout.LabelField("Occupancy", EditorStyles.boldLabel);
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
      EditorGUILayout.LabelField("Movement", EditorStyles.boldLabel);
      using (new EditorGUI.IndentLevelScope()) {
        EditorGUILayout.PropertyField(_flippableProp);
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(_draggableProp);
        if (_draggableProp.boolValue) DrawDraggableProperties();

        EditorGUILayout.PropertyField(_canRotateProp);
        CheckRotationOverride();

        EditorGUILayout.PropertyField(_canSelectProp);

      }
      EditorGUILayout.Space();
    }

    private void DrawDraggableProperties() {
      using (new EditorGUI.IndentLevelScope()) {
        if (_element.TryGetComponent<DraggingElementComponent>(out var draggingComponent)) {
          EditorGUILayout.HelpBox("See DraggingElementComponent component for Dragging settings", MessageType.Info);
          //link to component
          if (GUILayout.Button("Go To Dragging Component", GUILayout.Width(250))) {
            Selection.activeGameObject = draggingComponent.gameObject;
          }
        }
        else {
          EditorGUILayout.HelpBox("No DraggingElementComponent component found. Add one to configure dragging settings.", MessageType.Warning);
          if (GUILayout.Button("Add DraggingElementComponent Component", GUILayout.Width(250))) {
            _element.gameObject.AddComponent<DraggingElementComponent>();
          }
        }
      }
      EditorGUILayout.Space();
    }

    private void CheckRotationOverride() {
      using (new EditorGUI.IndentLevelScope()) {
        PhysicsElementComponent physicsComponent = _element.GetComponent<PhysicsElementComponent>();
        bool hasRotationOverride = physicsComponent != null && physicsComponent.ChangeRotationOnCollision;
        // Show info message if rotation is overridden
        string helpBoxMessage = hasRotationOverride ?
            "Rotation is managed by the PhysicsElementComponent." :
            "Rotation is not managed by the PhysicsElementComponent.";
        EditorGUILayout.HelpBox(helpBoxMessage, MessageType.Info);

        if (physicsComponent != null) {
          if (GUILayout.Button("Go To Physics Component", GUILayout.Width(250))) {
            Selection.activeGameObject = physicsComponent.gameObject;
          }
        }
        else {
          if (GUILayout.Button("Add PhysicsElementComponent", GUILayout.Width(250))) {
            _element.gameObject.AddComponent<PhysicsElementComponent>();
          }
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
    private System.Collections.Generic.List<Vector2Int> CalculateOccupiedCells(SpriteRenderer spriteRenderer, Transform snapTransform, float cellSize, float tolerance) {
      var occupiedCells = new System.Collections.Generic.List<Vector2Int>();
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