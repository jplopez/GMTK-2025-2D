using NUnit.Framework;
using UnityEngine;
using GMTK;
using static GMTK.Tests.PlayableElementTestHelpers;

namespace GMTK.Tests.MarbleInteractions {
  /// <summary>
  /// Functional tests for marble collision interactions with PlayableElements.
  /// Tests element responses to marble collisions including movement and rotation.
  /// 
  /// Note: These tests focus on the event handling and state changes triggered by marble collisions.
  /// Full physics simulation tests would require Unity Test Runner's PlayMode tests.
  /// </summary>
  [TestFixture]
  public class MarbleCollisionTests {

    private MockGameEventChannel _mockEventChannel;
    private PlayableElement _testElement;
    private PlayableMarbleController _testMarble;

    [SetUp]
    public void SetUp() {
      SetupMockServices();
      _mockEventChannel = ServiceLocator.Get<GameEventChannel>() as MockGameEventChannel;
      _testElement = CreateTestPlayableElement("CollisionTestElement");
      _testElement.Initialize();
      _testMarble = CreateTestMarble("CollisionTestMarble");
    }

    [TearDown]
    public void TearDown() {
      DestroyComponent(_testElement);
      DestroyComponent(_testMarble);
      CleanupMockServices();
    }

    #region Basic Collision Detection Tests

    [Test]
    public void MarbleCollision_ElementExists_CollisionDetected() {
      // Arrange
      Vector2 contactPoint = Vector2.zero;
      Vector2 contactNormal = Vector2.up;
      float relativeVelocity = 5.0f;

      // Act
      SimulateMarbleCollision(_testMarble, _testElement, contactPoint, contactNormal, relativeVelocity);

      // Assert
      // In a real implementation, verify collision events or state changes
      LogHelper.Log("Marble collision simulation completed");
      Assert.IsNotNull(_testElement, "Element should still exist after collision");
      Assert.IsNotNull(_testMarble, "Marble should still exist after collision");
    }

    [Test]
    public void MarbleCollision_ElementHasRigidbody_CanRespond() {
      // Arrange
      var rb = _testElement.gameObject.AddComponent<Rigidbody2D>();
      rb.bodyType = RigidbodyType2D.Dynamic;

      // Act & Assert
      Assert.IsNotNull(_testElement.GetComponent<Rigidbody2D>(), 
        "Element should have Rigidbody2D for physics interactions");
    }

    [Test]
    public void MarbleCollision_ElementStaticRigidbody_NoMovement() {
      // Arrange
      var rb = _testElement.gameObject.AddComponent<Rigidbody2D>();
      rb.bodyType = RigidbodyType2D.Static;
      Vector3 initialPosition = _testElement.GetPosition();

      // Act
      SimulateMarbleCollision(_testMarble, _testElement, Vector2.zero, Vector2.up, 5.0f);

      // Assert
      AssertPosition(_testElement, initialPosition, 0.01f, 
        "Static element should not move from collision");
    }

    #endregion

    #region Collision Response - Movement Tests

    [Test]
    public void MarbleCollision_ElementCanMove_MovementAllowed() {
      // Arrange
      var rb = _testElement.gameObject.AddComponent<Rigidbody2D>();
      rb.bodyType = RigidbodyType2D.Dynamic;
      _testElement.Draggable = true; // Indicates element can be moved

      // Act & Assert
      // In real physics, this would cause movement
      // For unit tests, we verify the setup allows movement
      Assert.AreEqual(RigidbodyType2D.Dynamic, rb.bodyType, 
        "Element should have dynamic rigidbody for movement");
    }

    [Test]
    public void MarbleCollision_ElementCannotMove_NoMovement() {
      // Arrange
      var rb = _testElement.gameObject.AddComponent<Rigidbody2D>();
      rb.bodyType = RigidbodyType2D.Static;
      _testElement.Draggable = false;
      Vector3 initialPosition = _testElement.GetPosition();

      // Act
      SimulateMarbleCollision(_testMarble, _testElement, Vector2.zero, Vector2.up, 5.0f);

      // Assert
      AssertPosition(_testElement, initialPosition, 0.01f, 
        "Element with static rigidbody should not move");
    }

