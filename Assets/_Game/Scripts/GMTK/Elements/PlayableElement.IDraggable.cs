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

      // Notify components
      var eventArgs = new PlayableElementEventArgs(this, transform.position, PlayableElementEventType.DragStart);
      OnPlayableElementEvent?.Invoke(eventArgs);

      this.LogDebug($"Drag started on {name}");
    }

    public virtual void OnDragUpdate(Vector3 worldPosition) {
      if (IsBeingDragged) {
        UpdatePosition(worldPosition);

        // Notify components
        var eventArgs = new PlayableElementEventArgs(this, worldPosition, PlayableElementEventType.DragUpdate);
        OnPlayableElementEvent?.Invoke(eventArgs);
      }
    }

    public virtual void OnDragEnd() {
      IsBeingDragged = false;

      // Notify components
      var eventArgs = new PlayableElementEventArgs(this, transform.position, PlayableElementEventType.DragEnd);
      OnPlayableElementEvent?.Invoke(eventArgs);

      this.LogDebug($"Drag ended on {name}");
    }


    public virtual void OnBecomeActive() {
      IsActive = true;
      var eventArgs = new PlayableElementEventArgs(this, transform.position, PlayableElementEventType.BecomeActive);
      OnPlayableElementEvent?.Invoke(eventArgs);
      this.LogDebug($"became active");
    }

    public virtual void OnBecomeInactive() {
      IsActive = false;
      var eventArgs = new PlayableElementEventArgs(this, transform.position, PlayableElementEventType.BecomeInactive);
      OnPlayableElementEvent?.Invoke(eventArgs);
      this.LogDebug($"became inactive");
    }

    #endregion
  }
}