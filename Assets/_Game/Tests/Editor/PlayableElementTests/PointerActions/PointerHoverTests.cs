using NUnit.Framework;
using UnityEngine;
using GMTK;
using static GMTK.Tests.PlayableElementTestHelpers;

namespace GMTK.Tests.PointerActions {
  /// <summary>
  /// Functional tests for pointer hover interactions on PlayableElements.
  /// Tests all combinations of hover behavior, selection triggers, and element states.
  /// </summary>
  [TestFixture]
  public class PointerHoverTests {

    private MockGameEventChannel _mockEventChannel;
    private PlayableElement _testElement;

    [SetUp]
    public void SetUp() {
      SetupMockServices();
      _mockEventChannel = ServiceLocator.Get<GameEventChannel>() as MockGameEventChannel;
      _testElement = CreateTestPlayableElement("HoverTestElement");
      _testElement.Initialize();
    }

    [TearDown]
    public void TearDown() {
      DestroyComponent(_testElement);
      CleanupMockServices();
    }

    #region Hover Start Tests

    [Test]
    public void PointerHoverOver_ElementNotHovered_Success() {
      // Arrange
      AssertHoverState(_testElement, false, "Element should not be hovered initially");

      // Act
      SimulatePointerHover(_testElement, Vector3.zero);

      // Assert
      AssertHoverState(_testElement, true, "Element should be hovered after hover event");
      Assert.IsTrue(_mockEventChannel.WasEventRaised(GameEventType.ElementHovered), 
        "ElementHovered event should be raised");
    }

    [Test]
    public void PointerHoverOver_ElementAlreadyHovered_NoChange() {
      // Arrange
      SimulatePointerHover(_testElement, Vector3.zero);
      _mockEventChannel.ClearEvents();
      
      // Act
      SimulatePointerHover(_testElement, Vector3.zero);

      // Assert
      AssertHoverState(_testElement, true, "Element should remain hovered");
      Assert.AreEqual(0, _mockEventChannel.GetEventCount(GameEventType.ElementHovered),
        "No additional hover events should be raised");
    }

    [Test]
    public void PointerHoverOver_HoveringDisabled_Fail() {
      // Arrange
      _testElement.EnableHovering(false);

      // Act
      SimulatePointerHover(_testElement, Vector3.zero);

      // Assert
      AssertHoverState(_testElement, false, "Element should not be hovered when hovering is disabled");
    }

    [Test]
    public void PointerHoverOver_SelectionTriggerOnHover_ElementSelected() {
      // Arrange
      _testElement.SelectionTriggers = SelectionTrigger.OnHover;
      
      // Act
      SimulatePointerHover(_testElement, Vector3.zero);

      // Assert
      AssertHoverState(_testElement, true, "Element should be hovered");
      AssertSelectionState(_testElement, true, "Element should be selected when hover triggers selection");
      Assert.IsTrue(_mockEventChannel.WasEventRaised(GameEventType.ElementSelected),
        "ElementSelected event should be raised");
    }

    [Test]
    public void PointerHoverOver_SelectionTriggerOnClick_ElementNotSelected() {
      // Arrange
      _testElement.SelectionTriggers = SelectionTrigger.OnClick;
      
      // Act
      SimulatePointerHover(_testElement, Vector3.zero);

      // Assert
      AssertHoverState(_testElement, true, "Element should be hovered");
      AssertSelectionState(_testElement, false, "Element should not be selected when click trigger is set");
    }

    [Test]
    public void PointerHoverOver_SelectionTriggerOnDoubleClick_ElementNotSelected() {
      // Arrange
      _testElement.SelectionTriggers = SelectionTrigger.OnDoubleClick;
      
      // Act
      SimulatePointerHover(_testElement, Vector3.zero);

      // Assert
      AssertHoverState(_testElement, true, "Element should be hovered");
      AssertSelectionState(_testElement, false, "Element should not be selected when double-click trigger is set");
    }

    [Test]
    public void PointerHoverOver_MultipleSelectionTriggers_ElementSelectedOnHover() {
      // Arrange
      _testElement.SelectionTriggers = SelectionTrigger.OnHover | SelectionTrigger.OnClick;
      
      // Act
      SimulatePointerHover(_testElement, Vector3.zero);

      // Assert
      AssertHoverState(_testElement, true, "Element should be hovered");
      AssertSelectionState(_testElement, true, "Element should be selected when hover is one of multiple triggers");
    }

    #endregion

    #region Hover End Tests

    [Test]
    public void PointerHoverOut_ElementHovered_Success() {
      // Arrange
      SimulatePointerHover(_testElement, Vector3.zero);
      _mockEventChannel.ClearEvents();

      // Act
      SimulatePointerUnhover(_testElement);

      // Assert
      AssertHoverState(_testElement, false, "Element should not be hovered after unhover event");
      Assert.IsTrue(_mockEventChannel.WasEventRaised(GameEventType.ElementUnhovered),
        "ElementUnhovered event should be raised");
    }

    [Test]
    public void PointerHoverOut_ElementNotHovered_NoChange() {
      // Arrange
      AssertHoverState(_testElement, false, "Element should not be hovered initially");

      // Act
      SimulatePointerUnhover(_testElement);

      // Assert
      AssertHoverState(_testElement, false, "Element should remain unhovered");
      Assert.IsFalse(_mockEventChannel.WasEventRaised(GameEventType.ElementUnhovered),
        "No unhover event should be raised");
    }

