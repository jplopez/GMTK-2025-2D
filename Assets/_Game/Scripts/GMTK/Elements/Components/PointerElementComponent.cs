using Ameba;
using MoreMountains.Feedbacks;
using System;
using System.Collections;
using UnityEngine;
using static Codice.Client.BaseCommands.Import.Commit;

namespace GMTK {

  [Flags]
  public enum SelectionTrigger {
    None = 0,
    OnHover = 1,
    OnClick = 2,
    OnDoubleClick = 4
  }

  /// <summary>
  /// Component that enhances the PlayableElement with pointer-based selection functionality.<br/>
  /// This component allows elements to be selected via mouse, touch input or both, supporting hover and click interactions, as well as playing feedbacks.<br/>
  /// </summary>
  [AddComponentMenu("GMTK/Playable Element Components/Pointer Element Component")]
  public class PointerElementComponent : PlayableElementComponent {

    [Header("Selection Settings")]
    [Tooltip("Selection trigger methods. Multiple options can be selected.")]
    public SelectionTrigger SelectionTriggers = SelectionTrigger.OnClick;
    [Tooltip("Whether this element is currently selected")]
    [SerializeField][DisplayWithoutEdit] private bool _isSelected = false;
    [Tooltip("Whether this element can be selected")]
    [SerializeField] private bool _canSelect = true;
    [Tooltip("If true, this component will sync its selection state with the PlayableElement's IsSelected property each frame")]
    public bool SyncStateWithElement = false;

    [Header("Click/Touch Accuracy")]
    [Tooltip("Selection accuracy within element boundaries (0 = less strict, 1 = strictly within boundaries)")]
    [Range(0f, 1f)]
    public float Accuracy = 0.8f;
    [Tooltip("Maximum offset from element boundaries considered valid when accuracy is 0")]
    [Range(0f, 5f)]
    public float MaxOffset = 2f;

    [Header("Select Feedbacks")]
    public MMF_Player OnSelectedFeedback;
    public MMF_Player OnDeselectedFeedback;

    [Space]

    [Header("Hover Settings")]
    [Tooltip("Minimum time pointer must hover over element to select it (seconds)")]
    [Range(0.1f, 5f)]
    public float HoverThreshold = 0.2f;
    [Tooltip("Whether the pointer is currently hovering over the element")]
    [SerializeField][DisplayWithoutEdit] private bool _isHovering = false;
    [Tooltip("Whether this element can be hovered over")]
    [SerializeField] private bool _canHover = true;

    [Header("Hover Feedbacks")]
    public MMF_Player OnHoverFeedback;
    public MMF_Player OnUnhoverFeedback;


    // ISelectable properties
    public bool IsSelected => _isSelected;
    public bool CanSelect {
      get => _canSelect;
      set => _canSelect = value;
    }

    // IHoverable properties
    public bool IsHovering => _isHovering;
    public bool CanHover {
      get => _canHover;
      set => _canHover = value;
    }

    // Private state
    private float _hoverStartTime = 0f;
    private Coroutine _hoverCoroutine;
    private Camera _camera;

    #region PlayableElementComponent Overrides

    protected override void Initialize() {
      _camera = Camera.main;
      if (_camera == null) {
        this.LogWarning($"No main camera found for {_playableElement.name}");
      }
    }

    protected override bool Validate() {
      return _playableElement != null && _canSelect;
    }

    protected override void OnUpdate() {
      base.OnUpdate();
      if (SyncStateWithElement) {
        if (_playableElement.IsSelected != _isSelected) {
          _isSelected = _playableElement.IsSelected;
        }

        if (_playableElement.CanSelect != _canSelect) {
          _canSelect = _playableElement.CanSelect;
        }
      }
    }

    #endregion

    #region Event Listeners

