using NUnit.Framework;
using UnityEngine;
using Ameba;
using Ameba.Testing;
using GMTK;

namespace Ameba.Tests {

  /// <summary>
  /// Example tests demonstrating how to use EventChannelTestUtils and EventChannelAssert
  /// for testing EventChannel functionality.
  /// </summary>
  [TestFixture]
  public class EventChannelTestUtilsExample {

    private GameEventChannel _eventChannel;
    private EventChannelTestUtils<GameEventType> _testUtils;

    [SetUp]
    public void SetUp() {
      // Create a test event channel instance
      _eventChannel = ScriptableObject.CreateInstance<GameEventChannel>();
      _eventChannel.name = "TestEventChannel";
      
      // Create test utils for this channel
      _testUtils = new EventChannelTestUtils<GameEventType>(_eventChannel);
    }

    [TearDown]
    public void TearDown() {
      // Clean up
      _testUtils.StopTracking();
      _eventChannel.RemoveAllListeners();
      
      if (_eventChannel != null) {
        Object.DestroyImmediate(_eventChannel);
      }
    }

    #region Basic Event Raising Tests

    [Test]
    public void RaiseVoidEvent_EventIsRaised() {
      // Arrange
      _testUtils.StartTracking();
      _testUtils.TrackEvent(GameEventType.GameStarted);

      // Act
      _testUtils.RaiseEvent(GameEventType.GameStarted);

      // Assert
      EventChannelAssert.WasRaised(_testUtils, GameEventType.GameStarted);
      EventChannelAssert.WasRaisedTimes(_testUtils, GameEventType.GameStarted, 1);
    }

    [Test]
    public void RaiseEventWithPayload_EventIsRaisedWithCorrectPayload() {
      // Arrange
      _testUtils.StartTracking();
      _testUtils.TrackEvent<int>(GameEventType.RaiseScore);
      int expectedScore = 100;

      // Act
      _testUtils.RaiseEvent(GameEventType.RaiseScore, expectedScore);

      // Assert
      EventChannelAssert.WasRaised(_testUtils, GameEventType.RaiseScore);
      EventChannelAssert.LastPayloadEquals(_testUtils, GameEventType.RaiseScore, expectedScore);
    }

    [Test]
    public void EventNotRaised_AssertNotRaisedPasses() {
      // Arrange
      _testUtils.StartTracking();
      _testUtils.TrackEvent(GameEventType.GameStarted);

      // Act
      // (Don't raise the event)

      // Assert
      EventChannelAssert.WasNotRaised(_testUtils, GameEventType.GameStarted);
    }

    #endregion

    #region Event Count Tests

    [Test]
    public void RaiseEventMultipleTimes_CountIsCorrect() {
      // Arrange
      _testUtils.StartTracking();
      _testUtils.TrackEvent(GameEventType.LevelStart);

      // Act
      _testUtils.RaiseEvent(GameEventType.LevelStart);
      _testUtils.RaiseEvent(GameEventType.LevelStart);
      _testUtils.RaiseEvent(GameEventType.LevelStart);

      // Assert
      EventChannelAssert.WasRaisedTimes(_testUtils, GameEventType.LevelStart, 3);
      EventChannelAssert.WasRaisedAtLeast(_testUtils, GameEventType.LevelStart, 2);
    }

    #endregion

    #region Payload Tests

    [Test]
    public void RaiseEventWithDifferentPayloads_AllPayloadsAreCaptured() {
      // Arrange
      _testUtils.StartTracking();
      _testUtils.TrackEvent<int>(GameEventType.RaiseScore);

      // Act
      _testUtils.RaiseEvent(GameEventType.RaiseScore, 10);
      _testUtils.RaiseEvent(GameEventType.RaiseScore, 20);
      _testUtils.RaiseEvent(GameEventType.RaiseScore, 30);

      // Assert
      EventChannelAssert.WasRaisedTimes(_testUtils, GameEventType.RaiseScore, 3);
      EventChannelAssert.PayloadCountEquals<GameEventType, int>(_testUtils, GameEventType.RaiseScore, 3);
      EventChannelAssert.LastPayloadEquals(_testUtils, GameEventType.RaiseScore, 30);
      EventChannelAssert.PayloadEquals(_testUtils, GameEventType.RaiseScore, 20);
    }

