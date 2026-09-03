using UnityEngine;

/// <summary>
/// Kulka z fizyką 2D - spada pod wpływem grawitacji i odbija się od pierścienia.
/// Po określonym czasie zamraża się (staje statyczna).
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(TrailRenderer))]
[RequireComponent(typeof(BallTrailEffect))]
public class Ball : MonoBehaviour
{
    private Rigidbody2D rb;
    private CircleCollider2D circleCollider;
    private SpriteRenderer spriteRenderer;
    private BallTrailEffect trailEffect;
    private GameSettings settings;
    private SpawnShape shape;
    
    // Freeze system
    private float lifeTimer = 0f;
    private float maxLifeTime = 3f;      // Limit czasu dla tej piłki
    private int currentBounces = 0;      // Licznik odbić
    private int maxBounces = 4;          // Limit odbić dla tej piłki
    private bool isFrozen = false;
    private bool canFreeze = true;
    private Color ballColor;
    private float frozenAngle;
    private float frozenRadiusRatio = 1f;
    private bool hasFrozenOrbitData = false;
    
    // Freeze visual effect
    private GameObject freezeEffectObj;
    private SpriteRenderer freezeEffectRenderer;
    
    /// <summary>
    /// Event wywoływany przy kolizji z pierścieniem.
    /// Parametr: prędkość względna kolizji (magnitude).
    /// </summary>
    public event System.Action<float> OnBounce;
    
    /// <summary>
    /// Event wywoływany gdy kulka się zamrozi.
    /// </summary>
    public event System.Action<Ball> OnFrozen;
    
    /// <summary>
    /// Czy kulka jest zamrożona.
    /// </summary>
    public bool IsFrozen => isFrozen;
    
    /// <summary>
    /// Kolor tej kulki.
    /// </summary>
    public Color BallColor => ballColor;
    
    /// <summary>
    /// Pozostała liczba odbić (dla trybu Bounces).
    /// </summary>
    public int RemainingBounces => Mathf.Max(0, maxBounces - currentBounces);
    
