# Unit Tests Documentation

## Overview
This document outlines the comprehensive unit test suite implemented for all MonoBehaviour and ScriptableObject classes in the GMTK-2025-2D project.

## Test Structure
- **Runtime Tests**: Located in `Assets/_Game/Scripts/Tests/Runtime/`
- **Editor Tests**: Located in `Assets/_Game/Scripts/Tests/Editor/`
- **Test Assemblies**: Properly configured with Unity Test Framework and NUnit

## MonoBehaviour Classes Tested (25+)

### Core Controllers
- `SceneController` - Scene configuration, event management, loading behavior
- `PlayableMarbleController` - Physics properties, movement tracking, component interaction
- `GMTKBootstrap` - Service initialization, configuration validation
- `GUIController` - Basic instantiation and component validation
- `LevelCompleteController` - Component creation and basic functionality

### Grid System
- `LevelGrid` - Grid dimensions, bounds configuration, origin settings
- `GridSnappable` - Dragging, rotation, feedback, and snapping behavior
- `SnappableInputHandler` - Input handling for grid-based interactions

### Game Elements  
- `Booster` - Force application, collision detection, cooldown mechanics
- `Checkpoint` - Visual cue management, position tracking, trigger events

### Inventory System
- `LevelInventory` - Component instantiation and basic setup

### Scene Management
- `LevelManager` - Scene lifecycle management
- `RaiseGameEventConfig` - Event configuration extension
- `EndSceneConfig` - Scene ending configuration  
- `ScoreConfig` - Scoring system configuration
- `SceneTransitionConfig` - Scene transition settings

### Input System
- `PlayerInputActionDispatcher` - Input action handling and interface compliance

### UI Components
- `ScoreTextAnimator` - Text animation and display functionality

### Ameba Framework Components
- `RegistryPool` - Object pooling system
- `RuntimeVariableBinder` - Variable binding mechanisms
- `RuntimeVariablePoller` - Variable polling and updates
- `AudioSourcePool` - Audio source management and pooling
- `ScoreKeeperController` - Score management and tracking

### Snappable Components
- `SnappablePhysics` - Physics-based snapping, rotation, auto-rotation
- `DragFeedbackComponent` - Visual feedback during dragging operations

## ScriptableObject Classes Tested (12+)

### Game Data
- `GameElement` - Element properties, instantiation, equality comparison
- `GameInventory` - Inventory data management
- `LevelService` - Level configuration and data services

### State Management  
- `AmebaStateMachine` - State transitions, validation, restriction handling
- `ServiceRegistry` - Service registration, type resolution, validation

### Runtime Systems
- `RuntimeRegistry` - Prefab registration, retrieval, component access
- `RuntimeVariable` - Runtime data storage and access
- `RuntimeMap` - Runtime mapping and lookup functionality

### Scoring System
- `ScoreGateKeeper` - Score validation and gating logic

### Supporting Classes
- `RegistryEntry` - IPoolable implementation, property binding, prefab management

## Editor Tests

### Unity Integration Validation
- **CreateAssetMenu Attributes** - All ScriptableObjects properly configured for asset creation
- **AddComponentMenu Attributes** - MonoBehaviours have correct component menu entries
- **RequireComponent Attributes** - Component dependencies properly enforced
- **DefaultExecutionOrder** - Bootstrap execution priority validation

### Editor Compatibility
- **Instantiation Tests** - All classes can be created in editor without errors
- **Assembly References** - Proper assembly dependencies configured

## Test Coverage Features

### Comprehensive Property Testing
- Default value validation
- Property setter/getter functionality  
- Boundary condition testing
- Type safety verification

### Component Interaction
- Required component validation
- Interface implementation verification
- Component dependency testing

### Framework Integration
- Unity lifecycle method compatibility
- Serialization support validation
- Editor integration verification

## Running the Tests

### Using Unity Test Runner
1. Open Unity Test Runner window (`Window > General > Test Runner`)
2. Select "PlayMode" or "EditMode" tab
3. Run individual tests or entire test suites
4. View results and detailed output

### Command Line (if available)
```bash
# Run all tests
Unity -batchmode -runTests -testPlatform PlayMode

# Run specific test assembly
Unity -batchmode -runTests -testResults results.xml -testPlatform PlayMode
```

## Test Maintenance

### Adding New Classes
1. Create corresponding test file in appropriate folder
2. Follow existing naming convention (`ClassNameTests.cs`)
3. Include basic instantiation, property, and functionality tests
4. Update this documentation

### Test Patterns Used
- **Setup/TearDown** - Proper resource management
- **Descriptive Test Names** - Clear test intentions
- **Arrange/Act/Assert** - Structured test organization
- **Edge Case Testing** - Null checks, boundary values
- **Interface Validation** - Proper contract compliance

## Notes
- Tests are designed to be lightweight and fast-running
- All tests clean up resources properly to avoid memory leaks
- Editor tests validate Unity-specific attributes and configurations
- Runtime tests focus on functional behavior and property management
- Test assemblies are properly isolated with correct dependencies