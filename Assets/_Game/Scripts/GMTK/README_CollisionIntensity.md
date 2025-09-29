# Collision Intensity Calculation System

This system provides high-performance collision intensity calculation for the PlayableMarbleController, with support for both component-based and Burst Jobs approaches.

## Overview

The collision intensity calculation has been moved from a hypothetical `MarbleCollisionIntensityCalculator` component to a more efficient Burst Jobs implementation. The system can handle 50+ collisions per second with improved performance.

## Components

### 1. MarbleCollisionIntensityCalculator (Component-based approach)
- **File**: `Assets/_Game/Scripts/GMTK/Controllers/MarbleCollisionIntensityCalculator.cs`
- **Purpose**: Baseline component for performance comparison
- **Usage**: Automatically calculates collision intensity using Unity's physics events
- **Performance**: Suitable for occasional collisions, processes one collision at a time

### 2. MarbleCollisionIntensityJobManager (Burst Jobs approach)
- **File**: `Assets/_Game/Scripts/GMTK/Controllers/MarbleCollisionIntensityJobManager.cs`  
- **Purpose**: High-performance collision intensity calculation using Unity's Job System
- **Usage**: Queues collision data and processes multiple collisions in parallel using Burst-compiled jobs
- **Performance**: Optimized for high-frequency collisions (50+ per second)

### 3. CollisionIntensityJob (Job Implementation)
- **File**: `Assets/_Game/Scripts/GMTK/Jobs/CollisionIntensityJob.cs`
- **Purpose**: Burst-compiled job for parallel collision intensity calculation
- **Features**: 
  - Parallel processing of multiple collisions
  - Fallback implementation when Job System is not available
  - Optimized math operations using Unity.Mathematics

### 4. PlayableMarbleController (Enhanced)
- **File**: `Assets/_Game/Scripts/GMTK/Controllers/PlayableMarbleController.cs`
- **New Features**:
  - Toggle between component-based and Burst Jobs approaches
  - Runtime switching between calculation methods
  - Statistics tracking and performance monitoring
  - API for accessing collision intensity data

## Usage

### Setting up Collision Intensity Calculation

1. **Enable collision intensity calculation** on the PlayableMarbleController:
   ```csharp
   playableMarble.EnableCollisionIntensity = true;
   ```

2. **Choose calculation method**:
   ```csharp
   // Use Burst Jobs (recommended for high collision scenarios)
   playableMarble.UseBurstJobs = true;
   
   // Use component-based approach (for baseline comparison)
   playableMarble.UseBurstJobs = false;
   ```

3. **Switch methods at runtime**:
   ```csharp
   playableMarble.SwitchCollisionIntensityMethod(true); // Switch to Burst Jobs
   ```

### Accessing Collision Data

```csharp
// Get collision statistics
var stats = playableMarble.GetCollisionIntensityStats();
Debug.Log($"Total collisions: {stats.TotalCollisions}");
Debug.Log($"Average intensity: {stats.AverageIntensity}");
Debug.Log($"Using Burst Jobs: {stats.UsingBurstJobs}");

// Reset statistics
playableMarble.ResetCollisionIntensityStats();
```

## Performance Testing

### CollisionIntensityPerformanceTest
- **File**: `Assets/_Game/Scripts/GMTK/Testing/CollisionIntensityPerformanceTest.cs`
- **Purpose**: Automated performance comparison between component and Burst Jobs approaches
- **Features**:
  - Simulates 50+ collisions per second
  - Measures execution time, frame time, and throughput
  - Provides performance comparison metrics

### Running Performance Tests

1. Add the `CollisionIntensityPerformanceTest` component to a GameObject in your scene
2. Assign the `TestMarble` reference to your PlayableMarbleController
3. Configure test parameters (collisions per second, test duration, etc.)
4. Click "Run Performance Test" in the Inspector (Editor only)

## Collision Intensity Calculation

The system calculates collision intensity using a kinetic energy approach:

```
Intensity = 0.5 * effective_mass * normal_velocity² * intensity_multiplier
```

Where:
- `effective_mass` = Combined mass of colliding objects
- `normal_velocity` = Velocity component along collision normal
- `intensity_multiplier` = Configurable scaling factor

## Configuration Options

### MarbleCollisionIntensityCalculator
- `IntensityMultiplier`: Scaling factor for intensity calculation

### MarbleCollisionIntensityJobManager
- `IntensityMultiplier`: Scaling factor for intensity calculation
- `JobBatchSize`: Batch size for parallel job execution (default: 32)
- `MaxCollisionsPerFrame`: Maximum collisions processed per frame (default: 100)

### PlayableMarbleController
- `EnableCollisionIntensity`: Enable/disable collision intensity calculation
- `UseBurstJobs`: Choose between component-based and Burst Jobs approaches

## Performance Characteristics

### Component-based Approach
- **Pros**: Simple, straightforward implementation
- **Cons**: Processes one collision at a time, can cause frame drops with many collisions
- **Use case**: Low-frequency collisions, debugging, baseline comparison

### Burst Jobs Approach  
- **Pros**: Parallel processing, Burst-compiled for performance, handles high collision rates
- **Cons**: More complex implementation, requires Job System packages
- **Use case**: High-frequency collisions (50+ per second), production scenarios

## Compatibility

The system includes fallback implementations for scenarios where Unity's Job System is not available:
- Automatic detection of Job System availability
- Graceful fallback to standard calculation methods
- Consistent API regardless of underlying implementation

## Dependencies

### Required (always):
- Unity 2022.3+ (for modern physics and scripting features)
- UnityEngine.Physics2D

### Optional (for Burst Jobs):
- Unity.Collections
- Unity.Jobs  
- Unity.Mathematics
- Unity.Burst (recommended for optimal performance)

If Job System packages are not available, the system will automatically use fallback implementations.