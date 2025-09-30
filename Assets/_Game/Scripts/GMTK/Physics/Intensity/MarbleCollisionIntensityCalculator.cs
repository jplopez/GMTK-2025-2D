using UnityEngine;
using MoreMountains.Tools;

namespace GMTK {
  /// <summary>
  /// MonoBehaviour implementation of IIntensityCalculator specifically designed for marble collision intensity calculations.
  /// This component handles all the configuration and logic for determining collision feedback intensity.
  /// </summary>
  [AddComponentMenu("GMTK/Marble/Marble Collision Intensity Calculator")]
  public class MarbleCollisionIntensityCalculator : MonoBehaviour, IIntensityCalculator {

    public enum IntensityCalculationMethod {
      Minimum,       // Use the lowest factor
      Maximum,       // Use the highest factor
      Average,       // Average of all factors
      Additive,      // Sum of all factors
      Multiplicative // Product of all factors
    }

    [Header("Calculation Settings")]
    [MMInformation("Limit the min and max values this calculator can provide. For example, (0.5, 1) makes all collisions at least half as intense, while (0, 0.5) will tone down all collisions to half their intensity. You can change this value at runtime. Values are inclusive", MMInformationAttribute.InformationType.Info, false)]
    [Tooltip("Range of min and max intensity value")]
    public Vector2 CollisionIntensityRange = new(0f, 1f);
    [Tooltip("Method used to combine different intensity factors into a final intensity value")]
    public IntensityCalculationMethod CalculationMethod = IntensityCalculationMethod.Average;

    [Space(10)]


    //[MMInspectorGroup("Velocity Range", true, 11)]
    [Header("Velocity Range")]
    [MMInformation("Here you can define a range of velocity where the Calculator operates. For values outside the range, this factor will not affect the calculation", MMInformationAttribute.InformationType.Info, false)]

    [Tooltip("If true, the intensity calculation will consider collision velocity")]
    public bool ConsiderVelocity = true;
    [Tooltip("Collision velocity range for intensity calculation (x = min, y = max). Values are inclusive")]
    [MMCondition("ConsiderVelocity", true)]
    public Vector2 collisionVelocityRange = new(1f, 20f);
    [Tooltip("Curve defining the collision velocity intensity factor")]
    [MMCondition("ConsiderVelocity", true)]
    public AnimationCurve collisionVelocityCurve = new(
      new Keyframe(0f, 0.1f),    // Low velocity = 0.1x intensity
      new Keyframe(0.5f, 0.5f),    // Mid velocity = 0.5x intensity
      new Keyframe(1f, 1.2f)     // High velocity = 1.2x intensity
    );

    //[MMInspectorGroup("Fall Distance Range", true, 3)]
    [Header("Fall Distance Range")]

    [MMInformation("Here you can define a fall distance where the Calculator operates. For values outside the range, this factor will not affect the calculation", MMInformationAttribute.InformationType.Info, false)]

    [Tooltip("If true, the intensity calculation will consider fall distance")]
    public bool ConsiderFallDistance = true;
    [Tooltip("Fall distance range for intensity calculation (x = min, y = max). Values are inclusive")]
    [MMCondition("ConsiderFallDistance", true)]
    public Vector2 fallDistanceRange = new(2f, 10f);
    [Tooltip("Curve defining the fall distance intensity factor")]
    [MMCondition("ConsiderFallDistance", true)]
    public AnimationCurve fallDistanceCurve = new(
      new Keyframe(0f, 0.1f),    // Low distance = 0.1x intensity
      new Keyframe(0.5f, 0.5f),    // Mid distance = 0.5x intensity
      new Keyframe(1f, 1.2f)     // High distance = 1.2x intensity
    );

    [Header("Collision Angle")]

    [MMInformation("Adjusts how much the collision angle affects intensity. For example 0 = no effect, 1 = full effect (perpendicular collisions are more intense)", MMInformationAttribute.InformationType.Info, false)]
    [Tooltip("If true, the intensity calculation will consider the collision angle")]
    public bool ConsiderAngle = true;

