
using Ameba;

namespace GMTK {

  public class GameStateEventArgs : StateMachineEventArg<GameStates> {
    public GameStateEventArgs(GameStates fromState, GameStates toState) : base(fromState, toState) {
    }
  }
}