# PlayableElement Functional Tests

## Overview

This directory contains comprehensive functional tests for `PlayableElement` and `PlayableElementComponent` classes. The tests are organized into three main test suites that cover all aspects of playable element interactions.

## Test Structure

### Test Suites

#### 1. Pointer Actions (`PointerActions/`)

Tests for pointer-based interactions (hover, select, drag) through Unity's Input System and GameEventChannel.

- **PointerHoverTests.cs** - Tests hover start/end events
  - All selection trigger combinations (OnHover, OnClick, OnDoubleClick)
  - Hover with different element states
  - Rapid hover/unhover sequences
  - Enable/disable hovering during hover state

- **PointerSelectTests.cs** - Tests selection/deselection
  - All selection trigger combinations
  - Selection with hover interactions
  - Selection with drag interactions
  - Enable/disable selection during selected state

- **PointerDragTests.cs** - Tests drag start/update/end
  - Drag with different element states
  - Position updates during drag
  - Complete drag sequences
  - All combinations of Draggable and CanSelect flags

#### 2. Marble Interactions (`MarbleInteractions/`)

Tests for marble collision responses including movement and rotation.

- **MarbleCollisionTests.cs** - Tests marble collision responses
  - Basic collision detection
  - Element movement from collisions
  - Element rotation from collisions
  - All behavior combinations:
    - Moves only (CanMove=true, CanRotate=false)
    - Rotates only (CanMove=false, CanRotate=true)
    - Both (CanMove=true, CanRotate=true)
    - Neither (CanMove=false, CanRotate=false)
  - Constrained movement (fixed axis)
  - Collision intensity variations

#### 3. Player Input (`PlayerInput/`)

Tests for direct player input actions (rotate, flip) through GameEventChannel.

- **PlayerInputRotateTests.cs** - Tests rotation controls
  - Rotate clockwise/counter-clockwise
  - Rotation with all flag combinations
  - Multiple rotations and angle verification
  - Rotation with other element states

- **PlayerInputFlipTests.cs** - Tests flip controls
  - Flip X and Flip Y
  - Multiple flips and state restoration
  - Combined X and Y flips
  - Flip with other element states

### Test Helpers (`TestHelpers/`)

- **PlayableElementTestHelpers.cs** - Common utilities for all tests
  - GameObject and component creation helpers
  - Mock GameEventChannel implementation
  - Event simulation methods
  - Custom assertions for PlayableElement state
  - Cleanup utilities

## Test Naming Convention

Tests follow the naming convention:
```
<ActionOrEventTriggering>_<GlobalConditionOrConstrain>_<PlayableElementConditionOrConstraint>_[Success/Fail]
```

Examples:
- `PointerHoverOver_ElementNotHovered_Success`
- `PointerDragStart_ElementNotDraggable_DragNotStarted`
- `PlayerInputRotateCW_CanRotate_Success`
- `MarbleCollision_ElementCanMove_MovementAllowed`

## Running the Tests

These tests are designed to run in Unity's Edit Mode Test Runner.

### In Unity Editor:

1. Open Unity Test Runner window: `Window > General > Test Runner`
2. Select "EditMode" tab
3. Click "Run All" or select specific tests to run

### Via Command Line (Unity Batch Mode):

```bash
Unity -runTests -batchmode -projectPath /path/to/project -testResults /path/to/results.xml -testPlatform editmode
```

## Test Requirements

All tests:
- Are triggered by simulating events through GameEventChannel
- Use Unity's MonoBehaviour lifecycle methods (Awake, Start, Update)
- Are isolated from each other with proper Setup/TearDown
- Test all combinations of relevant flags and states
- Verify correct event raising through mock GameEventChannel

## Coverage

### Selection Triggers Tested
- `SelectionTrigger.None`
- `SelectionTrigger.OnHover`
- `SelectionTrigger.OnClick`
- `SelectionTrigger.OnDoubleClick`
- All combinations (bitwise OR)

### Element Flags Tested
- `Draggable` (true/false)
- `CanRotate` (true/false)
- `Flippable` (true/false)
- `CanSelect` (true/false)
- `CanHover` (true/false)

### Element States Tested
- `IsSelected`
- `IsHovered`
- `IsBeingDragged`
- `IsActive`

### Collision Behaviors Tested
- Movement (with/without constraints)
- Rotation (with/without constraints)
- Combined movement and rotation
- No movement or rotation (static)

## Extending Tests

To add new tests:

1. Create a new test class in the appropriate suite directory
2. Inherit from appropriate base test class or create standalone
3. Use `PlayableElementTestHelpers` for common operations
4. Follow the naming convention for test methods
5. Ensure proper Setup/TearDown for test isolation

Example:
```csharp
using NUnit.Framework;
using GMTK;
using static GMTK.Tests.PlayableElementTestHelpers;

namespace GMTK.Tests.PointerActions {
  [TestFixture]
  public class MyNewTests {
    private PlayableElement _element;
    
    [SetUp]
    public void SetUp() {
      SetupMockServices();
      _element = CreateTestPlayableElement();
    }
    
    [TearDown]
    public void TearDown() {
      DestroyComponent(_element);
      CleanupMockServices();
    }
    
    [Test]
    public void MyTest_Condition_ExpectedResult() {
      // Arrange
      // Act
      // Assert
    }
  }
}
```

## Known Limitations

- Physics simulations in Edit Mode tests are limited
- Marble collision tests focus on setup and state rather than actual physics
- Full physics integration tests should use PlayMode tests
- Input System event simulation is mocked rather than using actual input

## Future Improvements

- Add PlayMode integration tests for full physics simulation
- Add performance tests for rapid state changes
- Add tests for PlayableElementComponent implementations
- Add tests for specific component interactions
- Add stress tests with many elements