    [MMInformation("Here you can define a collision angle where the Calculator operates. For values outside the range, this factor will not affect the calculation", MMInformationAttribute.InformationType.Info, false)]
    [Tooltip("Minimum angle value (inclusive) where this factor applies")]
    [MMCondition("ConsiderAngle", true)]
    public float minAngle = 0f;
    [Tooltip("Maximum angle value (inclusive) where this factor applies")]
    [MMCondition("ConsiderAngle", true)]
    public float maxAngle = 90f;
    [Tooltip("Curve defining the collision angle intensity factor")]
    [MMCondition("ConsiderAngle", true)]
    public AnimationCurve angleCurve = new(
      new Keyframe(0f, 0.1f),    // Low angle = 0.1x intensity
      new Keyframe(0.5f, 0.5f),    // Mid angle = 0.5x intensity
      new Keyframe(1f, 1.2f)     // High angle = 1.2x intensity
    );

    [Range(0f, 1f)]
    [Tooltip("Multiplier for collision angle effect (0 = no effect, 1 = full effect)")]
    public float angleIntensityMultiplier = 0.3f;

    [Header("Material Properties")]
    [MMInformation("Use these curves to define how friction and bounciness levels affect collision intensity. X-axis represents the material level (0=Low, 0.5=Mid, 1=High), Y-axis represents the intensity multiplier", MMInformationAttribute.InformationType.Info, false)]

    [Tooltip("If true, the intensity calculation will consider material friction")]
    public bool ConsiderFriction = true;
    [Tooltip("Curve defining friction intensity multipliers (X: 0=Low, 0.5=Mid, 1=High, Y: intensity multiplier)")]
    [MMCondition("ConsiderFriction", true)]
    public AnimationCurve frictionIntensityCurve = new(
      new Keyframe(0f, 0.9f),    // Low friction = 0.9x intensity
      new Keyframe(0.5f, 1f),    // Mid friction = 1x intensity  
      new Keyframe(1f, 1.2f)     // High friction = 1.2x intensity
    );
    [Space]

    [Tooltip("If true, the intensity calculation will consider material bounciness")]
    public bool ConsiderBounciness = true;
    [Tooltip("Curve defining bounciness intensity multipliers (X: 0=Low, 0.5=Mid, 1=High, Y: intensity multiplier)")]
    [MMCondition("ConsiderBounciness", true)]
    public AnimationCurve bouncinessIntensityCurve = new(
      new Keyframe(0f, 0.8f),    // Low bounciness = 0.8x intensity
      new Keyframe(0.5f, 1f),    // Mid bounciness = 1x intensity
      new Keyframe(1f, 1.3f)     // High bounciness = 1.3x intensity
    );

    [Space(10)]

    //[MMInspectorGroup("Debug", true, 25)]
    [Header("Debug")]
    [Tooltip("Enable detailed logging of intensity calculations")]
    public bool enableDebugLogging = false;



    // Interface properties - updated to use Vector2 ranges
    public float MinCollisionVelocity => collisionVelocityRange.x;
    public float MaxCollisionVelocity => collisionVelocityRange.y;
    public float MinFallDistance => fallDistanceRange.x;
    public float MaxFallDistance => fallDistanceRange.y;
    public float AngleIntensityMultiplier => angleIntensityMultiplier;
   

    // New properties for the ranges
    public Vector2 CollisionVelocityRange => collisionVelocityRange;
    public Vector2 FallDistanceRange => fallDistanceRange;
    public AnimationCurve FrictionIntensityCurve => frictionIntensityCurve;
    public AnimationCurve BouncinessIntensityCurve => bouncinessIntensityCurve;

    private void OnValidate() {
      // Ensure valid velocity range
      if (collisionVelocityRange.y <= collisionVelocityRange.x) {
        collisionVelocityRange.y = collisionVelocityRange.x + 1f;
      }
      collisionVelocityRange.x = Mathf.Max(0.1f, collisionVelocityRange.x);

      // Ensure valid fall distance range
      if (fallDistanceRange.y <= fallDistanceRange.x) {
        fallDistanceRange.y = fallDistanceRange.x + 1f;
      }
      fallDistanceRange.x = Mathf.Max(0.1f, fallDistanceRange.x);

      // Ensure valid angle range
      if (maxAngle <= minAngle) {
        maxAngle = minAngle + 1f;
      }
      minAngle = Mathf.Clamp(minAngle, 0f, 180f);
      maxAngle = Mathf.Clamp(maxAngle, minAngle, 180f);

      // Ensure valid intensity range
      CollisionIntensityRange.x = Mathf.Clamp01(CollisionIntensityRange.x);
      CollisionIntensityRange.y = Mathf.Clamp01(CollisionIntensityRange.y);
      if (CollisionIntensityRange.y < CollisionIntensityRange.x) {
        CollisionIntensityRange.y = CollisionIntensityRange.x;
      }

      // Validate curves have proper keyframes
      ValidateCurve(frictionIntensityCurve, "Friction");
      ValidateCurve(bouncinessIntensityCurve, "Bounciness");
      ValidateCurve(collisionVelocityCurve, "Collision Velocity");
      ValidateCurve(fallDistanceCurve, "Fall Distance");
      ValidateCurve(angleCurve, "Angle");
    }

