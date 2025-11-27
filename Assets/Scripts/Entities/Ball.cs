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
    private bool canFreeze = true; // Flaga kontrolująca czy piłka może się zamrozić
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
    /// Pozostały czas do zamrożenia.
    /// </summary>
    public float TimeRemaining => settings != null ? Mathf.Max(0, settings.ballFreezeTime - lifeTimer) : 0f;
    
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
        
        // Licznik czasu życia
        lifeTimer += Time.deltaTime;
        
        // Sprawdź czy czas na zamrożenie
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
        // Ustaw promień collidera - 0.5f (jednostkowe koło), skala obiektu definiuje rzeczywisty rozmiar
        circleCollider.radius = 0.5f;
        
        // Konfiguracja Rigidbody2D
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = settings.gravity / -9.81f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        
        // Stwórz Physics Material 2D dla odbić
        PhysicsMaterial2D material = new PhysicsMaterial2D("BallMaterial")
        {
            bounciness = settings.bounciness,
            friction = settings.friction
        };
        circleCollider.sharedMaterial = material;
        
        // Zresetuj prędkość
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }
    
    private void SetupVisuals(bool randomColor)
    {
        // Ustaw skalę sprite'a na podstawie promienia
        float diameter = settings.ballRadius * 2f;
        transform.localScale = new Vector3(diameter, diameter, 1f);
        
        // Ustaw kolor
        if (randomColor && settings.useRandomBallColors)
        {
            ballColor = GenerateRandomBrightColor();
        }
        else
        {
            ballColor = settings.ballColor;
        }
        spriteRenderer.color = ballColor;
    }
    
    /// <summary>
    /// Generuje losowy jasny kolor.
    /// </summary>
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
        
        // Zatrzymaj fizykę
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Static;
        }
        
        // Lekko przyciemnij kolor zamrożonej kulki
        if (spriteRenderer != null)
        {
            spriteRenderer.color = ballColor * 0.8f;
            spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 1f);
        }
        
        OnFrozen?.Invoke(this);
    }
    
    /// <summary>
    /// Wyłącza timer zamrażania - piłka nie zamrozi się automatycznie.
    /// Używane gdy piłka uciekła przez lukę.
    /// </summary>
    public void DisableFreezeTimer()
    {
        canFreeze = false;
    }
    
    /// <summary>
    /// Odmraża kulkę - przywraca fizykę.
    /// </summary>
    public void Unfreeze()
    {
        if (!isFrozen) return;
        
        isFrozen = false;
        lifeTimer = 0f;
        
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = ballColor;
        }
    }
    
    /// <summary>
    /// Resetuje pozycję i prędkość kulki.
    /// </summary>
    public void ResetBall(Vector2 position)
    {
        transform.position = new Vector3(position.x, position.y, 0f);
        isFrozen = false;
        lifeTimer = 0f;
        
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
    
    /// <summary>
    /// Zwraca aktualną pozycję kulki (względem Ring center).
    /// </summary>
    public Vector2 GetPosition()
    {
        return transform.position;
    }
    
    /// <summary>
    /// Ustawia pozycję kulki względem podanego centrum.
    /// </summary>
    public void SetPositionRelativeTo(Vector2 center, Vector2 offset)
    {
        transform.position = new Vector3(center.x + offset.x, center.y + offset.y, 0f);
    }
    
    /// <summary>
    /// Zwraca aktualną prędkość kulki.
    /// </summary>
    public Vector2 GetVelocity()
    {
        return rb != null ? rb.linearVelocity : Vector2.zero;
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
    
    // Legacy methods for compatibility
    public float GetAngleFromCenter() => GetAngleFromCenter(Vector2.zero);
    public float GetDistanceFromCenter() => GetDistanceFromCenter(Vector2.zero);
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isFrozen) return;
        OnBounce?.Invoke();
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isFrozen ? Color.blue : Color.cyan;
        Vector2 pos = transform.position;
        Gizmos.DrawLine(pos, Vector2.zero);
    }
#endif
}
