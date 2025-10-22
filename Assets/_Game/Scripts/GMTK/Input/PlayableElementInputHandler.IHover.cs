using Ameba;
using UnityEngine;

namespace GMTK {

  /// <summary>
  /// IHover interface implementation designed to handle Input Events related to hovering over PlayableElement.
  /// Handles input events for OnElementHovered, OnElementUnhovered
  /// </summary>
  public partial class PlayableElementInputHandler : IHover<PlayableElement> {

    [Header("Hover Settings")]
    [SerializeField] private bool _canHover = true;
    [SerializeField] private PlayableElement _hoveredElement;

    // IHover<PlayableElement> implementation
    public bool CanHover {
      get => _canHover;
      set => _canHover = value;
    }
    public bool IsHovering => _hoveredElement != null;
    public PlayableElement HoveredElement => _hoveredElement;

    #region IHover<PlayableElement> Implementation

    public bool TryGetHoverableAt(Vector3 worldPosition, out PlayableElement element) {
      element = null;

      if (!CanHover) return false;

      // Find hoverable element at world position
      Vector2 worldPos2D = new(worldPosition.x, worldPosition.y);
      RaycastHit2D hit = Physics2D.Raycast(worldPos2D, Vector2.zero);

      if (hit && hit.collider != null) {
        if (hit.collider.gameObject.TryGetComponent(out PlayableElement foundElement)) {
          // Check if element can be hovered
          if (foundElement.CanHover) {
            element = foundElement;
            return true;
          }
        }
      }

      return false;
    }

    public bool TryGetHoverableAt(Vector2 screenPosition, out PlayableElement element) {
      element = null;

      if (!CanHover) return false;

      // Convert screen position to world position
      Camera camera = Camera.main;
      if (camera == null) {
        this.LogWarning("[PlayableElementInputHandler] No main camera found for screen to world conversion");
        return false;
      }

      Vector3 worldPos = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, camera.nearClipPlane));
      return TryGetHoverableAt(worldPos, out element);
    }

    public void StartHover(PlayableElement element) {
      if (!CanHover || element == null || !element.CanHover) return;

      // If already hovering over this element, do nothing
      if (_hoveredElement == element) return;

      // Stop hovering over current element first
      if (_hoveredElement != null) {
        StopHover();
      }

      // Start hovering over new element
      _hoveredElement = element;
      _lastElementOver = element; // Update for compatibility
      IsOverElement = true;

      // Notify the element
      element.MarkHovered(true);

      this.LogDebug($"Started hovering over element: {element.name}");
    }

    public void StopHover() {
      if (_hoveredElement == null) return;

      var elementToStop = _hoveredElement;
      _hoveredElement = null;
      IsOverElement = false;

      // Clear last element over if it was the hovered element
      if (_lastElementOver == elementToStop) {
        _lastElementOver = null;
      }

      // Notify the element
      elementToStop.MarkHovered(false);

      this.LogDebug($"Stopped hovering over element: {elementToStop.name}");
    }

    public void UpdateHover() {
      if (!CanHover) {
        // If hover is disabled, stop any current hover
        if (IsHovering) {
          StopHover();
        }
        return;
      }

      // Check for hoverable element at current pointer position
      if (TryGetHoverableAt(_pointerWorldPos, out PlayableElement element)) {
        // Start hovering if not already hovering over this element
        if (_hoveredElement != element) {
          StartHover(element);
        }
      }
      else {
        // No hoverable element found, stop hovering
        if (IsHovering) {
          StopHover();
        }
      }
    }

    #endregion
  }
}