    /// <summary>
    /// Validates that curves have keyframes in the expected range
    /// </summary>
    private void ValidateCurve(AnimationCurve curve, string curveName) {
      if (curve == null || curve.keys.Length == 0) {
        this.LogWarning($"{curveName} curve is empty or null. Please add keyframes.");
        return;
      }

      // For material curves, ensure curve covers the 0-1 range for material levels
      if (curveName.Contains("Friction") || curveName.Contains("Bounciness")) {
        bool hasLow = false, hasMid = false, hasHigh = false;
        foreach (var key in curve.keys) {
          if (Mathf.Approximately(key.time, 0f)) hasLow = true;
          if (Mathf.Approximately(key.time, 0.5f)) hasMid = true;
          if (Mathf.Approximately(key.time, 1f)) hasHigh = true;
        }

        if (!hasLow || !hasMid || !hasHigh) {
          this.LogWarning($"{curveName} curve should have keyframes at 0 (Low), 0.5 (Mid), and 1 (High) for proper material level mapping.");
        }
      }
    }

    /// <summary>
    /// Calculates collision intensity based on velocity, fall distance, angle, and material properties.
    /// </summary>
    /// <param name="context">The collision context containing all relevant data</param>
    /// <returns>A normalized intensity value between CollisionIntensityRange.x and CollisionIntensityRange.y</returns>
    public float CalculateIntensity(IntensityContext context) {
      var marbleContext = context as CollisionIntensityContext;

      IntensityFactors factors = new() {
        VelocityFactor = 1f,
        FallFactor = 1f,
        AngleFactor = 1f,
        MaterialFactor = 1f
      };

      string logStr = "Intensity Calculation: ";

      // Apply velocity factor if enabled and within range
      if (ConsiderVelocity && IsWithinVelocityRange(marbleContext.Velocity)) {
        float speedFactor = CalculateVelocityIntensity(marbleContext.Velocity);
        factors.VelocityFactor = speedFactor;
        logStr += $"Velocity={speedFactor:F3}, ";
      }

      // Apply fall distance factor if enabled and within range
      if (ConsiderFallDistance && IsWithinFallDistanceRange(marbleContext.FallDistance)) {
        float fallFactor = CalculateFallIntensity(marbleContext.FallDistance);
        factors.FallFactor = fallFactor;
        logStr += $"Fall={fallFactor:F3}, ";
      }

      // Apply angle factor if enabled and within range
      if (ConsiderAngle && IsWithinAngleRange(marbleContext.CollisionAngle)) {
        float angleFactor = CalculateAngleIntensity(marbleContext.CollisionAngle);
        factors.AngleFactor = angleFactor;
        logStr += $"Angle={angleFactor:F3}, ";
      }

      // Apply material factor if enabled
      float materialFactor = CalculateMaterialMultiplier(marbleContext);
      factors.MaterialFactor = materialFactor;
      logStr += $"Material={materialFactor:F3}, ";

      // Apply intensity range remapping
      float finalIntensity = IntensityByCalculationMethod(factors);

      this.Log(logStr + $"Factors={factors:F3}, Final={finalIntensity:F3}");
      if (enableDebugLogging) {
        this.LogDebug(logStr + $"Factors={factors:F3}, Final={finalIntensity:F3}");
      }

      return finalIntensity;
    }

    public bool TryCalculateIntensity(IntensityContext context, out float intensity) {
      if (context is CollisionIntensityContext) {
        intensity = CalculateIntensity(context);
        return true;
      }
      intensity = 0f;
      return false;
    }

