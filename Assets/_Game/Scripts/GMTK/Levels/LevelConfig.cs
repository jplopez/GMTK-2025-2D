using UnityEngine;

namespace GMTK {
  [System.Serializable]
  public class LevelConfig {
    //"Basic Info"
    public string SceneName;
    public string DisplayName;
    public SceneType Type = SceneType.Level;

    //"Game State"
    [Tooltip("If true, the game state will be set to the InitialGameState when the scene is loaded.")]
    public bool SetStateOnLoad = true;
    [Tooltip("The game state to set when the scene is loaded, if SetStateOnLoad is true.")]
    public GameStates InitialGameState = GameStates.Preparation;

    //"Scene Management"
    [Tooltip("If true, this scene can be re-loaded. For example, the player can restart the level.")]
    public bool CanRestart = true;
    [Tooltip("If true, this scene can be skipped. For example, the player can skip a cutscene.")]
    public bool CanSkip = false;
    [Tooltip("The name of the scene to load when the player goes to the previous scene. It can be empty")]
    public string PreviousSceneName;
    [Tooltip("The name of the scene to load when the player goes to the next scene. It can be empty")]
    public string NextSceneName;
    [Tooltip("If greater than 0, the scene will be loaded after a delay. This can be used to create a loading screen.")]
    public float LoadDelay = 0f;

    //public bool IsUnlocked = true;
    //public string[] UnlockConditions;

    //"Optional Overrides"

    public LevelConfig() { }

    //Copy Constructor
    public LevelConfig(LevelConfig other) {
      if (other == null) return;
      SceneName = other.SceneName;
      DisplayName = other.DisplayName;
      Type = other.Type;
      SetStateOnLoad = other.SetStateOnLoad;
      InitialGameState = other.InitialGameState;
      CanRestart = other.CanRestart;
      CanSkip = other.CanSkip;
      NextSceneName = other.NextSceneName;
      PreviousSceneName = other.PreviousSceneName;
      LoadDelay = other.LoadDelay;
      //IsUnlocked = other.IsUnlocked;
      //UnlockConditions = other.UnlockConditions != null ? (string[])other.UnlockConditions.Clone() : null;
    }

    public static LevelConfig Clone(LevelConfig config) => new(config);
  }

}
