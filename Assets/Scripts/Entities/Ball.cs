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
    
    // Freeze system
    private float lifeTimer = 0f;
    private float maxLifeTime = 3f;      // Limit czasu dla tej piłki
    private int currentBounces = 0;      // Licznik odbić
    private int maxBounces = 4;          // Limit odbić dla tej piłki
    private bool isFrozen = false;
    private bool canFreeze = true;
    private Color ballColor;
    
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
        if (isFrozen || settings == null || !canFreeze) return;
        
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
        
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Static;
        }
        
        // Przyciemnij kolor zamrożonej kulki (zachowaj pełną alpha)
        Color dimmedColor = ballColor * 0.8f;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(dimmedColor.r, dimmedColor.g, dimmedColor.b, 1f);
        }
        
        // Zaktualizuj trail effect - ustaw zamrożenie i kolor
        if (trailEffect != null)
        {
            trailEffect.SetFrozen(true);
            trailEffect.UpdateColor(dimmedColor);
        }
        
        OnFrozen?.Invoke(this);
    }
    
    /// <summary>
    /// Wyłącza timer zamrażania - piłka nie zamrozi się automatycznie.
    /// </summary>
    public void DisableFreezeTimer()
    {
        canFreeze = false;
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
        
        float relativeVelocity = collision.relativeVelocity.magnitude;
        OnBounce?.Invoke(relativeVelocity);
        
        // Sprawdzanie limitu odbić (tylko w trybie Bounces)
        if (settings != null && settings.freezeMode == FreezeMode.Bounces)
        {
            currentBounces++;
            
            if (currentBounces >= maxBounces)
            {
                Freeze();
            }
        }
    }
}
