using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GMTK;
using System.Collections.Generic;
using System.Linq;
using static GMTK.MarbleCollisionIntensityCalculator;
using UnityEngine.Rendering;

namespace GMTK.Tests {

  /// <summary>
  /// Unit tests for MarbleCollisionIntensityCalculator testing all calculation methods 
  /// with all combinations of factors on and off.
  /// </summary>
  [TestFixture]
  public class MarbleCollisionIntensityCalculatorTests {

    #region Constants

    // Constants for test value ranges
    public const float MIN_INTENSITY = 0f;
    public const float MAX_INTENSITY = 1f;

    // Values outside expected ranges to test edge cases, and to prevent heavy computing calculations 
    public const float MIN_TESTING_VALUE = -100f;
    public const float MAX_TESTING_VALUE = 100f;

    // Default values for calculator settings
    public const float DEFAULT_VELOCITY_MIN = 1f;
    public const float DEFAULT_VELOCITY_MAX = 20f;
    public const float DEFAULT_FALL_DISTANCE_MIN = 2f;
    public const float DEFAULT_FALL_DISTANCE_MAX = 10f;
    public const float DEFAULT_ANGLE_MIN = 0f;
    public const float DEFAULT_ANGLE_MAX = 90f;

    public const float DEFAULT_TEST_SPEED = 5f;
    public const float DEFAULT_TEST_FALL_DISTANCE = 4f;
    public const float DEFAULT_TEST_ANGLE = 45f;

    #endregion

    #region Test Objects and Setup
    // Test objects
    private MarbleCollisionIntensityCalculator calculator;
    private GameObject testObject;
    private CollisionIntensityContext baseContext;
    // Wrapper to access protected methods for testing
    private MarbleCollisionIntensityCalculatorWrapper testableCalculator;

    [SetUp]
    public void SetUp() {
      // Create test GameObject with calculator component
      testObject = new GameObject("TestCalculator");
      calculator = testObject.AddComponent<MarbleCollisionIntensityCalculator>();
      // Initialize calculator with default settings
      InitializeCalculator();

      // testable calculator to access factor calculation methods
      testableCalculator = testObject.AddComponent<MarbleCollisionIntensityCalculatorWrapper>();
      testableCalculator.CopyProperties(calculator);

      // Set up base context for testing
      baseContext = new CollisionIntensityContext {
        Velocity = DEFAULT_TEST_SPEED,
        FallDistance = DEFAULT_TEST_FALL_DISTANCE,
        CollisionAngle = DEFAULT_TEST_ANGLE,
        CollisionType = CollisionType.PlayableElement,
        Material = new MaterialProperties {
          Friction = FrictionLevel.Mid,
          Bounciness = BouncinessLevel.Mid,
          HasCustomProperties = true
        }
      };

    }

    [TearDown]
    public void TearDown() {
      if (testObject != null) {
        UnityEngine.Object.DestroyImmediate(testObject);
      }

      if(calculator != null) {
        UnityEngine.Object.DestroyImmediate(calculator);
      }
      if(testableCalculator != null) {
        UnityEngine.Object.DestroyImmediate(testableCalculator);
      }
    }

    private void InitializeCalculator() {
      // Set ranges to ensure test values are within bounds
      calculator.SetCollisionVelocityRange(DEFAULT_VELOCITY_MIN, DEFAULT_VELOCITY_MAX);
      calculator.SetFallDistanceRange(DEFAULT_FALL_DISTANCE_MIN, DEFAULT_FALL_DISTANCE_MAX);
      calculator.SetAngleRange(DEFAULT_ANGLE_MIN, DEFAULT_ANGLE_MAX);
      calculator.SetIntensityRange(new Vector2(MIN_INTENSITY, MAX_INTENSITY));

      // Reset curves to default values
      calculator.ResetAllCurvesToDefault();

      // Enable all factors by default
      calculator.ConsiderVelocity = true;
      calculator.ConsiderFallDistance = true;
      calculator.ConsiderAngle = true;
      calculator.ConsiderFriction = true;
      calculator.ConsiderBounciness = true;
    }

    #endregion


    #region Custom Asserts

    private void AssertValueWithinRange(float value, float expectedMin, float expectedMax, string valueName) {
      valueName = string.IsNullOrEmpty(valueName) ? "Value" : valueName;
      Assert.That(value, Is.GreaterThanOrEqualTo(expectedMin),
          $"{valueName} should be >= {expectedMin}. Was {value}");
      Assert.That(value, Is.LessThanOrEqualTo(expectedMax),
          $"{valueName} should be <= {expectedMax}. Was {value}");
    }

