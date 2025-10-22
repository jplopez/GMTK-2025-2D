
namespace GMTK {
  public enum GameEventType {

    //void
    GameStarted,
    LevelStart,
    LevelPlay,
    LevelReset,
    GameOver,
    EnterOptions, ExitOptions,
    EnterPause, ExitPause,


    // Sequential Level Completion Events

    LevelObjectiveCompleted,// When level objectives are met (triggers state change)
    LevelCompleted,        // After state changes to LevelComplete (triggers UI, scoring, etc.)
    LevelCompletionProcessed,// After completion processing (triggers scene loading)

    // Scene events
    SceneGoToNextLevel,
    SceneGoToStart,
    SceneGoToOptions, 
    SceneGoToPause,
    SceneGoToGameover,
    SceneLoading,
    SceneLoadingComplete,

    //int
    RaiseInt,
    SetInt,
    RaiseScore,
    SetScoreValue,
    ResetScore,
    //float

    //bool
    ShowPlaybackControls,
    EnablePlaybackControls,

    //string 
    //These events include the Checkpoint id
    EnterCheckpoint,
    ExitCheckpoint,

    //Input events
    //some of these events will be triggered along the 'Element' events group below.
    //use these events if you care about the actual input being pressed.
    InputPointerPosition, //when the pointer position is updated
    InputSelected, //when mouse left-button is clicked
    InputSecondary,//when mouse right-button is clicked
    InputCancel,   //when the input to Cancel is pressed
    InputRotateCW, //when the input to RotateCW is pressed
    InputRotateCCW,//when the input to RotateCCW is pressed 
    InputFlippedX, //when the input to FlipX is pressed
    InputFlippedY, //when the input to FlipY is pressed

    //Mouse Pointer events, for object that need to signal when the pointer is over them
    OnPointerOver,
    OnPointerOut,

    //Elements specific events related to Input and PointerSelection
    //These events will send a GridSnappableEventArg as the payload.
    //Some of these events will be triggered along with some of the 'Input' group.
    //Use these events if you care about the element. 
    ElementHovered,
    ElementUnhovered,
    ElementSelected,
    ElementDeselected,
    ElementDragStart, // when an element starts being dragged
    ElementDragging,  // while an element is being dragged
    ElementDropped,   // when an element is dropped
    ElementSetActive,  // when an element becomes active
    ElementSetInactive,// when an element becomes inactive

    //Elements  events triggered after ElementDropped
    ElementMovedToInventory,
    ElementMovedToGrid,

    //PlayableElementComponent Base Event
    PlayableElementEvent,         // for generic PlayableElement events
    PlayableElementInternalEvent, // when a PlayableElementComponent wants to broadcast events internally to other PlayableElementCompnents on the same PlayableElement

    // Inventory Events - using EventArgs pattern
    InventoryAddRequest,        // Request to add element (InventoryEventData)
    InventoryRetrieveRequest,   // Request to retrieve element (InventoryEventData)  
    InventoryQueryRequest,      // Request to check availability (InventoryEventData)

    InventoryElementAdded,      // Confirmation element was added (InventoryEventData)
    InventoryElementRetrieved,  // Confirmation element was retrieved (InventoryEventData)
    InventoryElementQueried,    // Response to availability query (InventoryEventData)

    InventoryOperationFailed,   // When an inventory operation fails (InventoryEventData)
    InventoryUpdated,          // When inventory state changes (InventoryEventData)
    InventoryFull,             // When inventory reaches capacity (InventoryEventData)
  }
}