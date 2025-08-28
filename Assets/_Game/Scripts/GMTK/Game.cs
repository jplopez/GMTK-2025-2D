using Ameba;
using System;
using UnityEngine;

namespace GMTK {
  /// <summary>
  /// Static class that pre-loads all ScriptableObjects used in the game, to guarantee they're accessible when GameObjects begin loading
  /// </summary>
  public static class Game {

    // Static references (initialized by InitializationManager)
    private static GameStateMachine _gameStateMachine;
    private static GameEventChannel _gameEventChannel;
    private static InputActionEventChannel _inputEventChannel;
    private static LevelSequence _levelSequence;
    private static ScoreGateKeeper _scoreKeeper;
    private static HUD _HUD;
    private static GameStateHandlerRegistry _handlerRegistry;

    // GameContext reference (set by active scene)
    private static GameContext _context;

    // ScriptableObject access (always available)
    public static GameStateMachine StateMachine => _gameStateMachine;
    public static GameEventChannel EventChannel => _gameEventChannel;
    public static InputActionEventChannel InputEventChannel => _inputEventChannel;
    public static LevelSequence LevelSequence => _levelSequence;
    public static ScoreGateKeeper ScoreKeeper => _scoreKeeper;
    public static HUD HUD => _HUD;
    public static GameStateHandlerRegistry HandlerRegistry => _handlerRegistry;

    // Scene management access (may be null between scenes)
    public static GameContext Context => _context;

    // Internal setters for initialization
    internal static void SetGameStateMachine(GameStateMachine stateMachine) => _gameStateMachine = stateMachine;
    internal static void SetGameEventChannel(GameEventChannel eventChannel) => _gameEventChannel = eventChannel;
    internal static void SetInputEventChannel(InputActionEventChannel inputChannel) => _inputEventChannel = inputChannel;
    internal static void SetLevelSequence(LevelSequence levelSequence) => _levelSequence = levelSequence;
    internal static void SetScoreKeeper(ScoreGateKeeper scoreKeeper) => _scoreKeeper = scoreKeeper;
    internal static void SetHUD(HUD hUD) => _HUD = hUD;
    internal static void SetHandlerRegistry(GameStateHandlerRegistry registry) => _handlerRegistry = registry;
    internal static void SetContext(GameContext context) => _context = context;

  }
}