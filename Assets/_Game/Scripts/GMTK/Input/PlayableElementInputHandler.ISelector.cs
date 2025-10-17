using Ameba;
using Unity.Android.Gradle;
using UnityEngine;

namespace GMTK {

  /// <summary>
  /// PlayableElementInputHandler partial class implementing ISelector interface functionality.
  /// Handles selection logic and element selection state management.
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
      Vector2 worldPos2D = new Vector2(worldPosition.x, worldPosition.y);
      RaycastHit2D hit = Physics2D.Raycast(worldPos2D, Vector2.zero);

      if (hit && hit.collider != null) {
        if (hit.collider.gameObject.TryGetComponent(out PlayableElement foundElement)) {
          element = foundElement;
          return TrySelect(foundElement);
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
      if (!CanSelect || element == null) return false;
      this.LogDebug($"Attempting to select element: {element.name} CanSelect: {element.CanSelect}");
      this.LogDebug($"IsSelecting: {IsSelecting} CanSelect {CanSelect}");

      if (_selectedElement != null)
        this.LogDebug($"_selectedElement: {_selectedElement.name}");
      else this.LogDebug($"_selectedElement: null");

      // Check if element can be selected
      if (!element.CanSelect) return false;

      // If we already have a selected element, deselect it first
      if (_selectedElement != null && _selectedElement != element) {
        this.LogDebug($"Deselecting current element");
        DeselectCurrentElement();
      }

      // if selected is null (first time) or different from the new element, select it
      if (_selectedElement == null || _selectedElement != element) {
        _selectedElement = element;

        this.LogDebug($"Marking element: {element.name}");
        element.MarkSelected(true);

        // Trigger selection events
        _eventsChannel.Raise(GameEventType.ElementSelected,
            new GridSnappableEventArgs(ConvertToGridSnappable(element), _pointerScreenPos, _pointerWorldPos));
        this.LogDebug($"Selected element: {element.name}");
        return true;
      }

      return false;
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

        //legacy event for GridSnappable
        _eventsChannel.Raise(GameEventType.ElementDeselected,
            new GridSnappableEventArgs(ConvertToGridSnappable(elementToDeselect), _pointerScreenPos, _pointerWorldPos));

        this.LogDebug($"Deselected element: {elementToDeselect.name}");
      }
    }

    #endregion

    #region Public Selection API

    /// <summary>
    /// Programmatically select an element
    /// </summary>
    public bool SelectElement(PlayableElement element) {
      return TrySelect(element);
    }

    /// <summary>
    /// Programmatically deselect the current element
    /// </summary>
    public void DeselectElement() {
      DeselectCurrentElement();
    }

    /// <summary>
    /// Check if an element is currently selected
    /// </summary>
    public bool IsElementSelected(PlayableElement element) {
      return _selectedElement == element;
    }

    #endregion
  }
}