using Ameba;
using System;
using UnityEngine;

namespace GMTK {


  /// <summary>
  /// PlayableElement partial class implementing ISelectable interface functionality.
  /// Handles selection behavior and delegates to SelectableElementComponent.
  /// </summary>
  public partial class PlayableElement : ISelectable {

    // ISelectable interface properties - delegated to SelectableElementComponent
    public bool IsSelected => _selectableComponent != null && _selectableComponent.IsSelected;
    public bool CanSelect => _selectableComponent != null && _selectableComponent.CanSelect;
    public Transform SelectTransform => SnapTransform != null ? SnapTransform : transform;

    #region ISelectable Implementation (Delegated to SelectableElementComponent)

    public void MarkSelected(bool selected = true) {
      if (_selectableComponent != null) {
        _selectableComponent.MarkSelected(selected);
      }
      else {
        this.LogWarning($"[PlayableElement] SelectableElementComponent not found on {name}. Cannot mark selected.");
      }
    }

    public void EnableSelectable(bool selectable = true) {
      if (_selectableComponent != null) {
        _selectableComponent.EnableSelectable(selectable);
      }
      else {
        this.LogWarning($"[PlayableElement] SelectableElementComponent not found on {name}. Cannot enable/disable selectable.");
      }
    }

    public void OnSelect() {
      if (_selectableComponent != null) {
        _selectableComponent.OnSelect();
      }

      // Trigger PlayableElement event for components
      var eventArgs = new PlayableElementEventArgs(this, transform.position, PlayableElementEventType.Selected);
      OnPlayableElementEvent?.Invoke(eventArgs);
    }

    public void OnSelectUpdate() {
      if (_selectableComponent != null) {
        // Delegate to component if it implements this functionality
        // For now, we don't have specific update behavior
      }
    }

    public void OnDeselect() {
      if (_selectableComponent != null) {
        _selectableComponent.OnDeselect();
      }

      // Trigger PlayableElement event for components
      var eventArgs = new PlayableElementEventArgs(this, transform.position, PlayableElementEventType.Deselected);
      OnPlayableElementEvent?.Invoke(eventArgs);
    }

    public void OnSelectDisabled() {
      // Handle when selection is disabled
      if (IsSelected) {
        MarkSelected(false);
      }
    }

    public void OnSelectEnabled() {
      // Handle when selection is enabled
      // No specific behavior needed for now
    }

    #endregion
  }
}