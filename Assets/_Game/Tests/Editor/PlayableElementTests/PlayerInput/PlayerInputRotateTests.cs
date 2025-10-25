using NUnit.Framework;
using UnityEngine;
using GMTK;
using static GMTK.Tests.PlayableElementTestHelpers;

namespace GMTK.Tests.PlayerInput {
  /// <summary>
  /// Functional tests for player input rotation on PlayableElements.
  /// Tests rotation behavior with all combinations of flags and constraints.
  /// </summary>
  [TestFixture]
  public class PlayerInputRotateTests {

    private MockGameEventChannel _mockEventChannel;
    private PlayableElement _testElement;

    [SetUp]
    public void SetUp() {
      SetupMockServices();
      _mockEventChannel = ServiceLocator.Get<GameEventChannel>() as MockGameEventChannel;
      _testElement = CreateTestPlayableElement("RotateTestElement");
      _testElement.Initialize();
    }

    [TearDown]
    public void TearDown() {
      DestroyComponent(_testElement);
      CleanupMockServices();
    }

    #region Rotate Clockwise Tests

    [Test]
    public void PlayerInputRotateCW_CanRotate_Success() {
      // Arrange
      _testElement.CanRotate = true;
      Quaternion initialRotation = _testElement.GetRotation();

      // Act
      _testElement.RotateClockwise();

      // Assert
      Quaternion currentRotation = _testElement.GetRotation();
      Assert.AreNotEqual(initialRotation, currentRotation, 
        "Rotation should change after RotateClockwise");
      Assert.IsTrue(_mockEventChannel.WasEventRaised(GameEventType.PlayableElementEvent),
        "PlayableElementEvent should be raised");
    }

    [Test]
    public void PlayerInputRotateCW_CannotRotate_Fail() {
      // Arrange
      _testElement.CanRotate = false;
      Quaternion initialRotation = _testElement.GetRotation();

      // Act
      _testElement.RotateClockwise();

      // Assert
      Quaternion currentRotation = _testElement.GetRotation();
      AssertRotation(_testElement, initialRotation, 0.01f, 
        "Rotation should not change when CanRotate is false");
    }

    [Test]
    public void PlayerInputRotateCW_MultipleRotations_CorrectAngle() {
      // Arrange
      _testElement.CanRotate = true;
      int rotations = 4; // Should complete a full 360° turn

      // Act
      for (int i = 0; i < rotations; i++) {
        _testElement.RotateClockwise();
      }

      // Assert
      // After 4 90-degree clockwise rotations, should be back to original orientation
      float angle = _testElement.GetRotation().eulerAngles.z;
      Assert.That(angle, Is.EqualTo(0).Within(1f), 
        "After 4 90° rotations, element should be at 0° (or 360°)");
    }

    [Test]
    public void PlayerInputRotateCW_ElementSelected_CanRotate() {
      // Arrange
      _testElement.CanRotate = true;
      _testElement.SelectionTriggers = SelectionTrigger.OnClick;
      SimulateSelection(_testElement);
      Quaternion initialRotation = _testElement.GetRotation();

      // Act
      _testElement.RotateClockwise();

      // Assert
      Assert.AreNotEqual(initialRotation, _testElement.GetRotation(), 
        "Selected element should be rotatable");
      AssertSelectionState(_testElement, true, "Element should remain selected");
    }

    [Test]
    public void PlayerInputRotateCW_ElementNotSelected_CanRotate() {
      // Arrange
      _testElement.CanRotate = true;
      AssertSelectionState(_testElement, false, "Element should not be selected");
      Quaternion initialRotation = _testElement.GetRotation();

      // Act
      _testElement.RotateClockwise();

      // Assert
      Assert.AreNotEqual(initialRotation, _testElement.GetRotation(), 
        "Element should be rotatable even when not selected");
    }

    #endregion

    #region Rotate Counter-Clockwise Tests

