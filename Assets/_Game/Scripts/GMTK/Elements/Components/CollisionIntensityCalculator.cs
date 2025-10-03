using GMTK;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;

[AddComponentMenu("GMTK/Collision Intensity Calculator Component")]
public class CollisionIntensityCalculator : MonoBehaviour, IIntensityCalculator {

  [Header("General Settings")]
  public Vector2 IntensityRange = new(0, 1);

  [Tooltip("When not using a Fixed intensity. If this field is true, the default intensity will be the max intensity range value. If false, it will be the min intensity range value.")]
  public bool UseMaxRangeAsDefault = true;

  [Tooltip("Method to combine individual factor intensities into a final intensity value.")]
  public IntensityCalculationMethod CalculationMethod = IntensityCalculationMethod.Multiplicative;
  [Space]
  public bool UseFixedIntensity = false;
  [Range(0, 1)] public float FixedIntensity = 0.5f;

  [Space(10)]

  [Header("Collision Intensity Calculator Settings")]
  [Help("The following section applies only when UseFixedIntensity is false.")]
  [Space]
  public bool ConsiderMass = true;
  [MMFCondition("ConsiderMass")]
  public Vector2 MassRange = new(0f, 100f);
  [MMFCondition("ConsiderMass")]
  public AnimationCurve MassCurve = AnimationCurve.Linear(0, 0, 10, 1);
  [Space]
  public bool ConsiderMaterial = true;
  [MMFCondition("ConsiderMaterial")]
  public AnimationCurve FrictionCurve = AnimationCurve.Linear(0, 0, 1, 1);
  [MMFCondition("ConsiderMaterial")]
  public AnimationCurve BouncinessCurve = AnimationCurve.Linear(0, 0, 1, 1);
  [Space]
  public bool ConsiderAngle = true;
  [MMFCondition("ConsiderAngle")]
  [Range(0f, 90f)]
  public float MinAngle = 0f;
  [MMFCondition("ConsiderAngle")]
  [Range(0f, 90f)]
  public float MaxAngle = 90f;
  [MMFCondition("ConsiderAngle")]
  public AnimationCurve AngleCurve = AnimationCurve.Linear(0, 0, 180, 1);
  [Space]
  public bool ConsiderMarbleIntensity = true;
  [MMFCondition("ConsiderMarbleIntensity")]
  public AnimationCurve MarbleIntensityCurve = AnimationCurve.Linear(0, 0, 1, 1);

  public float MinIntensity => IntensityRange.x;  
  public float MaxIntensity => IntensityRange.y;

  public float DefaultIntensity { get {
      if (UseFixedIntensity) return FixedIntensity;
      return UseMaxRangeAsDefault ? IntensityRange.y : IntensityRange.x;
    } }

  protected PlayableElement _playableElement;
  protected PhysicsElementComponent _physicsElementComponent;
  protected PhysicalMaterialsElementComponent _physicalMaterialsElementComponent;
  protected Rigidbody2D _rigidbody2D;

  private CollisionIntensityContext _lastCalculatedContext;
  private float _lastCalculatedIntensity;

  #region Intensity Calculation

  public float CalculateIntensity(IntensityContext context) {
    if (UseFixedIntensity) {
      return Mathf.Clamp(FixedIntensity, MinIntensity, MaxIntensity);
    }

    if (context is not CollisionIntensityContext collisionContext) {
      this.LogWarning($"Invalid context type. Expected CollisionIntensityContext, got {context?.GetType().Name ?? "null"}");
      return DefaultIntensity;
    }

    // Calculate individual factor intensities
    float massIntensity = CalculateMassIntensity(collisionContext);
    float materialIntensity = CalculateMaterialIntensity(collisionContext);
    float angleIntensity = CalculateAngleIntensity(collisionContext);
    float marbleIntensity = CalculateMarbleIntensity(collisionContext);

    // Combine intensities based on calculation method
    float finalIntensity = CombineIntensities(massIntensity, materialIntensity, angleIntensity, marbleIntensity);

    // Clamp to intensity range
    return Mathf.Clamp(finalIntensity, MinIntensity, MaxIntensity);
  }