    /// <summary>
    /// Event handler for PlayableElementEventType.OnPointerOver events.
    /// </summary>
    /// <param name="evt"></param>
    protected void OnPointerOver(PlayableElementEventArgs evt) {
      this.LogDebug($"OnPointerOver event received for {evt.Element?.name}");
      // edge cases
      if (!CanHover //cant hover
          || (HasSelectionTrigger(SelectionTrigger.OnHover) && !CanSelect) //cant select by hover
          ) return;
      this.LogDebug($"IsHovering: {IsHovering}, CanHover {CanHover}, SelectOnHover: {HasSelectionTrigger(SelectionTrigger.OnHover)}");
      // only handle events for our own element, and only if not already hovering
      if (evt.Element != null && evt.Element == _playableElement && !IsHovering) {

        _isHovering = true;
        _hoverStartTime = Time.time;

        if (_hoverCoroutine != null) {
          StopCoroutine(_hoverCoroutine);
        }
        _hoverCoroutine = StartCoroutine(HoverSelectionCoroutine());
      }
    }

    /// <summary>
    /// Event handler for PlayableElementEventType.OnPointerOut events.
    /// </summary>
    /// <param name="evt"></param>
    protected void OnPointerOut(PlayableElementEventArgs evt) {
      this.LogDebug($"OnPointerOut event received for {evt.Element?.name}");
      // edge cases
      if (!CanHover //cant hover
         || (HasSelectionTrigger(SelectionTrigger.OnHover) && !CanSelect) //cant select by hover
         ) return;
      this.LogDebug($"IsHovering: {IsHovering}, CanHover {CanHover}, SelectOnHover: {HasSelectionTrigger(SelectionTrigger.OnHover)}");
      // only handle events for our own element, and only if currently hovering
      if (evt.Element != null && evt.Element == _playableElement && IsHovering) {

        _isHovering = false;

        if (_hoverCoroutine != null) {
          StopCoroutine(_hoverCoroutine);
          _hoverCoroutine = null;
        }

        // if we were selected by hover, we deselect
        if (HasSelectionTrigger(SelectionTrigger.OnHover) && IsSelected) {
          this.LogDebug($"Element {_playableElement.name} deselected on unhover");
          MarkSelected(false);
          PlayFeedback(OnDeselectedFeedback);
        }
        // otherwise we just play unhover feedback
        else {
          this.LogDebug($"Element {_playableElement.name} unhover feedback played");
          //skip unhover feedback if we are selected, to avoid feedback overlap
          if (!IsSelected) PlayFeedback(OnUnhoverFeedback);
        }
      }
    }

    /// <summary>
    /// Event handler for PlayableElementEventType.OnSelected events.
    /// </summary>
    /// <param name="evt"></param>

    public void OnSelected(PlayableElementEventArgs evt) {
      this.LogDebug($"OnSelected event received for {evt.Element?.name}");
      if (!CanSelect) return;
      if (evt.Element != null && evt.Element == _playableElement && !IsSelected) {
        TrySelectingElementAtWorldPos(evt.WorldPosition, true);
      }
    }

    /// <summary>
    /// Event handler for PlayableElementEventType.OnDeselected events.
    /// </summary>
    /// <param name="evt"></param>

    public void OnDeselected(PlayableElementEventArgs evt) {
      this.LogDebug($"OnDeselected event received for {evt.Element?.name}");
      if (!CanSelect) return;
      if (evt.Element != null && evt.Element == _playableElement && IsSelected) {
        TrySelectingElementAtWorldPos(evt.WorldPosition, false);
      }
    }

    #endregion

    #region Select/Hover logic

    private void TrySelectingElementAtWorldPos(Vector3 worldPosition, bool selected) {
      if (HasSelectionTrigger(SelectionTrigger.OnClick)) {
        if (IsValidClickPosition(worldPosition)) {
          this.LogDebug($"Element {name} {(selected ? "Selected" : "Deselected")} at position {worldPosition}");
          MarkSelected(selected);
          ApplyFeedback(selectedChanged: true);
        }
      }
    }

