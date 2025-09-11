using UnityEngine;

namespace Ameba {

  /// <summary>
  /// Manages the configuration and behavior of the score-keeping system in the game.
  /// </summary>
  /// <remarks>This controller is responsible for initializing and updating the <see cref="ScoreGateKeeper"/>
  /// instance,  applying the selected score calculation strategy, and managing score-related settings such as pausing 
  /// and resetting the score. It provides options to configure the score system at runtime, including  whether to reset
  /// or set a starting score on initialization.  The <see cref="ScoreKeeper"/> property provides access to the
  /// underlying <see cref="ScoreGateKeeper"/>  instance, which handles the actual score calculations and
  /// state.</remarks>
  public class ScoreKeeperController : MonoBehaviour {

    [Header("Score Keeper")]
    [SerializeField] protected ScoreGateKeeper _scoreKeeper;
    [Tooltip("The strategy to use to calculate the score")]
    public ScoreCalculationStrategy CalculationStrategy;
    [Tooltip("The Transform of the Player, used by some strategies")]
    public Transform PlayerTransform;
    
    [Header("Score Control")]
    [Tooltip("If true, the score calculation is paused")]
    public bool PauseScore = false;

    [Header("Initialization Options")]
    [Tooltip("If true, the Score is reset to zero on Start")]
    public bool ResetOnStart = false;
    [Tooltip("The Score to set on Start")]
    public int StartingScore = 0;
    [Tooltip("If true, the StartingScore is set on Start")]
    public bool SetStartingScoreOnStart = false;

    public ScoreGateKeeper ScoreKeeper => _scoreKeeper;

    private void Awake() {
      if(_scoreKeeper == null) {
        _scoreKeeper = ServiceLocator.Get<ScoreGateKeeper>();
      }
      if(!_scoreKeeper.HasStrategy() && CalculationStrategy != null) {
        PlayerTransform = PlayerTransform == null ? this.transform : PlayerTransform;
        _scoreKeeper.SetStrategy(CalculationStrategy, PlayerTransform);
      }
    }
    
    private void Start() {
      if(ResetOnStart) _scoreKeeper.ResetScore();
      if(SetStartingScoreOnStart) _scoreKeeper.SetScore(StartingScore);
    }

    private void Update() {
      _scoreKeeper.PauseScore(PauseScore);
      _scoreKeeper.Tick(Time.deltaTime);
    }

  }
}