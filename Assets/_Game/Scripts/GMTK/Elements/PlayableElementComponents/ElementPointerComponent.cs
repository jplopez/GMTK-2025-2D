using System;
using System.Collections;
using UnityEngine;
using MoreMountains.Feedbacks;

namespace GMTK {

  /// <summary>
  /// Component that enhances the PlayableElement with pointer-based selection functionality.<br/>
  /// This component enables interactions when the element is selected, hovered or dragged (mouse, touch input or both) and playing feedbacks.<br/>
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
        ApplyFeedback(hoverChanged: true);
      }
      // reset coroutine reference
      _hoverCoroutine = null;
    }

    /// <summary>
    /// Plays the correct feedback based on the changes on 'selected' and 'hovered' states.
    /// </summary>
    /// <param name="selectedChanged"></param>
    /// <param name="hoverChanged"></param>
    private void ApplyFeedback(bool selectedChanged = false, bool hoverChanged = false) {
      // select/deselect feedback has priority over hover/unhover feedback
      if (selectedChanged) ToggleSelectedFeedbacks();
      if (hoverChanged) ToggleHoverFeedbacks();
    }

    /// <summary>
    /// Toggles between the Selected and Deselected feedbacks depending on the actual selected state
    /// </summary>
    private void ToggleSelectedFeedbacks()
    {
      if (!_playableElement.IsSelected) // this is the CURRENT state, meaning we have to play the opposite
      {
        StopFeedback(OnSelectedFeedback);
        PlayFeedback(OnDeselectedFeedback);
        this.LogDebug($"Feedbacks: Played OnSelectedFeedback for {_playableElement.name}");
      }
      else
      {
        StopFeedback(OnDeselectedFeedback);
        PlayFeedback(OnSelectedFeedback);
        this.LogDebug($"Feedbacks: Played OnDeselectedFeedback for {_playableElement.name}");
      }
    }

    /// <summary>
    /// Toggles between the Hover and UnHover feedbacks depending on the actual hovered state.
    /// If the element is selected, this method will do nothing to prevent feedback overlaps
    /// </summary>
    private void ToggleHoverFeedbacks()
    {
      if (_playableElement.IsSelected) return; 
        
      if (_playableElement.IsHovered) // this is the CURRENT state, meaning we have to play the opposite
      {
        StopFeedback(OnUnhoverFeedback);
        PlayFeedback(OnHoverFeedback);
        this.LogDebug($"Feedbacks: Played OnUnhoverFeedback for {_playableElement.name}");
      }
      else
      {
        StopFeedback(OnHoverFeedback);
        PlayFeedback(OnUnhoverFeedback);
        this.LogDebug($"Feedbacks: Played OnHoverFeedback for {_playableElement.name}");
      }
    }

    #endregion


  }
}