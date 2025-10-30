using Ameba;
using UnityEngine;

namespace GMTK {

  /// <summary>
  /// PlayableElement partial class implementing IHoverable interface functionality.
  /// Handles hover behavior and visual feedback.
  /// </summary>
  public partial class PlayableElement : IHoverable {


    [Tooltip("Whether the element is currently hovered by a pointer")]
    [SerializeField] protected bool _isHovered = false;
    [Tooltip("If true, this element can be hovered by a pointer")]
    [SerializeField][DisplayWithoutEdit] protected bool _canHover = true;

    [Tooltip("Minimum time pointer must hover over element to select it (seconds)")]
    [Range(0.1f, 5f)]
    public float HoverThreshold = 0.2f;

    // IHoverable interface properties
    // PlayableElements can always be hovered
    public bool CanHover => _canHover; 
    public bool IsHovered => _isHovered;

    public Transform HoverTransform => SnapTransform != null ? SnapTransform : transform;

    #region IHoverable Implementation

    public void MarkHovered(bool hovered = true) {
      
      if (_isHovered == hovered) return;
      _isHovered = hovered && CanHover;
      this.LogDebug($"Element {name} marked as {(hovered ? "Hovered" : "Unhovered")}");

      // PlayableElement event type
      var peEvent = hovered ? PlayableElementEventType.PointerOver : PlayableElementEventType.PointerOut;
      var gameEvent = hovered ? GameEventType.ElementHovered : GameEventType.ElementUnhovered;

      // Raise main hover/unhover event
      var eventArgs = RaiseGameEvent(gameEvent, peEvent);

      //if selection is trigger on hover, raise selected/unselected event as well
      if (HasSelectionTrigger(SelectionTrigger.OnHover)) {
        var selectionEvent = hovered ? GameEventType.ElementSelected : GameEventType.ElementDeselected;
        _isSelected = hovered; //directly set selected state to avoid redundant checks
        RaiseGameEvent(selectionEvent, peEvent);
      }

      //Call pointer component, if present, to handle the hovered event effects
      if (!TryDelegateToPointerComponent(eventArgs)) {
        this.LogWarning($"No PointerElementComponent found on {name} to handle hover event");
      }
      //unity event
      var unityEvent = hovered ? OnHovered : OnUnhovered;
      unityEvent?.Invoke(eventArgs);
      //RaisePlayableElementEvent(peEvent);
    }

    public void EnableHovering(bool enable=true) {
      if (CanHover == enable) return;
      _canHover = enable;
      //_canHover = enable;
      if (!CanHover && _isHovered) {
        MarkHovered(false);
      }
    }

    public void OnHover() => MarkHovered(true);
    public void OnUnhover() => MarkHovered(false);

    public void OnHoverUpdate() {
      if (!_isHovered) return;
      // Continuous hover update - can be used for animations or state updates
      // Currently no specific behavior needed
    }

    public void OnHoverEnabled() => EnableHovering(true);
    public void OnHoverDisabled() => EnableHovering(false);

    public void OnPointerOver() => OnHover();
    public void OnPointerOut() => OnUnhover();

    #endregion
  }
}