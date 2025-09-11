using System;
using System.Collections.Generic;

namespace Ameba {
  [Serializable]
  public class StateTransition<T> {
    public T From;
    public List<T> Targets = new();

    public StateTransition(T from, List<T> targets = null) {
      From = from;
      Targets = targets ?? new();
    }

    public StateTransition(T from, T[] targets) {
      From = from;
      Targets = targets != null ? new List<T>(targets) : new();
    }

    public StateTransition(T from, T target) {
      From = from;
      Targets = new() { target };
    }

    public bool IsEmpty() => EqualityComparer<T>.Default.Equals(From, default) && (Targets == null || Targets.Count == 0);
    public static StateTransition<T> Empty => new(default);
    public static bool IsNullOrEmpty(StateTransition<T> transition) => transition == null || transition.IsEmpty();
  }

}