    [Test]
    public void PlayerInputRotateCCW_CanRotate_Success() {
      // Arrange
      _testElement.CanRotate = true;
      Quaternion initialRotation = _testElement.GetRotation();

      // Act
      _testElement.RotateCounterClockwise();

      // Assert
      Quaternion currentRotation = _testElement.GetRotation();
      Assert.AreNotEqual(initialRotation, currentRotation, 
        "Rotation should change after RotateCounterClockwise");
      Assert.IsTrue(_mockEventChannel.WasEventRaised(GameEventType.PlayableElementEvent),
        "PlayableElementEvent should be raised");
    }

    [Test]
    public void PlayerInputRotateCCW_CannotRotate_Fail() {
      // Arrange
      _testElement.CanRotate = false;
      Quaternion initialRotation = _testElement.GetRotation();

      // Act
      _testElement.RotateCounterClockwise();

      // Assert
      AssertRotation(_testElement, initialRotation, 0.01f, 
        "Rotation should not change when CanRotate is false");
    }

    [Test]
    public void PlayerInputRotateCCW_MultipleRotations_CorrectAngle() {
      // Arrange
      _testElement.CanRotate = true;
      int rotations = 4; // Should complete a full 360° turn

      // Act
      for (int i = 0; i < rotations; i++) {
        _testElement.RotateCounterClockwise();
      }

      // Assert
      // After 4 90-degree counter-clockwise rotations, should be back to original orientation
      float angle = _testElement.GetRotation().eulerAngles.z;
      Assert.That(angle, Is.EqualTo(0).Within(1f), 
        "After 4 90° rotations, element should be at 0° (or 360°)");
    }

    #endregion

    #region Clockwise and Counter-Clockwise Combined

    [Test]
    public void PlayerInputRotate_CWThenCCW_BackToOriginal() {
      // Arrange
      _testElement.CanRotate = true;
      Quaternion initialRotation = _testElement.GetRotation();

      // Act
      _testElement.RotateClockwise();
      _testElement.RotateCounterClockwise();

      // Assert
      AssertRotation(_testElement, initialRotation, 0.5f, 
        "Element should return to original rotation after CW then CCW");
    }

    [Test]
    public void PlayerInputRotate_AlternatingCWAndCCW_CorrectRotation() {
      // Arrange
      _testElement.CanRotate = true;

      // Act - Alternating rotations (net effect: 2 CW)
      _testElement.RotateClockwise();    // +90°
      _testElement.RotateClockwise();    // +90° = 180°
      _testElement.RotateCounterClockwise(); // -90° = 90°
      _testElement.RotateClockwise();    // +90° = 180°

      // Assert
      float angle = _testElement.GetRotation().eulerAngles.z;
      // Should be at 180 degrees
      Assert.That(angle, Is.EqualTo(180).Within(1f), 
        "After net 2 CW rotations, element should be at 180°");
    }

    #endregion

    #region Rotation with Other States

    [Test]
    public void PlayerInputRotate_ElementDragging_CanRotate() {
      // Arrange
      _testElement.CanRotate = true;
      _testElement.Draggable = true;
      SimulateDragStart(_testElement);
      Quaternion initialRotation = _testElement.GetRotation();

      // Act
      _testElement.RotateClockwise();

      // Assert
      Assert.AreNotEqual(initialRotation, _testElement.GetRotation(), 
        "Element should be rotatable while dragging");
      AssertDragState(_testElement, true, "Element should still be dragging");
    }

    [Test]
    public void PlayerInputRotate_ElementHovered_CanRotate() {
      // Arrange
      _testElement.CanRotate = true;
      SimulatePointerHover(_testElement, Vector3.zero);
      Quaternion initialRotation = _testElement.GetRotation();

      // Act
      _testElement.RotateClockwise();

      // Assert
      Assert.AreNotEqual(initialRotation, _testElement.GetRotation(), 
        "Element should be rotatable while hovered");
      AssertHoverState(_testElement, true, "Element should still be hovered");
    }

