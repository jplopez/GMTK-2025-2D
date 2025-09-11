using System;

namespace Ameba {

  /// <summary>
  /// <para>
  /// A <see cref="GameState"/> encapsulates the concept of a named state in a game, with support for
  /// lifecycle events such as entering and exiting the state.<br/>
  /// GameStates can be used along <see cref="AmebaStateMachine"/> to create a full state machine system.
  /// </para>
  /// Basic usage:
  /// <code>
  ///     GameState shopping = new GameState("Shopping");
  ///     GameState gamestart = new GameState("gamestart");
  ///     // your logic to enter a state
  ///     shopping.Enter();
  ///     if(shopping.IsActive) { ... } // true
  ///     // your logic to exit a state
  ///     shopping.Exit();
  ///     if(shopping.IsActive) { ... } // false
  /// </code>
  /// <para>
  /// The class also supports comparison and conversion operations with string, so you can use it like:
  /// <code>
  ///     GameState current = // some logic to get current state
  ///     
  ///     // different ways to compare
  ///     if(current == "Shopping") { ... } // valid comparison
  ///     if(current.Is("Shopping")) { ... } // also valid
  ///     if(current != GameState.NullState) { ... } // also valid, compares to the static NullState
  ///
  ///     // implicit conversion to string containing the 'Name' of the state
  ///     string stateName = current; 
  ///
  ///     // implicit conversion from string to GameState
  ///     // keep in mind this creates a new GameState instance, thus any callbacks attached to the previous instance will be lost
  ///     GameState newState = stateName; 
  /// </code>
  /// </para>
  /// <para>
  /// It provides hooks to trigger events when the game enters to a state or exists an state.
  /// <code>
  ///     GameState gs = new GameState("Shopping");
  ///     gs.OnEnter += () =&gt; { Debug.Log("Entered Shopping State"); };
  ///     gs.OnExit += () =&gt; { Debug.Log("Exited Shopping State"); };
  ///     
  ///     // check if there are any callbacks attached
  ///     if (gs.WillTriggerOnEnter) { ... }
  ///     if (gs.WillTriggerOnExit) { ... }
  ///     
  ///     // clear all callbacks if needed
  ///     gs.ClearCallbacks();
  ///     // or clear only one type of callback
  ///     gs.ClearOnEnterCallbacks();
  ///     gs.ClearOnExitCallbacks();
  /// </code>
  /// </para>
  /// <para>
  /// This class is serializable and includes static members for working with a "null" state, to prevent null reference issues.
  /// <code>
  ///     if(someState == GameState.NullState) { ... }
  ///     // or you can also do
  ///     if(someState.Null) { ... }
  /// </code>
  /// You can also attach callbacks to the NullState itself, as a global listener:
  /// <code>
  /// 
  ///     GameState.NullState.OnEnter += () =&gt; { Debug.Log("Entered Null State"); };
  ///     GameState.NullState.OnExit += () =&gt; { Debug.Log("Exited Null State"); };
  /// </code>
  /// </para>
  /// </summary>
  [Serializable]
  public class GameState {

    public string Name;

    public event Action OnEnter;
    public event Action OnExit;

    protected bool _isActive = false;

    public bool IsActive => _isActive;

    public bool Is(string name) => Name == name;

    public bool Null => Name == "null";

    public GameState(string name) { Name = name; }

    public bool WillTriggerOnEnter => OnEnter != null && OnEnter.GetInvocationList().Length > 0;
    public bool WillTriggerOnExit => OnExit != null && OnExit.GetInvocationList().Length > 0;

    public void Enter() {
      OnEnter?.Invoke();
      _isActive = true;
    }
    public void Exit() {
      OnExit?.Invoke();
      _isActive = false;
    }

    public void ClearCallbacks() {
      ClearOnEnterCallbacks();
      ClearOnExitCallbacks();
    }

    public void ClearOnEnterCallbacks() {
      if (OnEnter != null) {
        foreach (var d in OnEnter.GetInvocationList()) {
          OnEnter -= (Action)d;
        }
      }
    }

    public void ClearOnExitCallbacks() {
      if (OnExit != null) {
        foreach (var d in OnExit.GetInvocationList()) {
          OnExit -= (Action)d;
        }
      }
    }

    public static GameState NullState => new("null");

    public static implicit operator string(GameState state) => state != null ? state.Name : "null";

    public static implicit operator GameState(string name) => new (name);

    public static bool operator ==(GameState a, GameState b) {
      if (a is null && b is null) return true;
      if (a is null || b is null) return false;
      return a.Name == b.Name;
    }

    public static bool operator !=(GameState a, GameState b) => !(a == b);

    public override bool Equals(object obj) {
      if (obj is GameState otherState)
        return this == otherState;
      if (obj is string otherString)
        return Name == otherString;
      return false;
    }

    public override int GetHashCode() => Name.GetHashCode();
    public override string ToString() => Name;
  }

}