    /// <summary>
    /// Calculates the intensity based on the selected calculation method and factors passed in the <see cref="IntensityFactors"/> struct
    /// </summary>
    /// <param name="factors"></param>
    /// <returns></returns>
    protected virtual float IntensityByCalculationMethod(IntensityFactors factors, bool clampWithinRange=true) {

      float intensity = 0f;
      var values = new float[4];
      switch (CalculationMethod) {
        case IntensityCalculationMethod.Minimum:
          if (ConsiderVelocity) values[0] = factors.VelocityFactor; else values[0] = float.MaxValue;
          if (ConsiderFallDistance) values[1] = factors.FallFactor; else values[1] = float.MaxValue;
          if (ConsiderAngle) values[2] = factors.AngleFactor; else values[2] = float.MaxValue;
          if (ConsiderBounciness || ConsiderFriction) values[3] = factors.MaterialFactor; else values[3] = float.MaxValue;
          intensity = Mathf.Min(values);
          break;
        case IntensityCalculationMethod.Maximum:
          
          if (ConsiderVelocity) values[0] = factors.VelocityFactor; else values[0] = float.MinValue;
          if (ConsiderFallDistance) values[1] = factors.FallFactor; else values[1] = float.MinValue;
          if (ConsiderAngle) values[2] = factors.AngleFactor; else values[2] = float.MinValue;
          if (ConsiderBounciness || ConsiderFriction) values[3] = factors.MaterialFactor; else values[3] = float.MinValue;

          intensity = Mathf.Max(values);
          break;

        case IntensityCalculationMethod.Average:
          int i = 0;
          if (ConsiderVelocity) values[i++] = factors.VelocityFactor; 
          if (ConsiderFallDistance) values[i++] = factors.FallFactor;
          if (ConsiderAngle) values[i++] = factors.AngleFactor; 
          if (ConsiderBounciness || ConsiderFriction) values[i++] = factors.MaterialFactor;

          intensity = Average(values);
          break;

        case IntensityCalculationMethod.Additive:
          if (ConsiderVelocity) values[0] = factors.VelocityFactor; else { values[0] = 0; }
          if (ConsiderFallDistance) values[1] = factors.FallFactor; else { values[1] = 0; }
          if (ConsiderAngle) values[2] = factors.AngleFactor; else { values[2] = 0;  }
          if (ConsiderBounciness || ConsiderFriction) values[3] = factors.MaterialFactor; else { values[3] = 0; }

          intensity = factors.VelocityFactor + factors.FallFactor + factors.AngleFactor + factors.MaterialFactor;
          break;

        case IntensityCalculationMethod.Multiplicative:
          if (ConsiderVelocity) values[0] = factors.VelocityFactor; else { values[0] = 1; }
          if (ConsiderFallDistance) values[1] = factors.FallFactor; else { values[1] = 1; }
          if (ConsiderAngle) values[2] = factors.AngleFactor; else { values[2] = 1; }
          if (ConsiderBounciness || ConsiderFriction) values[3] = factors.MaterialFactor; else { values[3] = 1; }

          intensity = factors.VelocityFactor * factors.FallFactor * factors.AngleFactor * factors.MaterialFactor;
          break;

        default:
          intensity = 1f;
          break;

      }
      // Remap intensity to configured range
      if (clampWithinRange) {
        intensity = Mathf.Clamp(intensity, CollisionIntensityRange.x, CollisionIntensityRange.y);
      }
      return intensity;
    }

    protected float Average(float[] values) {
      if (values is null) return 0f;
      float sum = 0f;
      foreach (var v in values) {
        sum += v;
      }
      return sum / values.Length;
    }

    /// <summary>
    /// Checks if the velocity is within the configured velocity range.
    /// </summary>
    protected bool IsWithinVelocityRange(float speed) {
      return speed >= collisionVelocityRange.x && speed <= collisionVelocityRange.y;
    }

    /// <summary>
    /// Checks if the fall distance is within the configured range.
    /// </summary>
    protected bool IsWithinFallDistanceRange(float fallDistance) {
      return fallDistance >= fallDistanceRange.x && fallDistance <= fallDistanceRange.y;
    }

