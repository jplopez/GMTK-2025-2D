# EventChannel Test Utilities

Test utilities for testing `EventChannel<TEnum>` in Unity projects using the Unity Test Framework.

## Overview

The Ameba EventChannel Test Utilities provide a comprehensive set of tools for testing event-driven code in Unity. These utilities allow you to:

- Programmatically trigger events from EventChannel
- Track and verify which events were raised
- Mock and validate event payloads
- Use custom assertions integrated with NUnit

## Components

### 1. EventChannelTestUtils<TEnum>

The main utility class for testing EventChannel instances.

#### Key Features

- **Event Tracking**: Monitor which events are raised during test execution
- **Event Triggering**: Programmatically raise events for testing
- **Payload Capture**: Capture and retrieve event payloads
- **Event Counting**: Track how many times each event was raised

#### Basic Usage

```csharp
using Ameba.Testing;
using NUnit.Framework;

[TestFixture]
public class MyEventChannelTests {
    private GameEventChannel _eventChannel;
    private EventChannelTestUtils<GameEventType> _testUtils;

    [SetUp]
    public void SetUp() {
        _eventChannel = ScriptableObject.CreateInstance<GameEventChannel>();
        _testUtils = new EventChannelTestUtils<GameEventType>(_eventChannel);
    }

    [TearDown]
    public void TearDown() {
        _testUtils.StopTracking();
        _eventChannel.RemoveAllListeners();
        Object.DestroyImmediate(_eventChannel);
    }

    [Test]
    public void TestEventIsRaised() {
        // Arrange
        _testUtils.StartTracking();
        _testUtils.TrackEvent(GameEventType.GameStarted);

        // Act
        _testUtils.RaiseEvent(GameEventType.GameStarted);

        // Assert
        Assert.IsTrue(_testUtils.WasEventRaised(GameEventType.GameStarted));
    }
}
```

### 2. EventChannelAssert

Custom assertions that integrate with NUnit for cleaner, more readable tests.

#### Available Assertions

- `WasRaised<TEnum>()` - Assert an event was raised
- `WasNotRaised<TEnum>()` - Assert an event was NOT raised
- `WasRaisedTimes<TEnum>()` - Assert event was raised exactly N times
- `WasRaisedAtLeast<TEnum>()` - Assert event was raised at least N times
- `PayloadEquals<TEnum, TPayload>()` - Assert payload equals expected value
- `LastPayloadEquals<TEnum, TPayload>()` - Assert last payload equals value
- `AllPayloadsMatch<TEnum, TPayload>()` - Assert all payloads match predicate
- `AnyPayloadMatches<TEnum, TPayload>()` - Assert at least one payload matches
- `PayloadCountEquals<TEnum, TPayload>()` - Assert number of payloads

#### Usage Examples

```csharp
[Test]
public void TestEventWithPayload() {
    // Arrange
    _testUtils.StartTracking();
    _testUtils.TrackEvent<int>(GameEventType.RaiseScore);

    // Act
    _testUtils.RaiseEvent(GameEventType.RaiseScore, 100);

    // Assert with custom assertions
    EventChannelAssert.WasRaised(_testUtils, GameEventType.RaiseScore);
    EventChannelAssert.LastPayloadEquals(_testUtils, GameEventType.RaiseScore, 100);
}

[Test]
public void TestMultipleEvents() {
    // Arrange
    _testUtils.StartTracking();
    _testUtils.TrackEvent<int>(GameEventType.RaiseScore);

    // Act
    _testUtils.RaiseEvent(GameEventType.RaiseScore, 10);
    _testUtils.RaiseEvent(GameEventType.RaiseScore, 20);
    _testUtils.RaiseEvent(GameEventType.RaiseScore, 30);

    // Assert
    EventChannelAssert.WasRaisedTimes(_testUtils, GameEventType.RaiseScore, 3);
    EventChannelAssert.AllPayloadsMatch<GameEventType, int>(
        _testUtils, 
        GameEventType.RaiseScore, 
        score => score > 0
    );
}
```

## Testing MonoBehaviors and ScriptableObjects

### Testing MonoBehaviors