    private IEnumerator HoverSelectionCoroutine() {
      yield return new WaitForSeconds(HoverThreshold);
      this.LogDebug($"Hover threshold reached for {_playableElement.name}");
      // if hovering after threshold we apply hover selection logic
      if (_isHovering) {

        // select if still hovering and selection by hover is enabled
        if (HasSelectionTrigger(SelectionTrigger.OnHover) && CanSelect && !IsSelected) {
          this.LogDebug($"Element {_playableElement.name} selected by hover");
          MarkSelected(true);
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

    private bool HasSelectionTrigger(SelectionTrigger trigger) {
      return (SelectionTriggers & trigger) != 0;
    }

    private bool IsValidClickPosition(Vector3 worldPosition) {
      if (_playableElement.InteractionCollider == null) return true;

      // Calculate effective offset based on accuracy
      float effectiveOffset = MaxOffset * (1f - Accuracy);

      // Check if position is within collider bounds
      bool withinCollider = _playableElement.InteractionCollider.bounds.Contains(worldPosition);

      if (withinCollider) {
        return true; // Always valid if within collider
      }

      if (effectiveOffset <= 0f) {
        return false; // Strict accuracy, must be within collider
      }

      // Check if within offset distance
      Vector3 closestPoint = _playableElement.InteractionCollider.ClosestPoint(worldPosition);
      float distance = Vector3.Distance(worldPosition, closestPoint);

      return distance <= effectiveOffset;
    }

    /// <summary>
    /// Plays the correct feedback based on selection and hover state changes.
    /// </summary>
    /// <param name="selectedChanged"></param>
    /// <param name="hoverChanged"></param>
    private void ApplyFeedback(bool selectedChanged=false, bool hoverChanged=false) {
      // select/deselect feedback has priority over hover/unhover feedback
      if (selectedChanged) {
        PlayFeedback(IsSelected? OnSelectedFeedback : OnDeselectedFeedback);
      }
      else if (hoverChanged) {
        if (!IsSelected) { //skip hover feedback if we are selected, to avoid feedback overlap
          PlayFeedback(IsHovering ? OnHoverFeedback : OnUnhoverFeedback);
        }
      }
    }


    #endregion

    #region ISelectable Implementation

    public void MarkSelected(bool selected = true) => _isSelected = selected && CanSelect;

    public void EnableSelectable(bool selectable = true) {
      _canSelect = selectable;
      if (!selectable && _isSelected) {
        MarkSelected(false);
      }
    }

    #endregion

    #region Legacy GridSnappable Support

    protected override void HandleElementSelected(GridSnappableEventArgs evt) {
      // Handle legacy click selection
    }

    protected override void HandleElementDropped(GridSnappableEventArgs evt) {
      // Handle legacy drop events if needed
    }

    protected override void HandleElementHovered(GridSnappableEventArgs evt) {
      // Handled by PlayableElement events instead
    }

    protected override void HandleElementUnhovered(GridSnappableEventArgs evt) {
      // Handled by PlayableElement events instead
    }

    #endregion

    #region Public API

    /// <summary>
    /// Sets the selection triggers programmatically
    /// </summary>
    public void SetSelectionTriggers(SelectionTrigger triggers) {
      SelectionTriggers = triggers;
    }

    /// <summary>
    /// Sets hover settings
    /// </summary>
    public void SetHoverSettings(float threshold) {
      HoverThreshold = Mathf.Clamp(threshold, 0.1f, 5f);
    }

    /// <summary>
    /// Sets click accuracy settings
    /// </summary>
    public void SetAccuracySettings(float accuracy, float maxOffset) {
      Accuracy = Mathf.Clamp01(accuracy);
      MaxOffset = Mathf.Max(0f, maxOffset);
    }

    /// <summary>
    /// Checks if a world position would be considered a valid selection
    /// </summary>
    public bool IsValidSelectionPosition(Vector3 worldPosition) {
      return IsValidClickPosition(worldPosition);
    }

    #endregion

    protected override void ResetComponent() {
      MarkSelected(false);
      _isHovering = false;

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
  }
}