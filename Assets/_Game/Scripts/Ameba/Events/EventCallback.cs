using System;
using System.Collections.Generic;

namespace Ameba {

  public class EventCallback<T> : IEventCallback {
    public Action<T> Action { get; }
    public Type PayloadType => typeof(T);

    public EventCallback(Action<T> action) => Action = action;
    public void Invoke(object payload) => Action?.Invoke((T)payload);

    public static IEventCallback BuildCallback(Action<int> action) => new EventCallback<int>(action);
    public static IEventCallback BuildCallback(Action<float> action) => new EventCallback<float>(action);
    public static IEventCallback BuildCallback(Action<string> action) => new EventCallback<string>(action);
    public static IEventCallback VoidCallback(Action action) => new VoidEventCallback(action);
    public static IEventCallback BuildCallback(Action<EventArgs> action) => new EventCallback<EventArgs>(action);

    public override bool Equals(object obj) {
      return obj is EventCallback<T> callback &&
             EqualityComparer<Action<T>>.Default.Equals(Action, callback.Action) &&
             EqualityComparer<Type>.Default.Equals(PayloadType, callback.PayloadType);
    }

    public override int GetHashCode() {
      return HashCode.Combine(Action, PayloadType);
    }
  }

  public class VoidEventCallback : IEventCallback {
    public Action Action { get; }
    public Type PayloadType => typeof(void);

    public VoidEventCallback(Action action) => Action = action;
    public void Invoke(object payload) => Action?.Invoke();

    public override bool Equals(object obj) {
      return obj is VoidEventCallback callback &&
             EqualityComparer<Action>.Default.Equals(Action, callback.Action) &&
             EqualityComparer<Type>.Default.Equals(PayloadType, callback.PayloadType);
    }

    public override int GetHashCode() {
      return HashCode.Combine(Action, PayloadType);
    }
  }
}