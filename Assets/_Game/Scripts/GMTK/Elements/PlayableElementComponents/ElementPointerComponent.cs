using System;
using System.Collections;
using UnityEngine;
using MoreMountains.Feedbacks;

namespace GMTK {

  /// <summary>
  /// Component that enhances the PlayableElement with pointer-based selection functionality.<br/>
  /// This component enables interactions when the element is selected, hovered or dragged (mouse, touch input or both) as well as playing feedbacks.<br/>
  /// </summary>
  [AddComponentMenu("GMTK/Playable Element Components/Element Pointer Component")]
  public class ElementPointerComponent : PlayableElementComponent {

    [Header("Select Feedbacks")]
    public MMF_Player OnSelectedFeedback;
    public MMF_Player OnDeselectedFeedback;
    [Space]

    [Header("Hover Feedbacks")]
    public MMF_Player OnHoverFeedback;
    public MMF_Player OnUnhoverFeedback;

    private Coroutine _hoverCoroutine;

    #region PlayableElementComponent Overrides

    protected override void Initialize() => ExcludeAllEvents();

    protected override bool Validate() => _playableElement != null;
    protected override void ResetComponent() {
      if (_hoverCoroutine != null) {
        StopCoroutine(_hoverCoroutine);
        _hoverCoroutine = null;
      }
    }

    protected override void FinalizeComponent() {
      if (_hoverCoroutine != null) {
        StopCoroutine(_hoverCoroutine);
      }
    }

    #endregion

    #region Event Listeners

    /// <summary>
    /// Event handler for PlayableElementEventType.OnHovered events.
    /// </summary>
    /// <param name="evt"></param>
    public void OnPointerOver(PlayableElementEventArgs evt) => ToggleHover(evt, true);

    /// <summary>
    /// Event handler for PlayableElementEventType.OnUnhovered events.
    /// </summary>
    /// <param name="evt"></param>
    public void OnPointerOut(PlayableElementEventArgs evt) => ToggleHover(evt, false);

    private void ToggleHover(PlayableElementEventArgs args, bool toggle = true) {
      if (args.Element != null && args.Element == _playableElement) {
        this.LogDebug($"Toggling Hover for {_playableElement.name} to '{(toggle ? "Over" : "Out")}'");

        if (_hoverCoroutine != null) {
          StopCoroutine(_hoverCoroutine);
          _hoverCoroutine = null;
        }

        // check if hover triggers selection
        if (HasSelectionTrigger(SelectionTrigger.OnHover) && _playableElement.CanSelect) {
            this.LogDebug($"Element {_playableElement.name} selected by hover, delegating to " + (toggle ? "OnSelect" : "OnDeselected"));
          //we check if toggle matches element.IsSelected, because
          //that change occurs in the PlayableElement before raising the event
          if (toggle && _playableElement.IsSelected) {
            OnSelected(args);
            return;
          }
          else if (!toggle && !_playableElement.IsSelected) {
            OnDeselected(args);
            return;
          }
        }
        // otherwise we just play unhover feedback
        else {
          if (toggle) { //pointer over
            //skip unhover feedback if we are selected, to avoid feedback overlap
            if (_playableElement.IsSelected) return;

            this.LogDebug($"Starting hover coroutine for {_playableElement.name}");
            if (_hoverCoroutine != null) {
              StopCoroutine(_hoverCoroutine);
            }
            _hoverCoroutine = StartCoroutine(HoverSelectionCoroutine());
          }
          else { //pointer out
            //skip unhover feedback if we are selected, to avoid feedback overlap
            if (!_playableElement.IsSelected) PlayFeedback(OnUnhoverFeedback);
          }
        }
      }
    }

    public override void OnHovered(PlayableElementEventArgs args) => OnPointerOver(args);
    public override void OnUnhovered(PlayableElementEventArgs args) => OnPointerOut(args);

    /// <summary>
    /// Event handler for PlayableElementEventType.OnSelect events.
    /// </summary>
    /// <param name="evt"></param>
    public override void OnSelected(PlayableElementEventArgs evt) => ToggleSelected(evt, true);

    /// <summary>
    /// Event handler for PlayableElementEventType.OnDeselected events.
    /// </summary>
    /// <param name="evt"></param>
    public override void OnDeselected(PlayableElementEventArgs evt) => ToggleSelected(evt, false);

    private void ToggleSelected(PlayableElementEventArgs args, bool selected) {
      if (args.Element != null && args.Element == _playableElement) {
        this.LogDebug($"Toggle selected '{selected}' for {_playableElement.name}");
        if (!_playableElement.CanSelect) return;
        ApplyFeedback(selectedChanged: true);
      }
    }

    #endregion

    #region Select/Hover logic

    private IEnumerator HoverSelectionCoroutine() {
      yield return new WaitForSeconds(_playableElement.HoverThreshold);
      this.LogDebug($"Hover threshold reached for {_playableElement.name}");
      // if hovering after threshold we apply hover selection logic
      if (_playableElement.IsHovered) {

        // select if still hovering and selection by hover is enabled
        if (HasSelectionTrigger(SelectionTrigger.OnHover) && _playableElement.CanSelect && !_playableElement.IsSelected) {
          this.LogDebug($"Element {_playableElement.name} selected via hovering");
          ApplyFeedback(selectedChanged: true, hoverChanged: true);
        }
        // play hover feedback if not selecting
        else {
          this.LogDebug($"Element {_playableElement.name} hover feedback played");
          ApplyFeedback(hoverChanged: true);
        }
      }
      // reset coroutine reference
      _hoverCoroutine = null;
    }
    private bool HasSelectionTrigger(SelectionTrigger trigger) => (_playableElement.SelectionTriggers & trigger) != 0;

    /// <summary>
    /// Plays the correct feedback based on selection and hover state changes.
    /// </summary>
    /// <param name="selectedChanged"></param>
    /// <param name="hoverChanged"></param>
    private void ApplyFeedback(bool selectedChanged = false, bool hoverChanged = false) {
      // select/deselect feedback has priority over hover/unhover feedback
      if (selectedChanged) {
        PlayFeedback(_playableElement.IsSelected ? OnSelectedFeedback : OnDeselectedFeedback);
        this.LogDebug($"Played {(_playableElement.IsSelected ? "OnSelectedFeedback" : "OnDeselectedFeedback")} for {_playableElement.name}");
      }
      else if (hoverChanged) {
        if (!_playableElement.IsSelected) { //skip hover feedback if we are selected, to avoid feedback overlap
          PlayFeedback(_playableElement.IsHovered ? OnHoverFeedback : OnUnhoverFeedback);
          this.LogDebug($"Played {(_playableElement.IsHovered ? "OnHoverFeedback" : "OnUnhoverFeedback")} for {_playableElement.name}");
        }
      }
    }

    #endregion


  }
}