    private void AssertIntensityIsWithinCalculatorRange(float intensity, IntensityCalculationMethod method) => AssertValueWithinRange(intensity, calculator.CollisionIntensityRange.x, calculator.CollisionIntensityRange.y, $"Intensity ({method})");

    /// <summary>
    /// Generate all possible combinations of factor states (on/off).
    /// </summary>
    private List<FactorCombination> GenerateFactorCombinations() {
      var combinations = new List<FactorCombination>();

      // Generate all 2^5 = 32 combinations
      for (int i = 0; i < 32; i++) {
        combinations.Add(new FactorCombination {
          Velocity = (i & 1) != 0,
          FallDistance = (i & 2) != 0,
          Angle = (i & 4) != 0,
          Friction = (i & 8) != 0,
          Bounciness = (i & 16) != 0
        });
      }

      return combinations;
    }

    /// <summary>
    /// Convenience method to Assert a specific combo of calculation method and factors
    /// </summary>
    /// <param name="method"></param>
    /// <param name="factors"></param>
    private void TestSpecificMethodAndFactorCombination(IntensityCalculationMethod method, FactorCombination factors) {
      // Set up calculator with specific method and factor combination
      SetCalculationMethodAndFactors(method, factors);

      // Calculate intensity
      float intensity = calculator.CalculateIntensity(baseContext);
      float expectedMin = calculator.CollisionIntensityRange.x;
      float expectedMax = calculator.CollisionIntensityRange.y;
      Debug.Log($"Testing method {method}. Factors: {factors}. Expected intensity range [{expectedMin},{expectedMax}]");

      // Verify result is valid
      AssertIntensityIsWithinCalculatorRange(intensity, method);

      Assert.That(float.IsNaN(intensity), Is.False,
          $"Intensity should not be NaN for {method} with factors: {factors}");
      Assert.That(float.IsInfinity(intensity), Is.False,
          $"Intensity should not be infinite for {method} with factors: {factors}");

      // Test specific method behaviors
      AssertIntensityCalculationMethod(method, factors, intensity);
    }

    /// <summary>
    /// Asserts the given calculation method with the factors to be the value of intensity.<br/>
    /// This method acts as a hub, calling a specific Assert per each calculation method
    /// 
    /// </summary>
    /// <param name="method"></param>
    /// <param name="factors"></param>
    /// <param name="intensity"></param>
    private void AssertIntensityCalculationMethod(IntensityCalculationMethod method, FactorCombination factors, float intensity) {
      switch (method) {
        case IntensityCalculationMethod.Minimum:
          AssertMinimumCalculationMethodValue(factors, intensity);
          break;

        case IntensityCalculationMethod.Maximum:
          AssertMaximumCalculationMethodValue(factors, intensity);
          break;

        case IntensityCalculationMethod.Average:
          AssertAverageCalculationMethodValue(factors, intensity);
          break;

        case IntensityCalculationMethod.Additive:
          AssertAdditiveCalculationMethodValue(factors, intensity);
          break;

        case IntensityCalculationMethod.Multiplicative:
          AssertMultiplicativeCalculationMethodValue(factors, intensity);
          break;
      }
    }

    private void AssertMinimumCalculationMethodValue(FactorCombination factors, float intensity, string message = "") {

      IntensityFactors intensityFactors = new() {
        AngleFactor = 1f,
        FallFactor = 1f,
        VelocityFactor = 1f,
        MaterialFactor = 1f
      };

      if (factors.Velocity) {
        float expectedValue = testableCalculator.CalculateVelocityIntensityWrapper(baseContext.Velocity);
        intensityFactors.VelocityFactor = expectedValue;
        Assert.LessOrEqual(intensity, expectedValue, $"Minimum method intensity ({intensity}) should be <= velocity intensity ({expectedValue}). {message}");
      }

      if (factors.FallDistance) {
        float expectedValue = testableCalculator.CalculateFallIntensityWrapper(baseContext.FallDistance);
        intensityFactors.FallFactor = expectedValue;
        Assert.LessOrEqual(intensity, expectedValue, $"Minimum method intensity ({intensity}) should be <= fall intensity ({expectedValue}). {message}");
      }

      if (factors.Angle) {
        float expectedValue = testableCalculator.CalculateAngleIntensityWrapper(baseContext.CollisionAngle);
        intensityFactors.AngleFactor = expectedValue;
        Assert.LessOrEqual(intensity, expectedValue, $"Minimum method intensity ({intensity}) should be <= angle intensity ({expectedValue}). {message}");
      }

      if (factors.Friction || factors.Bounciness) {
        float expectedValue = testableCalculator.CalculateMaterialMultiplierWrapper(baseContext);
        intensityFactors.MaterialFactor = expectedValue;
        Assert.LessOrEqual(intensity, expectedValue, $"Minimum method intensity ({intensity}) should be <= material intensity ({expectedValue}). {message}");
      }

      float rawIntensity = testableCalculator.IntensityByCalculationMethodWrapper(intensityFactors);
      AssertFinalCalculatedIntensity(intensity, message, rawIntensity);
    }

