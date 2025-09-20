using Ameba;
using System;
using UnityEngine;

namespace GMTK {


  /// <summary>
  /// PlayableElement partial class implementing ISelectable interface functionality.
  /// Handles selection behavior and delegates to PointerElementComponent, if present.
  /// </summary>
  public partial class PlayableElement : ISelectable {

    [Tooltip("If true, this element can be selected.")]
    [SerializeField] protected bool _isSelected = false;
    [Tooltip("If true, this element can be selected.")]
    [SerializeField] protected bool _canSelect = true;

    // ISelectable interface properties
    public bool IsSelected { get => _isSelected; protected set => _isSelected = value; } 
    //=> _pointerComponent != null && _pointerComponent.IsSelected;
    public bool CanSelect { get => _canSelect; set => _canSelect = value; } 
    //=> _pointerComponent != null && _pointerComponent.CanSelect.

    public Transform SelectTransform => SnapTransform != null ? SnapTransform : transform;


    #region ISelectable Implementation

    public void MarkSelected(bool selected = true) {
      if (IsSelected == selected) return; //avoid redundant state change
      IsSelected = selected && CanSelect;
      this.Log($"Element {name} marked as {(IsSelected ? "Selected" : "Deselected")}");
      var eventArgs = BuildEventArgs(selected ? PlayableElementEventType.Selected : PlayableElementEventType.Deselected);
      // first try to delegate to component if it exists
      if (!TryDelegateToPointerComponent(eventArgs)) {
        this.LogWarning($"No PointerElementComponent found on {name} to handle selection event");
      }
      // then raise event for other components to handle
      _gameEventChannel.Raise(GameEventType.PlayableElementEvent, eventArgs);
    }

    public void EnableSelectable(bool selectable = true) {
      if (CanSelect == selectable) return; //avoid redundant state change
      CanSelect = selectable;
      if (!CanSelect && IsSelected) {
        MarkSelected(false);
      }
    }

    public void OnSelect() => MarkSelected(true);
    public void OnDeselect() => MarkSelected(false);

    public void OnSelectUpdate() {
      // Delegate to component if it implements this functionality
      // For now, we don't have specific update behavior
    }

    public void OnSelectEnabled() => EnableSelectable(true);
    public void OnSelectDisabled() => EnableSelectable(false);


    #endregion

    #region SelectableElementComponent Reference

    /// <summary>
    /// This methods invokes the PlayableElementEvent handlers for the PointerElementComponent, if it exists.<br/>
    /// The purpose is to prioritize the handling by that component, before raising the event to other listeners.
    /// </summary>
    /// <param name="eventArgs"></param>
    /// <returns>true if the event was handled by the PointerElementComponent. false otherwise</returns>
    private bool TryDelegateToPointerComponent(PlayableElementEventArgs eventArgs) {
      if (_pointerComponent == null) return false;
      this.Log($"Delegating {eventArgs.EventType} event to PointerElementComponent on {name}");
      switch (eventArgs.EventType) {
        case PlayableElementEventType.Selected:
          _pointerComponent.OnSelected(eventArgs);
          break;
        case PlayableElementEventType.Deselected:
          _pointerComponent.OnDeselected(eventArgs);
          break;
      }
      return true;
    }

    #endregion
  }
}