```csharp
[Test]
public void TestMonoBehaviorRaisesEvent() {
    // Arrange
    _testUtils.StartTracking();
    _testUtils.TrackEvent(GameEventType.LevelStart);
    
    var gameObject = new GameObject("TestObject");
    var component = gameObject.AddComponent<MyComponent>();
    component.Initialize(_eventChannel);

    // Act
    component.StartLevel();

    // Assert
    EventChannelAssert.WasRaised(_testUtils, GameEventType.LevelStart);

    // Cleanup
    Object.DestroyImmediate(gameObject);
}
```

### Testing ScriptableObjects

```csharp
[Test]
public void TestScriptableObjectListensToEvent() {
    // Arrange
    _testUtils.StartTracking();
    var scriptableObject = ScriptableObject.CreateInstance<MyScriptableObject>();
    scriptableObject.Subscribe(_eventChannel);
    
    // Act
    _testUtils.RaiseEvent(GameEventType.GameStarted);

    // Assert
    Assert.IsTrue(scriptableObject.WasNotified);

    // Cleanup
    Object.DestroyImmediate(scriptableObject);
}
```

## Advanced Usage

### Payload Predicates

Test complex payload conditions:

```csharp
[Test]
public void TestPayloadConditions() {
    _testUtils.StartTracking();
    _testUtils.TrackEvent<int>(GameEventType.RaiseScore);

    _testUtils.RaiseEvent(GameEventType.RaiseScore, 75);
    _testUtils.RaiseEvent(GameEventType.RaiseScore, 150);

    // Check if any score is above 100
    EventChannelAssert.AnyPayloadMatches<GameEventType, int>(
        _testUtils,
        GameEventType.RaiseScore,
        score => score > 100
    );

    // Check if all scores are positive
    EventChannelAssert.AllPayloadsMatch<GameEventType, int>(
        _testUtils,
        GameEventType.RaiseScore,
        score => score > 0
    );
}
```

### Testing Event Sequences

```csharp
[Test]
public void TestEventSequence() {
    _testUtils.StartTracking();
    _testUtils.TrackEvent(GameEventType.GameStarted);
    _testUtils.TrackEvent(GameEventType.LevelStart);
    _testUtils.TrackEvent(GameEventType.LevelPlay);

    // Act - raise events in sequence
    _testUtils.RaiseEvent(GameEventType.GameStarted);
    _testUtils.RaiseEvent(GameEventType.LevelStart);
    _testUtils.RaiseEvent(GameEventType.LevelPlay);

    // Assert sequence
    var raisedEvents = _testUtils.GetRaisedEventTypes();
    Assert.AreEqual(3, raisedEvents.Count);
    Assert.AreEqual(GameEventType.GameStarted, raisedEvents[0]);
    Assert.AreEqual(GameEventType.LevelStart, raisedEvents[1]);
    Assert.AreEqual(GameEventType.LevelPlay, raisedEvents[2]);
}
```

### Clearing Event History

```csharp
[Test]
public void TestClearHistory() {
    _testUtils.StartTracking();
    _testUtils.TrackEvent(GameEventType.GameStarted);
    
    _testUtils.RaiseEvent(GameEventType.GameStarted);
    Assert.AreEqual(1, _testUtils.GetEventRaisedCount(GameEventType.GameStarted));

    // Clear and verify
    _testUtils.ClearTrackedEvents();
    Assert.AreEqual(0, _testUtils.GetEventRaisedCount(GameEventType.GameStarted));
}
```

## Best Practices

1. **Always call StartTracking()** - Call this in your test setup or at the beginning of each test
2. **Clean up in TearDown** - Call `StopTracking()` and `RemoveAllListeners()` to avoid test pollution
3. **Use custom assertions** - `EventChannelAssert` provides clearer, more readable test code
4. **Test in isolation** - Each test should create its own EventChannel instance
5. **Track before raising** - Call `TrackEvent()` before the code that raises events

## Integration with Unity Test Framework

These utilities are designed to work seamlessly with:
- Unity Test Framework (UTF)
- NUnit assertions
- Edit mode and Play mode tests

## Assembly Definition

The utilities are in the `Ameba.Testing` assembly, which:
- Is Editor-only
- References the `Ameba` assembly
- Has `UNITY_INCLUDE_TESTS` define constraint
- Requires NUnit framework

## See Also

- [EventChannelTestUtilsExample.cs](../../Tests/Editor/EventChannelTestUtilsExample.cs) - Complete example tests
- Unity Test Framework documentation
- NUnit assertions documentation
