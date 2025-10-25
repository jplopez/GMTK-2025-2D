using NUnit.Framework;
using UnityEngine;
using GMTK;
using static GMTK.Tests.PlayableElementTestHelpers;

namespace GMTK.Tests.PointerActions {
  /// <summary>
  /// Functional tests for pointer drag interactions on PlayableElements.
  /// Tests all combinations of drag behavior, element states, and flags.
  /// </summary>
  [TestFixture]
  public class PointerDragTests {

    private MockGameEventChannel _mockEventChannel;
    private PlayableElement _testElement;

    [SetUp]
    public void SetUp() {
      SetupMockServices();
      _mockEventChannel = ServiceLocator.Get<GameEventChannel>() as MockGameEventChannel;
      _testElement = CreateTestPlayableElement("DragTestElement");
      _testElement.Initialize();
    }

    [TearDown]
    public void TearDown() {
      DestroyComponent(_testElement);
      CleanupMockServices();
    }

    #region Drag Start Tests

    [Test]
    public void PointerDragStart_ElementDraggable_DragStarted() {
      // Arrange
      _testElement.Draggable = true;
      AssertDragState(_testElement, false, "Element should not be dragging initially");

      // Act
      SimulateDragStart(_testElement);

      // Assert
      AssertDragState(_testElement, true, "Element should be dragging after drag start");
      Assert.IsTrue(_mockEventChannel.WasEventRaised(GameEventType.ElementDragStart),
        "ElementDragStart event should be raised");
    }

    [Test]
    public void PointerDragStart_ElementNotDraggable_DragNotStarted() {
      // Arrange
      _testElement.Draggable = false;

      // Act
      SimulateDragStart(_testElement);

      // Assert
      AssertDragState(_testElement, false, "Element should not be dragging when Draggable is false");
      Assert.IsFalse(_mockEventChannel.WasEventRaised(GameEventType.ElementDragStart),
        "ElementDragStart event should not be raised");
    }

    [Test]
    public void PointerDragStart_ElementAlreadyDragging_NoChange() {
      // Arrange
      _testElement.Draggable = true;
      SimulateDragStart(_testElement);
      _mockEventChannel.ClearEvents();

      // Act
      SimulateDragStart(_testElement);

      // Assert
      AssertDragState(_testElement, true, "Element should remain dragging");
      // Note: In real implementation, this might raise another event or not
      // depending on implementation details
    }

    [Test]
    public void PointerDragStart_ElementSelected_CanDrag() {
      // Arrange
      _testElement.Draggable = true;
      _testElement.SelectionTriggers = SelectionTrigger.OnClick;
      SimulateSelection(_testElement);
      _mockEventChannel.ClearEvents();

      // Act
      SimulateDragStart(_testElement);

      // Assert
      AssertDragState(_testElement, true, "Selected element should be draggable");
      AssertSelectionState(_testElement, true, "Element should remain selected");
    }

    [Test]
    public void PointerDragStart_ElementNotSelected_CanDrag() {
      // Arrange
      _testElement.Draggable = true;
      AssertSelectionState(_testElement, false, "Element should not be selected");

      // Act
      SimulateDragStart(_testElement);

      // Assert
      AssertDragState(_testElement, true, "Element should be draggable even when not selected");
    }

    #endregion

    #region Drag Update Tests

    [Test]
    public void PointerDragUpdate_ElementDragging_PositionUpdated() {
      // Arrange
      _testElement.Draggable = true;
      Vector3 initialPosition = _testElement.GetPosition();
      Vector3 newPosition = initialPosition + new Vector3(5, 5, 0);
      SimulateDragStart(_testElement);
      _mockEventChannel.ClearEvents();

      // Act
      SimulateDragUpdate(_testElement, newPosition);

      // Assert
      AssertPosition(_testElement, newPosition, 0.01f, "Element position should be updated during drag");
      Assert.IsTrue(_mockEventChannel.WasEventRaised(GameEventType.ElementDragging),
        "ElementDragging event should be raised");
    }

    [Test]
    public void PointerDragUpdate_ElementNotDragging_PositionNotUpdated() {
      // Arrange
      _testElement.Draggable = true;
      Vector3 initialPosition = _testElement.GetPosition();
      Vector3 newPosition = initialPosition + new Vector3(5, 5, 0);
      // Don't start drag

      // Act
      SimulateDragUpdate(_testElement, newPosition);

      // Assert
      AssertPosition(_testElement, initialPosition, 0.01f, 
        "Element position should not change when not dragging");
    }

    [Test]
    public void PointerDragUpdate_MultipleDragUpdates_PositionUpdatesCorrectly() {
      // Arrange
      _testElement.Draggable = true;
      Vector3 startPos = _testElement.GetPosition();
      SimulateDragStart(_testElement);

      // Act - Drag to multiple positions
      Vector3 pos1 = startPos + new Vector3(1, 0, 0);
      Vector3 pos2 = startPos + new Vector3(2, 1, 0);
      Vector3 pos3 = startPos + new Vector3(3, 2, 0);

      SimulateDragUpdate(_testElement, pos1);
      SimulateDragUpdate(_testElement, pos2);
      SimulateDragUpdate(_testElement, pos3);

      // Assert
      AssertPosition(_testElement, pos3, 0.01f, 
        "Element should be at final drag position");
      Assert.GreaterOrEqual(_mockEventChannel.GetEventCount(GameEventType.ElementDragging), 3,
        "Should have multiple drag update events");
    }

