using Ameba;
using UnityEngine;

namespace GMTK {

  /// <summary>
  /// ISelector interface implementation designed to handle Input Events related to selecting PlayableElement.
  /// Handles OnSelect and deselection input events.
  /// </summary>
  public partial class PlayableElementInputHandler : ISelector<PlayableElement> {

    [Header("Selection Settings")]
    [SerializeField] private bool _canSelect = true;
    [SerializeField]
    private SelectionTrigger _selectionTriggers = SelectionTrigger.OnClick | SelectionTrigger.OnKeyPress;
    
    public bool HasSelectionTrigger(SelectionTrigger trigger) => (_selectionTriggers & trigger) != 0;
    
    // ISelector<PlayableElement> implementation
    public bool CanSelect {
      get => _canSelect;
      set => _canSelect = value;
    }
    public bool IsSelecting => _activeElement != null;
    
    public PlayableElement SelectedElement => (_activeElement && _activeElement.IsSelected)? _activeElement : null;

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

    // ReSharper disable Unity.PerformanceAnalysis
    public bool TrySelect(PlayableElement element) {

      var selectedElementName = _activeElement != null ? _activeElement.name : "null";
      this.LogDebug($"[TrySelect] element: '{element.name}' selectable? {element.CanSelect} " +
                    $"_activeElement: {selectedElementName}");
      
      if (!CanSelect || element == null || !element.CanSelect) return false;

      // deselect current if different from new
      if (_activeElement != null && _activeElement != element) {
        DeselectActiveElement();
      }

      element.MarkSelected(true);
      _activeElement = element;

      this.LogDebug($"[TrySelect] Selected element: '{element.name}'");
      return true;
    }

    /// <summary>
    /// Attempts to deselect <see cref="SelectedElement"/>. 
    /// </summary>
    /// <returns> <see langword="true"/> if an element was deselected, <see langword="false"/> if there was no active selection to deselect</returns>
    public bool TryDeselect() {
      if (_activeElement == null) return false;
      DeselectActiveElement();
      return true;
    }

    private void DeselectActiveElement() {
      if (_activeElement != null) {
        var elementToDeselect = _activeElement;
        _activeElement = null;
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