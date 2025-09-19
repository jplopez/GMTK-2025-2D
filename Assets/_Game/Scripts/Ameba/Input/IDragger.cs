using UnityEngine;

namespace Ameba {

  /// <summary>
  /// Interface for objects that can drag IDraggable objects.
  /// Provides methods to initiate and manage dragging operations.
  /// </summary>
  public interface IDragger<T> where T : IDraggable {
    /// <summary>
    /// Whether dragging is currently enabled.
    /// </summary>
    bool CanDrag { get; set; }

    /// <summary>
    /// Whether a dragging operation is currently active.
    /// </summary>
    bool IsDragging { get; }

    /// <summary>
    /// The currently dragged element, if any.
    /// </summary>
    T DraggedElement { get; }

    /// <summary>
    /// Start dragging the specified element.
    /// </summary>
    /// <param name="element">The element to start dragging</param>
    /// <returns>True if dragging started successfully</returns>
    bool TryStartDrag(T element);

    /// <summary>
    /// Start dragging an element at the specified world position.
    /// </summary>
    /// <param name="worldPosition">World position to search for draggable element</param>
    /// <param name="element">The element that was found and started dragging</param>
    /// <returns>True if an element was found and dragging started</returns>
    bool TryStartDrag(Vector3 worldPosition, out T element);

    /// <summary>
    /// Start dragging an element at the specified screen position.
    /// </summary>
    /// <param name="screenPosition">Screen position to search for draggable element</param>
    /// <param name="element">The element that was found and started dragging</param>
    /// <returns>True if an element was found and dragging started</returns>
    bool TryStartDrag(Vector2 screenPosition, out T element);

    /// <summary>
    /// Update the drag operation with a new position.
    /// </summary>
    /// <param name="worldPosition">New world position for the dragged element</param>
    void UpdateDrag(Vector3 worldPosition);

    /// <summary>
    /// Stop the current dragging operation.
    /// </summary>
    /// <returns>True if there was an active drag to stop</returns>
    bool TryStopDrag();
  }
}