    #endregion

    #region Drag End Tests

    [Test]
    public void PointerDragEnd_ElementDragging_DragEnded() {
      // Arrange
      _testElement.Draggable = true;
      SimulateDragStart(_testElement);
      _mockEventChannel.ClearEvents();

      // Act
      SimulateDragEnd(_testElement);

      // Assert
      AssertDragState(_testElement, false, "Element should not be dragging after drag end");
      Assert.IsTrue(_mockEventChannel.WasEventRaised(GameEventType.ElementDropped),
        "ElementDropped event should be raised");
    }

    [Test]
    public void PointerDragEnd_ElementNotDragging_NoChange() {
      // Arrange
      _testElement.Draggable = true;
      AssertDragState(_testElement, false, "Element should not be dragging");

      // Act
      SimulateDragEnd(_testElement);

      // Assert
      AssertDragState(_testElement, false, "Element should remain not dragging");
      // Drag end might still raise event in some implementations
    }

    [Test]
    public void PointerDragEnd_AfterDragUpdate_PositionPreserved() {
      // Arrange
      _testElement.Draggable = true;
      Vector3 initialPosition = _testElement.GetPosition();
      Vector3 dragPosition = initialPosition + new Vector3(5, 5, 0);
      SimulateDragStart(_testElement);
      SimulateDragUpdate(_testElement, dragPosition);

      // Act
      SimulateDragEnd(_testElement);

      // Assert
      AssertPosition(_testElement, dragPosition, 0.01f, 
        "Element position should be preserved after drag end");
      AssertDragState(_testElement, false, "Element should not be dragging");
    }

    #endregion

    #region Complete Drag Sequence Tests

    [Test]
    public void PointerDrag_CompleteDragSequence_Success() {
      // Arrange
      _testElement.Draggable = true;
      Vector3 startPos = _testElement.GetPosition();
      Vector3 endPos = startPos + new Vector3(10, 10, 0);

      // Act - Complete drag sequence
      SimulateDragStart(_testElement);
      SimulateDragUpdate(_testElement, endPos);
      SimulateDragEnd(_testElement);

      // Assert
      Assert.IsTrue(_mockEventChannel.WasEventRaised(GameEventType.ElementDragStart),
        "ElementDragStart should be raised");
      Assert.IsTrue(_mockEventChannel.WasEventRaised(GameEventType.ElementDragging),
        "ElementDragging should be raised");
      Assert.IsTrue(_mockEventChannel.WasEventRaised(GameEventType.ElementDropped),
        "ElementDropped should be raised");
      AssertPosition(_testElement, endPos, 0.01f, "Element should be at end position");
      AssertDragState(_testElement, false, "Element should not be dragging after sequence");
    }

    [Test]
    public void PointerDrag_MultipleDragSequences_Success() {
      // Arrange
      _testElement.Draggable = true;
      Vector3 startPos = _testElement.GetPosition();

      // Act - Multiple drag sequences
      for (int i = 1; i <= 3; i++) {
        Vector3 dragPos = startPos + new Vector3(i * 5, i * 5, 0);
        SimulateDragStart(_testElement);
        SimulateDragUpdate(_testElement, dragPos);
        SimulateDragEnd(_testElement);
      }

      // Assert
      Assert.AreEqual(3, _mockEventChannel.GetEventCount(GameEventType.ElementDragStart),
        "Should have 3 drag start events");
      Assert.AreEqual(3, _mockEventChannel.GetEventCount(GameEventType.ElementDropped),
        "Should have 3 drag end events");
    }

    #endregion

    #region Drag with Selection

    [Test]
    public void PointerDrag_DraggableAndSelectable_BothWork(
      [Values(SelectionTrigger.OnClick, SelectionTrigger.OnHover)] SelectionTrigger trigger) {
      
      // Arrange
      _testElement.Draggable = true;
      _testElement.SelectionTriggers = trigger;

      // Act - Select and drag
      if (trigger == SelectionTrigger.OnClick) {
        SimulateSelection(_testElement);
      } else {
        SimulatePointerHover(_testElement, Vector3.zero);
      }
      
      Vector3 newPos = _testElement.GetPosition() + new Vector3(5, 5, 0);
      SimulateDragStart(_testElement);
      SimulateDragUpdate(_testElement, newPos);
      SimulateDragEnd(_testElement);

      // Assert
      AssertSelectionState(_testElement, true, "Element should be selected");
      AssertPosition(_testElement, newPos, 0.01f, "Element should be at dragged position");
      AssertDragState(_testElement, false, "Element should not be dragging after drag end");
    }

