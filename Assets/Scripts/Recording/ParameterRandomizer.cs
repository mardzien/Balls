using UnityEngine;

/// <summary>
/// Zakres wartości do randomizacji.
/// </summary>
[System.Serializable]
public class FloatRange
{
    public float min;
    public float max;
    public bool enabled = true;
    
    public FloatRange(float min, float max, bool enabled = true)
    {
        this.min = min;
        this.max = max;
        this.enabled = enabled;
    }
    
    public float GetRandom()
    {
        return Random.Range(min, max);
    }
}

/// <summary>
/// Randomizuje parametry gry przed każdą rundą.
/// Pozwala na tworzenie różnorodnych nagrań automatycznie.
/// </summary>
public class ParameterRandomizer : MonoBehaviour
{
    [Header("Shape Type")]
    [Tooltip("Włącz losowanie typu kształtu")]
    [SerializeField] private bool randomizeShapeType = true;
    
    [Tooltip("Dozwolone typy kształtów")]
    [SerializeField] private bool allowRing = true;
    [SerializeField] private bool allowEllipse = true;
    
    [Header("Trail Style")]
    [Tooltip("Włącz losowanie stylu ogonka")]
    [SerializeField] private bool randomizeTrailStyle = true;
    
    [Tooltip("Dozwolone style ogonków")]
    [SerializeField] private bool allowComet = true;
    [SerializeField] private bool allowFadingTrail = true;
    [SerializeField] private bool allowThinUniform = true;
    [SerializeField] private bool allowNoTrail = false;
    
    [Header("Gap Movement")]
    [SerializeField] private FloatRange rotationSpeed = new FloatRange(30f, 90f);
    
    [Header("Gap")]
    [SerializeField] private FloatRange gapAngleDegrees = new FloatRange(20f, 45f);
    
    [Header("Physics")]
    [SerializeField] private FloatRange gravity = new FloatRange(-25f, -15f);
    [SerializeField] private FloatRange bounciness = new FloatRange(0.7f, 1.0f, false); // Disabled - always use 1.0 for perfect bounce
    
    [Header("Ring Size")]
    [SerializeField] private FloatRange ringRadius = new FloatRange(4.0f, 5.0f);
    
    [Header("Ellipse Size")]
    [SerializeField] private FloatRange ellipseWidthRadius = new FloatRange(3.5f, 4.5f);
    [SerializeField] private FloatRange ellipseHeightRadius = new FloatRange(6f, 8f);
    
    [Header("Ball")]
    [SerializeField] private FloatRange ballRadius = new FloatRange(0.2f, 0.35f, false); // Disabled by default
    
    [Header("Trail Effect")]
    [SerializeField] private FloatRange trailTime = new FloatRange(0.15f, 0.4f, false); // Disabled by default
    [SerializeField] private FloatRange trailWidthMultiplier = new FloatRange(0.6f, 1.2f, false); // Disabled by default
    
    [Header("Debug")]
    [SerializeField] private bool logRandomization = true;
    
