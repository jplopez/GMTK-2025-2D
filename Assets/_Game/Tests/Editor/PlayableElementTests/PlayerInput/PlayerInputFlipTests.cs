using NUnit.Framework;
using UnityEngine;
using GMTK;
using static GMTK.Tests.PlayableElementTestHelpers;

namespace GMTK.Tests.PlayerInput {
  /// <summary>
  /// Functional tests for player input flip on PlayableElements.
  /// Tests flip behavior with all combinations of flags.
  /// </summary>
  [TestFixture]
  public class PlayerInputFlipTests {

    private MockGameEventChannel _mockEventChannel;
    private PlayableElement _testElement;

    [SetUp]
    public void SetUp() {
      SetupMockServices();
      _mockEventChannel = ServiceLocator.Get<GameEventChannel>() as MockGameEventChannel;
      _testElement = CreateTestPlayableElement("FlipTestElement");
      _testElement.Initialize();
    }

    [TearDown]
    public void TearDown() {
      DestroyComponent(_testElement);
      CleanupMockServices();
    }

    #region Flip X Tests

    [Test]
    public void PlayerInputFlipX_Flippable_Success() {
      // Arrange
      _testElement.Flippable = true;
      Vector3 initialScale = _testElement.SnapTransform.localScale;

      // Act
      _testElement.FlipX();

      // Assert
      Vector3 currentScale = _testElement.SnapTransform.localScale;
      Assert.AreNotEqual(initialScale.x, currentScale.x, 
        "X scale should change after FlipX");
      Assert.IsTrue(_mockEventChannel.WasEventRaised(GameEventType.PlayableElementEvent),
        "PlayableElementEvent should be raised");
    }

    [Test]
    public void PlayerInputFlipX_NotFlippable_Fail() {
      // Arrange
      _testElement.Flippable = false;
      Vector3 initialScale = _testElement.SnapTransform.localScale;

      // Act
      _testElement.FlipX();

      // Assert
      Vector3 currentScale = _testElement.SnapTransform.localScale;
      Assert.AreEqual(initialScale, currentScale, 
        "Scale should not change when Flippable is false");
    }

    [Test]
    public void PlayerInputFlipX_TwiceFlips_BackToOriginal() {
      // Arrange
      _testElement.Flippable = true;
      Vector3 initialScale = _testElement.SnapTransform.localScale;

      // Act
      _testElement.FlipX();
      _testElement.FlipX();

      // Assert
      Vector3 currentScale = _testElement.SnapTransform.localScale;
      Assert.AreEqual(initialScale.x, currentScale.x, 
        "X scale should return to original after flipping twice");
    }

    [Test]
    public void PlayerInputFlipX_ElementSelected_CanFlip() {
      // Arrange
      _testElement.Flippable = true;
      _testElement.SelectionTriggers = SelectionTrigger.OnClick;
      SimulateSelection(_testElement);
      Vector3 initialScale = _testElement.SnapTransform.localScale;

      // Act
      _testElement.FlipX();

      // Assert
      Assert.AreNotEqual(initialScale.x, _testElement.SnapTransform.localScale.x, 
        "Selected element should be flippable");
      AssertSelectionState(_testElement, true, "Element should remain selected");
    }

    [Test]
    public void PlayerInputFlipX_ElementNotSelected_CanFlip() {
      // Arrange
      _testElement.Flippable = true;
      AssertSelectionState(_testElement, false, "Element should not be selected");
      Vector3 initialScale = _testElement.SnapTransform.localScale;

      // Act
      _testElement.FlipX();

      // Assert
      Assert.AreNotEqual(initialScale.x, _testElement.SnapTransform.localScale.x, 
        "Element should be flippable even when not selected");
    }

    #endregion

    #region Flip Y Tests

    [Test]
    public void PlayerInputFlipY_Flippable_Success() {
      // Arrange
      _testElement.Flippable = true;
      Vector3 initialScale = _testElement.SnapTransform.localScale;

      // Act
      _testElement.FlipY();

      // Assert
      Vector3 currentScale = _testElement.SnapTransform.localScale;
      Assert.AreNotEqual(initialScale.y, currentScale.y, 
        "Y scale should change after FlipY");
      Assert.IsTrue(_mockEventChannel.WasEventRaised(GameEventType.PlayableElementEvent),
        "PlayableElementEvent should be raised");
    }

    [Test]
    public void PlayerInputFlipY_NotFlippable_Fail() {
      // Arrange
      _testElement.Flippable = false;
      Vector3 initialScale = _testElement.SnapTransform.localScale;

      // Act
      _testElement.FlipY();

      // Assert
      Vector3 currentScale = _testElement.SnapTransform.localScale;
      Assert.AreEqual(initialScale, currentScale, 
        "Scale should not change when Flippable is false");
    }