    [Test]
    public void PlayerInputRotate_ElementDraggableButNotRotatable_CannotRotate() {
      // Arrange
      _testElement.Draggable = true;
      _testElement.CanRotate = false;
      Quaternion initialRotation = _testElement.GetRotation();

      // Act
      _testElement.RotateClockwise();

      // Assert
      AssertRotation(_testElement, initialRotation, 0.01f, 
        "Element should not rotate when CanRotate is false, even if draggable");
    }

    #endregion

    #region All Combinations Tests

    [Test]
    public void PlayerInputRotate_AllCanRotateFlagCombinations_CorrectBehavior(
      [Values(true, false)] bool canRotate,
      [Values(true, false)] bool draggable,
      [Values(true, false)] bool canSelect) {
      
      // Arrange
      _testElement.CanRotate = canRotate;
      _testElement.Draggable = draggable;
      _testElement.SelectionTriggers = canSelect ? SelectionTrigger.OnClick : SelectionTrigger.None;
      Quaternion initialRotation = _testElement.GetRotation();
      _mockEventChannel.ClearEvents();

      // Act
      _testElement.RotateClockwise();

      // Assert
      if (canRotate) {
        Assert.AreNotEqual(initialRotation, _testElement.GetRotation(), 
          $"Element should rotate: canRotate={canRotate}, draggable={draggable}, canSelect={canSelect}");
      } else {
        AssertRotation(_testElement, initialRotation, 0.01f, 
          $"Element should not rotate: canRotate={canRotate}, draggable={draggable}, canSelect={canSelect}");
      }
    }

    [Test]
    public void PlayerInputRotate_BothDirections_AllFlagCombinations(
      [Values(true, false)] bool canRotate) {
      
      // Arrange
      _testElement.CanRotate = canRotate;
      Quaternion initialRotation = _testElement.GetRotation();

      // Act
      _testElement.RotateClockwise();
      Quaternion afterCW = _testElement.GetRotation();
      _testElement.RotateCounterClockwise();
      Quaternion afterCCW = _testElement.GetRotation();

      // Assert
      if (canRotate) {
        Assert.AreNotEqual(initialRotation, afterCW, 
          "Element should rotate clockwise when CanRotate is true");
        AssertRotation(_testElement, initialRotation, 0.5f, 
          "Element should return to original rotation after CW then CCW");
      } else {
        AssertRotation(_testElement, initialRotation, 0.01f, 
          "Element should not rotate when CanRotate is false");
      }
    }

    [Test]
    public void PlayerInputRotate_WithAllStates_CorrectBehavior(
      [Values(true, false)] bool canRotate,
      [Values(true, false)] bool isSelected,
      [Values(true, false)] bool isHovered,
      [Values(true, false)] bool isDragging) {
      
      // Arrange
      _testElement.CanRotate = canRotate;
      _testElement.Draggable = true;
      _testElement.SelectionTriggers = SelectionTrigger.OnClick;
      
      if (isSelected) SimulateSelection(_testElement);
      if (isHovered) SimulatePointerHover(_testElement, Vector3.zero);
      if (isDragging) SimulateDragStart(_testElement);
      
      Quaternion initialRotation = _testElement.GetRotation();

      // Act
      _testElement.RotateClockwise();

      // Assert
      if (canRotate) {
        Assert.AreNotEqual(initialRotation, _testElement.GetRotation(), 
          $"Element should rotate regardless of other states when CanRotate=true");
      } else {
        AssertRotation(_testElement, initialRotation, 0.01f, 
          $"Element should not rotate when CanRotate=false");
      }
      
      // Verify other states preserved
      AssertSelectionState(_testElement, isSelected, "Selection state should be preserved");
      AssertHoverState(_testElement, isHovered, "Hover state should be preserved");
      AssertDragState(_testElement, isDragging, "Drag state should be preserved");
    }

    #endregion
  }
}
