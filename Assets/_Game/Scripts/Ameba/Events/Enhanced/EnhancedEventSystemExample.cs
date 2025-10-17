using System;
using UnityEngine;

namespace Ameba.Events {

  /// <summary>
  /// Example component demonstrating how to use the Enhanced Event System programmatically.
  /// This shows how developers can use the system in code, while designers can use the components.
  /// </summary>
  public class EnhancedEventSystemExample : MonoBehaviour {

    [Header("Event Channel")]
    [SerializeField] private EnhancedEventChannel eventChannel;

    [Header("Example Events")]
    [SerializeField] private string playerDeathEvent = "PlayerDeath";
    [SerializeField] private string scoreUpdateEvent = "ScoreUpdate";
    [SerializeField] private int gameStateChangeEvent = 100;

    private void Start() {
      // Subscribe to events programmatically
      SubscribeToEvents();
      
      // Test events after a short delay
      Invoke(nameof(TestEvents), 2f);
    }

    private void OnDestroy() {
      // Unsubscribe to prevent memory leaks
      UnsubscribeFromEvents();
    }

    #region Event Subscription Examples

    private void SubscribeToEvents() {
      if (eventChannel == null) return;

      // Subscribe to void events
      eventChannel.AddListener(playerDeathEvent, OnPlayerDeath);

      // Subscribe to events with payloads
      eventChannel.AddListener<int>(scoreUpdateEvent, OnScoreUpdate);
      eventChannel.AddListener<bool>(gameStateChangeEvent, OnGameStateChange);

      // Subscribe to custom EventArgs events
      eventChannel.AddListener<GenericEventArgs>("CustomEvent", OnCustomEvent);

      Debug.Log("Subscribed to all example events");
    }

    private void UnsubscribeFromEvents() {
      if (eventChannel == null) return;

      eventChannel.RemoveListener(playerDeathEvent, OnPlayerDeath);
      eventChannel.RemoveListener<int>(scoreUpdateEvent, OnScoreUpdate);
      eventChannel.RemoveListener<bool>(gameStateChangeEvent, OnGameStateChange);
      eventChannel.RemoveListener<GenericEventArgs>("CustomEvent", OnCustomEvent);

      Debug.Log("Unsubscribed from all example events");
    }

    #endregion

    #region Event Handlers

    private void OnPlayerDeath() {
      Debug.Log("Player has died! Handling death event...");
      // Handle player death logic here
    }

    private void OnScoreUpdate(int newScore) {
      Debug.Log($"Score updated to: {newScore}");
      // Update UI, save high score, etc.
    }

    private void OnGameStateChange(bool isGameActive) {
      Debug.Log($"Game state changed. Active: {isGameActive}");
      // Handle game state changes
    }

    private void OnCustomEvent(GenericEventArgs eventArgs) {
      Debug.Log($"Custom event received: {eventArgs.Message} at {eventArgs.Timestamp}");
      // Handle custom event
    }

    #endregion

    #region Test Event Triggering

    [ContextMenu("Test Events")]
    private void TestEvents() {
      if (eventChannel == null) {
        Debug.LogError("Event channel not assigned!");
        return;
      }

      Debug.Log("=== Testing Enhanced Event System ===");

      // Test void event
      eventChannel.Raise(playerDeathEvent);

      // Test event with int payload
      eventChannel.Raise(scoreUpdateEvent, 1500);

      // Test event with bool payload
      eventChannel.Raise(gameStateChangeEvent, true);

      // Test event with custom EventArgs
      var customArgs = new GenericEventArgs("This is a test message");
      eventChannel.Raise("CustomEvent", customArgs);

      Debug.Log("=== Event testing complete ===");
    }

    [ContextMenu("Test String Identifiers")]
    private void TestStringIdentifiers() {
      if (eventChannel == null) return;

      Debug.Log("Testing string identifiers...");
      
      eventChannel.Raise("Level_Start");
      eventChannel.Raise("Level_Complete", 100);
      eventChannel.Raise("Player_TookDamage", 25.5f);
      eventChannel.Raise("UI_ShowMessage", "Hello World!");
    }

    [ContextMenu("Test Int Identifiers")]
    private void TestIntIdentifiers() {
      if (eventChannel == null) return;

      Debug.Log("Testing int identifiers...");
      
      eventChannel.Raise(1001); // Game Started
      eventChannel.Raise(1002, true); // Game Paused
      eventChannel.Raise(1003, 42); // Level Selected
    }

    [ContextMenu("Test Enum Identifiers")]
    private void TestEnumIdentifiers() {
      if (eventChannel == null) return;

      Debug.Log("Testing enum identifiers...");
      
      // These would work with actual enums in your project
      // eventChannel.Raise(GameEvents.PlayerSpawned);
      // eventChannel.Raise(GameEvents.EnemyDefeated, 10);
    }

    #endregion

    #region Utility Methods

    //[ContextMenu("Print Event Channel Statistics")]
    //private void PrintStatistics() {
    //  if (eventChannel == null) return;

    //  var registeredEvents = eventChannel.GetRegisteredEvents();
    //  Debug.Log($"Event Channel '{eventChannel.name}' has {registeredEvents.Length} registered events:");
      
    //  foreach (var eventId in registeredEvents) {
    //    var listenerCount = eventChannel.GetListenerCount(eventId);
    //    Debug.Log($"  - {eventId}: {listenerCount} listeners");
    //  }
    //}

    #endregion
  }

  /// <summary>
  /// Example game events enum that could be used with the system.
  /// </summary>
  public enum GameEvents {
    PlayerSpawned,
    PlayerDied,
    EnemyDefeated,
    LevelComplete,
    GamePaused,
    GameResumed,
    ScoreChanged,
    PowerUpCollected
  }

  /// <summary>
  /// Example custom EventArgs for game-specific events.
  /// </summary>
  [Serializable]
  public class PlayerEventArgs : EventArgs {
    public string PlayerName { get; }
    public int PlayerId { get; }
    public Vector3 Position { get; }

    public PlayerEventArgs(string playerName, int playerId, Vector3 position) {
      PlayerName = playerName;
      PlayerId = playerId;
      Position = position;
    }
  }

  /// <summary>
  /// Example custom EventArgs for score events.
  /// </summary>
  [Serializable]
  public class ScoreEventArgs : EventArgs {
    public int OldScore { get; }
    public int NewScore { get; }
    public int ScoreDifference => NewScore - OldScore;

    public ScoreEventArgs(int oldScore, int newScore) {
      OldScore = oldScore;
      NewScore = newScore;
    }
  }
}