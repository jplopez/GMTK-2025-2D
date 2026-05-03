using UnityEngine;
using Ameba;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace GMTK {

  /// <summary>
  /// PlayableElement partial class implementing ISelectable interface functionality.
  /// Handles selection behavior and delegates to ElementPointerComponent, if present.
  /// </summary>
  public partial class PlayableElement : ISelectable {
    
    [Header("Selection Settings")]
    [Tooltip("Whether this element is currently selected by a pointer")]
    [SerializeField] protected bool isSelected = false;
    [Tooltip("If true, this element can be selected by a pointer")]
    [SerializeField] protected bool canSelect = true;

    /*[MMFCondition("canSelect", true)]
    [Tooltip("Selection trigger methods. Multiple options can be selected.")]
    public SelectionTrigger SelectionTriggers = SelectionTrigger.OnClick;*/
    [MMFCondition("canSelect", true)]
    [Tooltip("Selection accuracy within element boundaries (0 = less strict, 1 = strictly within boundaries)")]
    [Range(0f, 1f)]
    public float Accuracy = 0.8f;
    [MMFCondition("canSelect", true)]
    [Tooltip("Maximum offset from element boundaries considered valid when accuracy is 0")]
    [Range(0f, 5f)]
    public float MaxOffset = 2f;

    // ISelectable interface properties
    public bool IsSelected { get => isSelected; private set => isSelected = value; }

    public bool CanSelect => canSelect;

    //public bool HasAnySelectionTrigger => SelectionTriggers != SelectionTrigger.None;

    public Transform SelectTransform => SnapTransform != null ? SnapTransform : transform;

    #region ISelectable Implementation

    public void MarkSelected(bool selected = true) {
      
      if (IsSelected == selected || !CanSelect ) return; //avoid redundant state change
      IsSelected = selected;
      this.LogDebug($"[Begin MarkSelected:{(selected ? "Selected" : "Deselected")}] '{name}'");

      var peEvent = selected ? PlayableElementEventType.Selected : PlayableElementEventType.Deselected;
      var gameEvent = selected ? GameEventType.ElementSelected : GameEventType.ElementDeselected;

      // If selection is triggered on click, raise selected or unselected event
      var eventArgs = RaiseGameEvent(gameEvent, peEvent);

      // call pointer component - if present - to handle the selection event effects
      if (!TryDelegateToPointerComponent(eventArgs)) {
        this.LogWarning($"No ElementPointerComponent found on {name} to handle selection event");
      }

      // unity event 
      var unityEvent = selected ? OnSelected : OnDeselected;
      unityEvent?.Invoke(eventArgs);

      this.LogDebug($"[End MarkSelected:{IsSelected}] '{name}'");
    }

    [Button]
    public void OnSelect() => MarkSelected(true);

    [Button]
    public void OnDeselect() => MarkSelected(false);
    

    #endregion

  }
}