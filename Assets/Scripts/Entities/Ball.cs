using UnityEngine;

/// <summary>
/// Kulka z fizyką 2D - spada pod wpływem grawitacji i odbija się od pierścienia.
/// Po określonym czasie zamraża się (staje statyczna).
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Ball : MonoBehaviour
{
    private Rigidbody2D rb;
    private CircleCollider2D circleCollider;
    private SpriteRenderer spriteRenderer;
    private GameSettings settings;
    
    // Freeze system
    private float lifeTimer = 0f;
    private bool isFrozen = false;
    private bool canFreeze = true;
    private Color ballColor;
    
    /// <summary>
    /// Event wywoływany przy kolizji z pierścieniem.
    /// </summary>
    public event System.Action OnBounce;
    
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
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        circleCollider = GetComponent<CircleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    private void Update()
    {
        if (isFrozen || settings == null || !canFreeze) return;
        
        lifeTimer += Time.deltaTime;
        
        if (lifeTimer >= settings.ballFreezeTime)
        {
            Freeze();
        }
    }
    
    /// <summary>
    /// Inicjalizuje kulkę z podanymi ustawieniami.
    /// </summary>
    public void Initialize(GameSettings gameSettings, bool randomColor = true)
    {
        settings = gameSettings;
        isFrozen = false;
        canFreeze = true;
        lifeTimer = 0f;
        
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (circleCollider == null) circleCollider = GetComponent<CircleCollider2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        
        SetupPhysics();
        SetupVisuals(randomColor);
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
    
    private void SetupVisuals(bool randomColor)
    {
        float diameter = settings.ballRadius * 2f;
        transform.localScale = new Vector3(diameter, diameter, 1f);
        
        ballColor = (randomColor && settings.useRandomBallColors) 
            ? GenerateRandomBrightColor() 
            : settings.ballColor;
            
        spriteRenderer.color = ballColor;
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
        if (spriteRenderer != null)
        {
            Color dimmed = ballColor * 0.8f;
            spriteRenderer.color = new Color(dimmed.r, dimmed.g, dimmed.b, 1f);
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
        OnBounce?.Invoke();
    }
}
