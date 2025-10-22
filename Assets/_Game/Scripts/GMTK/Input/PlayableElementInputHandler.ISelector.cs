using Ameba;
using UnityEngine;

namespace GMTK {

  /// <summary>
  /// ISelector interface implementation designed to handle Input Events related to selecting PlayableElement.
  /// Handles OnElementSelected and deselection input events.
  /// </summary>
  public partial class PlayableElementInputHandler : ISelector<PlayableElement> {

    [Header("Selection Settings")]
    [SerializeField] private bool _canSelect = true;
    [SerializeField] private PlayableElement _selectedElement;

    // ISelector<PlayableElement> implementation
    public bool CanSelect {
      get => _canSelect;
      set => _canSelect = value;
    }
    public bool IsSelecting => _selectedElement != null;
    public PlayableElement SelectedElement => _selectedElement;

    #region ISelector<PlayableElement> Implementation

    public bool TrySelect(Vector3 worldPosition, out PlayableElement element) {
      element = null;

      if (!CanSelect) return false;

      // Find element at world position using raycast
      Vector2 worldPos2D = new(worldPosition.x, worldPosition.y);
      RaycastHit2D hit = Physics2D.Raycast(worldPos2D, Vector2.zero);

      if (hit && hit.collider != null) {
        if (hit.collider.gameObject.TryGetComponent(out PlayableElement foundElement)) {
          //validates click using the element accuracy parameters
          if (IsValidClickPosition(element, worldPosition)) {
            element = foundElement;
            return TrySelect(foundElement);
          }
        }
      }

      return false;
    }

    public bool TrySelect(Vector2 screenPosition, out PlayableElement element) {
      element = null;

      if (!CanSelect) return false;

      // Convert screen position to world position
      Camera camera = Camera.main;
      if (camera == null) {
        this.LogWarning("No main camera found for screen to world conversion");
        return false;
      }

      Vector3 worldPos = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, camera.nearClipPlane));
      return TrySelect(worldPos, out element);
    }

    public bool TrySelect(PlayableElement element) {
      if (!CanSelect || element == null || !element.CanSelect) return false;
      this.LogDebug($"Attempting to select element: {element.name} CanSelect: {element.CanSelect}");
      this.LogDebug($"IsSelecting: {IsSelecting} CanSelect {CanSelect}");
      this.LogDebug("_selectedElement: " + ((_selectedElement == null) ? "null" : _selectedElement.name));

      // deselect current if different than new
      if (_selectedElement != null && _selectedElement != element) {
        this.LogDebug($"Deselecting current element");
        DeselectCurrentElement();
      }

      this.LogDebug($"Marking element: {element.name}");
      element.MarkSelected(true);
      _selectedElement = element;

      this.LogDebug($"Selected element: {element.name}");
      return true;
    }

    public bool TryDeselect() {
      if (_selectedElement == null) return false;
      DeselectCurrentElement();
      return true;
    }

    private void DeselectCurrentElement() {
      if (_selectedElement != null) {
        var elementToDeselect = _selectedElement;
        _selectedElement = null;
        elementToDeselect.MarkSelected(false);

        this.LogDebug($"Deselected element: {elementToDeselect.name}");
      }
    }

    private bool IsValidClickPosition(PlayableElement elem, Vector3 worldPosition) {
      if (elem.InteractionCollider == null) return true;

      // Calculate effective offset based on accuracy
      float effectiveOffset = elem.MaxOffset * (1f - elem.Accuracy);

      // Check if position is within collider bounds
      bool withinCollider = elem.InteractionCollider.bounds.Contains(worldPosition);

      // Always valid if within collider
      if (withinCollider) return true;

      // Strict accuracy, must be within collider
      if (effectiveOffset <= 0f) return false;

      // Check if within offset distance
      Vector3 closestPoint = elem.InteractionCollider.ClosestPoint(worldPosition);
      float distance = Vector3.Distance(worldPosition, closestPoint);

      return distance <= effectiveOffset;
    }

    #endregion

  }
}