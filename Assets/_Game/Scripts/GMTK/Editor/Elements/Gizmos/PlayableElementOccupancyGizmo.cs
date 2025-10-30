#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace GMTK {
  /// <summary>
  /// Shared gizmo drawer for PlayableElement occupied cells visualization.
  /// Uses Unity's [DrawGizmo] attribute to automatically draw gizmos for any selected PlayableElement.
  /// This approach allows the gizmo functionality to be reused across multiple editors.
  /// </summary>
  public static class PlayableElementOccupancyGizmo {

    // Default settings that can be overridden by editors
    public static float DefaultCellSize = 1.0f;
    public static float DefaultTolerance = 0.4f;
    public static Color DefaultColor = Color.yellow;
    public static bool ShowGizmosGlobally = true;

    /// <summary>
    /// Draws gizmos when PlayableElement is selected.
    /// This method is automatically called by Unity for any selected PlayableElement.
    /// </summary>
    [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
    public static void DrawPlayableElementGizmosSelected(PlayableElement element, GizmoType gizmoType) {
      if (!ShowGizmosGlobally || element == null) return;

      // Try to get settings from registered editors, fallback to defaults
      var gizmoSettings = GetGizmoSettingsForElement(element);
      if (!gizmoSettings.showGizmos) return;

      DrawOccupiedCellsVisualization(element, gizmoSettings);
    }

    /// <summary>
    /// Optionally draws gizmos when PlayableElement is not selected.
    /// Uncomment the attribute if you want gizmos to show for non-selected elements.
    /// </summary>
    // [DrawGizmo(GizmoType.NonSelected)]
    public static void DrawPlayableElementGizmosNonSelected(PlayableElement element, GizmoType gizmoType) {
      if (!ShowGizmosGlobally || element == null) return;

      var gizmoSettings = GetGizmoSettingsForElement(element);
      if (!gizmoSettings.showGizmos || !gizmoSettings.showWhenNotSelected) return;

      // Draw with reduced alpha for non-selected
      var fadedColor = gizmoSettings.color;
      fadedColor.a *= 0.5f;

      var fadedSettings = gizmoSettings;
      fadedSettings.color = fadedColor;

      DrawOccupiedCellsVisualization(element, fadedSettings);
    }

    /// <summary>
    /// Core method that draws the complete occupied cells visualization.
    /// </summary>
    private static void DrawOccupiedCellsVisualization(PlayableElement element, GizmoSettings settings) {
      var gizmoData = PrepareGizmoData(element, settings.cellSize, settings.tolerance, settings.color);

      if (!gizmoData.IsValid) return;

      DrawOccupiedCellsGizmos(gizmoData);

      if (settings.showAdditionalGizmos) {
        DrawAdditionalGizmos(gizmoData);
      }
    }

    /// <summary>
    /// Prepares gizmo data for visualizing occupied cells in the Scene view.
    /// Uses SnapTransform as the source of truth for position, scale, and rotation.
    /// </summary>
    private static PlayableElementGizmoData PrepareGizmoData(PlayableElement element, float cellSize, float tolerance, Color color) {
      var gizmoData = new PlayableElementGizmoData {
        cellSize = cellSize,
        color = color,
        occupiedCells = new List<Vector2Int>(),
        rotatedCells = new List<Vector2Int>()
      };

      if (element == null) return gizmoData;

      // Use SnapTransform as the source of truth
      Transform snapTransform = element.SnapTransform != null ? element.SnapTransform : element.transform;
      
      // Get sprite renderer for validation
      SpriteRenderer spriteRenderer = GetSpriteRenderer(element);
      if (spriteRenderer == null || spriteRenderer.sprite == null) return gizmoData;

      // Set gizmo data using SnapTransform
      gizmoData.rotationAngle = snapTransform.rotation.eulerAngles.z;
      gizmoData.pivotWorldPosition = snapTransform.position;
      gizmoData.spriteRenderer = spriteRenderer;
      gizmoData.scale = snapTransform.lossyScale;
      gizmoData.spriteBounds = spriteRenderer.sprite.bounds;

      // Get the base footprint (non-rotated cells)
      var baseFootprint = new List<Vector2Int>(element.OccupiedCells);
      gizmoData.occupiedCells = baseFootprint;

      // Calculate the current world occupied cells considering the actual rotation
      if (gizmoData.HasRotation) {
        // Use the element's rotation calculation to get the actual rotated positions
        int normalizedRotation = Mathf.RoundToInt(gizmoData.rotationAngle / 90f) * 90;
        var worldCells = new List<Vector2Int>();
        foreach (var cell in element.GetWorldOccupiedCells(Vector2Int.zero, false, false, normalizedRotation)) {
          worldCells.Add(cell);
        }
        gizmoData.rotatedCells = worldCells;
      }
      else {
        gizmoData.rotatedCells = new List<Vector2Int>(gizmoData.occupiedCells);
      }

      return gizmoData;
    }

    /// <summary>
    /// Draws the occupied cell gizmos using SnapTransform position.
    /// </summary>
    private static void DrawOccupiedCellsGizmos(PlayableElementGizmoData gizmoData) {
      if (!gizmoData.IsValid || gizmoData.occupiedCells == null) return;

      // Use the pivotWorldPosition which is set from SnapTransform
      Vector3 elementPosition = gizmoData.pivotWorldPosition;
      //Debug.Log($"Drawing gizmos at position: {elementPosition}");
      // Only draw one set of gizmos - the current actual occupied cells
      Gizmos.color = gizmoData.color;
      
      //Debug.Log($"occupiedCells {string.Join(", ", gizmoData.occupiedCells)}");
      //Debug.Log($"rotatedCells {string.Join(", ", gizmoData.rotatedCells)}");

      // Use rotated cells if the element is rotated, otherwise use base cells
      var cellsToDraw = gizmoData.HasRotation ? gizmoData.rotatedCells : gizmoData.occupiedCells;
      //Debug.Log($"HasRotation {gizmoData.HasRotation}");
      //Debug.Log($"cellsToDraw: {string.Join(", ", cellsToDraw)}");

      foreach (var cell in cellsToDraw) {
        Vector3 cellCenter = GetCellWorldPosition(cell, elementPosition, gizmoData.cellSize);
        
        // Draw the cell cube - no need for rotation transformation since the cell positions are already in world space
        Gizmos.DrawWireCube(cellCenter, Vector3.one * gizmoData.cellSize * 0.9f);
      }

      // Optionally draw the original footprint with reduced opacity for reference when rotated
      if (gizmoData.HasRotation && gizmoData.occupiedCells != null) {
        Gizmos.color = new Color(gizmoData.color.r, gizmoData.color.g, gizmoData.color.b, 0.2f);
        foreach (var cell in gizmoData.occupiedCells) {
          Vector3 cellCenter = GetCellWorldPosition(cell, elementPosition, gizmoData.cellSize);
          Gizmos.DrawWireCube(cellCenter, Vector3.one * gizmoData.cellSize * 0.5f);
        }
      }
    }

    /// <summary>
    /// Calculates the world position of a grid cell center.
    /// Uses the same logic as PlayableElement.GetWorldOccupiedCells to ensure consistency.
    /// </summary>
    private static Vector3 GetCellWorldPosition(Vector2Int cell, Vector3 elementPosition, float cellSize) {
      // The element position is treated as the origin of the local grid
      // Cell (0,0) has its bottom-left corner at the element position
      Vector3 cellBottomLeft = elementPosition + new Vector3(cell.x * cellSize, cell.y * cellSize, 0);
      Vector3 cellCenter = cellBottomLeft + new Vector3(cellSize * 0.5f, cellSize * 0.5f, 0);
      return cellCenter;
    }

    /// <summary>
    /// Draws additional gizmos (pivot, bounds, rotation indicators).
    /// </summary>
    private static void DrawAdditionalGizmos(PlayableElementGizmoData gizmoData) {
      if (!gizmoData.IsValid) return;

      // Draw the pivot point for reference (SnapTransform position)
      Gizmos.color = Color.red;
      Gizmos.DrawSphere(gizmoData.pivotWorldPosition, 0.1f);

      // Draw the sprite bounds for reference
      Gizmos.color = Color.blue;
      DrawSpriteBounds(gizmoData);

      // Draw rotation indicator
      if (gizmoData.HasRotation) {
        Gizmos.color = Color.green;
        Vector3 rotationDirection = Quaternion.Euler(0, 0, gizmoData.rotationAngle) * Vector3.right;
        Gizmos.DrawRay(gizmoData.pivotWorldPosition, rotationDirection * gizmoData.cellSize * 2f);

        // Draw rotation arc
        DrawRotationArc(gizmoData.pivotWorldPosition, gizmoData.rotationAngle, gizmoData.cellSize * 1.5f);
      }
    }

    /// <summary>
    /// Gets sprite renderer from the PlayableElement, checking Model first.
    /// </summary>
    private static SpriteRenderer GetSpriteRenderer(PlayableElement element) {
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

    #region Helper Methods

    /// <summary>
    /// Settings structure for gizmo rendering.
    /// </summary>
    public struct GizmoSettings {
      public bool showGizmos;
      public bool showAdditionalGizmos;
      public bool showWhenNotSelected;
      public float cellSize;
      public float tolerance;
      public Color color;

      public static GizmoSettings Default => new GizmoSettings {
        showGizmos = true,
        showAdditionalGizmos = false,
        showWhenNotSelected = false,
        cellSize = DefaultCellSize,
        tolerance = DefaultTolerance,
        color = DefaultColor
      };
    }

    /// <summary>
    /// Gizmo data structure for drawing operations.
    /// </summary>
    public struct PlayableElementGizmoData {
      public float cellSize;
      public float rotationAngle;
      public List<Vector2Int> occupiedCells;
      public List<Vector2Int> rotatedCells;
      public Color color;
      public Vector3 pivotWorldPosition;
      public SpriteRenderer spriteRenderer;
      public Bounds spriteBounds;
      public Vector3 scale;

      // Helper properties for drawing
      public bool HasRotation => Mathf.Abs(rotationAngle) > 0.1f;
      public bool IsValid => spriteRenderer != null && spriteRenderer.sprite != null;
    }

    /// <summary>
    /// Gets gizmo settings for a specific element, using efficient registration system.
    /// </summary>
    public static GizmoSettings GetGizmoSettingsForElement(PlayableElement element) {
      // Check registered custom settings first (most efficient)
      if (_customSettings.TryGetValue(element, out var customSettings)) {
        return customSettings;
      }

      // Only search for editors if no registration exists (fallback for legacy support)
      var editors = Resources.FindObjectsOfTypeAll<PlayableElementEditor>();
      foreach (var editor in editors) {
        if (editor.target == element) {
          var settings = new GizmoSettings {
            showGizmos = PlayableElementEditor.ShowOccupiedCells(element),  // Fixed: Use editor instance property
            showAdditionalGizmos = false,
            showWhenNotSelected = false,
            cellSize = editor.GizmoCellSize,
            tolerance = editor.GizmoTolerance,
            color = editor.GizmoColor
          };

          // Cache it for next time to avoid future expensive lookups
          RegisterGizmoSettings(element, settings);
          return settings;
        }
      }

      // Fallback to default settings
      return GizmoSettings.Default;
    }

    /// <summary>
    /// Registration system for efficient gizmo settings lookup.
    /// </summary>
    private static readonly Dictionary<PlayableElement, GizmoSettings> _customSettings = new Dictionary<PlayableElement, GizmoSettings>();

    /// <summary>
    /// Registers gizmo settings for a specific element.
    /// This is the preferred way for editors to provide their settings.
    /// </summary>
    public static void RegisterGizmoSettings(PlayableElement element, GizmoSettings settings) {
      if (element == null) return;
      _customSettings[element] = settings;
    }

    /// <summary>
    /// Unregisters gizmo settings for a specific element.
    /// Should be called when editors are disabled to prevent memory leaks.
    /// </summary>
    public static void UnregisterGizmoSettings(PlayableElement element) {
      if (element == null) return;
      _customSettings.Remove(element);
    }

    /// <summary>
    /// Clears all registered settings. Useful for cleanup or reset scenarios.
    /// </summary>
    public static void ClearAllRegisteredSettings() {
      _customSettings.Clear();
    }

    /// <summary>
    /// Gets the number of registered elements (for debugging).
    /// </summary>
    public static int GetRegisteredElementsCount() => _customSettings.Count;

    private static void DrawSpriteBounds(PlayableElementGizmoData gizmoData) {
      Vector3[] corners = new Vector3[4];
      Vector3 size = new Vector3(
        gizmoData.spriteBounds.size.x * Mathf.Abs(gizmoData.scale.x),
        gizmoData.spriteBounds.size.y * Mathf.Abs(gizmoData.scale.y),
        0
      );

      Vector3 center = new Vector3(
        (gizmoData.spriteBounds.min.x + gizmoData.spriteBounds.max.x) * 0.5f * gizmoData.scale.x,
        (gizmoData.spriteBounds.min.y + gizmoData.spriteBounds.max.y) * 0.5f * gizmoData.scale.y,
        0
      );

      // Define corners relative to center
      corners[0] = center + new Vector3(-size.x * 0.5f, -size.y * 0.5f, 0);
      corners[1] = center + new Vector3(size.x * 0.5f, -size.y * 0.5f, 0);
      corners[2] = center + new Vector3(size.x * 0.5f, size.y * 0.5f, 0);
      corners[3] = center + new Vector3(-size.x * 0.5f, size.y * 0.5f, 0);

      Quaternion rotation = Quaternion.Euler(0, 0, gizmoData.rotationAngle);
      for (int i = 0; i < corners.Length; i++) {
        corners[i] = rotation * corners[i] + gizmoData.pivotWorldPosition;
      }

      for (int i = 0; i < corners.Length; i++) {
        int nextIndex = (i + 1) % corners.Length;
        Gizmos.DrawLine(corners[i], corners[nextIndex]);
      }
    }

    private static void DrawRotationArc(Vector3 center, float angle, float radius) {
      const int segments = 16;
      float normalizedAngle = angle * Mathf.Deg2Rad;
      Vector3 startPoint = center + Vector3.right * radius;
      Vector3 lastPoint = startPoint;

      for (int i = 1; i <= segments; i++) {
        float t = (float)i / segments;
        float currentAngle = normalizedAngle * t;
        Vector3 currentPoint = center + new Vector3(
          Mathf.Cos(currentAngle) * radius,
          Mathf.Sin(currentAngle) * radius,
          0
        );
        Gizmos.DrawLine(lastPoint, currentPoint);
        lastPoint = currentPoint;
      }

      if (Mathf.Abs(angle) > 5f) {
        Vector3 arrowDir = Quaternion.Euler(0, 0, angle - 15f) * Vector3.right;
        Vector3 arrowEnd = center + arrowDir * radius * 0.9f;
        Gizmos.DrawLine(lastPoint, arrowEnd);

        arrowDir = Quaternion.Euler(0, 0, angle + 15f) * Vector3.right;
        arrowEnd = center + arrowDir * radius * 0.9f;
        Gizmos.DrawLine(lastPoint, arrowEnd);
      }
    }

    #endregion
  }
}
#endif