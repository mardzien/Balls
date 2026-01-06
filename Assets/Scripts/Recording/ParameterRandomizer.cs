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
    [Header("Game Mode")]
    [Tooltip("Włącz losowanie trybu gry")]
    [SerializeField] private bool randomizeGameMode = false;
    
    [Tooltip("Dozwolone tryby gry")]
    [SerializeField] private bool allowNormal = true;
    [SerializeField] private bool allowBattle = true;
    
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
    
    [Header("Shape Color")]
    [Tooltip("Włącz losowanie koloru kształtu (pierścienia/elipsy)")]
    [SerializeField] private bool randomizeShapeColor = true;
    
    [Header("Gap Movement")]
    [SerializeField] private FloatRange rotationSpeed = new FloatRange(-50f, 50f);
    
    [Header("Gap")]
    [SerializeField] private FloatRange gapAngleDegrees = new FloatRange(20f, 45f);
    [SerializeField] private FloatRange gapInitialAngle = new FloatRange(0f, 360f, true);
    
    [Header("Ball")]
    [SerializeField] private FloatRange ballRadius = new FloatRange(0.2f, 0.35f, true); // Disabled by default
    
    [Header("Trail Effect")]
    [SerializeField] private FloatRange trailTime = new FloatRange(0.1f, 0.4f, true); // Disabled by default
    [SerializeField] private FloatRange trailWidthMultiplier = new FloatRange(0.2f, 0.8f, true); // Disabled by default
    
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
        
        // Game Mode
        if (randomizeGameMode)
        {
            settings.gameMode = GetRandomGameMode();
            log.AppendLine($"  - gameMode: {settings.gameMode}");
        }
        
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
        
        // Shape Color
        if (randomizeShapeColor)
        {
            settings.ringColor = GenerateRandomBrightColor(
                settings.shapeColorMinBrightness, 
                settings.shapeColorMinSaturation);
            log.AppendLine($"  - shapeColor: {ColorUtility.ToHtmlStringRGB(settings.ringColor)}");
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
        
        // Gap Initial Angle (position)
        if (gapInitialAngle.enabled)
        {
            settings.gapInitialAngle = gapInitialAngle.GetRandom();
            log.AppendLine($"  - gapInitialAngle: {settings.gapInitialAngle:F1}");
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
    
    private GameMode GetRandomGameMode()
    {
        var allowedModes = new System.Collections.Generic.List<GameMode>();
        
        if (allowNormal) allowedModes.Add(GameMode.Normal);
        if (allowBattle) allowedModes.Add(GameMode.Battle);
        
        if (allowedModes.Count == 0)
        {
            Debug.LogWarning("[ParameterRandomizer] No game modes allowed! Defaulting to Normal.");
            return GameMode.Normal;
        }
        
        return allowedModes[Random.Range(0, allowedModes.Count)];
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
    /// Generuje losowy jasny kolor używając przestrzeni HSV.
    /// </summary>
    private Color GenerateRandomBrightColor(float minBrightness, float minSaturation)
    {
        float hue = Random.Range(0f, 1f);
        float saturation = Random.Range(minSaturation, 1f);
        float brightness = Random.Range(minBrightness, 1f);
        
        return Color.HSVToRGB(hue, saturation, brightness);
    }
    
    /// <summary>
    /// Resetuje wszystkie zakresy do wartości domyślnych.
    /// </summary>
    [ContextMenu("Reset to Defaults")]
    public void ResetToDefaults()
    {
        randomizeGameMode = false;
        allowNormal = true;
        allowBattle = true;
        
        randomizeShapeType = true;
        allowRing = true;
        allowEllipse = true;
        
        randomizeTrailStyle = true;
        allowComet = true;
        allowFadingTrail = true;
        allowThinUniform = true;
        allowNoTrail = false;
        
        randomizeShapeColor = true;
        
        rotationSpeed = new FloatRange(30f, 90f);
        gapAngleDegrees = new FloatRange(20f, 45f);
        gapInitialAngle = new FloatRange(0f, 360f, true);
        ballRadius = new FloatRange(0.2f, 0.35f, true);
        trailTime = new FloatRange(0.15f, 0.4f, false);
        trailWidthMultiplier = new FloatRange(0.6f, 1.2f, false);
        
        Debug.Log("[ParameterRandomizer] Reset to defaults.");
    }
}
