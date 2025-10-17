namespace GMTK {
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