    [Test]
    public void PointerHoverOut_SelectionTriggerOnHover_ElementDeselected() {
      // Arrange
      _testElement.SelectionTriggers = SelectionTrigger.OnHover;
      SimulatePointerHover(_testElement, Vector3.zero);
      _mockEventChannel.ClearEvents();

      // Act
      SimulatePointerUnhover(_testElement);

      // Assert
      AssertHoverState(_testElement, false, "Element should not be hovered");
      AssertSelectionState(_testElement, false, "Element should be deselected when unhovered with hover trigger");
      Assert.IsTrue(_mockEventChannel.WasEventRaised(GameEventType.ElementDeselected),
        "ElementDeselected event should be raised");
    }

    [Test]
    public void PointerHoverOut_SelectionTriggerOnClick_ElementStillSelected() {
      // Arrange
      _testElement.SelectionTriggers = SelectionTrigger.OnClick;
      SimulateSelection(_testElement); // Select element via click
      SimulatePointerHover(_testElement, Vector3.zero);
      _mockEventChannel.ClearEvents();

      // Act
      SimulatePointerUnhover(_testElement);

      // Assert
      AssertHoverState(_testElement, false, "Element should not be hovered");
      AssertSelectionState(_testElement, true, "Element should remain selected when unhovered with click trigger");
    }

    #endregion

    #region Hover State Transitions

    [Test]
    public void PointerHover_RapidHoverUnhover_StateCorrect() {
      // Act - Rapid hover/unhover cycles
      for (int i = 0; i < 5; i++) {
        SimulatePointerHover(_testElement, Vector3.zero);
        SimulatePointerUnhover(_testElement);
      }

      // Assert
      AssertHoverState(_testElement, false, "Element should not be hovered after final unhover");
      Assert.AreEqual(5, _mockEventChannel.GetEventCount(GameEventType.ElementHovered),
        "Should have 5 hover events");
      Assert.AreEqual(5, _mockEventChannel.GetEventCount(GameEventType.ElementUnhovered),
        "Should have 5 unhover events");
    }

    [Test]
    public void PointerHover_DisableHoveringWhileHovered_ElementUnhovered() {
      // Arrange
      SimulatePointerHover(_testElement, Vector3.zero);
      _mockEventChannel.ClearEvents();

      // Act
      _testElement.EnableHovering(false);

      // Assert
      AssertHoverState(_testElement, false, "Element should be unhovered when hovering is disabled");
      Assert.IsTrue(_mockEventChannel.WasEventRaised(GameEventType.ElementUnhovered),
        "ElementUnhovered event should be raised when hovering is disabled");
    }

    [Test]
    public void PointerHover_EnableHoveringAfterDisable_Success() {
      // Arrange
      _testElement.EnableHovering(false);
      _mockEventChannel.ClearEvents();

      // Act
      _testElement.EnableHovering(true);
      SimulatePointerHover(_testElement, Vector3.zero);

      // Assert
      AssertHoverState(_testElement, true, "Element should be hovered after re-enabling hovering");
      Assert.IsTrue(_mockEventChannel.WasEventRaised(GameEventType.ElementHovered),
        "ElementHovered event should be raised");
    }

    #endregion

    #region Hover with Drag

    [Test]
    public void PointerHover_ElementDraggable_CanHover() {
      // Arrange
      _testElement.Draggable = true;

      // Act
      SimulatePointerHover(_testElement, Vector3.zero);

      // Assert
      AssertHoverState(_testElement, true, "Draggable element should be hoverable");
    }

    [Test]
    public void PointerHover_ElementNotDraggable_CanHover() {
      // Arrange
      _testElement.Draggable = false;

      // Act
      SimulatePointerHover(_testElement, Vector3.zero);

      // Assert
      AssertHoverState(_testElement, true, "Non-draggable element should still be hoverable");
    }

    [Test]
    public void PointerHover_ElementBeingDragged_StillHovered() {
      // Arrange
      SimulateDragStart(_testElement);
      _mockEventChannel.ClearEvents();

      // Act
      SimulatePointerHover(_testElement, Vector3.zero);

      // Assert
      AssertHoverState(_testElement, true, "Element being dragged should remain hoverable");
      AssertDragState(_testElement, true, "Element should still be in dragging state");
    }

    #endregion

    #region All Combinations Tests

    [Test]
    public void PointerHover_AllSelectionTriggerCombinations_CorrectBehavior(
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
      SimulatePointerHover(_testElement, Vector3.zero);

      // Assert
      AssertHoverState(_testElement, true, $"Element should be hovered with trigger {trigger}");
      
      bool shouldBeSelected = (trigger & SelectionTrigger.OnHover) != 0;
      AssertSelectionState(_testElement, shouldBeSelected, 
        $"Element selection state should match OnHover trigger presence: {trigger}");
    }

    [Test]
    public void PointerHover_AllDraggableFlagCombinations_CorrectBehavior(
      [Values(true, false)] bool draggable,
      [Values(true, false)] bool hoveringEnabled) {
      
      // Arrange
      _testElement.Draggable = draggable;
      _testElement.EnableHovering(hoveringEnabled);
      _mockEventChannel.ClearEvents();

      // Act
      SimulatePointerHover(_testElement, Vector3.zero);

      // Assert
      AssertHoverState(_testElement, hoveringEnabled, 
        $"Element hover state should match hovering enabled: draggable={draggable}, hoveringEnabled={hoveringEnabled}");
    }

    #endregion
  }
}