    [Test]
    public void PlayerInputFlipY_TwiceFlips_BackToOriginal() {
      // Arrange
      _testElement.Flippable = true;
      Vector3 initialScale = _testElement.SnapTransform.localScale;

      // Act
      _testElement.FlipY();
      _testElement.FlipY();

      // Assert
      Vector3 currentScale = _testElement.SnapTransform.localScale;
      Assert.AreEqual(initialScale.y, currentScale.y, 
        "Y scale should return to original after flipping twice");
    }

    #endregion

    #region Flip X and Y Combined

    [Test]
    public void PlayerInputFlip_XAndY_BothFlipped() {
      // Arrange
      _testElement.Flippable = true;
      Vector3 initialScale = _testElement.SnapTransform.localScale;

      // Act
      _testElement.FlipX();
      _testElement.FlipY();

      // Assert
      Vector3 currentScale = _testElement.SnapTransform.localScale;
      Assert.AreNotEqual(initialScale.x, currentScale.x, "X should be flipped");
      Assert.AreNotEqual(initialScale.y, currentScale.y, "Y should be flipped");
    }

    [Test]
    public void PlayerInputFlip_XAndYTwice_BackToOriginal() {
      // Arrange
      _testElement.Flippable = true;
      Vector3 initialScale = _testElement.SnapTransform.localScale;

      // Act
      _testElement.FlipX();
      _testElement.FlipY();
      _testElement.FlipX();
      _testElement.FlipY();

      // Assert
      Vector3 currentScale = _testElement.SnapTransform.localScale;
      Assert.AreEqual(initialScale, currentScale, 
        "Scale should return to original after flipping X and Y twice");
    }

    [Test]
    public void PlayerInputFlip_MultipleXFlips_AlternatesCorrectly() {
      // Arrange
      _testElement.Flippable = true;
      Vector3 initialScale = _testElement.SnapTransform.localScale;

      // Act & Assert - Multiple flips should alternate
      _testElement.FlipX();
      float firstFlipX = _testElement.SnapTransform.localScale.x;
      Assert.AreNotEqual(initialScale.x, firstFlipX, "First flip should change X");

      _testElement.FlipX();
      Assert.AreEqual(initialScale.x, _testElement.SnapTransform.localScale.x, 
        "Second flip should restore X");

      _testElement.FlipX();
      Assert.AreEqual(firstFlipX, _testElement.SnapTransform.localScale.x, 
        "Third flip should match first flip");
    }

    #endregion

    #region Flip with Other States

    [Test]
    public void PlayerInputFlip_ElementDragging_CanFlip() {
      // Arrange
      _testElement.Flippable = true;
      _testElement.Draggable = true;
      SimulateDragStart(_testElement);
      Vector3 initialScale = _testElement.SnapTransform.localScale;

      // Act
      _testElement.FlipX();

      // Assert
      Assert.AreNotEqual(initialScale.x, _testElement.SnapTransform.localScale.x, 
        "Element should be flippable while dragging");
      AssertDragState(_testElement, true, "Element should still be dragging");
    }

    [Test]
    public void PlayerInputFlip_ElementHovered_CanFlip() {
      // Arrange
      _testElement.Flippable = true;
      SimulatePointerHover(_testElement, Vector3.zero);
      Vector3 initialScale = _testElement.SnapTransform.localScale;

      // Act
      _testElement.FlipX();

      // Assert
      Assert.AreNotEqual(initialScale.x, _testElement.SnapTransform.localScale.x, 
        "Element should be flippable while hovered");
      AssertHoverState(_testElement, true, "Element should still be hovered");
    }

    [Test]
    public void PlayerInputFlip_ElementDraggableButNotFlippable_CannotFlip() {
      // Arrange
      _testElement.Draggable = true;
      _testElement.Flippable = false;
      Vector3 initialScale = _testElement.SnapTransform.localScale;

      // Act
      _testElement.FlipX();

      // Assert
      Assert.AreEqual(initialScale, _testElement.SnapTransform.localScale, 
        "Element should not flip when Flippable is false, even if draggable");
    }

    [Test]
    public void PlayerInputFlip_ElementRotatable_BothWork() {
      // Arrange
      _testElement.Flippable = true;
      _testElement.CanRotate = true;
      Vector3 initialScale = _testElement.SnapTransform.localScale;
      Quaternion initialRotation = _testElement.GetRotation();

      // Act
      _testElement.FlipX();
      _testElement.RotateClockwise();

      // Assert
      Assert.AreNotEqual(initialScale.x, _testElement.SnapTransform.localScale.x, 
        "Element should flip");
      Assert.AreNotEqual(initialRotation, _testElement.GetRotation(), 
        "Element should rotate");
    }

    #endregion

    #region All Combinations Tests

