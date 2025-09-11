using System;
using System.Collections.Generic;

namespace Ameba {
  [Serializable]
  public class StateChangePayload<T> : EventArgs  {

    public T FromState { get; }
    public T TargetState { get; }

    public StateChangePayload(T from, T target) {
      FromState = from;
      TargetState = target;
    }

    public bool ComesFrom(T other) => other != null && FromState != null && FromState.Equals(other);
    public bool HasTarget(T other) => other != null && TargetState != null && TargetState.Equals(other);
    public bool HasNoPayload() => EqualityComparer<T>.Default.Equals(FromState, default) && EqualityComparer<T>.Default.Equals(TargetState, default);
    public static StateChangePayload<T> NoPayload => new(default,default);
    public StateChangePayload<T> From(T from) => new(from,TargetState);
    public StateChangePayload<T> Target(T target) => new(FromState, target);
  }

}