  public bool TryCalculateIntensity(IntensityContext context, out float intensity) {
    try {
      intensity = CalculateIntensity(context);
      return true;
    }
    catch (System.Exception ex) {
      Debug.LogError($"[CollisionIntensityCalculator] Failed to calculate intensity: {ex.Message}");
      intensity = DefaultIntensity;
      return false;
    }
  }

  #endregion

  #region Individual Factor Calculations

  private float CalculateMassIntensity(CollisionIntensityContext context) {
    //if not considered, send max value
    if (!ConsiderMass) return DefaultIntensity;

    float rawMass = context.Mass;
    float mass = Mathf.Clamp(rawMass, MassRange.x, MassRange.y);
    return MassCurve.Evaluate(mass);
  }

  private float CalculateMaterialIntensity(CollisionIntensityContext context) {
    if (!ConsiderMaterial) return DefaultIntensity;

    float materialIntensity = 1f;

    // Try to get material properties from the context first
    if (context.Material.HasCustomProperties) {
      float frictionValue = context.Material.Friction.NormalizedValue();
      float bouncinessValue = context.Material.Bounciness.NormalizedValue();

      float frictionIntensity = FrictionCurve.Evaluate(frictionValue);
      float bouncinessIntensity = BouncinessCurve.Evaluate(bouncinessValue);

      // Combine friction and bounciness using multiplicative method
      materialIntensity = frictionIntensity * bouncinessIntensity;
    }
    // Fallback to component's material properties
    else if (_physicalMaterialsElementComponent != null) {
      float frictionValue = _physicalMaterialsElementComponent.Friction.NormalizedValue();
      float bouncinessValue = _physicalMaterialsElementComponent.Bounciness.NormalizedValue();

      float frictionIntensity = FrictionCurve.Evaluate(frictionValue);
      float bouncinessIntensity = BouncinessCurve.Evaluate(bouncinessValue);

      materialIntensity = frictionIntensity * bouncinessIntensity;
    }

    return materialIntensity;
  }

  private float CalculateAngleIntensity(CollisionIntensityContext context) {
    if (!ConsiderAngle) return DefaultIntensity;

    float angle = context.CollisionAngle;
    // Clamp angle to valid range (0-180 degrees)
    angle = Mathf.Clamp(angle, 0f, 180f);
    return AngleCurve.Evaluate(angle);
  }

  private float CalculateMarbleIntensity(CollisionIntensityContext context) {
    if (!ConsiderMarbleIntensity) return DefaultIntensity;

    // Use material intensity multiplier from context if available
    float marbleIntensity = context.MaterialIntensityMultiplier;

    // Clamp to 0-1 range for curve evaluation
    marbleIntensity = Mathf.Clamp01(marbleIntensity);
    return MarbleIntensityCurve.Evaluate(marbleIntensity);
  }

  #endregion

  #region Intensity Combination Methods

  private float CombineIntensities(float mass, float material, float angle, float marble) {
    return CalculationMethod switch {
      IntensityCalculationMethod.Minimum => CombineMinimum(mass, material, angle, marble),
      IntensityCalculationMethod.Maximum => CombineMaximum(mass, material, angle, marble),
      IntensityCalculationMethod.Average => CombineAverage(mass, material, angle, marble),
      IntensityCalculationMethod.Additive => CombineAdditive(mass, material, angle, marble),
      IntensityCalculationMethod.Multiplicative => CombineMultiplicative(mass, material, angle, marble),
      _ => CombineMultiplicative(mass, material, angle, marble),
    };
  }

  private float CombineMinimum(float mass, float material, float angle, float marble) {
    float minValue = float.MaxValue;

    if (ConsiderMass) minValue = Mathf.Min(minValue, mass);

    if (ConsiderMaterial) minValue = Mathf.Min(minValue, material);
    if (ConsiderAngle) minValue = Mathf.Min(minValue, angle);
    if (ConsiderMarbleIntensity) minValue = Mathf.Min(minValue, marble);

    return minValue == float.MaxValue ? 1f : minValue;
  }

  private float CombineMaximum(float mass, float material, float angle, float marble) {
    float maxValue = float.MinValue;

    if (ConsiderMass) maxValue = Mathf.Max(maxValue, mass);
    if (ConsiderMaterial) maxValue = Mathf.Max(maxValue, material);
    if (ConsiderAngle) maxValue = Mathf.Max(maxValue, angle);
    if (ConsiderMarbleIntensity) maxValue = Mathf.Max(maxValue, marble);

    return maxValue == float.MinValue ? 1f : maxValue;
  }