    private void AssertMaximumCalculationMethodValue(FactorCombination factors, float intensity, string message = "") {
      IntensityFactors intensityFactors = new() {
        AngleFactor = 1f,
        FallFactor = 1f,
        VelocityFactor = 1f,
        MaterialFactor = 1f
      };

      if (factors.Velocity) {
        float expectedValue = testableCalculator.CalculateVelocityIntensityWrapper(baseContext.Velocity);
        intensityFactors.VelocityFactor = expectedValue;
        Assert.GreaterOrEqual(intensity, expectedValue, $"Minimum method intensity ({intensity}) should be >= velocity intensity ({expectedValue}). {message}");
      }

      if (factors.FallDistance) {
        float expectedValue = testableCalculator.CalculateFallIntensityWrapper(baseContext.FallDistance);
        intensityFactors.FallFactor = expectedValue;
        Assert.GreaterOrEqual(intensity, expectedValue, $"Minimum method intensity ({intensity}) should be >= fall intensity ({expectedValue}). {message}");
      }

      if (factors.Angle) {
        float expectedValue = testableCalculator.CalculateAngleIntensityWrapper(baseContext.CollisionAngle);
        intensityFactors.AngleFactor = expectedValue;
        Assert.GreaterOrEqual(intensity, expectedValue, $"Minimum method intensity ({intensity}) should be >= angle intensity ({expectedValue}). {message}");
      }

      if (factors.Friction || factors.Bounciness) {
        float expectedValue = testableCalculator.CalculateMaterialMultiplierWrapper(baseContext);
        intensityFactors.MaterialFactor = expectedValue;
        Assert.GreaterOrEqual(intensity, expectedValue, $"Minimum method intensity ({intensity}) should be >= material intensity ({expectedValue}). {message}");
      }

      float rawIntensity = testableCalculator.IntensityByCalculationMethodWrapper(intensityFactors);
      AssertFinalCalculatedIntensity(intensity, message, rawIntensity);

    }

    private void AssertAverageCalculationMethodValue(FactorCombination factors, float intensity, string message = "") {

      IntensityFactors intensityFactors = new() {
        AngleFactor = 1f,
        FallFactor = 1f,
        VelocityFactor = 1f,
        MaterialFactor = 1f
      };

      if (factors.Velocity) {
        float expectedValue = testableCalculator.CalculateVelocityIntensityWrapper(baseContext.Velocity);
        intensityFactors.VelocityFactor = expectedValue;
      }

      if (factors.FallDistance) {
        float expectedValue = testableCalculator.CalculateFallIntensityWrapper(baseContext.FallDistance);
        intensityFactors.FallFactor = expectedValue;
      }

      if (factors.Angle) {
        float expectedValue = testableCalculator.CalculateAngleIntensityWrapper(baseContext.CollisionAngle);
        intensityFactors.AngleFactor = expectedValue;
      }

      if (factors.Friction || factors.Bounciness) {
        float expectedValue = testableCalculator.CalculateMaterialMultiplierWrapper(baseContext);
        intensityFactors.MaterialFactor = expectedValue;
      }

      float rawIntensity = testableCalculator.IntensityByCalculationMethodWrapper(intensityFactors);
      AssertFinalCalculatedIntensity(intensity, message, rawIntensity);
    }

