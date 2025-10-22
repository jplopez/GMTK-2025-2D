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

    public virtual void OnDragStart() {
      IsBeingDragged = true;
      RaiseGameEvent(GameEventType.ElementDragStart, PlayableElementEventType.DragStart);
      RaisePlayableElementEvent(PlayableElementEventType.DragStart);
    }

    public virtual void OnDragUpdate(Vector3 worldPosition) {
      if (IsBeingDragged) {
        UpdatePosition(worldPosition);
        RaiseGameEvent(GameEventType.ElementDragging, PlayableElementEventType.DragUpdate);
        RaisePlayableElementEvent(PlayableElementEventType.DragUpdate);
      }
    }

    public virtual void OnDragEnd() {
      IsBeingDragged = false;
      RaiseGameEvent(GameEventType.ElementDropped, PlayableElementEventType.DragEnd);
      RaisePlayableElementEvent(PlayableElementEventType.DragEnd);
    }

    public virtual void OnBecomeActive() {
      IsActive = true;
      RaiseGameEvent(GameEventType.ElementSetActive, PlayableElementEventType.BecomeActive);
      RaisePlayableElementEvent(PlayableElementEventType.BecomeActive);
    }

    public virtual void OnBecomeInactive() {
      IsActive = false;
      RaiseGameEvent(GameEventType.ElementSetInactive, PlayableElementEventType.BecomeInactive);
      RaisePlayableElementEvent(PlayableElementEventType.BecomeInactive);
    }

    #endregion
  }
}