    [Test]
    public void PlayerInputFlip_AllFlippableFlagCombinations_CorrectBehavior(
      [Values(true, false)] bool flippable,
      [Values(true, false)] bool draggable,
      [Values(true, false)] bool canSelect) {
      
      // Arrange
      _testElement.Flippable = flippable;
      _testElement.Draggable = draggable;
      _testElement.SelectionTriggers = canSelect ? SelectionTrigger.OnClick : SelectionTrigger.None;
      Vector3 initialScale = _testElement.SnapTransform.localScale;
      _mockEventChannel.ClearEvents();

      // Act
      _testElement.FlipX();

      // Assert
      if (flippable) {
        Assert.AreNotEqual(initialScale.x, _testElement.SnapTransform.localScale.x, 
          $"Element should flip: flippable={flippable}, draggable={draggable}, canSelect={canSelect}");
      } else {
        Assert.AreEqual(initialScale, _testElement.SnapTransform.localScale, 
          $"Element should not flip: flippable={flippable}, draggable={draggable}, canSelect={canSelect}");
      }
    }

    [Test]
    public void PlayerInputFlip_BothAxes_AllFlagCombinations(
      [Values(true, false)] bool flippable) {
      
      // Arrange
      _testElement.Flippable = flippable;
      Vector3 initialScale = _testElement.SnapTransform.localScale;

      // Act
      _testElement.FlipX();
      Vector3 afterFlipX = _testElement.SnapTransform.localScale;
      _testElement.FlipY();
      Vector3 afterFlipY = _testElement.SnapTransform.localScale;

      // Assert
      if (flippable) {
        Assert.AreNotEqual(initialScale.x, afterFlipX.x, 
          "Element should flip X when Flippable is true");
        Assert.AreNotEqual(afterFlipX.y, afterFlipY.y, 
          "Element should flip Y when Flippable is true");
      } else {
        Assert.AreEqual(initialScale, afterFlipX, 
          "Element should not flip X when Flippable is false");
        Assert.AreEqual(afterFlipX, afterFlipY, 
          "Element should not flip Y when Flippable is false");
      }
    }

    [Test]
    public void PlayerInputFlip_WithAllStates_CorrectBehavior(
      [Values(true, false)] bool flippable,
      [Values(true, false)] bool isSelected,
      [Values(true, false)] bool isHovered,
      [Values(true, false)] bool isDragging) {
      
      // Arrange
      _testElement.Flippable = flippable;
      _testElement.Draggable = true;
      _testElement.SelectionTriggers = SelectionTrigger.OnClick;
      
      if (isSelected) SimulateSelection(_testElement);
      if (isHovered) SimulatePointerHover(_testElement, Vector3.zero);
      if (isDragging) SimulateDragStart(_testElement);
      
      Vector3 initialScale = _testElement.SnapTransform.localScale;

      // Act
      _testElement.FlipX();

      // Assert
      if (flippable) {
        Assert.AreNotEqual(initialScale.x, _testElement.SnapTransform.localScale.x, 
          $"Element should flip regardless of other states when Flippable=true");
      } else {
        Assert.AreEqual(initialScale, _testElement.SnapTransform.localScale, 
          $"Element should not flip when Flippable=false");
      }
      
      // Verify other states preserved
      AssertSelectionState(_testElement, isSelected, "Selection state should be preserved");
      AssertHoverState(_testElement, isHovered, "Hover state should be preserved");
      AssertDragState(_testElement, isDragging, "Drag state should be preserved");
    }

    [Test]
    public void PlayerInputFlip_AllTransformationFlags_CorrectBehavior(
      [Values(true, false)] bool flippable,
      [Values(true, false)] bool canRotate,
      [Values(true, false)] bool draggable) {
      
      // Arrange
      _testElement.Flippable = flippable;
      _testElement.CanRotate = canRotate;
      _testElement.Draggable = draggable;
      Vector3 initialScale = _testElement.SnapTransform.localScale;
      Quaternion initialRotation = _testElement.GetRotation();
      Vector3 initialPosition = _testElement.GetPosition();

      // Act
      _testElement.FlipX();
      _testElement.RotateClockwise();
      if (draggable) {
        Vector3 newPos = initialPosition + new Vector3(5, 5, 0);
        SimulateDragStart(_testElement);
        SimulateDragUpdate(_testElement, newPos);
        SimulateDragEnd(_testElement);
      }

      // Assert - Each transformation should work independently based on its flag
      if (flippable) {
        Assert.AreNotEqual(initialScale.x, _testElement.SnapTransform.localScale.x, 
          "Flip should work when Flippable=true");
      } else {
        Assert.AreEqual(initialScale, _testElement.SnapTransform.localScale, 
          "Flip should not work when Flippable=false");
      }

      if (canRotate) {
        Assert.AreNotEqual(initialRotation, _testElement.GetRotation(), 
          "Rotate should work when CanRotate=true");
      } else {
        AssertRotation(_testElement, initialRotation, 0.01f, 
          "Rotate should not work when CanRotate=false");
      }

      if (draggable) {
        Assert.AreNotEqual(initialPosition, _testElement.GetPosition(), 
          "Drag should work when Draggable=true");
      }
    }

    #endregion
  }
}