  private float CombineAverage(float mass, float material, float angle, float marble) {
    float sum = 0f;
    int count = 0;

    if (ConsiderMass) { sum += mass; count++; }
    if (ConsiderMaterial) { sum += material; count++; }
    if (ConsiderAngle) { sum += angle; count++; }
    if (ConsiderMarbleIntensity) { sum += marble; count++; }

    return count > 0 ? sum / count : 1f;
  }

  private float CombineAdditive(float mass, float material, float angle, float marble) {
    float sum = 0f;

    if (ConsiderMass) sum += mass;
    if (ConsiderMaterial) sum += material;
    if (ConsiderAngle) sum += angle;
    if (ConsiderMarbleIntensity) sum += marble;

    return sum;
  }

  private float CombineMultiplicative(float mass, float material, float angle, float marble) {
    float product = 1f;

    if (ConsiderMass) product *= mass;
    if (ConsiderMaterial) product *= material;
    if (ConsiderAngle) product *= angle;
    if (ConsiderMarbleIntensity) product *= marble;

    return product;
  }

  #endregion

  #region Factor Calculations

  private float GetMassFactor() {
    //edge cases
    if (!ConsiderMass
      || _rigidbody2D == null
      || _rigidbody2D.bodyType == RigidbodyType2D.Static
      ) return DefaultIntensity;

    //read mass from _rigidbody2D, clamp to MassRange and eval with MassCurve
    float rawMass = _rigidbody2D.mass;
    float clampedMass = Mathf.Clamp(rawMass, MassRange.x, MassRange.y);
    return MassCurve.Evaluate(clampedMass);
  }

  private float GetAngleFactor() {
    if (!ConsiderAngle
      || _rigidbody2D == null
      || !_physicsElementComponent.CanCurrentlyRotate()) return DefaultIntensity;

    // read rotation angle from PlayableElement, clamp to AngleRange and eval with AngleCurve
    float rawAngle = _playableElement.SnapTransform.rotation.eulerAngles.z;
    float angle = Mathf.Clamp(rawAngle, MinAngle, MaxAngle);
    return AngleCurve.Evaluate(angle);
  }

  private MaterialProperties GetMaterialProperties() {
    if (!ConsiderMaterial
      || _rigidbody2D == null
      || _rigidbody2D.bodyType == RigidbodyType2D.Static
      || _rigidbody2D.sharedMaterial == null
      ) return new MaterialProperties();

    //TODO evaluate if this logic makes sense, or should all validations come from the component
    if (_physicalMaterialsElementComponent == null) return new MaterialProperties();

    return new MaterialProperties() {
      Friction = _physicalMaterialsElementComponent.Friction,
      Bounciness = _physicalMaterialsElementComponent.Bounciness,
      HasCustomProperties = true
    };
  }

  #endregion

  #region MonoBehaviour

  protected void Awake() {
    // Cache component references
    if (TryGetComponent<PlayableElement>(out var pe)) {
      _playableElement = pe;

      // Get other components only if PlayableElement is present
      if (TryGetComponent<PhysicsElementComponent>(out var physComp)) {
        _physicsElementComponent = physComp;
      }
      if (TryGetComponent<PhysicalMaterialsElementComponent>(out var matComp)) {
        _physicalMaterialsElementComponent = matComp;
      }
      if (TryGetComponent<Rigidbody2D>(out var rb)) {
        _rigidbody2D = rb;
      }
    }
    else {
      this.LogError($"No PlayableElement found on Collision Calculator {name}. This component will use a fixed intensity");
      UseFixedIntensity = true;
      return;
    }

    // Component can work without dependencies, but warn if missing important ones
    if (_rigidbody2D == null && ConsiderMass) {
      this.LogWarning($"ConsiderMass is enabled but not RigidBody2D was found on {_playableElement.name}. ConsiderMass will be set to false");
      ConsiderMass = false; //disable to avoid confusion
    }

    if (_physicalMaterialsElementComponent == null && ConsiderMaterial) {
      this.LogWarning($"ConsiderMaterial is enabled but not PhysicalMaterialsElementComponent was found on {_playableElement.name}. ConsiderMaterial will be set to false");
      ConsiderMaterial = false; //disable to avoid confusion
    }

    // Validate curves
    ValidateCurves();

    this.LogDebug("CollisionIntensityCalculator initialized");
  }