    private void AssertAdditiveCalculationMethodValue(FactorCombination factors, float intensity, string message = "") {

      IntensityFactors intensityFactors = new() {
        AngleFactor = 1f,
        FallFactor = 1f,
        VelocityFactor = 1f,
        MaterialFactor = 1f
      };

      if (factors.Velocity) {
        float expectedValue = testableCalculator.CalculateVelocityIntensityWrapper(baseContext.Velocity);
        intensityFactors.VelocityFactor = expectedValue;
      }

      if (factors.FallDistance) {
        float expectedValue = testableCalculator.CalculateFallIntensityWrapper(baseContext.FallDistance);
        intensityFactors.FallFactor = expectedValue;
      }

      if (factors.Angle) {
        float expectedValue = testableCalculator.CalculateAngleIntensityWrapper(baseContext.CollisionAngle);
        intensityFactors.AngleFactor = expectedValue;
      }

      if (factors.Friction || factors.Bounciness) {
        float expectedValue = testableCalculator.CalculateMaterialMultiplierWrapper(baseContext);
        intensityFactors.MaterialFactor = expectedValue;
      }

      float rawIntensity = testableCalculator.IntensityByCalculationMethodWrapper(intensityFactors);
      AssertFinalCalculatedIntensity(intensity, message, rawIntensity);

    }

    private void AssertMultiplicativeCalculationMethodValue(FactorCombination factors, float intensity, string message = "") {
      IntensityFactors intensityFactors = new() {
        AngleFactor = 1f,
        FallFactor = 1f,
        VelocityFactor = 1f,
        MaterialFactor = 1f
      };

      if (factors.Velocity) {
        float expectedValue = testableCalculator.CalculateVelocityIntensityWrapper(baseContext.Velocity);
        intensityFactors.VelocityFactor = expectedValue;
      }

      if (factors.FallDistance) {
        float expectedValue = testableCalculator.CalculateFallIntensityWrapper(baseContext.FallDistance);
        intensityFactors.FallFactor = expectedValue;
      }

      if (factors.Angle) {
        float expectedValue = testableCalculator.CalculateAngleIntensityWrapper(baseContext.CollisionAngle);
        intensityFactors.AngleFactor = expectedValue;
      }

      if (factors.Friction || factors.Bounciness) {
        float expectedValue = testableCalculator.CalculateMaterialMultiplierWrapper(baseContext);
        intensityFactors.MaterialFactor = expectedValue;
      }

      float rawIntensity = testableCalculator.IntensityByCalculationMethodWrapper(intensityFactors);
      AssertFinalCalculatedIntensity(intensity, message, rawIntensity);
    }


    /// <summary>
    /// Asserts that the final calculated intensity is correctly clamped within the calculator's defined range.
    /// And if the raw intensity is within range, that the final intensity matches the raw intensity.
    /// </summary>
    /// <param name="intensity"></param>
    /// <param name="message"></param>
    /// <param name="rawIntensity"></param>
    private void AssertFinalCalculatedIntensity(float intensity, string message, float rawIntensity) {
      // test that final intensity is clamped within range if raw exceeds bounds
      if (rawIntensity < testableCalculator.CollisionIntensityRange.x) {
        Assert.That(intensity, Is.GreaterThan(rawIntensity),
            $"Additive method intensity ({intensity}) should be clamped to raw intensity ({rawIntensity}). {message}");
      }
      else if (rawIntensity > testableCalculator.CollisionIntensityRange.y) {
        Assert.That(intensity, Is.LessThan(rawIntensity),
            $"Additive method intensity ({intensity}) should be clamped to raw intensity ({rawIntensity}). {message}");
      }
      else {
        //raw is within bounds, means intensity should be equal
        Assert.That(intensity, Is.EqualTo(rawIntensity).Within(0.001f),
            $"Additive method intensity ({intensity}) should equal raw intensity ({rawIntensity}). {message}");
      }
      //finally, test intensity is within calculator range
      AssertValueWithinRange(intensity, testableCalculator.CollisionIntensityRange.x, testableCalculator.CollisionIntensityRange.y, "Additive Intensity");
    }

    #endregion

    #region Factor Combination Tests

    /// <summary>
    /// Tests all possible combinations of factors (on/off) for each calculation method.
    /// This generates 2^5 = 32 combinations per method, testing 160 total scenarios.
    /// </summary>
    [Test]
    public void TestAllCalculationMethodsWithAllFactorCombinations() {
      var methods = System.Enum.GetValues(typeof(IntensityCalculationMethod))
          .Cast<IntensityCalculationMethod>();

      var factorCombinations = GenerateFactorCombinations();

      foreach (var method in methods) {
        foreach (var combination in factorCombinations) {
          TestSpecificMethodAndFactorCombination(method, combination);
        }
      }
    }