    /// <summary>
    /// Checks if the collision angle is within the configured range.
    /// </summary>
    protected bool IsWithinAngleRange(float angle) {
      return angle >= minAngle && angle <= maxAngle;
    }

    /// <summary>
    /// Calculates the velocity-based intensity factor using the velocity curve.
    /// </summary>
    protected float CalculateVelocityIntensity(float speed) {
      float normalizedVelocity = Mathf.InverseLerp(collisionVelocityRange.x, collisionVelocityRange.y, speed);
      return collisionVelocityCurve?.Evaluate(normalizedVelocity) ?? 1f;
    }

    /// <summary>
    /// Calculates the fall distance intensity factor using the fall distance curve.
    /// </summary>
    protected float CalculateFallIntensity(float fallDistance) {
      float normalizedDistance = Mathf.InverseLerp(fallDistanceRange.x, fallDistanceRange.y, fallDistance);
      return fallDistanceCurve?.Evaluate(normalizedDistance) ?? 1f;
    }

    /// <summary>
    /// Calculates the collision angle intensity factor using the angle curve.
    /// </summary>
    protected float CalculateAngleIntensity(float collisionAngle) {
      float normalizedAngle = Mathf.InverseLerp(minAngle, maxAngle, collisionAngle);
      float curveValue = angleCurve?.Evaluate(normalizedAngle) ?? 1f;

      // Apply the angle intensity multiplier
      return Mathf.Lerp(1f, curveValue, angleIntensityMultiplier);
    }

    /// <summary>
    /// Calculates the material-based intensity multiplier using curves.
    /// </summary>
    protected float CalculateMaterialMultiplier(CollisionIntensityContext context) {
      // For level grid bounds, use default multiplier
      if (context.CollisionType == CollisionType.LevelGridBound) {
        return 1f;
      }

      // Use material properties if available
      if (!context.Material.HasCustomProperties) {
        return 1f;
      }

      float materialMultiplier = 1f;

      // Apply friction multiplier if enabled
      if (ConsiderFriction) {
        float frictionMultiplier = GetFrictionMultiplierFromCurve(context.Material.Friction);
        materialMultiplier *= frictionMultiplier;
      }

      // Apply bounciness multiplier if enabled
      if (ConsiderBounciness) {
        float bouncinessMultiplier = GetBouncinessMultiplierFromCurve(context.Material.Bounciness);
        materialMultiplier *= bouncinessMultiplier;
      }

      return materialMultiplier;
    }

    /// <summary>
    /// Gets the friction-based intensity multiplier from the curve.
    /// </summary>
    private float GetFrictionMultiplierFromCurve(FrictionLevel friction) {
      if (!ConsiderFriction) return 1f;

      float curveInput = friction switch {
        FrictionLevel.Low => 0f,
        FrictionLevel.Mid => 0.5f,
        FrictionLevel.High => 1f,
        _ => 0.5f
      };

      return frictionIntensityCurve?.Evaluate(curveInput) ?? 1f;
    }

    /// <summary>
    /// Gets the bounciness-based intensity multiplier from the curve.
    /// </summary>
    private float GetBouncinessMultiplierFromCurve(BouncinessLevel bounciness) {
      if (!ConsiderBounciness) return 1f;

      float curveInput = bounciness switch {
        BouncinessLevel.Low => 0f,
        BouncinessLevel.Mid => 0.5f,
        BouncinessLevel.High => 1f,
        _ => 0.5f
      };

      return bouncinessIntensityCurve?.Evaluate(curveInput) ?? 1f;
    }

    /// <summary>
    /// Public method to update collision velocity range at runtime.
    /// </summary>
    public void SetCollisionVelocityRange(Vector2 range) {
      collisionVelocityRange.x = Mathf.Max(0.1f, range.x);
      collisionVelocityRange.y = Mathf.Max(collisionVelocityRange.x + 0.1f, range.y);
    }

    /// <summary>
    /// Public method to update collision velocity thresholds at runtime.
    /// </summary>
    public void SetCollisionVelocityRange(float min, float max) {
      SetCollisionVelocityRange(new Vector2(min, max));
    }

