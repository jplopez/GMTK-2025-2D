using UnityEngine;

namespace Ameba {
  /// <summary>
  /// General purpose interface for objects that can be dragged in the game.
  /// Provides the necessary properties and methods to support dragging functionality.
  /// </summary>
  public interface IDraggable {
    /// <summary>
    /// Whether this object can currently be dragged.
    /// </summary>
    bool IsDraggable { get; }

    /// <summary>
    /// Whether this object is currently being dragged.
    /// </summary>
    bool IsBeingDragged { get; }

    /// <summary>
    /// Whether this object is currently the active/selected element.
    /// </summary>
    bool IsActive { get; set; }

    /// <summary>
    /// The transform used for dragging operations.
    /// </summary>
    Transform DragTransform { get; }

    /// <summary>
    /// The bounds or collider used for interaction detection.
    /// </summary>
    Collider2D InteractionCollider { get; }

    /// <summary>
    /// Called when the object starts being dragged.
    /// </summary>
    void DragStart();

    /// <summary>
    /// Called continuously while the object is being dragged.
    /// </summary>
    /// <param name="worldPosition">Current world position of the pointer</param>
    void DraggingUpdate(Vector3? worldPosition);

    /// <summary>
    /// Called when the object stops being dragged.
    /// </summary>
    void DragEnd();

  }
}