    /// <summary>
    /// Pozostały czas (dla trybu Time).
    /// </summary>
    public float RemainingTime => Mathf.Max(0f, maxLifeTime - lifeTimer);
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        circleCollider = GetComponent<CircleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        trailEffect = GetComponent<BallTrailEffect>();
    }
    
    private void Update()
    {
        if (settings == null) return;

        if (isFrozen)
        {
            UpdateFrozenOrbit();
            return;
        }

        if (!canFreeze) return;
        
        // Sprawdzanie limitu czasu (tylko w trybie Time)
        if (settings.freezeMode == FreezeMode.Time)
        {
            lifeTimer += Time.deltaTime;
            
            if (lifeTimer >= maxLifeTime)
            {
                Freeze();
            }
        }
    }
    
    /// <summary>
    /// Inicjalizuje kulkę z podanymi ustawieniami (losowy kolor).
    /// </summary>
    public void Initialize(GameSettings gameSettings)
    {
        Initialize(gameSettings, null, gameSettings.ballFreezeTime, gameSettings.ballMaxBounces);
    }
    
    /// <summary>
    /// Inicjalizuje kulkę z podanymi ustawieniami i opcjonalnym kolorem.
    /// </summary>
    /// <param name="gameSettings">Ustawienia gry</param>
    /// <param name="specificColor">Konkretny kolor (null = losowy)</param>
    public void Initialize(GameSettings gameSettings, Color? specificColor)
    {
        Initialize(gameSettings, specificColor, gameSettings.ballFreezeTime, gameSettings.ballMaxBounces);
    }
    
    /// <summary>
    /// Inicjalizuje kulkę z podanymi ustawieniami, kolorem i limitami zamrażania.
    /// </summary>
    /// <param name="gameSettings">Ustawienia gry</param>
    /// <param name="specificColor">Konkretny kolor (null = losowy)</param>
    /// <param name="freezeTime">Limit czasu dla tej piłki</param>
    /// <param name="maxBouncesLimit">Limit odbić dla tej piłki</param>
    public void Initialize(GameSettings gameSettings, Color? specificColor, float freezeTime, int maxBouncesLimit)
    {
        settings = gameSettings;
        isFrozen = false;
        canFreeze = true;
        lifeTimer = 0f;
        maxLifeTime = freezeTime;
        currentBounces = 0;
        maxBounces = maxBouncesLimit;
        hasFrozenOrbitData = false;
        
        ClearFreezeEffect();
        
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (circleCollider == null) circleCollider = GetComponent<CircleCollider2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        
        SetupPhysics();
        SetupVisuals(specificColor);
    }
    
    private void SetupPhysics()
    {
        // Jednostkowe koło - skala obiektu definiuje rzeczywisty rozmiar
        circleCollider.radius = 0.5f;
        
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = settings.gravity / -9.81f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        
        PhysicsMaterial2D material = new PhysicsMaterial2D("BallMaterial")
        {
            bounciness = settings.bounciness,
            friction = settings.friction
        };
        circleCollider.sharedMaterial = material;
        
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }
    
    private void SetupVisuals(Color? specificColor = null)
    {
        float diameter = settings.ballRadius * 2f;
        transform.localScale = new Vector3(diameter, diameter, 1f);
        
        // Use specific color if provided, otherwise generate random
        ballColor = specificColor ?? GenerateRandomBrightColor();
        spriteRenderer.color = ballColor;
        
        // Setup trail effect
        SetupTrailEffect();
    }
    
    private void SetupTrailEffect()
    {
        if (trailEffect == null)
        {
            trailEffect = GetComponent<BallTrailEffect>();
        }
        
        if (trailEffect != null)
        {
            float trailWidth = settings.ballRadius * settings.trailWidthMultiplier;
            trailEffect.Initialize(settings.trailStyle, ballColor, settings.trailTime, trailWidth);
            trailEffect.SetFrozen(false);
            trailEffect.ClearTrail();
            trailEffect.StartEmitDelay(0.15f);
        }
    }
    
    private Color GenerateRandomBrightColor()
    {
        float hue = Random.Range(0f, 1f);
        float saturation = Random.Range(settings.ballColorMinSaturation, 1f);
        float value = Random.Range(settings.ballColorMinBrightness, 1f);
        return Color.HSVToRGB(hue, saturation, value);
    }
    
    /// <summary>
    /// Zamraża kulkę - zatrzymuje fizykę.
    /// </summary>
    public void Freeze()
    {
        if (isFrozen) return;
        
        isFrozen = true;
        CacheFrozenOrbit();
        
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        
        ApplyFreezeVisuals();
        
        // Zaktualizuj trail effect - ustaw zamrożenie i kolor
        if (trailEffect != null)
        {
            trailEffect.SetFrozen(true);
            trailEffect.SetEmitting(false);
            trailEffect.ClearTrail();
            trailEffect.UpdateColor(spriteRenderer != null ? spriteRenderer.color : ballColor);
        }
        
        OnFrozen?.Invoke(this);
    }
    
    /// <summary>
    /// Wyłącza timer zamrażania - piłka nie zamrozi się automatycznie.
    /// </summary>
    public void DisableFreezeTimer()
    {
        canFreeze = false;
        currentBounces = 0;
    }

    /// <summary>
    /// Przypisuje aktualny kształt spawnu (Ring/Ellipse).
    /// </summary>
    public void SetShape(SpawnShape spawnShape)
    {
        shape = spawnShape;
    }
    
    /// <summary>
    /// Oblicza kąt pozycji kulki względem podanego centrum w stopniach.
    /// </summary>
    public float GetAngleFromCenter(Vector2 center)
    {
        Vector2 pos = (Vector2)transform.position - center;
        return Mathf.Atan2(pos.y, pos.x) * Mathf.Rad2Deg;
    }
    
    /// <summary>
    /// Oblicza odległość kulki od podanego centrum.
    /// </summary>
    public float GetDistanceFromCenter(Vector2 center)
    {
        return ((Vector2)transform.position - center).magnitude;
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isFrozen) return;
        
        if (canFreeze && settings != null && settings.enableInstantFreezeOnShapeContact &&
            collision.collider != null && collision.collider.GetComponentInParent<SpawnShape>() != null)
        {
            PlayFreezeCollisionSfx();
            Freeze();
            return;
        }
        
        float relativeVelocity = collision.relativeVelocity.magnitude;
        OnBounce?.Invoke(relativeVelocity);
        
        // Sprawdzanie limitu odbić (tylko w trybie Bounces)
        if (canFreeze && settings != null && settings.freezeMode == FreezeMode.Bounces)
        {
            currentBounces++;
            
            if (currentBounces >= maxBounces)
            {
                Freeze();
            }
        }
    }

    private void PlayFreezeCollisionSfx()
    {
        if (settings == null || settings.freezeCollisionClip == null) return;
        
        AudioSource.PlayClipAtPoint(
            settings.freezeCollisionClip,
            transform.position,
            settings.freezeCollisionVolume
        );
    }

    private void CacheFrozenOrbit()
    {
        if (shape == null) return;
        
        Vector2 center = shape.Center;
        Vector2 offset = (Vector2)transform.position - center;
        frozenAngle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        
        float radiusAtAngle = shape.GetRadiusAtAngle(frozenAngle);
        float distance = offset.magnitude;
        frozenRadiusRatio = radiusAtAngle > 0f ? Mathf.Clamp01(distance / radiusAtAngle) : 1f;
        hasFrozenOrbitData = true;
    }

    private void UpdateFrozenOrbit()
    {
        if (shape == null || settings == null || !hasFrozenOrbitData) return;
        if (!settings.enableFrozenBallRotation) return;
        
        frozenAngle += settings.rotationSpeed * Time.deltaTime;
        frozenAngle %= 360f;
        
        float radiusAtAngle = shape.GetRadiusAtAngle(frozenAngle);
        float distance = radiusAtAngle * frozenRadiusRatio;
        float angleRad = frozenAngle * Mathf.Deg2Rad;
        
        Vector2 targetPos = shape.Center + new Vector2(
            Mathf.Cos(angleRad) * distance,
            Mathf.Sin(angleRad) * distance
        );
        
        if (rb != null)
        {
            rb.position = targetPos;
        }
        else
        {
            transform.position = targetPos;
        }
    }

    private void ApplyFreezeVisuals()
    {
        if (spriteRenderer == null) return;
        
        if (settings != null && settings.enableFreezeEffect)
        {
            Color frozenColor = Color.Lerp(ballColor, Color.gray, 0.7f);
            spriteRenderer.color = new Color(frozenColor.r, frozenColor.g, frozenColor.b, 1f);
            EnsureFreezeEffect();
        }
        else
        {
            Color dimmedColor = ballColor * 0.8f;
            spriteRenderer.color = new Color(dimmedColor.r, dimmedColor.g, dimmedColor.b, 1f);
        }
    }

    private void EnsureFreezeEffect()
    {
        if (freezeEffectObj != null) return;
        
        freezeEffectObj = new GameObject("FreezeEffect");
        freezeEffectObj.transform.SetParent(transform, false);
        freezeEffectObj.transform.localPosition = Vector3.zero;
        freezeEffectObj.transform.localScale = Vector3.one * 1.15f;
        
        freezeEffectRenderer = freezeEffectObj.AddComponent<SpriteRenderer>();
        freezeEffectRenderer.sprite = SpriteUtility.GetIceRingSprite();
        freezeEffectRenderer.color = new Color(0.8f, 0.95f, 1f, 0.85f);
        if (spriteRenderer != null)
        {
            freezeEffectRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
        }
    }

    private void ClearFreezeEffect()
    {
        if (freezeEffectObj != null)
        {
            Destroy(freezeEffectObj);
            freezeEffectObj = null;
            freezeEffectRenderer = null;
        }
    }
}
