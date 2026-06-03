using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Ameba.Testing {

  /// <summary>
  /// Custom assertions for EventChannel testing.
  /// Integrates with NUnit.Framework assertions.
  /// </summary>
  public static class EventChannelAssert {

    /// <summary>
    /// Asserts that a specific event was raised at least once.
    /// </summary>
    /// <typeparam name="TEnum">The enum type used by the EventChannel</typeparam>
    /// <param name="utils">The EventChannelTestUtils instance</param>
    /// <param name="eventType">The event type to check</param>
    /// <param name="message">Optional custom assertion message</param>
    public static void WasRaised<TEnum>(EventChannelTestUtils<TEnum> utils, TEnum eventType, string message = null) 
      where TEnum : Enum {
      if (utils == null) throw new ArgumentNullException(nameof(utils));
      
      bool wasRaised = utils.WasEventRaised(eventType);
      string assertMessage = message ?? $"Expected event '{eventType}' to be raised, but it was not.";
      
      Assert.IsTrue(wasRaised, assertMessage);
    }

    /// <summary>
    /// Asserts that a specific event was NOT raised.
    /// </summary>
    /// <typeparam name="TEnum">The enum type used by the EventChannel</typeparam>
    /// <param name="utils">The EventChannelTestUtils instance</param>
    /// <param name="eventType">The event type to check</param>
    /// <param name="message">Optional custom assertion message</param>
    public static void WasNotRaised<TEnum>(EventChannelTestUtils<TEnum> utils, TEnum eventType, string message = null) 
      where TEnum : Enum {
      if (utils == null) throw new ArgumentNullException(nameof(utils));
      
      bool wasRaised = utils.WasEventRaised(eventType);
      string assertMessage = message ?? $"Expected event '{eventType}' NOT to be raised, but it was.";
      
      Assert.IsFalse(wasRaised, assertMessage);
    }

    /// <summary>
    /// Asserts that an event was raised a specific number of times.
    /// </summary>
    /// <typeparam name="TEnum">The enum type used by the EventChannel</typeparam>
    /// <param name="utils">The EventChannelTestUtils instance</param>
    /// <param name="eventType">The event type to check</param>
    /// <param name="expectedCount">The expected number of times the event was raised</param>
    /// <param name="message">Optional custom assertion message</param>
    public static void WasRaisedTimes<TEnum>(EventChannelTestUtils<TEnum> utils, TEnum eventType, int expectedCount, string message = null) 
      where TEnum : Enum {
      if (utils == null) throw new ArgumentNullException(nameof(utils));
      
      int actualCount = utils.GetEventRaisedCount(eventType);
      string assertMessage = message ?? 
        $"Expected event '{eventType}' to be raised {expectedCount} time(s), but was raised {actualCount} time(s).";
      
      Assert.AreEqual(expectedCount, actualCount, assertMessage);
    }

    /// <summary>
    /// Asserts that an event was raised at least a minimum number of times.
    /// </summary>
    /// <typeparam name="TEnum">The enum type used by the EventChannel</typeparam>
    /// <param name="utils">The EventChannelTestUtils instance</param>
    /// <param name="eventType">The event type to check</param>
    /// <param name="minCount">The minimum number of times the event should be raised</param>
    /// <param name="message">Optional custom assertion message</param>
    public static void WasRaisedAtLeast<TEnum>(EventChannelTestUtils<TEnum> utils, TEnum eventType, int minCount, string message = null) 
      where TEnum : Enum {
      if (utils == null) throw new ArgumentNullException(nameof(utils));
      
      int actualCount = utils.GetEventRaisedCount(eventType);
      string assertMessage = message ?? 
        $"Expected event '{eventType}' to be raised at least {minCount} time(s), but was raised {actualCount} time(s).";
      
      Assert.GreaterOrEqual(actualCount, minCount, assertMessage);
    }

    /// <summary>
    /// Asserts that an event payload has a specific value.
    /// </summary>
    /// <typeparam name="TEnum">The enum type used by the EventChannel</typeparam>
    /// <typeparam name="TPayload">The payload type</typeparam>
    /// <param name="utils">The EventChannelTestUtils instance</param>
    /// <param name="eventType">The event type to check</param>
    /// <param name="expectedPayload">The expected payload value</param>
    /// <param name="message">Optional custom assertion message</param>
    public static void PayloadEquals<TEnum, TPayload>(EventChannelTestUtils<TEnum> utils, TEnum eventType, TPayload expectedPayload, string message = null) 
      where TEnum : Enum {
      if (utils == null) throw new ArgumentNullException(nameof(utils));
      
      var payloads = utils.GetEventPayloads<TPayload>(eventType);
      string assertMessage = message ?? 
        $"Expected at least one event '{eventType}' with payload '{expectedPayload}', but none were found.";
      
      Assert.That(payloads, Does.Contain(expectedPayload), assertMessage);
    }

    /// <summary>
    /// Asserts that the last raised event has a specific payload value.
    /// </summary>
    /// <typeparam name="TEnum">The enum type used by the EventChannel</typeparam>
    /// <typeparam name="TPayload">The payload type</typeparam>
    /// <param name="utils">The EventChannelTestUtils instance</param>
    /// <param name="eventType">The event type to check</param>
    /// <param name="expectedPayload">The expected payload value</param>
    /// <param name="message">Optional custom assertion message</param>
    public static void LastPayloadEquals<TEnum, TPayload>(EventChannelTestUtils<TEnum> utils, TEnum eventType, TPayload expectedPayload, string message = null) 
      where TEnum : Enum {
      if (utils == null) throw new ArgumentNullException(nameof(utils));
      
      TPayload actualPayload = utils.GetLastEventPayload<TPayload>(eventType);
      string assertMessage = message ?? 
        $"Expected last event '{eventType}' payload to be '{expectedPayload}', but was '{actualPayload}'.";
      
      Assert.AreEqual(expectedPayload, actualPayload, assertMessage);
    }

    /// <summary>
    /// Asserts that all payloads for an event match a predicate.
    /// </summary>
    /// <typeparam name="TEnum">The enum type used by the EventChannel</typeparam>
    /// <typeparam name="TPayload">The payload type</typeparam>
    /// <param name="utils">The EventChannelTestUtils instance</param>
    /// <param name="eventType">The event type to check</param>
    /// <param name="predicate">The predicate to test each payload against</param>
    /// <param name="message">Optional custom assertion message</param>
    public static void AllPayloadsMatch<TEnum, TPayload>(EventChannelTestUtils<TEnum> utils, TEnum eventType, Predicate<TPayload> predicate, string message = null) 
      where TEnum : Enum {
      if (utils == null) throw new ArgumentNullException(nameof(utils));
      if (predicate == null) throw new ArgumentNullException(nameof(predicate));
      
      var payloads = utils.GetEventPayloads<TPayload>(eventType);
      string assertMessage = message ?? 
        $"Expected all payloads for event '{eventType}' to match predicate, but some did not.";
      
      Assert.That(payloads, Is.All.Matches(predicate), assertMessage);
    }

    /// <summary>
    /// Asserts that at least one payload for an event matches a predicate.
    /// </summary>
    /// <typeparam name="TEnum">The enum type used by the EventChannel</typeparam>
    /// <typeparam name="TPayload">The payload type</typeparam>
    /// <param name="utils">The EventChannelTestUtils instance</param>
    /// <param name="eventType">The event type to check</param>
    /// <param name="predicate">The predicate to test each payload against</param>
    /// <param name="message">Optional custom assertion message</param>
    public static void AnyPayloadMatches<TEnum, TPayload>(EventChannelTestUtils<TEnum> utils, TEnum eventType, Predicate<TPayload> predicate, string message = null) 
      where TEnum : Enum {
      if (utils == null) throw new ArgumentNullException(nameof(utils));
      if (predicate == null) throw new ArgumentNullException(nameof(predicate));
      
      var payloads = utils.GetEventPayloads<TPayload>(eventType);
      bool anyMatch = payloads.Exists(predicate);
      string assertMessage = message ?? 
        $"Expected at least one payload for event '{eventType}' to match predicate, but none did.";
      
      Assert.IsTrue(anyMatch, assertMessage);
    }

    /// <summary>
    /// Asserts that the event payloads list has a specific count.
    /// </summary>
    /// <typeparam name="TEnum">The enum type used by the EventChannel</typeparam>
    /// <typeparam name="TPayload">The payload type</typeparam>
    /// <param name="utils">The EventChannelTestUtils instance</param>
    /// <param name="eventType">The event type to check</param>
    /// <param name="expectedCount">The expected number of payloads</param>
    /// <param name="message">Optional custom assertion message</param>
    public static void PayloadCountEquals<TEnum, TPayload>(EventChannelTestUtils<TEnum> utils, TEnum eventType, int expectedCount, string message = null) 
      where TEnum : Enum {
      if (utils == null) throw new ArgumentNullException(nameof(utils));
      
      var payloads = utils.GetEventPayloads<TPayload>(eventType);
      int actualCount = payloads.Count;
      string assertMessage = message ?? 
        $"Expected {expectedCount} payload(s) for event '{eventType}', but found {actualCount}.";
      
      Assert.AreEqual(expectedCount, actualCount, assertMessage);
    }
  }
}