  private void Update() {
    //precalculate context, but only calculate intensity at request
    if (enabled && !UseFixedIntensity) {
      CollisionIntensityContext context = new() {
        Mass = GetMassFactor(),
        CollisionAngle = GetAngleFactor(),
        Material = GetMaterialProperties(),
        //always playable element
        CollisionType = CollisionType.PlayableElement,
        //not used
        Velocity = 0f,
        FallDistance = 0f
      };
      if(!context.Equals(_lastCalculatedContext)) {
        _lastCalculatedContext = context;
      }
    }
  }

  private void ValidateCurves() {
    // Ensure all curves have valid keyframes
    if (MassCurve == null || MassCurve.keys.Length == 0) {
      MassCurve = AnimationCurve.Constant(0, 0, 1);
      this.LogWarning($"MassCurve was null or empty, reset to default on {_playableElement.name}");
    }

    if (FrictionCurve == null || FrictionCurve.keys.Length == 0) {
      FrictionCurve = AnimationCurve.Constant(0, 0, 1);
      this.LogWarning($"FrictionCurve was null or empty, reset to default on {_playableElement.name}");
    }

    if (BouncinessCurve == null || BouncinessCurve.keys.Length == 0) {
      BouncinessCurve = AnimationCurve.Constant(0, 0, 1);
      this.LogWarning($"BouncinessCurve was null or empty, reset to default on {_playableElement.name}");
    }

    if (AngleCurve == null || AngleCurve.keys.Length == 0) {
      AngleCurve = AnimationCurve.Constant(0, 0, 1);
      this.LogWarning($"AngleCurve was null or empty, reset to default on {_playableElement.name}");
    }

    if (MarbleIntensityCurve == null || MarbleIntensityCurve.keys.Length == 0) {
      MarbleIntensityCurve = AnimationCurve.Constant(0, 0, 1);
      this.LogWarning($"MarbleIntensityCurve was null or empty, reset to default on {_playableElement.name}");
    }
  }

  #endregion

  #region Public API

  public float CalculateCurrentIntensity() {
    if (UseFixedIntensity) return FixedIntensity;

    if (_lastCalculatedContext == null) {
      this.LogWarning("No collision context available to calculate intensity. Returning max intensity.");
      return IntensityRange.y;
    }
    _lastCalculatedIntensity = CalculateIntensity(_lastCalculatedContext);
    return _lastCalculatedIntensity;
  }


  /// <summary>
  /// Sets whether to use a fixed intensity value instead of calculating it.
  /// </summary>
  /// <param name="useFixed">Whether to use fixed intensity</param>
  /// <param name="fixedValue">The fixed intensity value to use (0-1)</param>
  public void SetUseFixedIntensity(bool useFixed, float fixedValue = 0.5f) {
    UseFixedIntensity = useFixed;
    FixedIntensity = Mathf.Clamp01(fixedValue);
  }

  /// <summary>
  /// Sets the intensity calculation method.
  /// </summary>
  /// <param name="method">The calculation method to use</param>
  public void SetCalculationMethod(IntensityCalculationMethod method) {
    CalculationMethod = method;
  }

  /// <summary>
  /// Sets which factors to consider in intensity calculations.
  /// </summary>
  public void SetConsiderFactors(bool mass = true, bool material = true, bool angle = true, bool marbleIntensity = true) {
    ConsiderMass = mass;
    ConsiderMaterial = material;
    ConsiderAngle = angle;
    ConsiderMarbleIntensity = marbleIntensity;
  }

  /// <summary>
  /// Sets the intensity range for the output values.
  /// </summary>
  /// <param name="min">Minimum intensity value</param>
  /// <param name="max">Maximum intensity value</param>
  public void SetIntensityRange(float min, float max) {
    IntensityRange = new Vector2(Mathf.Max(0f, min), Mathf.Max(min, max));
  }

  /// <summary>
  /// Gets the current intensity range.
  /// </summary>
  /// <returns>Vector2 with x=min, y=max intensity values</returns>
  public Vector2 GetIntensityRange() => IntensityRange;

  #endregion
}