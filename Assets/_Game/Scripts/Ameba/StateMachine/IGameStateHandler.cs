using System;

namespace Ameba {
  public interface IGameStateHandler<T> where T: Enum {
    void HandleStateChange(StateMachineEventArg<T> eventArg);
    int Priority { get; } // For execution order
  }

  public interface IGameStateValidator<T> where T: Enum {
    bool CanTransitionTo(T fromState, T toState);
  }
}
