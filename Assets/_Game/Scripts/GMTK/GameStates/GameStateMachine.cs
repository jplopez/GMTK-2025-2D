using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Ameba;

namespace GMTK {

  public enum GameStates { Start, Preparation, Playing, Reset, LevelComplete, Gameover, Pause, Options }

  [CreateAssetMenu(menuName = "GMTK/Game State Machine")]
  public class GameStateMachine : StateMachine<GameStates> {

    //[Tooltip("Reference to the EventChannel to listen for event that trigger game state changes")]
    //public GameEventChannel eventChannel;
    [Tooltip("keeps history of recent status changes. Change the history length with HistoryLength. Keep in mind a longer history might impact performance")] 
    [SerializeField] protected LinkedList<GameStates> _history = new();
    [Tooltip("how many game states back the history contains")]
    public int HistoryLength = 3;

    protected override void OnEnable() {
      base.OnEnable();
      StartingState = GameStates.Start;
      AddDefaultTransitions();
      //AddGameEventListeners();
    }

    //protected override void OnDisable() {
    //  base.OnDisable();
    //  RemoveGameEventListeners();
    //}

    /// <summary>
    /// Overrides the default method to add support of history of previous GameState changes.
    /// We use this history for states like Pause and Options, that need to know the previous state when player exit those screens.
    /// </summary>
    /// <param name="newState"></param>
    /// <returns></returns>
    public override bool ChangeState(GameStates newState) {
      AddStateChangeToHistory(newState);
      return base.ChangeState(newState);
    }

    #region Add/Remove Transitions
    private void AddDefaultTransitions() {

      //Add here any transition between states the game should consider valid
      AddTransition(GameStates.Start, GameStates.Preparation);
      AddTransition(GameStates.Start, GameStates.Options);
      AddTransition(GameStates.Preparation, GameStates.Playing);
      AddTransition(GameStates.Preparation, GameStates.Reset);
      AddTransition(GameStates.Preparation, GameStates.Pause);
      AddTransition(GameStates.Preparation, GameStates.Options);
      AddTransition(GameStates.Playing, GameStates.Reset);
      AddTransition(GameStates.Playing, GameStates.LevelComplete);
      AddTransition(GameStates.Playing, GameStates.Pause);
      AddTransition(GameStates.Playing, GameStates.Options);
      AddTransition(GameStates.Reset, GameStates.Preparation);
      AddTransition(GameStates.Reset, GameStates.Pause);
      AddTransition(GameStates.Reset, GameStates.Options);
      AddTransition(GameStates.LevelComplete, GameStates.Preparation);
      AddTransition(GameStates.Pause, GameStates.Preparation);
      AddTransition(GameStates.Pause, GameStates.Playing);
      AddTransition(GameStates.Pause, GameStates.Reset);
      AddTransition(GameStates.Options, GameStates.Start);
      AddTransition(GameStates.Options, GameStates.Preparation);
      AddTransition(GameStates.Options, GameStates.Playing);
      AddTransition(GameStates.Options, GameStates.Reset);
      //Gameover will be a default exit for all
      AddTransition(GameStates.Start, GameStates.Gameover);
      AddTransition(GameStates.Preparation, GameStates.Gameover);
      AddTransition(GameStates.Playing, GameStates.Gameover);
      AddTransition(GameStates.Reset, GameStates.Gameover);
      AddTransition(GameStates.LevelComplete, GameStates.Gameover);
    }

    #endregion


    /**
     * These methods listen to particular events from GameEventChannel that trigger 
     * State changes in the game.
     * Keep in mind the ChangeState method triggers an event itself. The methods in this class
     * are not supposed to have game logic
     **/
    #region EventChannel Handlers

    public void HandleStartGame() => ChangeState(GameStates.Start);
    public void HandleLevelStart() => ChangeState(GameStates.Preparation);
    public void HandleLevelPlay() => ChangeState(GameStates.Playing);
    public void HandleLevelReset() => ChangeState(GameStates.Reset);
    public void HandleLevelComplete() => ChangeState(GameStates.LevelComplete);
    public void HandleGameOver() => ChangeState(GameStates.Gameover);
    public void HandleEnterOptions() => ChangeState(GameStates.Options);
    public void HandleExitOptions() {
      //unexpected state, for now just return
      if (Current != GameStates.Options) return;
      //this code assumes the last state was to Options
      var previousGameState = GetHistoryStateChangeAtIndex(CurrentHistoryCount() - 2);

      //this means the only state change in the history is Options. 
      //we will log a warning and return without changing states
      if (previousGameState == default) {
        Debug.LogWarning("GameStateMachine: Can't change GameState for 'Exit Options' event: There is no history of the previous GameState");
        return;
      }
      ChangeState(previousGameState);
    }
    public void HandleEnterPause() => ChangeState(GameStates.Pause);
    public void HandleExitPause() {
      //unexpected state, for now just return
      if (Current != GameStates.Pause) return;
      //this code assumes the last state was to Pause
      var previousGameState = GetHistoryStateChangeAtIndex(CurrentHistoryCount() - 2);

      //this means the only state change in the history is Options. 
      //we will log a warning and return without changing states
      if (previousGameState == default) {
        Debug.LogWarning("GameStateMachine: Can't change GameState for 'Exit Pause' event: There is no history of the previous GameState");
        return;
      }
      ChangeState(previousGameState);
    }

    #endregion

    #region History methods

    /// <summary>
    /// <para>Adds <c>gameStates</c> to the history of state changes (i.e. Push).</para>
    /// <para>The added state is considered the Last added state, so you can 
    /// immediatelly query it using LastHistoryStateChange</para>
    /// </summary>
    /// <param name="gameStates"></param>
    protected void AddStateChangeToHistory(GameStates gameStates) {
      _history.AddLast(gameStates);
      if (_history.Count > HistoryLength) {
        _history.RemoveFirst();
      }
    }

    /// <summary>
    /// The count of state changes currently in history
    /// </summary>
    public int CurrentHistoryCount() => _history.Count;

    /// <summary>
    /// Returns a readonly list of all state changes currently in history
    /// </summary>
    public IReadOnlyList<GameStates> GetStateChangesHistory() => _history.ToList();

    /// <summary>
    /// Returns the most recent state change added to history (i.e. Pop)
    /// </summary>
    public GameStates LastHistoryStateChange() => _history.Last();

    /// <summary>
    /// Returns the oldest state change currently in history
    /// </summary>
    public GameStates OldestHistoryStateChange() => _history.First();

    /// <summary>
    /// <para>Returns the state change at position <c>index</c>.</para>
    /// <para>Returns <c>null</c> if the index is out of bounds.</para>
    /// </summary>
    /// <param name="index">Must be positive and less than HistoryCount</param>
    public GameStates GetHistoryStateChangeAtIndex(int index) =>
      (index < 0 || index >= _history.Count) ? default : _history.ElementAt(index);

    public bool ContainsHistoryStateChange(GameStates gameState) => _history.Contains(gameState);



    /// <summary>
    /// Clears the state changes history
    /// </summary>
    public void ClearHistoryStateChanges() => _history.Clear();
    #endregion
  }
}