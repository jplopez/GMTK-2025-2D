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
    [SerializeField] [DisplayWithoutEdit] protected bool _canHover = true;

    // IHoverable interface properties
    // PlayableElements can always be hovered
    public bool CanHover => _canHover; 
    public bool IsHovered => _isHovered;

    public Transform HoverTransform => SnapTransform != null ? SnapTransform : transform;

    #region IHoverable Implementation

    public void MarkHovered(bool hovered = true) {
      if (_isHovered == hovered) return;
      _isHovered = hovered && _canHover;

      this.Log($"Element {name} marked as {(hovered ? "Hovered" : "Unhovered")}");
      var eventArgs = BuildEventArgs(hovered ? PlayableElementEventType.PointerOver : PlayableElementEventType.PointerOut);
      //first try to delegate to component if it exists
      if (!TryDelegateToPointerComponent(eventArgs)) {
        this.LogWarning($"No PointerElementComponent found on {name} to handle hover event");
      }
      _gameEventChannel.Raise(GameEventType.PlayableElementEvent, eventArgs);
    }

    public void EnableHovering(bool enable=true) {
      if (_canHover == enable) return;
      _canHover = enable;
      if (!_canHover && _isHovered) {
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

    public void OnPointerEnter() => OnHover();
    public void OnPointerExit() => OnUnhover();

    #endregion
  }
}