    /// <summary>
    /// Public method to update fall distance range at runtime.
    /// </summary>
    public void SetFallDistanceRange(Vector2 range) {
      fallDistanceRange.x = Mathf.Max(0.1f, range.x);
      fallDistanceRange.y = Mathf.Max(fallDistanceRange.x + 0.1f, range.y);
    }

    /// <summary>
    /// Public method to update fall distance thresholds at runtime.
    /// </summary>
    public void SetFallDistanceRange(float min, float max) {
      SetFallDistanceRange(new Vector2(min, max));
    }

    /// <summary>
    /// Public method to update angle range at runtime.
    /// </summary>
    public void SetAngleRange(float min, float max) {
      minAngle = Mathf.Clamp(min, 0f, 180f);
      maxAngle = Mathf.Clamp(max, minAngle, 180f);
    }

    /// <summary>
    /// Public method to update intensity remapping range at runtime.
    /// </summary>
    public void SetIntensityRange(Vector2 range) {
      CollisionIntensityRange.x = Mathf.Clamp01(range.x);
      CollisionIntensityRange.y = Mathf.Clamp01(Mathf.Max(range.y, CollisionIntensityRange.x));
    }

    /// <summary>
    /// Sets a new friction intensity curve.
    /// </summary>
    public void SetFrictionIntensityCurve(AnimationCurve curve) {
      frictionIntensityCurve = curve ?? frictionIntensityCurve;
      ValidateCurve(frictionIntensityCurve, "Friction");
    }

    /// <summary>
    /// Sets a new bounciness intensity curve.
    /// </summary>
    public void SetBouncinessIntensityCurve(AnimationCurve curve) {
      bouncinessIntensityCurve = curve ?? bouncinessIntensityCurve;
      ValidateCurve(bouncinessIntensityCurve, "Bounciness");
    }

    /// <summary>
    /// Resets all curves to default values.
    /// </summary>
    [ContextMenu("Reset All Curves to Default")]
    public void ResetAllCurvesToDefault() {
      ResetVelocityCurveToDefault();
      ResetFallDistanceCurveToDefault();
      ResetAngleCurveToDefault();
      ResetFrictionCurveToDefault();
      ResetBouncinessCurveToDefault();
      this.Log("All curves reset to default values.");
    }

    [ContextMenu("Reset Velocity Curve to Default")]
    public void ResetVelocityCurveToDefault() {
      collisionVelocityCurve = new AnimationCurve(
        new Keyframe(0f, 0.1f),
        new Keyframe(0.5f, 0.5f),
        new Keyframe(1f, 1.2f)
      );
    }

    [ContextMenu("Reset Fall Distance Curve to Default")]
    public void ResetFallDistanceCurveToDefault() {
      fallDistanceCurve = new AnimationCurve(
        new Keyframe(0f, 0.1f),
        new Keyframe(0.5f, 0.5f),
        new Keyframe(1f, 1.2f)
      );
    }

    [ContextMenu("Reset Angle Curve to Default")]
    public void ResetAngleCurveToDefault() {
      angleCurve = new AnimationCurve(
        new Keyframe(0f, 0.1f),
        new Keyframe(0.5f, 0.5f),
        new Keyframe(1f, 1.2f)
      );
    }

    /// <summary>
    /// Resets friction curve to default values.
    /// </summary>
    [ContextMenu("Reset Friction Curve to Default")]
    public void ResetFrictionCurveToDefault() {
      frictionIntensityCurve = new AnimationCurve(
        new Keyframe(0f, 0.9f),    // Low friction = 0.9x intensity
        new Keyframe(0.5f, 1f),    // Mid friction = 1x intensity  
        new Keyframe(1f, 1.2f)     // High friction = 1.2x intensity
      );
      this.Log("Friction curve reset to default values.");
    }

    /// <summary>
    /// Resets bounciness curve to default values.
    /// </summary>
    [ContextMenu("Reset Bounciness Curve to Default")]
    public void ResetBouncinessCurveToDefault() {
      bouncinessIntensityCurve = new AnimationCurve(
        new Keyframe(0f, 0.8f),    // Low bounciness = 0.8x intensity
        new Keyframe(0.5f, 1f),    // Mid bounciness = 1x intensity
        new Keyframe(1f, 1.3f)     // High bounciness = 1.3x intensity
      );
      this.Log("Bounciness curve reset to default values.");
    }

    #region Debug Helpers

