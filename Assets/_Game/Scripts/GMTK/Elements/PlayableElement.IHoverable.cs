using Ameba;
using UnityEngine;

namespace GMTK {

  /// <summary>
  /// PlayableElement partial class implementing IHoverable interface functionality.
  /// Handles hover behavior and visual feedback.
  /// </summary>
  public partial class PlayableElement : IHoverable {

    // Private hover state
    private bool _isHovered = false;

    // IHoverable interface properties
    public bool CanHover => true; // PlayableElements can always be hovered
    public bool IsHovered => _isHovered;
    public Transform HoverTransform => SnapTransform != null ? SnapTransform : transform;

    #region IHoverable Implementation

    public void MarkHovered(bool hovered = true) {
      if (_isHovered == hovered) return;

      _isHovered = hovered;

      if (hovered) {
        OnHover();
      }
      else {
        OnUnhover();
      }
    }

    public void OnHover() {
      _isHovered = true;

      // Trigger PlayableElement event for components
      var eventArgs = new PlayableElementEventArgs(this, transform.position, PlayableElementEventType.PointerOver);
      OnPlayableElementEvent?.Invoke(eventArgs);

      SetGlow(true);
      this.LogDebug($"Hover started on {name}");
    }

    public void OnHoverUpdate() {
      if (!_isHovered) return;

      // Continuous hover update - can be used for animations or state updates
      // Currently no specific behavior needed
    }

    public void OnUnhover() {
      _isHovered = false;

      // Trigger PlayableElement event for components
      var eventArgs = new PlayableElementEventArgs(this, transform.position, PlayableElementEventType.PointerOut);
      OnPlayableElementEvent?.Invoke(eventArgs);

      SetGlow(false);
      this.LogDebug($"Hover ended on {name}");
    }

    public void OnHoverEnabled() {
      // Handle when hovering is enabled
      // No specific behavior needed for now
    }

    public void OnHoverDisabled() {
      // Handle when hovering is disabled
      if (_isHovered) {
        MarkHovered(false);
      }
    }

    #endregion

    #region Hover Visual Feedback

    public virtual void SetGlow(bool active) {
      if (HighlightModel != null) HighlightModel.SetActive(active);
    }

    public void OnPointerEnter() => OnHover();
    public void OnPointerExit() => OnUnhover();

    #endregion
  }
}