    #endregion

    #region Calculation Within Range Tests

    [Test]
    public void IntensityIsWithinCollisionRange_AllFactorsEnabled() {
      var methods = System.Enum.GetValues(typeof(IntensityCalculationMethod))
          .Cast<IntensityCalculationMethod>();
      foreach (var method in methods) {
        TestSpecificMethodAndFactorCombination(method, FactorCombination.AllEnabled());
      }
    }

    [Test]
    public void IntensityIsWithinCollisionRange_AllFactorsDisabled() {
      var methods = System.Enum.GetValues(typeof(IntensityCalculationMethod))
          .Cast<IntensityCalculationMethod>();
      foreach (var method in methods) {
        TestSpecificMethodAndFactorCombination(method, FactorCombination.AllDisabled());
      }
    }

    #endregion

    #region Edge Case Tests

    [Test]
    public void TestEdgeValues_AllMethods() {
      var methods = System.Enum.GetValues(typeof(IntensityCalculationMethod))
          .Cast<IntensityCalculationMethod>();

      var edgeContexts = CreateEdgeContexts();

      foreach (var method in methods) {
        SetCalculationMethod(method);
        EnableFactors();

        foreach (var context in edgeContexts) {
          float intensity = calculator.CalculateIntensity(context.Value);

          // Should handle edge cases gracefully
          Assert.That(float.IsNaN(intensity), Is.False,
              $"Method {method} should handle {context.Key} without NaN");
          Assert.That(float.IsInfinity(intensity), Is.False,
              $"Method {method} should handle {context.Key} without infinity");

          AssertIntensityIsWithinCalculatorRange(intensity, method);
        }
      }
    }

    [Test]
    public void TestOutOfRangeValues() {
      EnableFactors();

      var outOfRangeContext = new CollisionIntensityContext {
        Velocity = 100f,        // Outside velocity range (1-20)
        FallDistance = 0.5f, // Outside fall distance range (2-10)
        CollisionAngle = 180f, // Outside angle range (0-90)
        CollisionType = CollisionType.PlayableElement,
        Material = baseContext.Material
      };

      // Should handle out-of-range values by ignoring those factors
      float intensity = calculator.CalculateIntensity(outOfRangeContext);

      Assert.That(float.IsNaN(intensity), Is.False, "Should handle out-of-range values without NaN");
      Assert.That(float.IsInfinity(intensity), Is.False, "Should handle out-of-range values without infinity");
    }

    #endregion

    #region Material Property Tests

    [Test]
    public void TestMaterialProperties_AllCombinations() {
      var frictionLevels = System.Enum.GetValues(typeof(FrictionLevel)).Cast<FrictionLevel>();
      var bouncinessLevels = System.Enum.GetValues(typeof(BouncinessLevel)).Cast<BouncinessLevel>();

      EnableFactors();

      foreach (var friction in frictionLevels) {
        foreach (var bounciness in bouncinessLevels) {
          var materialContext = CloneContextWithNewMaterial(baseContext, new MaterialProperties {
            Friction = friction,
            Bounciness = bounciness,
            HasCustomProperties = true
          });

          float intensity = calculator.CalculateIntensity(materialContext);
          AssertIntensityIsWithinCalculatorRange(intensity, calculator.CalculationMethod);
        }
      }
    }

    [Test]
    public void TestMaterialPropertiesDisabled() {
      var contextWithoutMaterial = CloneContextWithNewMaterial(baseContext, new MaterialProperties {
        Friction = baseContext.Material.Friction,
        Bounciness = baseContext.Material.Bounciness,
        HasCustomProperties = false
      });
      EnableFactors();
      float withoutMaterial = calculator.CalculateIntensity(contextWithoutMaterial);
      AssertIntensityIsWithinCalculatorRange(withoutMaterial, calculator.CalculationMethod);
    }

    #endregion

    #region Collision Type Tests

