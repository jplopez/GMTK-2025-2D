using NUnit.Framework;
using UnityEngine;
using GMTK;
using static GMTK.Tests.PlayableElementTestHelpers;

namespace GMTK.Tests.PointerActions {
  /// <summary>
  /// Functional tests for pointer selection interactions on PlayableElements.
  /// Tests all combinations of selection triggers and element states.
  /// </summary>
  [TestFixture]
  public class PointerSelectTests {

    private MockGameEventChannel _mockEventChannel;
    private PlayableElement _testElement;

    [SetUp]
    public void SetUp() {
      SetupMockServices();
      _mockEventChannel = ServiceLocator.Get<GameEventChannel>() as MockGameEventChannel;
      _testElement = CreateTestPlayableElement("SelectTestElement");
      _testElement.Initialize();
    }

    [TearDown]
    public void TearDown() {
      DestroyComponent(_testElement);
      CleanupMockServices();
    }

    #region Selection Tests

    [Test]
    public void PointerClick_ElementNotSelected_ElementSelected() {
      // Arrange
      _testElement.SelectionTriggers = SelectionTrigger.OnClick;
      AssertSelectionState(_testElement, false, "Element should not be selected initially");

      // Act
      SimulateSelection(_testElement);

      // Assert
      AssertSelectionState(_testElement, true, "Element should be selected after click");
      Assert.IsTrue(_mockEventChannel.WasEventRaised(GameEventType.ElementSelected),
        "ElementSelected event should be raised");
    }

    [Test]
    public void PointerClick_ElementAlreadySelected_NoChange() {
      // Arrange
      _testElement.SelectionTriggers = SelectionTrigger.OnClick;
      SimulateSelection(_testElement);
      _mockEventChannel.ClearEvents();

      // Act
      SimulateSelection(_testElement);

      // Assert
      AssertSelectionState(_testElement, true, "Element should remain selected");
      Assert.AreEqual(0, _mockEventChannel.GetEventCount(GameEventType.ElementSelected),
        "No additional selection events should be raised");
    }

    [Test]
    public void PointerClick_SelectionDisabled_ElementNotSelected() {
      // Arrange
      _testElement.EnableSelectable(false);

      // Act
      SimulateSelection(_testElement);

      // Assert
      AssertSelectionState(_testElement, false, "Element should not be selected when selection is disabled");
      Assert.IsFalse(_mockEventChannel.WasEventRaised(GameEventType.ElementSelected),
        "ElementSelected event should not be raised");
    }

    [Test]
    public void PointerClick_SelectionTriggerNone_ElementNotSelected() {
      // Arrange
      _testElement.SelectionTriggers = SelectionTrigger.None;

      // Act
      SimulateSelection(_testElement);

      // Assert
      AssertSelectionState(_testElement, false, "Element should not be selected when SelectionTrigger is None");
    }

    [Test]
    public void PointerClick_SelectionTriggerOnClick_ElementSelected() {
      // Arrange
      _testElement.SelectionTriggers = SelectionTrigger.OnClick;

      // Act
      SimulateSelection(_testElement);

      // Assert
      AssertSelectionState(_testElement, true, "Element should be selected with OnClick trigger");
    }

    [Test]
    public void PointerClick_SelectionTriggerOnDoubleClick_ElementNotSelected() {
      // Arrange
      _testElement.SelectionTriggers = SelectionTrigger.OnDoubleClick;

      // Act
      SimulateSelection(_testElement); // Single click

      // Assert
      AssertSelectionState(_testElement, false, "Element should not be selected on single click with OnDoubleClick trigger");
    }

    [Test]
    public void PointerClick_MultipleSelectionTriggers_ElementSelected() {
      // Arrange
      _testElement.SelectionTriggers = SelectionTrigger.OnClick | SelectionTrigger.OnDoubleClick;

      // Act
      SimulateSelection(_testElement);

      // Assert
      AssertSelectionState(_testElement, true, "Element should be selected when click is one of multiple triggers");
    }

    #endregion

    #region Deselection Tests

