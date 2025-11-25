using UnityEngine;

/// <summary>
/// Kulka z fizyką 2D - spada pod wpływem grawitacji i odbija się od pierścienia.
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
    
    /// <summary>
    /// Event wywoływany przy kolizji z pierścieniem.
    /// </summary>
    public event System.Action OnBounce;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        circleCollider = GetComponent<CircleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    /// <summary>
    /// Inicjalizuje kulkę z podanymi ustawieniami.
    /// </summary>
    public void Initialize(GameSettings gameSettings)
    {
        settings = gameSettings;
        
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (circleCollider == null) circleCollider = GetComponent<CircleCollider2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        
        SetupPhysics();
        SetupVisuals();
    }
    
    private void SetupPhysics()
    {
        // Ustaw promień collidera
        circleCollider.radius = settings.ballRadius;
        
        // Konfiguracja Rigidbody2D
        rb.gravityScale = settings.gravity / -9.81f; // Normalizuj względem domyślnej grawitacji Unity
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
    
    private void SetupVisuals()
    {
        // Ustaw skalę sprite'a na podstawie promienia
        // Domyślny sprite Circle ma średnicę 1, więc skalujemy do 2*radius
        float diameter = settings.ballRadius * 2f;
        transform.localScale = new Vector3(diameter, diameter, 1f);
        
        // Ustaw kolor
        spriteRenderer.color = settings.ballColor;
    }
    
    /// <summary>
    /// Resetuje pozycję i prędkość kulki.
    /// </summary>
    public void ResetBall(Vector2 position)
    {
        transform.position = new Vector3(position.x, position.y, 0f);
        
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
    
    /// <summary>
    /// Zwraca aktualną pozycję kulki.
    /// </summary>
    public Vector2 GetPosition()
    {
        return transform.position;
    }
    
    /// <summary>
    /// Zwraca aktualną prędkość kulki.
    /// </summary>
    public Vector2 GetVelocity()
    {
        return rb != null ? rb.linearVelocity : Vector2.zero;
    }
    
    /// <summary>
    /// Oblicza kąt pozycji kulki względem środka (0,0) w stopniach.
    /// </summary>
    public float GetAngleFromCenter()
    {
        Vector2 pos = GetPosition();
        float angle = Mathf.Atan2(pos.y, pos.x) * Mathf.Rad2Deg;
        return angle;
    }
    
    /// <summary>
    /// Oblicza odległość kulki od środka.
    /// </summary>
    public float GetDistanceFromCenter()
    {
        return GetPosition().magnitude;
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Wywołaj event przy kolizji (do logowania/dźwięków w przyszłości)
        OnBounce?.Invoke();
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Rysuj kierunek do środka
        Gizmos.color = Color.cyan;
        Vector2 pos = transform.position;
        Gizmos.DrawLine(pos, Vector2.zero);
        
        // Pokaż kąt
        float angle = GetAngleFromCenter();
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.3f, $"Angle: {angle:F1}°");
    }
#endif
}