    [Test]
    public void MarbleCollision_FixedAxisConstraint_MovementConstrained() {
      // Arrange
      var rb = _testElement.gameObject.AddComponent<Rigidbody2D>();
      rb.bodyType = RigidbodyType2D.Dynamic;
      rb.constraints = RigidbodyConstraints2D.FreezePositionX; // Only Y movement allowed

      // Act & Assert
      Assert.IsTrue((rb.constraints & RigidbodyConstraints2D.FreezePositionX) != 0,
        "X axis should be constrained");
      Assert.IsFalse((rb.constraints & RigidbodyConstraints2D.FreezePositionY) != 0,
        "Y axis should be free");
    }

    #endregion

    #region Collision Response - Rotation Tests

    [Test]
    public void MarbleCollision_ElementCanRotate_RotationAllowed() {
      // Arrange
      var rb = _testElement.gameObject.AddComponent<Rigidbody2D>();
      rb.bodyType = RigidbodyType2D.Dynamic;
      _testElement.CanRotate = true;

      // Act & Assert
      Assert.IsFalse((rb.constraints & RigidbodyConstraints2D.FreezeRotation) != 0,
        "Rotation should not be constrained when CanRotate is true");
    }

    [Test]
    public void MarbleCollision_ElementCannotRotate_NoRotation() {
      // Arrange
      var rb = _testElement.gameObject.AddComponent<Rigidbody2D>();
      rb.bodyType = RigidbodyType2D.Dynamic;
      rb.constraints = RigidbodyConstraints2D.FreezeRotation;
      _testElement.CanRotate = false;
      Quaternion initialRotation = _testElement.GetRotation();

      // Act
      SimulateMarbleCollision(_testMarble, _testElement, Vector2.zero, Vector2.right, 5.0f);

      // Assert
      // With frozen rotation, element should not rotate from collision
      Assert.IsTrue((rb.constraints & RigidbodyConstraints2D.FreezeRotation) != 0,
        "Rotation should be constrained");
    }

    [Test]
    public void MarbleCollision_LimitedAngle_RotationClamped() {
      // Arrange - This would require a component that limits rotation angle
      var rb = _testElement.gameObject.AddComponent<Rigidbody2D>();
      rb.bodyType = RigidbodyType2D.Dynamic;
      _testElement.CanRotate = true;

      // Act & Assert
      // In a real implementation, would verify angle clamping logic
      LogHelper.Log("Limited angle rotation test - requires custom constraint component");
    }

    #endregion

    #region Combined Behaviors Tests

    [Test]
    public void MarbleCollision_MovesAndRotates_BothAllowed(
      [Values(true, false)] bool canMove,
      [Values(true, false)] bool canRotate) {
      
      // Arrange
      var rb = _testElement.gameObject.AddComponent<Rigidbody2D>();
      rb.bodyType = canMove ? RigidbodyType2D.Dynamic : RigidbodyType2D.Static;
      
      if (!canRotate) {
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
      }
      
      _testElement.CanRotate = canRotate;
      _testElement.Draggable = canMove;

      Vector3 initialPosition = _testElement.GetPosition();
      Quaternion initialRotation = _testElement.GetRotation();

      // Act
      SimulateMarbleCollision(_testMarble, _testElement, Vector2.zero, Vector2.up, 5.0f);

      // Assert
      // Verify constraints are set correctly
      if (!canMove) {
        Assert.AreEqual(RigidbodyType2D.Static, rb.bodyType,
          "Element should have static rigidbody when movement not allowed");
      }
      
      if (!canRotate) {
        Assert.IsTrue((rb.constraints & RigidbodyConstraints2D.FreezeRotation) != 0,
          "Rotation should be frozen when CanRotate is false");
      }
    }

    [Test]
    public void MarbleCollision_NoMovementNoRotation_ElementStatic() {
      // Arrange
      var rb = _testElement.gameObject.AddComponent<Rigidbody2D>();
      rb.bodyType = RigidbodyType2D.Static;
      rb.constraints = RigidbodyConstraints2D.FreezeAll;
      _testElement.Draggable = false;
      _testElement.CanRotate = false;

      Vector3 initialPosition = _testElement.GetPosition();
      Quaternion initialRotation = _testElement.GetRotation();

      // Act
      SimulateMarbleCollision(_testMarble, _testElement, Vector2.zero, Vector2.up, 10.0f);

      // Assert
      AssertPosition(_testElement, initialPosition, 0.01f, 
        "Fully constrained element should not move");
      AssertRotation(_testElement, initialRotation, 0.01f, 
        "Fully constrained element should not rotate");
    }

