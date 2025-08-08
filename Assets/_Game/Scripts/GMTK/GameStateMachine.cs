using Ameba;
using UnityEngine;

namespace GMTK {

  public enum GameStates { Start, Preparation, Playing, Reset, LevelComplete, Gameover, Pause, Options }

  [CreateAssetMenu(menuName = "GMTK/Game State Machine")]
  public class GameStateMachine : StateMachine<GameStates> {

    protected override void OnEnable() {
      base.OnEnable();
      StartingState = GameStates.Start;
      //TODO add valid transitions
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

  }

}