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

    [Header("Hover Settings")]
    [Tooltip("Minimum time pointer must hover over element to select it (seconds)")]
    [Range(0.1f, 5f)]
    public float HoverThreshold = 0.2f;

    [Header("Click/Touch Accuracy")]
    [Tooltip("Selection accuracy within element boundaries (0 = less strict, 1 = strictly within boundaries)")]
    [Range(0f, 1f)]
    public float Accuracy = 0.8f;

    [Tooltip("Maximum offset from element boundaries considered valid when accuracy is 0")]
    [Range(0f, 5f)]
    public float MaxOffset = 2f;

    [Header("Selection State")]
    [SerializeField] private bool _isSelected = false;
    [SerializeField] private bool _canSelect = true;
    [Tooltip("If true, this component will sync its selection state with the PlayableElement's IsSelected property each frame")]
    public bool SyncStateWithElement = false;

    [Header("Feedbacks")]
    public MMF_Player OnSelectedFeedback;
    public MMF_Player OnDeselectedFeedback;

    // ISelectable properties
    public bool IsSelected => _isSelected;
    public bool CanSelect {
      get => _canSelect;
      set => _canSelect = value;
    }

    // Private state
    private bool _isHovering = false;
    private float _hoverStartTime = 0f;
    private Coroutine _hoverCoroutine;
    private Camera _camera;

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
      if(SyncStateWithElement) {
        if (_playableElement.IsSelected != _isSelected) {
          _isSelected = _playableElement.IsSelected;
        }

        if(_playableElement.CanSelect != _canSelect) {
          _canSelect = _playableElement.CanSelect;
        }
      } 
    }

    protected void OnPointerOver(PlayableElementEventArgs evt) {
      if (!CanSelect || !HasSelectionTrigger(SelectionTrigger.OnHover)) return;

      _isHovering = true;
      _hoverStartTime = Time.time;

      if (_hoverCoroutine != null) {
        StopCoroutine(_hoverCoroutine);
      }
      _hoverCoroutine = StartCoroutine(HoverSelectionCoroutine());
    }

    protected void OnPointerOut(PlayableElementEventArgs evt) {
      _isHovering = false;

      if (_hoverCoroutine != null) {
        StopCoroutine(_hoverCoroutine);
        _hoverCoroutine = null;
      }

      if (HasSelectionTrigger(SelectionTrigger.OnHover) && IsSelected) {
        MarkSelected(false);
        PlayFeedback(OnDeselectedFeedback);
      }
    }

    public void OnSelected(PlayableElementEventArgs evt) {
      this.Log($"OnSelected event received for {evt.Element?.name}");
      if (!CanSelect) return;
      if (evt.Element != null && evt.Element == _playableElement && !IsSelected) {
        TrySelectingElementAtWorldPos(evt.WorldPosition, true);
      }
    }

    public void OnDeselected(PlayableElementEventArgs evt) {
      this.Log($"OnDeselected event received for {evt.Element?.name}");
      if (!CanSelect) return;
      if (evt.Element != null && evt.Element == _playableElement && IsSelected) {
        TrySelectingElementAtWorldPos(evt.WorldPosition, false);
      }
    }

    private void TrySelectingElementAtWorldPos(Vector3 worldPosition, bool selected) {
      if (HasSelectionTrigger(SelectionTrigger.OnClick)) {
        if (IsValidClickPosition(worldPosition)) {
          this.Log($"Element {name} {(selected ? "Selected" : "Deselected")} at position {worldPosition}");
          MarkSelected(selected);
          PlayFeedback(selected ? OnSelectedFeedback : OnDeselectedFeedback);
        }
      }
    }

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

    private IEnumerator HoverSelectionCoroutine() {
      yield return new WaitForSeconds(HoverThreshold);

      if (_isHovering && CanSelect) {
        MarkSelected(true);
        PlayFeedback(OnSelectedFeedback);
      }

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

    #region ISelectable Implementation

    public void MarkSelected(bool selected = true) => _isSelected = selected && CanSelect;

    public void EnableSelectable(bool selectable = true) {
      _canSelect = selectable;
      if (!selectable && _isSelected) {
        MarkSelected(false);
      }
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