    /// <summary>
    /// Randomizuje wszystkie włączone parametry w GameSettings.
    /// </summary>
    public void RandomizeAll(GameSettings settings)
    {
        if (settings == null)
        {
            Debug.LogError("[ParameterRandomizer] GameSettings is null!");
            return;
        }
        
        System.Text.StringBuilder log = new System.Text.StringBuilder();
        log.AppendLine("[ParameterRandomizer] Randomized parameters:");
        
        // Shape Type
        if (randomizeShapeType)
        {
            settings.shapeType = GetRandomShapeType();
            log.AppendLine($"  - shapeType: {settings.shapeType}");
        }
        
        // Trail Style
        if (randomizeTrailStyle)
        {
            settings.trailStyle = GetRandomTrailStyle();
            log.AppendLine($"  - trailStyle: {settings.trailStyle}");
        }
        
        // Rotation Speed
        if (rotationSpeed.enabled)
        {
            settings.rotationSpeed = rotationSpeed.GetRandom();
            log.AppendLine($"  - rotationSpeed: {settings.rotationSpeed:F1}");
        }
        
        // Gap Angle
        if (gapAngleDegrees.enabled)
        {
            settings.gapAngleDegrees = gapAngleDegrees.GetRandom();
            log.AppendLine($"  - gapAngleDegrees: {settings.gapAngleDegrees:F1}");
        }
        
        // Gravity
        if (gravity.enabled)
        {
            settings.gravity = gravity.GetRandom();
            Physics2D.gravity = new Vector2(0, settings.gravity);
            log.AppendLine($"  - gravity: {settings.gravity:F1}");
        }
        
        // Bounciness
        if (bounciness.enabled)
        {
            settings.bounciness = bounciness.GetRandom();
            log.AppendLine($"  - bounciness: {settings.bounciness:F2}");
        }
        
        // Ring Radius
        if (ringRadius.enabled)
        {
            settings.ringRadius = ringRadius.GetRandom();
            log.AppendLine($"  - ringRadius: {settings.ringRadius:F2}");
        }
        
        // Ellipse Dimensions
        if (ellipseWidthRadius.enabled)
        {
            settings.ellipseWidthRadius = ellipseWidthRadius.GetRandom();
            log.AppendLine($"  - ellipseWidthRadius: {settings.ellipseWidthRadius:F2}");
        }
        
        if (ellipseHeightRadius.enabled)
        {
            settings.ellipseHeightRadius = ellipseHeightRadius.GetRandom();
            log.AppendLine($"  - ellipseHeightRadius: {settings.ellipseHeightRadius:F2}");
        }
        
        // Ball Radius
        if (ballRadius.enabled)
        {
            settings.ballRadius = ballRadius.GetRandom();
            log.AppendLine($"  - ballRadius: {settings.ballRadius:F2}");
        }
        
        // Trail Time
        if (trailTime.enabled)
        {
            settings.trailTime = trailTime.GetRandom();
            log.AppendLine($"  - trailTime: {settings.trailTime:F2}");
        }
        
        // Trail Width Multiplier
        if (trailWidthMultiplier.enabled)
        {
            settings.trailWidthMultiplier = trailWidthMultiplier.GetRandom();
            log.AppendLine($"  - trailWidthMultiplier: {settings.trailWidthMultiplier:F2}");
        }
        
        // Mark settings as dirty for editor
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(settings);
#endif
        
        if (logRandomization)
        {
            Debug.Log(log.ToString());
        }
    }
    
    private ShapeType GetRandomShapeType()
    {
        var allowedTypes = new System.Collections.Generic.List<ShapeType>();
        
        if (allowRing) allowedTypes.Add(ShapeType.Ring);
        if (allowEllipse) allowedTypes.Add(ShapeType.Ellipse);
        
        if (allowedTypes.Count == 0)
        {
            Debug.LogWarning("[ParameterRandomizer] No shape types allowed! Defaulting to Ring.");
            return ShapeType.Ring;
        }
        
        return allowedTypes[Random.Range(0, allowedTypes.Count)];
    }
    
    private TrailStyle GetRandomTrailStyle()
    {
        var allowedStyles = new System.Collections.Generic.List<TrailStyle>();
        
        if (allowNoTrail) allowedStyles.Add(TrailStyle.None);
        if (allowComet) allowedStyles.Add(TrailStyle.Comet);
        if (allowFadingTrail) allowedStyles.Add(TrailStyle.FadingTrail);
        if (allowThinUniform) allowedStyles.Add(TrailStyle.ThinUniform);
        
        if (allowedStyles.Count == 0)
        {
            Debug.LogWarning("[ParameterRandomizer] No trail styles allowed! Defaulting to FadingTrail.");
            return TrailStyle.FadingTrail;
        }
        
        return allowedStyles[Random.Range(0, allowedStyles.Count)];
    }
    
    /// <summary>
    /// Resetuje wszystkie zakresy do wartości domyślnych.
    /// </summary>
    [ContextMenu("Reset to Defaults")]
    public void ResetToDefaults()
    {
        randomizeShapeType = true;
        allowRing = true;
        allowEllipse = true;
        
        randomizeTrailStyle = true;
        allowComet = true;
        allowFadingTrail = true;
        allowThinUniform = true;
        allowNoTrail = false;
        
        rotationSpeed = new FloatRange(30f, 90f);
        gapAngleDegrees = new FloatRange(20f, 45f);
        gravity = new FloatRange(-25f, -15f);
        bounciness = new FloatRange(0.7f, 1.0f, false); // Disabled - always use 1.0 for perfect bounce
        ringRadius = new FloatRange(4.0f, 5.0f);
        ellipseWidthRadius = new FloatRange(3.5f, 4.5f);
        ellipseHeightRadius = new FloatRange(6f, 8f);
        ballRadius = new FloatRange(0.2f, 0.35f, false);
        trailTime = new FloatRange(0.15f, 0.4f, false);
        trailWidthMultiplier = new FloatRange(0.6f, 1.2f, false);
        
        Debug.Log("[ParameterRandomizer] Reset to defaults.");
    }
}