    [Test]
    public void PointerDeselect_ElementSelected_ElementDeselected() {
      // Arrange
      _testElement.SelectionTriggers = SelectionTrigger.OnClick;
      SimulateSelection(_testElement);
      _mockEventChannel.ClearEvents();

      // Act
      SimulateDeselection(_testElement);

      // Assert
      AssertSelectionState(_testElement, false, "Element should be deselected");
      Assert.IsTrue(_mockEventChannel.WasEventRaised(GameEventType.ElementDeselected),
        "ElementDeselected event should be raised");
    }

    [Test]
    public void PointerDeselect_ElementNotSelected_NoChange() {
      // Arrange
      AssertSelectionState(_testElement, false, "Element should not be selected initially");

      // Act
      SimulateDeselection(_testElement);

      // Assert
      AssertSelectionState(_testElement, false, "Element should remain deselected");
      Assert.AreEqual(0, _mockEventChannel.GetEventCount(GameEventType.ElementDeselected),
        "No deselection events should be raised");
    }

    [Test]
    public void PointerDeselect_DisableSelectableWhileSelected_ElementDeselected() {
      // Arrange
      SimulateSelection(_testElement);
      _mockEventChannel.ClearEvents();

      // Act
      _testElement.EnableSelectable(false);

      // Assert
      AssertSelectionState(_testElement, false, "Element should be deselected when selection is disabled");
      Assert.IsTrue(_mockEventChannel.WasEventRaised(GameEventType.ElementDeselected),
        "ElementDeselected event should be raised");
    }

    #endregion

    #region Selection State Transitions

    [Test]
    public void PointerSelect_RapidSelectDeselect_StateCorrect() {
      // Arrange
      _testElement.SelectionTriggers = SelectionTrigger.OnClick;

      // Act - Rapid select/deselect cycles
      for (int i = 0; i < 5; i++) {
        SimulateSelection(_testElement);
        SimulateDeselection(_testElement);
      }

      // Assert
      AssertSelectionState(_testElement, false, "Element should not be selected after final deselect");
      Assert.AreEqual(5, _mockEventChannel.GetEventCount(GameEventType.ElementSelected),
        "Should have 5 selection events");
      Assert.AreEqual(5, _mockEventChannel.GetEventCount(GameEventType.ElementDeselected),
        "Should have 5 deselection events");
    }

    [Test]
    public void PointerSelect_EnableSelectableAfterDisable_Success() {
      // Arrange
      _testElement.EnableSelectable(false);
      _mockEventChannel.ClearEvents();

      // Act
      _testElement.EnableSelectable(true);
      SimulateSelection(_testElement);

      // Assert
      AssertSelectionState(_testElement, true, "Element should be selected after re-enabling selection");
      Assert.IsTrue(_mockEventChannel.WasEventRaised(GameEventType.ElementSelected),
        "ElementSelected event should be raised");
    }

    #endregion

    #region Selection with Hover

    [Test]
    public void PointerHover_SelectionTriggerOnHover_ElementSelectedOnHover() {
      // Arrange
      _testElement.SelectionTriggers = SelectionTrigger.OnHover;

      // Act
      SimulatePointerHover(_testElement, Vector3.zero);

      // Assert
      AssertSelectionState(_testElement, true, "Element should be selected when hovered with OnHover trigger");
      AssertHoverState(_testElement, true, "Element should also be hovered");
    }

    [Test]
    public void PointerHover_SelectionTriggerOnClick_ElementNotSelected() {
      // Arrange
      _testElement.SelectionTriggers = SelectionTrigger.OnClick;

      // Act
      SimulatePointerHover(_testElement, Vector3.zero);

      // Assert
      AssertSelectionState(_testElement, false, "Element should not be selected on hover with OnClick trigger");
      AssertHoverState(_testElement, true, "Element should still be hovered");
    }

