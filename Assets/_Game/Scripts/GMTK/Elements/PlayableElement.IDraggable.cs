using Ameba;
using UnityEngine;

namespace GMTK {

  /// <summary>
  /// PlayableElement partial class implementing IDraggable interface functionality.
  /// Handles dragging behavior and related events.
  /// </summary>
  public partial class PlayableElement : IDraggable {

    // IDraggable interface properties
    public bool IsDraggable => Draggable;

    [Header("Dragging Debug")]
    [DisplayWithoutEdit] public bool IsBeingDragged { get; private set; }
    [DisplayWithoutEdit] public bool IsActive { get; set; }

    public Transform DragTransform => SnapTransform != null ? SnapTransform : transform;
    public Collider2D InteractionCollider => _collider;

    #region IDraggable Implementation



    public virtual void OnDragStart() {
      IsBeingDragged = true;
      RaisePlayableElementEvent(PlayableElementEventType.DragStart);
    }

    public virtual void OnDragUpdate(Vector3 worldPosition) {
      if (IsBeingDragged) {
        UpdatePosition(worldPosition);
        RaisePlayableElementEvent(PlayableElementEventType.DragUpdate, worldPosition);
      }
    }

    public virtual void OnDragEnd() {
      IsBeingDragged = false;
      RaisePlayableElementEvent(PlayableElementEventType.DragEnd);
    }

    public virtual void OnBecomeActive() {
      IsActive = true;
      RaisePlayableElementEvent(PlayableElementEventType.BecomeActive);
    }

    public virtual void OnBecomeInactive() {
      IsActive = false;
      RaisePlayableElementEvent(PlayableElementEventType.BecomeInactive);
    }

    #endregion
  }
}