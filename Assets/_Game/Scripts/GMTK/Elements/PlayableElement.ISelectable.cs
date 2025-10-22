using UnityEngine;
using Ameba;
using MoreMountains.Feedbacks;

namespace GMTK {

  /// <summary>
  /// PlayableElement partial class implementing ISelectable interface functionality.
  /// Handles selection behavior and delegates to PointerElementComponent, if present.
  /// </summary>
  public partial class PlayableElement : ISelectable {

    [Header("Selection Settings")]
    [Tooltip("Whether this element is currently selected by a pointer")]
    [SerializeField] protected bool _isSelected = false;
    [Tooltip("If true, this element can be selected by a pointer")]
    [SerializeField] protected bool _canSelect = true;

    [MMFCondition("_canSelect", true)]
    [Tooltip("Selection trigger methods. Multiple options can be selected.")]
    public SelectionTrigger SelectionTriggers = SelectionTrigger.OnClick;
    [MMFCondition("_canSelect", true)]
    [Tooltip("Selection accuracy within element boundaries (0 = less strict, 1 = strictly within boundaries)")]
    [Range(0f, 1f)]
    public float Accuracy = 0.8f;
    [MMFCondition("_canSelect", true)]
    [Tooltip("Maximum offset from element boundaries considered valid when accuracy is 0")]
    [Range(0f, 5f)]
    public float MaxOffset = 2f;

    // ISelectable interface properties
    public bool IsSelected { get => _isSelected; protected set => _isSelected = value; }

    public bool CanSelect => _canSelect;

    public bool HasAnySelectionTrigger => SelectionTriggers != SelectionTrigger.None;

    public Transform SelectTransform => SnapTransform != null ? SnapTransform : transform;

    #region ISelectable Implementation

    public void MarkSelected(bool selected = true) {
      
      if (IsSelected == selected || !CanSelect) return; //avoid redundant state change
      IsSelected = selected;
      this.LogDebug($"Element {name} marked as {(IsSelected ? "Selected" : "Deselected")}");

      var peEvent = selected ? PlayableElementEventType.Selected : PlayableElementEventType.Deselected;
      var gameEvent = selected ? GameEventType.ElementSelected : GameEventType.ElementDeselected;

      // If selection is triggered on click, raise selected or unselected event
      if (HasSelectionTrigger(SelectionTrigger.OnClick)) {
        var eventArgs = RaiseGameEvent(gameEvent, peEvent);

        // call pointer component - if present - to handle the selection event effects
        if (!TryDelegateToPointerComponent(eventArgs)) {
          this.LogWarning($"No PointerElementComponent found on {name} to handle selection event");
        }

        // inner event 
        RaisePlayableElementEvent(peEvent);
      }
      else {
        //in the future maybe there's value on capturing a click over a non-selectable object
        this.LogWarning($"Selection event raised on {name} but SelectionTrigger does not include OnClick");
      }
    }

    public void EnableSelectable(bool selectable = true) {
      if (CanSelect == selectable) return; //avoid redundant state change
      // Note: We don't have a direct backing field for CanSelect since it's derived from SelectionTriggers
      // default select is onclick
      SelectionTriggers = selectable ? SelectionTrigger.OnClick : SelectionTrigger.None;
      
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

  }
}