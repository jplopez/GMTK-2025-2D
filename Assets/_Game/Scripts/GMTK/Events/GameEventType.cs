
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
    InputSelected, //when mouse left-button is clicked
    InputSecondary,//when mouse right-button is clicked
    InputRotateCW, //when the input to RotateCW is pressed
    InputRotateCCW,//when the input to RotateCCW is pressed 
    InputFlippedX, //when the input to FlipX is pressed
    InputFlippedY, //when the input to FlipY is pressed

    //Mouse Pointer events, for object that need to signal when the pointer is over them
    OnPointerOver,
    OnPointerOut,

    //Elements (GridSnappable) specific events related to Input and PointerSelection
    //These events will send a GridSnappableEventArg as the payload.
    //Some of these events will be triggered along with some of the 'Input' group.
    //Use these events if you care about the element. 
    ElementHovered,
    ElementUnhovered,
    ElementSelected,
    ElementDropped,
    //Elements (GridSnappable) specific events triggered after ElementDropped
    ElementMovedToInventory,
    ElementMovedToGrid,

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