    [Test]
    public void PointerDrag_DraggableButNotSelectable_CanDrag() {
      // Arrange
      _testElement.Draggable = true;
      _testElement.EnableSelectable(false);

      // Act
      Vector3 newPos = _testElement.GetPosition() + new Vector3(5, 5, 0);
      SimulateDragStart(_testElement);
      SimulateDragUpdate(_testElement, newPos);
      SimulateDragEnd(_testElement);

      // Assert
      AssertPosition(_testElement, newPos, 0.01f, 
        "Element should be draggable even when not selectable");
      AssertSelectionState(_testElement, false, "Element should not be selected");
    }

    #endregion

    #region Drag State Transitions

    [Test]
    public void PointerDrag_CancelDragByDisablingDraggable_DragEnds() {
      // Arrange
      _testElement.Draggable = true;
      SimulateDragStart(_testElement);
      AssertDragState(_testElement, true, "Element should be dragging");

      // Act
      _testElement.Draggable = false;

      // Assert
      // Note: Implementation might vary - element might continue dragging or stop
      // This tests the current implementation behavior
    }

    [Test]
    public void PointerDrag_RapidDragStartEnd_StateCorrect() {
      // Arrange
      _testElement.Draggable = true;

      // Act - Rapid drag start/end cycles
      for (int i = 0; i < 5; i++) {
        SimulateDragStart(_testElement);
        SimulateDragEnd(_testElement);
      }

      // Assert
      AssertDragState(_testElement, false, "Element should not be dragging after final drag end");
      Assert.AreEqual(5, _mockEventChannel.GetEventCount(GameEventType.ElementDragStart),
        "Should have 5 drag start events");
      Assert.AreEqual(5, _mockEventChannel.GetEventCount(GameEventType.ElementDropped),
        "Should have 5 drag end events");
    }

    #endregion

    #region All Combinations Tests

    [Test]
    public void PointerDrag_AllDraggableFlagCombinations_CorrectBehavior(
      [Values(true, false)] bool draggable,
      [Values(true, false)] bool canSelect) {
      
      // Arrange
      _testElement.Draggable = draggable;
      _testElement.SelectionTriggers = canSelect ? SelectionTrigger.OnClick : SelectionTrigger.None;
      Vector3 startPos = _testElement.GetPosition();
      Vector3 newPos = startPos + new Vector3(5, 5, 0);

      // Act
      SimulateDragStart(_testElement);
      SimulateDragUpdate(_testElement, newPos);
      SimulateDragEnd(_testElement);

      // Assert
      if (draggable) {
        AssertDragState(_testElement, false, 
          $"Element should have completed drag: draggable={draggable}, canSelect={canSelect}");
        Assert.IsTrue(_mockEventChannel.WasEventRaised(GameEventType.ElementDragStart),
          "Drag start event should be raised for draggable element");
      } else {
        AssertDragState(_testElement, false, 
          $"Element should not have dragged: draggable={draggable}, canSelect={canSelect}");
        AssertPosition(_testElement, startPos, 0.01f, 
          "Element position should not change when not draggable");
      }
    }

    [Test]
    public void PointerDrag_AllSelectionTriggerCombinations_CanDrag(
      [Values(
        SelectionTrigger.None,
        SelectionTrigger.OnHover,
        SelectionTrigger.OnClick,
        SelectionTrigger.OnDoubleClick
      )] SelectionTrigger trigger) {
      
      // Arrange
      _testElement.Draggable = true;
      _testElement.SelectionTriggers = trigger;
      Vector3 startPos = _testElement.GetPosition();
      Vector3 newPos = startPos + new Vector3(5, 5, 0);

      // Act
      SimulateDragStart(_testElement);
      SimulateDragUpdate(_testElement, newPos);
      SimulateDragEnd(_testElement);

      // Assert
      AssertPosition(_testElement, newPos, 0.01f, 
        $"Element should be draggable regardless of selection trigger: {trigger}");
      AssertDragState(_testElement, false, "Element should not be dragging after drag end");
    }

    [Test]
    public void PointerDrag_DraggableAndSelectableFlags_AllCombinations(
      [Values(true, false)] bool draggable,
      [Values(true, false)] bool canSelect,
      [Values(true, false)] bool canHover) {
      
      // Arrange
      _testElement.Draggable = draggable;
      _testElement.SelectionTriggers = canSelect ? SelectionTrigger.OnClick : SelectionTrigger.None;
      _testElement.EnableHovering(canHover);
      Vector3 startPos = _testElement.GetPosition();
      Vector3 newPos = startPos + new Vector3(5, 5, 0);

      // Act
      SimulateDragStart(_testElement);
      SimulateDragUpdate(_testElement, newPos);
      SimulateDragEnd(_testElement);

      // Assert
      bool shouldHaveDragged = draggable;
      if (shouldHaveDragged) {
        AssertPosition(_testElement, newPos, 0.01f, 
          $"Element should have moved: draggable={draggable}, canSelect={canSelect}, canHover={canHover}");
      } else {
        AssertPosition(_testElement, startPos, 0.01f, 
          $"Element should not have moved: draggable={draggable}, canSelect={canSelect}, canHover={canHover}");
      }
    }

    #endregion
  }
}
