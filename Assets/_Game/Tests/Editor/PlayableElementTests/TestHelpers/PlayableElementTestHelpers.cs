using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GMTK;
using System.Collections.Generic;
using Ameba;

namespace GMTK.Tests {
  /// <summary>
  /// Helper utilities for PlayableElement testing
  /// </summary>
  public static class PlayableElementTestHelpers {

    #region GameObject Creation

    /// <summary>
    /// Creates a basic GameObject with PlayableElement component configured for testing
    /// </summary>
    public static PlayableElement CreateTestPlayableElement(string name = "TestElement") {
      var go = new GameObject(name);
      go.layer = LayerMask.NameToLayer("Interactives");
      
      var element = go.AddComponent<PlayableElement>();
      
      // Add required components
      var spriteRenderer = go.AddComponent<SpriteRenderer>();
      var collider = go.AddComponent<PolygonCollider2D>();
      
      // Create a simple sprite for testing
      spriteRenderer.sprite = CreateTestSprite();
      
      return element;
    }

    /// <summary>
    /// Creates a test sprite for SpriteRenderer
    /// </summary>
    public static Sprite CreateTestSprite() {
      var texture = new Texture2D(32, 32);
      return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// Creates a PlayableElement with specific configuration
    /// </summary>
    public static PlayableElement CreateConfiguredElement(
      bool draggable = true,
      bool canRotate = false,
      bool flippable = false,
      SelectionTrigger selectionTriggers = SelectionTrigger.OnClick) {
      
      var element = CreateTestPlayableElement();
      element.Draggable = draggable;
      element.CanRotate = canRotate;
      element.Flippable = flippable;
      element.SelectionTriggers = selectionTriggers;
      
      return element;
    }

    /// <summary>
    /// Creates a PlayableMarbleController for testing marble interactions
    /// </summary>
    public static PlayableMarbleController CreateTestMarble(string name = "TestMarble") {
      var go = new GameObject(name);
      go.layer = LayerMask.NameToLayer("Default");
      
      var rb = go.AddComponent<Rigidbody2D>();
      rb.bodyType = RigidbodyType2D.Dynamic;
      
      var collider = go.AddComponent<CircleCollider2D>();
      collider.radius = 0.5f;
      
      var spriteRenderer = go.AddComponent<SpriteRenderer>();
      spriteRenderer.sprite = CreateTestSprite();
      
      var marble = go.AddComponent<PlayableMarbleController>();
      marble.Model = go;
      
      return marble;
    }

    #endregion

    #region Mock Services

    /// <summary>
    /// Creates a mock GameEventChannel for testing
    /// </summary>
    public static MockGameEventChannel CreateMockGameEventChannel() {
      return new MockGameEventChannel();
    }

    /// <summary>
    /// Sets up ServiceLocator with mock services for testing
    /// </summary>
    public static void SetupMockServices() {
      var mockEventChannel = CreateMockGameEventChannel();
      ServiceLocator.Register<GameEventChannel>(mockEventChannel);
    }

    /// <summary>
    /// Cleans up ServiceLocator after tests
    /// </summary>
    public static void CleanupMockServices() {
      ServiceLocator.Clear<GameEventChannel>();
    }

    #endregion

    #region Event Simulation

    /// <summary>
    /// Simulates a pointer hover over an element
    /// </summary>
    public static void SimulatePointerHover(PlayableElement element, Vector3 worldPosition) {
      element.MarkHovered(true);
    }

    /// <summary>
    /// Simulates a pointer unhover from an element
    /// </summary>
    public static void SimulatePointerUnhover(PlayableElement element) {
      element.MarkHovered(false);
    }

    /// <summary>
    /// Simulates a selection event on an element
    /// </summary>
    public static void SimulateSelection(PlayableElement element) {
      element.MarkSelected(true);
    }

    /// <summary>
    /// Simulates a deselection event on an element
    /// </summary>
    public static void SimulateDeselection(PlayableElement element) {
      element.MarkSelected(false);
    }

    /// <summary>
    /// Simulates a drag start event
    /// </summary>
    public static void SimulateDragStart(PlayableElement element) {
      element.OnDragStart();
    }

    /// <summary>
    /// Simulates a drag update event
    /// </summary>
    public static void SimulateDragUpdate(PlayableElement element, Vector3 worldPosition) {
      element.OnDragUpdate(worldPosition);
    }

    /// <summary>
    /// Simulates a drag end event
    /// </summary>
    public static void SimulateDragEnd(PlayableElement element) {
      element.OnDragEnd();
    }

    #endregion

    #region Collision Simulation

    /// <summary>
    /// Simulates a collision between a marble and a PlayableElement
    /// </summary>
    public static void SimulateMarbleCollision(
      PlayableMarbleController marble, 
      PlayableElement element,
      Vector2 contactPoint,
      Vector2 contactNormal,
      float relativeVelocity) {
      
      // This would trigger physics collision in a real scenario
      // For unit tests, we may need to invoke the collision handler directly
      // or use Unity's physics simulation in integration tests
      LogHelper.Log($"Simulating collision between {marble.name} and {element.name}");
    }

    #endregion

    #region State Verification

    /// <summary>
    /// Verifies that an element is in the expected hover state
    /// </summary>
    public static void AssertHoverState(PlayableElement element, bool expectedHovered, string message = "") {
      Assert.AreEqual(expectedHovered, element.IsHovered, 
        $"Element hover state mismatch. {message}");
    }

    /// <summary>
    /// Verifies that an element is in the expected selection state
    /// </summary>
    public static void AssertSelectionState(PlayableElement element, bool expectedSelected, string message = "") {
      Assert.AreEqual(expectedSelected, element.IsSelected, 
        $"Element selection state mismatch. {message}");
    }

    /// <summary>
    /// Verifies that an element is in the expected drag state
    /// </summary>
    public static void AssertDragState(PlayableElement element, bool expectedDragging, string message = "") {
      Assert.AreEqual(expectedDragging, element.IsBeingDragged, 
        $"Element drag state mismatch. {message}");
    }

    /// <summary>
    /// Verifies that an element's position matches expected position
    /// </summary>
    public static void AssertPosition(PlayableElement element, Vector3 expectedPosition, float tolerance = 0.01f, string message = "") {
      Assert.That(Vector3.Distance(element.GetPosition(), expectedPosition), Is.LessThan(tolerance),
        $"Element position mismatch. Expected: {expectedPosition}, Actual: {element.GetPosition()}. {message}");
    }

    /// <summary>
    /// Verifies that an element's rotation matches expected rotation
    /// </summary>
    public static void AssertRotation(PlayableElement element, Quaternion expectedRotation, float tolerance = 0.01f, string message = "") {
      Assert.That(Quaternion.Angle(element.GetRotation(), expectedRotation), Is.LessThan(tolerance),
        $"Element rotation mismatch. {message}");
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Destroys a GameObject and cleans up resources
    /// </summary>
    public static void DestroyGameObject(GameObject go) {
      if (go != null) {
        Object.DestroyImmediate(go);
      }
    }

    /// <summary>
    /// Destroys a component's GameObject
    /// </summary>
    public static void DestroyComponent(Component component) {
      if (component != null && component.gameObject != null) {
        Object.DestroyImmediate(component.gameObject);
      }
    }

    #endregion
  }

  #region Mock Classes

  /// <summary>
  /// Mock implementation of GameEventChannel for testing
  /// </summary>
  public class MockGameEventChannel : GameEventChannel {
    
    public List<GameEventRecord> RaisedEvents { get; private set; } = new List<GameEventRecord>();

    public new void Raise<T>(GameEventType eventType, T args = default) {
      RaisedEvents.Add(new GameEventRecord {
        EventType = eventType,
        Args = args,
        Timestamp = Time.time
      });

      // Call base implementation to invoke listeners
      base.Raise(eventType, args);
    }

    public new void Raise(GameEventType eventType) {
      RaisedEvents.Add(new GameEventRecord {
        EventType = eventType,
        Args = null,
        Timestamp = Time.time
      });

      // Call base implementation to invoke listeners
      base.Raise(eventType);
    }

    /// <summary>
    /// Clears all recorded events
    /// </summary>
    public void ClearEvents() {
      RaisedEvents.Clear();
    }

    /// <summary>
    /// Gets the count of events of a specific type
    /// </summary>
    public int GetEventCount(GameEventType eventType) {
      return RaisedEvents.FindAll(e => e.EventType == eventType).Count;
    }

    /// <summary>
    /// Gets the most recent event of a specific type
    /// </summary>
    public GameEventRecord GetLastEvent(GameEventType eventType) {
      for (int i = RaisedEvents.Count - 1; i >= 0; i--) {
        if (RaisedEvents[i].EventType == eventType) {
          return RaisedEvents[i];
        }
      }
      return null;
    }

    /// <summary>
    /// Checks if an event of a specific type was raised
    /// </summary>
    public bool WasEventRaised(GameEventType eventType) {
      return RaisedEvents.Exists(e => e.EventType == eventType);
    }
  }

  /// <summary>
  /// Record of a raised game event for testing
  /// </summary>
  public class GameEventRecord {
    public GameEventType EventType { get; set; }
    public object Args { get; set; }
    public float Timestamp { get; set; }

    public T GetArgs<T>() where T : class {
      return Args as T;
    }
  }

  #endregion

  #region Helper Classes for Logging

  /// <summary>
  /// Simple logger helper for tests
  /// </summary>
  public static class LogHelper {
    public static bool EnableLogging { get; set; } = false;

    public static void Log(string message) {
      if (EnableLogging) {
        Debug.Log($"[TEST] {message}");
      }
    }

    public static void LogWarning(string message) {
      if (EnableLogging) {
        Debug.LogWarning($"[TEST] {message}");
      }
    }

    public static void LogError(string message) {
      Debug.LogError($"[TEST] {message}");
    }
  }

  #endregion
}
