using System;
using UnityEngine;

namespace GMTK {

  /// <summary>
  /// Event arguments for PlayableElement events.
  /// Contains information about the element, event type, and world position.
  /// </summary>
  public class PlayableElementEventArgs : EventArgs {

    /// <summary>
    /// The PlayableElement that triggered the event.
    /// </summary>
    public PlayableElement Element { get; }

    /// <summary>
    /// The world position related to the event (e.g., drag position, click position).
    /// </summary>
    public Vector3 WorldPosition { get; }

    /// <summary>
    /// The other GameObject involved in the event, if applies (e.g., the object that moved the playable element).
    /// </summary>
    public GameObject OtherObject { get; }

    /// <summary>
    /// The type of event that occurred.
    /// </summary>
    public PlayableElementEventType EventType { get; }

    /// <summary>
    /// Whether this event has been handled by a component.
    /// Components can set this to true to prevent default behavior.
    /// </summary>
    public bool Handled { get; set; } = false;

    /// <summary>
    /// Constructor for PlayableElementEventArgs.
    /// </summary>
    /// <param name="element">The element that triggered the event</param>
    /// <param name="worldPosition">The world position associated with the event</param>
    /// <param name="eventType">The type of event</param>
    public PlayableElementEventArgs(PlayableElement element, Vector3 worldPosition, PlayableElementEventType eventType) {
      Element = element;
      WorldPosition = worldPosition;
      EventType = eventType;
    }

    /// <summary>
    /// Constructor for PlayableElementEventArgs.
    /// </summary>
    /// <param name="element">The element that triggered the event</param>
    /// <param name="worldPosition">The world position associated with the event</param>
    /// <param name="eventType">The type of event</param>
    public PlayableElementEventArgs(PlayableElement element, Vector3 worldPosition, PlayableElementEventType eventType, GameObject otherObject) {
      Element = element;
      WorldPosition = worldPosition;
      EventType = eventType;
      OtherObject = otherObject;
    }


    public override string ToString() {
      return $"PlayableElementEventArgs: Element={Element?.name}, EventType={EventType}, WorldPosition={WorldPosition}, Handled={Handled}";
    }
  }

  /// <summary>
  /// Enum defining the types of events that can occur on a PlayableElement.
  /// </summary>
  public enum PlayableElementEventType {
    // Drag events
    DragStart, DragUpdate, DragEnd,
    
    // Drop events
    DropSuccess, DropInvalid,
    
    // Pointer events
    PointerOver, PointerOut,
    
    // State events
    BecomeActive, BecomeInactive,
    
    // Transformation events
    RotateCW, RotateCCW,
    FlippedX, FlippedY,
    
    // Selection events
    Selected, Deselected,
    
    CollisionStart, CollisionEnd, Collisioning,
  }
}