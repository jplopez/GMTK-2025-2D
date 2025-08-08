using System;
using UnityEngine;

namespace GMTK {
  public class GridSnappableEventArgs : EventArgs {
    /// <summary>
    /// The GridSnappable element involved in the event.
    /// </summary>
    public GridSnappable Element { get; }

    /// <summary>
    /// The world Position of the pointer (if applicable).
    /// </summary>
    public Vector2 PointerPosition { get; }


    /// <summary>
    /// Indicates if the element was rotated clockwise.
    /// </summary>
    public bool RotatedCW { get; private set; }

    /// <summary>
    /// Indicates if the element was rotated counter-clockwise.
    /// </summary>
    public bool RotatedCCW { get; private set; }

    /// <summary>
    /// Indicates if the element was flipped horizontally.
    /// </summary>
    public bool FlippedX { get; private set; }

    /// <summary>
    /// Indicates if the element was flipped vertically.
    /// </summary>
    public bool FlippedY { get; private set; }

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

  }
}