    [Test]
    public void PointerUnhover_SelectionTriggerOnHover_ElementDeselected() {
      // Arrange
      _testElement.SelectionTriggers = SelectionTrigger.OnHover;
      SimulatePointerHover(_testElement, Vector3.zero);
      _mockEventChannel.ClearEvents();

      // Act
      SimulatePointerUnhover(_testElement);

      // Assert
      AssertSelectionState(_testElement, false, "Element should be deselected when unhovered with OnHover trigger");
      AssertHoverState(_testElement, false, "Element should not be hovered");
    }

    [Test]
    public void PointerUnhover_SelectionTriggerOnClick_ElementStillSelected() {
      // Arrange
      _testElement.SelectionTriggers = SelectionTrigger.OnClick;
      SimulateSelection(_testElement);
      SimulatePointerHover(_testElement, Vector3.zero);
      _mockEventChannel.ClearEvents();

      // Act
      SimulatePointerUnhover(_testElement);

      // Assert
      AssertSelectionState(_testElement, true, "Element should remain selected when unhovered with OnClick trigger");
      AssertHoverState(_testElement, false, "Element should not be hovered");
    }

    #endregion

    #region Selection with Drag

    [Test]
    public void PointerSelect_ElementDraggable_CanSelect() {
      // Arrange
      _testElement.Draggable = true;
      _testElement.SelectionTriggers = SelectionTrigger.OnClick;

      // Act
      SimulateSelection(_testElement);

      // Assert
      AssertSelectionState(_testElement, true, "Draggable element should be selectable");
    }

    [Test]
    public void PointerSelect_ElementNotDraggable_CanSelect() {
      // Arrange
      _testElement.Draggable = false;
      _testElement.SelectionTriggers = SelectionTrigger.OnClick;

      // Act
      SimulateSelection(_testElement);

      // Assert
      AssertSelectionState(_testElement, true, "Non-draggable element should still be selectable");
    }

    [Test]
    public void PointerSelect_ElementBeingDragged_StillSelected() {
      // Arrange
      _testElement.SelectionTriggers = SelectionTrigger.OnClick;
      SimulateSelection(_testElement);
      SimulateDragStart(_testElement);

      // Assert
      AssertSelectionState(_testElement, true, "Element should remain selected while being dragged");
      AssertDragState(_testElement, true, "Element should be in dragging state");
    }

    #endregion

    #region All Combinations Tests

    [Test]
    public void PointerSelect_AllSelectionTriggerCombinations_CorrectBehavior(
      [Values(
        SelectionTrigger.None,
        SelectionTrigger.OnHover,
        SelectionTrigger.OnClick,
        SelectionTrigger.OnDoubleClick,
        SelectionTrigger.OnHover | SelectionTrigger.OnClick,
        SelectionTrigger.OnHover | SelectionTrigger.OnDoubleClick,
        SelectionTrigger.OnClick | SelectionTrigger.OnDoubleClick,
        SelectionTrigger.OnHover | SelectionTrigger.OnClick | SelectionTrigger.OnDoubleClick
      )] SelectionTrigger trigger) {
      
      // Arrange
      _testElement.SelectionTriggers = trigger;
      _mockEventChannel.ClearEvents();

      // Act
      SimulateSelection(_testElement); // Simulate click selection

      // Assert
      bool shouldBeSelected = (trigger & SelectionTrigger.OnClick) != 0;
      AssertSelectionState(_testElement, shouldBeSelected, 
        $"Element selection state should match OnClick trigger presence: {trigger}");
    }

    [Test]
    public void PointerSelect_AllCanSelectFlagCombinations_CorrectBehavior(
      [Values(true, false)] bool canSelect,
      [Values(true, false)] bool draggable) {
      
      // Arrange
      _testElement.SelectionTriggers = canSelect ? SelectionTrigger.OnClick : SelectionTrigger.None;
      _testElement.Draggable = draggable;
      _mockEventChannel.ClearEvents();

      // Act
      SimulateSelection(_testElement);

      // Assert
      AssertSelectionState(_testElement, canSelect, 
        $"Element selection state should match CanSelect flag: canSelect={canSelect}, draggable={draggable}");
    }

    #endregion
  }
}