    [Test]
    public void TestCollisionTypes() {
      var collisionTypes = System.Enum.GetValues(typeof(CollisionType)).Cast<CollisionType>();

      EnableFactors();

      foreach (var collisionType in collisionTypes) {
        var context = CloneContext(baseContext);
        context.CollisionType = collisionType;

        float intensity = calculator.CalculateIntensity(context);

        // Should produce valid intensity for all collision types
        AssertIntensityIsWithinCalculatorRange(intensity, calculator.CalculationMethod);

        // LevelGridBound should use default multiplier (handled in material calculation)
        if (collisionType == CollisionType.LevelGridBound) {
          // Verify it doesn't crash and produces reasonable result
          Assert.That(float.IsNaN(intensity), Is.False);
        }
      }
    }

    #endregion

    #region Helper Methods

    private void SetCalculationMethodAndFactors(IntensityCalculationMethod method, FactorCombination factors) {
      SetCalculationMethod(method);
      SetFactorCombination(factors);
    }

    private void SetCalculationMethod(IntensityCalculationMethod method) {
      calculator.CalculationMethod = method;
      testableCalculator.CalculationMethod = method;
    }

    private void SetFactorCombination(FactorCombination factors) {
      calculator.ConsiderVelocity = factors.Velocity;
      calculator.ConsiderFallDistance = factors.FallDistance;
      calculator.ConsiderAngle = factors.Angle;
      calculator.ConsiderFriction = factors.Friction;
      calculator.ConsiderBounciness = factors.Bounciness;

      testableCalculator.ConsiderVelocity = factors.Velocity;
      testableCalculator.ConsiderFallDistance = factors.FallDistance;
      testableCalculator.ConsiderAngle = factors.Angle;
      testableCalculator.ConsiderFriction = factors.Friction;
      testableCalculator.ConsiderBounciness = factors.Bounciness;
    }

    private void EnableFactors(bool enable = true) {
      calculator.ConsiderVelocity = enable;
      calculator.ConsiderFallDistance = enable;
      calculator.ConsiderAngle = enable;
      calculator.ConsiderFriction = enable;
      calculator.ConsiderBounciness = enable;

      testableCalculator.ConsiderVelocity = enable;
      testableCalculator.ConsiderFallDistance = enable;
      testableCalculator.ConsiderAngle = enable;
      testableCalculator.ConsiderFriction = enable;
      testableCalculator.ConsiderBounciness = enable;
    }

    private CollisionIntensityContext CloneContext(CollisionIntensityContext original) {
      return new CollisionIntensityContext {
        Velocity = original.Velocity,
        FallDistance = original.FallDistance,
        CollisionAngle = original.CollisionAngle,
        CollisionType = original.CollisionType,
        Material = new MaterialProperties {
          Friction = original.Material.Friction,
          Bounciness = original.Material.Bounciness,
          HasCustomProperties = original.Material.HasCustomProperties,
          PhysicsMaterial = original.Material.PhysicsMaterial
        }
      };
    }

    private CollisionIntensityContext CloneContextWithNewMaterial(CollisionIntensityContext original, MaterialProperties newMaterial) {
      return new CollisionIntensityContext {
        Velocity = original.Velocity,
        FallDistance = original.FallDistance,
        CollisionAngle = original.CollisionAngle,
        CollisionType = original.CollisionType,
        Material = new MaterialProperties {
          Friction = newMaterial.Friction,
          Bounciness = newMaterial.Bounciness,
          HasCustomProperties = newMaterial.HasCustomProperties,
          PhysicsMaterial = newMaterial.PhysicsMaterial
        }
      };
    }

    private CollisionIntensityContext CreateExtremeContext() {
      return new CollisionIntensityContext {
        Velocity = MAX_TESTING_VALUE,
        FallDistance = MAX_TESTING_VALUE,
        CollisionAngle = MAX_TESTING_VALUE,
        CollisionType = CollisionType.PlayableElement,
        Material = new MaterialProperties {
          Friction = FrictionLevel.High,
          Bounciness = BouncinessLevel.High,
          HasCustomProperties = true
        }
      };
    }

    private Dictionary<string, CollisionIntensityContext> CreateEdgeContexts() {
      return new Dictionary<string, CollisionIntensityContext> {
        ["MinValues"] = new CollisionIntensityContext {
          Velocity = MIN_TESTING_VALUE,
          FallDistance = MIN_TESTING_VALUE,
          CollisionAngle = MIN_TESTING_VALUE,
          CollisionType = CollisionType.PlayableElement,
          Material = new MaterialProperties {
            Friction = FrictionLevel.Low,
            Bounciness = BouncinessLevel.Low,
            HasCustomProperties = true
          }
        },
        ["MaxValues"] = CreateExtremeContext(),
        ["ZeroSpeed"] = new CollisionIntensityContext {
          Velocity = 0f,
          FallDistance = baseContext.FallDistance,
          CollisionAngle = baseContext.CollisionAngle,
          CollisionType = baseContext.CollisionType,
          Material = baseContext.Material
        }
      };
    }


