
namespace GMTK {
  public enum GameEventType {

    //void
    GameStarted,
    LevelStart,
    LevelPlay,
    LevelReset,
    LevelCompleted,
    GameOver,
    EnterOptions, ExitOptions,
    EnterPause, ExitPause,

    //int
    RaiseInt,
    SetInt,
    ScoreRaised,
    ScoreChanged,

    //float

    //bool
    ShowPlaybackControls,
    EnablePlaybackControls,

    //string

  }
}