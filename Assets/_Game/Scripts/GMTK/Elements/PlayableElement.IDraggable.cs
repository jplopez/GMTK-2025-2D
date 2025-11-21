using Ameba;
using UnityEngine;

namespace GMTK {

  /// <summary>
  /// PlayableElement partial class implementing IDraggable interface functionality.
  /// Handles dragging behavior and related events.
  /// </summary>
  public partial class PlayableElement : IDraggable {

    [Tooltip("If true, this element is currently being dragged by a pointer")]
    [SerializeField] protected bool _isDragging = false;

    // IDraggable interface properties
    public bool IsDraggable => Draggable;
    public bool IsBeingDragged { get => _isDragging; private set => _isDragging = value; }
    
    [DisplayWithoutEdit] public bool IsActive { get; set; }

    public Transform DragTransform => SnapTransform != null ? SnapTransform : transform;
    public Collider2D InteractionCollider => _collider;

    #region IDraggable Implementation

    public virtual void DragStart() {
      IsBeingDragged = true;
      RaiseGameEvent(GameEventType.ElementDragStart, PlayableElementEventType.DragStart);
      //RaisePlayableElementEvent(PlayableElementEventType.DragStart);
      OnDragStart?.Invoke(BuildEventArgs(GameEventType.ElementDragStart, PlayableElementEventType.DragStart));
    }

    public virtual void DraggingUpdate(Vector3? worldPosition) {
      worldPosition = worldPosition ?? GetPosition();
      if (IsBeingDragged) {
        UpdatePosition(worldPosition.Value);
        if(CanDoDraggingUpdate) {
          //this.Log($"PlayableElement '{name}' raising dragging update event");
          RaiseGameEvent(GameEventType.ElementDragging, PlayableElementEventType.DragUpdate);
          //RaisePlayableElementEvent(PlayableElementEventType.DragUpdate);
          OnDragging?.Invoke(BuildEventArgs(GameEventType.ElementDragging, PlayableElementEventType.DragUpdate));
          ResetDraggingUpdateTimer();
        }
      }
    }

    public virtual void DragEnd() {
      IsBeingDragged = false;
      RaiseGameEvent(GameEventType.ElementDropped, PlayableElementEventType.DragEnd);
      //RaisePlayableElementEvent(PlayableElementEventType.DragEnd);
      OnDragEnd?.Invoke(BuildEventArgs(GameEventType.ElementDropped, PlayableElementEventType.DragEnd));
      _canDoDraggingUpdate = true;
    }

    #endregion
  }
}