    [ContextMenu("Test Intensity Calculation")]
    private void TestIntensityCalculation() {
      var testContext = new CollisionIntensityContext {
        Velocity = 5f,
        FallDistance = 3f,
        CollisionAngle = 45f,
        CollisionType = CollisionType.PlayableElement,
        Material = new MaterialProperties {
          Friction = FrictionLevel.Mid,
          Bounciness = BouncinessLevel.Mid,
          HasCustomProperties = true
        }
      };

      float intensity = CalculateIntensity(testContext);
      this.Log($"Test Intensity Calculation Result: {intensity:F4}");

      // Test factor inclusion/exclusion
      this.Log("=== Factor Inclusion Tests ===");
      this.Log($"Consider Velocity: {ConsiderVelocity} (Velocity {testContext.Velocity} in range [{collisionVelocityRange.x}, {collisionVelocityRange.y}]: {IsWithinVelocityRange(testContext.Velocity)})");
      this.Log($"Consider Fall Distance: {ConsiderFallDistance} (Distance {testContext.FallDistance} in range [{fallDistanceRange.x}, {fallDistanceRange.y}]: {IsWithinFallDistanceRange(testContext.FallDistance)})");
      this.Log($"Consider Angle: {ConsiderAngle} (Angle {testContext.CollisionAngle} in range [{minAngle}, {maxAngle}]: {IsWithinAngleRange(testContext.CollisionAngle)})");
      this.Log($"Consider Friction: {ConsiderFriction}");
      this.Log($"Consider Bounciness: {ConsiderBounciness}");

      // Test with different material levels
      this.Log("=== Material Multiplier Tests ===");
      foreach (FrictionLevel friction in System.Enum.GetValues(typeof(FrictionLevel))) {
        float frictionMult = GetFrictionMultiplierFromCurve(friction);
        this.Log($"Friction {friction}: {frictionMult:F3}x");
      }

      foreach (BouncinessLevel bounciness in System.Enum.GetValues(typeof(BouncinessLevel))) {
        float bouncinessMult = GetBouncinessMultiplierFromCurve(bounciness);
        this.Log($"Bounciness {bounciness}: {bouncinessMult:F3}x");
      }
    }

    [ContextMenu("Test Range Calculations")]
    private void TestRangeCalculations() {
      this.Log("=== Range Calculation Tests ===");

      // Test velocity intensity at various velocities
      float[] testVelocities = { 0f, collisionVelocityRange.x, (collisionVelocityRange.x + collisionVelocityRange.y) / 2f, collisionVelocityRange.y, collisionVelocityRange.y * 2f };
      foreach (float velocity in testVelocities) {
        bool inRange = IsWithinVelocityRange(velocity);
        float intensity = inRange ? CalculateVelocityIntensity(velocity) : 1f;
        this.Log($"Velocity {velocity:F1}: In Range={inRange}, Intensity={intensity:F3}");
      }

      // Test fall distance multiplier
      float[] testDistances = { 0f, fallDistanceRange.x, (fallDistanceRange.x + fallDistanceRange.y) / 2f, fallDistanceRange.y, fallDistanceRange.y * 2f };
      foreach (float distance in testDistances) {
        bool inRange = IsWithinFallDistanceRange(distance);
        float intensity = inRange ? CalculateFallIntensity(distance) : 1f;
        this.Log($"Fall Distance {distance:F1}: In Range={inRange}, Intensity={intensity:F3}");
      }

      // Test collision angles
      float[] testAngles = { 0f, minAngle, (minAngle + maxAngle) / 2f, maxAngle, 180f };
      foreach (float angle in testAngles) {
        bool inRange = IsWithinAngleRange(angle);
        float intensity = inRange ? CalculateAngleIntensity(angle) : 1f;
        this.Log($"Angle {angle:F1}: In Range={inRange}, Intensity={intensity:F3}");
      }
    }

    public struct IntensityFactors {
      public float VelocityFactor;
      public float FallFactor;
      public float AngleFactor;
      public float MaterialFactor;

      public override readonly string ToString() {
        return $"Velocity: {VelocityFactor:F3}, Fall: {FallFactor:F3}, Angle: {AngleFactor:F3}, Material: {MaterialFactor:F3}";
      }
    }

    #endregion
  }
}