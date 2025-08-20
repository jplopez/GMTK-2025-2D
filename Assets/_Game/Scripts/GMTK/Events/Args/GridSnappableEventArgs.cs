using System;
using UnityEngine;

namespace GMTK {

  public enum SnappableComponentEventType {
    RotateCW, RotateCCW, FlippedX, FlippedY, OnPointerOver, OnPointerOut, MovedToInventory, MovedToGrid
  }

  public class GridSnappableEventArgs : EventArgs {
    /// <summary>
    /// The GridSnappable element involved in the event.
    /// </summary>
    public GridSnappable Element { get; }

    /// <summary>
    /// The world Position of the pointer (if applicable).
    /// </summary>
    public Vector2 PointerPosition { get; }

    public SnappableComponentEventType ComponentEventType { get; }

    /// <summary>
    /// Basic constructor with only the element reference.
    /// </summary>
    public GridSnappableEventArgs(GridSnappable element) {
      Element = element;
    }

    /// <summary>
    /// Constructor with explicit pointerPosition and rotation values.
    /// </summary>
    public GridSnappableEventArgs(GridSnappable element, Vector2 pointerPosition) {
      Element = element;
      PointerPosition = pointerPosition;
    }

    public GridSnappableEventArgs(GridSnappable element, Vector2 pointerPosition, SnappableComponentEventType eventType) {
      Element = element;
      PointerPosition = pointerPosition;
      ComponentEventType = eventType;
    }

  }
}