    [Test]
    public void PayloadMatchesPredicate_AssertionPasses() {
      // Arrange
      _testUtils.StartTracking();
      _testUtils.TrackEvent<int>(GameEventType.RaiseScore);

      // Act
      _testUtils.RaiseEvent(GameEventType.RaiseScore, 50);
      _testUtils.RaiseEvent(GameEventType.RaiseScore, 75);
      _testUtils.RaiseEvent(GameEventType.RaiseScore, 100);

      // Assert - All payloads are greater than 25
      EventChannelAssert.AllPayloadsMatch<GameEventType, int>(
        _testUtils, 
        GameEventType.RaiseScore, 
        score => score > 25
      );
    }

    [Test]
    public void AnyPayloadMatchesPredicate_AssertionPasses() {
      // Arrange
      _testUtils.StartTracking();
      _testUtils.TrackEvent<int>(GameEventType.RaiseScore);

      // Act
      _testUtils.RaiseEvent(GameEventType.RaiseScore, 10);
      _testUtils.RaiseEvent(GameEventType.RaiseScore, 50);
      _testUtils.RaiseEvent(GameEventType.RaiseScore, 100);

      // Assert - At least one payload is exactly 100
      EventChannelAssert.AnyPayloadMatches<GameEventType, int>(
        _testUtils, 
        GameEventType.RaiseScore, 
        score => score == 100
      );
    }

    #endregion

    #region String Payload Tests

    [Test]
    public void RaiseEventWithStringPayload_PayloadIsCorrect() {
      // Arrange
      _testUtils.StartTracking();
      _testUtils.TrackEvent<string>(GameEventType.EnterCheckpoint);
      string checkpointId = "checkpoint_01";

      // Act
      _testUtils.RaiseEvent(GameEventType.EnterCheckpoint, checkpointId);

      // Assert
      EventChannelAssert.WasRaised(_testUtils, GameEventType.EnterCheckpoint);
      EventChannelAssert.LastPayloadEquals(_testUtils, GameEventType.EnterCheckpoint, checkpointId);
    }

    #endregion

    #region Integration Tests

    [Test]
    public void MonoBehaviorRaisesEvent_EventIsCaptured() {
      // This test demonstrates how to test MonoBehaviors that raise events
      // Arrange
      _testUtils.StartTracking();
      _testUtils.TrackEvent(GameEventType.LevelStart);
      
      var testObject = new GameObject("TestObject");
      var component = testObject.AddComponent<TestEventRaiser>();
      component.Initialize(_eventChannel);

      // Act
      component.RaiseLevelStartEvent();

      // Assert
      EventChannelAssert.WasRaised(_testUtils, GameEventType.LevelStart);

      // Cleanup
      Object.DestroyImmediate(testObject);
    }

    #endregion

    #region Utility Method Tests

    [Test]
    public void GetRaisedEventTypes_ReturnsAllRaisedEvents() {
      // Arrange
      _testUtils.StartTracking();
      _testUtils.TrackEvent(GameEventType.GameStarted);
      _testUtils.TrackEvent(GameEventType.LevelStart);
      _testUtils.TrackEvent<int>(GameEventType.RaiseScore);

      // Act
      _testUtils.RaiseEvent(GameEventType.GameStarted);
      _testUtils.RaiseEvent(GameEventType.LevelStart);
      _testUtils.RaiseEvent(GameEventType.RaiseScore, 100);

      // Assert
      var raisedEvents = _testUtils.GetRaisedEventTypes();
      Assert.AreEqual(3, raisedEvents.Count);
      Assert.That(raisedEvents, Does.Contain(GameEventType.GameStarted));
      Assert.That(raisedEvents, Does.Contain(GameEventType.LevelStart));
      Assert.That(raisedEvents, Does.Contain(GameEventType.RaiseScore));
    }

    [Test]
    public void ClearTrackedEvents_ResetsEventHistory() {
      // Arrange
      _testUtils.StartTracking();
      _testUtils.TrackEvent(GameEventType.GameStarted);
      _testUtils.RaiseEvent(GameEventType.GameStarted);
      
      // Verify event was tracked
      Assert.AreEqual(1, _testUtils.GetEventRaisedCount(GameEventType.GameStarted));

      // Act
      _testUtils.ClearTrackedEvents();

      // Assert
      Assert.AreEqual(0, _testUtils.GetEventRaisedCount(GameEventType.GameStarted));
    }

    #endregion
  }

  /// <summary>
  /// Simple MonoBehavior for testing event raising from components.
  /// </summary>
  public class TestEventRaiser : MonoBehaviour {
    private GameEventChannel _eventChannel;

    public void Initialize(GameEventChannel eventChannel) {
      _eventChannel = eventChannel;
    }

    public void RaiseLevelStartEvent() {
      _eventChannel?.Raise(GameEventType.LevelStart);
    }
  }
}