    #endregion
  }

  #region Helper Classes

  public class MarbleCollisionIntensityCalculatorWrapper :
     MarbleCollisionIntensityCalculator {

    public void CopyProperties(MarbleCollisionIntensityCalculator calculator) {
      //copy properties
      this.CalculationMethod = calculator.CalculationMethod;
      this.collisionVelocityRange = calculator.collisionVelocityRange;
      this.fallDistanceRange = calculator.FallDistanceRange;
      this.CollisionIntensityRange = calculator.CollisionIntensityRange;

      this.minAngle = calculator.minAngle;
      this.maxAngle = calculator.maxAngle;

      this.collisionVelocityCurve = new AnimationCurve(calculator.collisionVelocityCurve.keys);
      this.fallDistanceCurve = new AnimationCurve(calculator.fallDistanceCurve.keys);
      this.angleCurve = new AnimationCurve(calculator.angleCurve.keys);

      this.ConsiderVelocity = calculator.ConsiderVelocity;
      this.ConsiderFallDistance = calculator.ConsiderFallDistance;
      this.ConsiderAngle = calculator.ConsiderAngle;
      this.ConsiderFriction = calculator.ConsiderFriction;
      this.ConsiderBounciness = calculator.ConsiderBounciness;
    }

    public float CalculateFallIntensityWrapper(float fallDistance) => CalculateFallIntensity(fallDistance);

    public float CalculateVelocityIntensityWrapper(float velocity) => CalculateVelocityIntensity(velocity);

    public float CalculateAngleIntensityWrapper(float angle) => CalculateAngleIntensity(angle);

    public float CalculateMaterialMultiplierWrapper(CollisionIntensityContext context) => CalculateMaterialMultiplier(context);

    public float IntensityByCalculationMethodWrapper(IntensityFactors factors) => IntensityByCalculationMethod(factors);

  }

  /// <summary>
  /// Helper class to represent a combination of factor states.
  /// </summary>
  public class FactorCombination {
    public bool Velocity { get; set; } = true;
    public bool FallDistance { get; set; } = true;
    public bool Angle { get; set; } = true;
    public bool Friction { get; set; } = true;
    public bool Bounciness { get; set; } = true;

    public static FactorCombination AllEnabled() => new();

    public static FactorCombination AllDisabled()
      => new(false, false, false, false, false);

    public FactorCombination() { }

    public FactorCombination(bool velocity, bool fallDistance, bool angle, bool friction, bool bounciness) {
      Velocity = velocity;
      FallDistance = fallDistance;
      Angle = angle;
      Friction = friction;
      Bounciness = bounciness;
    }

    public bool HasAnyEnabled() {
      return Velocity || FallDistance || Angle || Friction || Bounciness;
    }

    public FactorCombination WithVelocity(bool enable = true) {
      Velocity = enable;
      return this;
    }

    public FactorCombination WithFallDistance(bool enable = true) {
      FallDistance = enable;
      return this;
    }

    public FactorCombination WithAngle(bool enable = true) {
      Angle = enable;
      return this;
    }

    public FactorCombination WithFriction(bool enable = true) {
      Friction = enable;
      return this;
    }

    public FactorCombination WithBounciness(bool enable = true) {
      Bounciness = enable;
      return this;
    }

    public FactorCombination WithValues(bool velocity = true, bool fallDistance = true, bool angle = true, bool friction = true, bool bounciness = true) {
      Velocity = velocity;
      FallDistance = fallDistance;
      Angle = angle;
      Friction = friction;
      Bounciness = bounciness;
      return this;
    }

    public FactorCombination DisableAll() => WithValues(false, false, false, false, false);

    public FactorCombination EnableAll() => WithValues(true, true, true, true, true);

    public override string ToString() {
      return $"V:{Velocity} F:{FallDistance} A:{Angle} Fr:{Friction} B:{Bounciness}";
    }
  }
  #endregion
}