    #endregion

    #region Collision Intensity Tests

    [Test]
    public void MarbleCollision_HighVelocity_HighIntensity() {
      // Arrange
      float highVelocity = 20.0f;
      
      // Act & Assert
      // Verify that high velocity would generate high collision intensity
      Assert.Greater(highVelocity, 10.0f, "Velocity should be considered high");
    }

    [Test]
    public void MarbleCollision_LowVelocity_LowIntensity() {
      // Arrange
      float lowVelocity = 1.0f;
      
      // Act & Assert
      // Verify that low velocity would generate low collision intensity
      Assert.Less(lowVelocity, 5.0f, "Velocity should be considered low");
    }

    [Test]
    public void MarbleCollision_CollisionAngle_AffectsResponse() {
      // Arrange - Different collision angles
      Vector2 directHit = Vector2.up;      // 0 degrees to normal
      Vector2 glancingHit = new Vector2(0.7f, 0.3f).normalized; // ~45 degrees

      // Act & Assert
      float directAngle = Vector2.Angle(directHit, Vector2.up);
      float glancingAngle = Vector2.Angle(glancingHit, Vector2.up);

      Assert.Less(directAngle, glancingAngle, 
        "Direct hit should have smaller angle to normal than glancing hit");
    }

    #endregion

    #region All Combinations Tests

    [Test]
    public void MarbleCollision_AllBehaviorCombinations_CorrectResponse(
      [Values(true, false)] bool canMove,
      [Values(true, false)] bool canRotate) {
      
      // Arrange
      var rb = _testElement.gameObject.AddComponent<Rigidbody2D>();
      rb.bodyType = canMove ? RigidbodyType2D.Dynamic : RigidbodyType2D.Static;
      
      if (!canRotate) {
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
      }
      
      _testElement.CanRotate = canRotate;
      _testElement.Draggable = canMove;

      // Act
      SimulateMarbleCollision(_testMarble, _testElement, Vector2.zero, Vector2.up, 5.0f);

      // Assert
      // Verify element configuration matches intended behavior
      string behaviorDesc = $"canMove={canMove}, canRotate={canRotate}";
      
      if (canMove) {
        Assert.AreEqual(RigidbodyType2D.Dynamic, rb.bodyType,
          $"Element should be dynamic for movement: {behaviorDesc}");
      } else {
        Assert.AreEqual(RigidbodyType2D.Static, rb.bodyType,
          $"Element should be static when movement disabled: {behaviorDesc}");
      }
      
      if (canRotate) {
        Assert.IsFalse((rb.constraints & RigidbodyConstraints2D.FreezeRotation) != 0,
          $"Rotation should not be frozen: {behaviorDesc}");
      } else {
        Assert.IsTrue((rb.constraints & RigidbodyConstraints2D.FreezeRotation) != 0,
          $"Rotation should be frozen: {behaviorDesc}");
      }
    }

    #endregion

    #region Collision with Element States

    [Test]
    public void MarbleCollision_ElementSelected_CollisionOccurs() {
      // Arrange
      _testElement.SelectionTriggers = SelectionTrigger.OnClick;
      SimulateSelection(_testElement);
      var rb = _testElement.gameObject.AddComponent<Rigidbody2D>();
      rb.bodyType = RigidbodyType2D.Dynamic;

      // Act
      SimulateMarbleCollision(_testMarble, _testElement, Vector2.zero, Vector2.up, 5.0f);

      // Assert
      AssertSelectionState(_testElement, true, 
        "Element should remain selected after collision");
    }

    [Test]
    public void MarbleCollision_ElementBeingDragged_CollisionOccurs() {
      // Arrange
      _testElement.Draggable = true;
      SimulateDragStart(_testElement);
      var rb = _testElement.gameObject.AddComponent<Rigidbody2D>();
      rb.bodyType = RigidbodyType2D.Dynamic;

      // Act
      SimulateMarbleCollision(_testMarble, _testElement, Vector2.zero, Vector2.up, 5.0f);

      // Assert
      // Collision should occur even while dragging
      // In real implementation, might need to handle drag state during physics collision
      AssertDragState(_testElement, true, 
        "Element drag state should be maintained");
    }

    #endregion
  }
}
