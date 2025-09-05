using Ameba;

namespace GMTK {
  /// <summary>
  /// Internal event channel specifically for GameStateMachine
  /// This prevents dependency issues during initialization
  /// </summary>
  public class GameStateMachineEventChannel : EventChannel<GameEventType> {
    // Inherits all functionality from EventChannel<GameEventType>
    // Can add GameStateMachine-specific methods here if needed
  }
}