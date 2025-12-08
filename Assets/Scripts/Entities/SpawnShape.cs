using UnityEngine;

/// <summary>
/// Bazowa klasa abstrakcyjna dla kształtów spawnu (Ring, Ellipse).
/// Definiuje wspólny interfejs dla wszystkich kształtów używanych w grze.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(EdgeCollider2D))]
public abstract class SpawnShape : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected GameSettings settings;
    
    protected LineRenderer lineRenderer;
    protected EdgeCollider2D edgeCollider;
    
    /// <summary>
    /// Aktualny kąt rotacji kształtu w stopniach.
    /// </summary>
    public abstract float CurrentAngle { get; protected set; }
    
    /// <summary>
    /// Aktualny kąt pozycji luki (dla wędrującej luki).
    /// </summary>
    public virtual float GapAngle { get; protected set; }
    
    /// <summary>
    /// Pozycja środka kształtu w świecie.
    /// </summary>
    public virtual Vector2 Center => transform.position;
    
    /// <summary>
    /// Wewnętrzny promień kształtu (dla spawnu piłek).
    /// </summary>
    public abstract float InnerRadius { get; }
    
    /// <summary>
    /// Zewnętrzny promień kształtu (dla detekcji ucieczki).
    /// </summary>
    public abstract float OuterRadius { get; }
    
    /// <summary>
    /// Kąt początku luki w stopniach (względem aktualnej rotacji/pozycji luki).
    /// </summary>
    public abstract float GapStartAngle { get; }
    
    /// <summary>
    /// Kąt końca luki w stopniach (względem aktualnej rotacji/pozycji luki).
    /// </summary>
    public abstract float GapEndAngle { get; }
    
    protected virtual void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        edgeCollider = GetComponent<EdgeCollider2D>();
    }
    
    protected virtual void Start()
    {
        if (settings == null)
        {
            Debug.LogError($"{GetType().Name}: GameSettings nie jest przypisany!");
            return;
        }
        
        SetupPosition();
        SetupLineRenderer();
        GenerateShape();
    }
    
    protected virtual void Update()
    {
        UpdateGapPosition();
    }
    
    /// <summary>
    /// Aktualizuje pozycję wędrującej luki.
    /// </summary>
    protected virtual void UpdateGapPosition()
    {
        if (settings == null) return;
        
        GapAngle += settings.rotationSpeed * Time.deltaTime;
        GapAngle %= 360f;
        
        // Regeneruj kształt gdy luka się przesuwa
        GenerateShape();
    }
    
    /// <summary>
    /// Inicjalizuje kształt z podanymi ustawieniami.
    /// </summary>
    public virtual void Initialize(GameSettings gameSettings)
    {
        settings = gameSettings;
        
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        if (edgeCollider == null) edgeCollider = GetComponent<EdgeCollider2D>();
        
        SetupPosition();
        SetupLineRenderer();
        GenerateShape();
    }
    
    /// <summary>
    /// Ustawia pozycję kształtu.
    /// </summary>
    protected virtual void SetupPosition()
    {
        transform.position = new Vector3(0, settings.ringVerticalOffset, 0);
    }
    
    /// <summary>
    /// Konfiguruje LineRenderer.
    /// </summary>
    protected virtual void SetupLineRenderer()
    {
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = false;
        lineRenderer.startWidth = settings.ringThickness;
        lineRenderer.endWidth = settings.ringThickness;
        lineRenderer.startColor = settings.ringColor;
        lineRenderer.endColor = settings.ringColor;
        
        if (lineRenderer.material == null || lineRenderer.material.shader.name == "Hidden/InternalErrorShader")
        {
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }
        lineRenderer.material.color = settings.ringColor;
    }
    
    /// <summary>
    /// Generuje geometrię kształtu (LineRenderer i EdgeCollider).
    /// </summary>
    protected abstract void GenerateShape();
    
    /// <summary>
    /// Sprawdza czy podany kąt (w stopniach) znajduje się w obszarze luki.
    /// </summary>
    public virtual bool IsInGap(float angleDegrees)
    {
        float normalizedAngle = ((angleDegrees % 360f) + 360f) % 360f;
        float normalizedGapStart = ((GapStartAngle % 360f) + 360f) % 360f;
        float normalizedGapEnd = ((GapEndAngle % 360f) + 360f) % 360f;
        
        if (normalizedGapStart > normalizedGapEnd)
        {
            return normalizedAngle >= normalizedGapStart || normalizedAngle <= normalizedGapEnd;
        }
        
        return normalizedAngle >= normalizedGapStart && normalizedAngle <= normalizedGapEnd;
    }
    
    /// <summary>
    /// Zwraca promień kształtu (dla escape detection).
    /// </summary>
    public abstract float GetRadius();
    
    /// <summary>
    /// Sprawdza czy punkt jest poza granicą kształtu.
    /// Prostsza metoda - sprawdza tylko czy odległość od środka 
    /// przekracza promień kształtu dla danego kąta.
    /// </summary>
    public virtual bool IsPointOutsideShape(Vector2 worldPoint, float buffer = 0f)
    {
        // Konwertuj punkt do lokalnych współrzędnych kształtu
        Vector2 localPoint = (Vector2)transform.InverseTransformPoint(worldPoint);
        
        // Oblicz odległość od środka
        float distanceFromCenter = localPoint.magnitude;
        
        // Oblicz kąt punktu
        float angle = Mathf.Atan2(localPoint.y, localPoint.x) * Mathf.Rad2Deg;
        
        // Pobierz promień kształtu dla tego kąta
        float shapeRadius = GetRadiusAtAngle(angle);
        
        // Punkt jest poza kształtem jeśli jego odległość przekracza promień + bufor
        return distanceFromCenter > shapeRadius + buffer;
    }
    
    /// <summary>
    /// Zwraca promień kształtu dla danego kąta (w stopniach).
    /// Dla kształtów innych niż koło, promień może się różnić w zależności od kąta.
    /// </summary>
    public virtual float GetRadiusAtAngle(float angleDegrees)
    {
        // Domyślna implementacja - stały promień (dla koła/ringa)
        return GetRadius();
    }
    
    /// <summary>
    /// Generuje losową pozycję spawnu wewnątrz kształtu (górna część).
    /// </summary>
    public abstract Vector2 GetRandomSpawnPosition();
    
    /// <summary>
    /// Resetuje pozycję luki do stanu początkowego.
    /// </summary>
    public virtual void ResetRotation()
    {
        GapAngle = 0f;
        GenerateShape();
    }
    
    /// <summary>
    /// Ustawia kolor kształtu.
    /// </summary>
    public virtual void SetColor(Color color)
    {
        if (lineRenderer != null)
        {
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            if (lineRenderer.material != null)
            {
                lineRenderer.material.color = color;
            }
        }
    }
    
    /// <summary>
    /// Ustawia widoczność kształtu (LineRenderer i EdgeCollider).
    /// </summary>
    public virtual void SetVisible(bool visible)
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = visible;
        }
        if (edgeCollider != null)
        {
            edgeCollider.enabled = visible;
        }
    }
    
    /// <summary>
    /// Oblicza efektywny kąt luki.
    /// </summary>
    protected float GetEffectiveGapBaseAngle()
    {
        // Bazowy kąt luki (90 stopni = góra) + przesunięcie wędrującej luki
        return 90f + GapAngle;
    }
    
    /// <summary>
    /// Normalizuje kąt do zakresu 0-360.
    /// </summary>
    protected static float NormalizeAngle(float angle)
    {
        return ((angle % 360f) + 360f) % 360f;
    }

#if UNITY_EDITOR
    protected virtual void OnDrawGizmosSelected()
    {
        if (settings == null) return;
        DrawGizmos();
    }
    
    /// <summary>
    /// Rysuje gizmos pomocnicze w edytorze.
    /// </summary>
    protected abstract void DrawGizmos();
    
    protected void DrawWireCircle(Vector3 center, float radius, int segments = 64)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 point = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }
    }
#endif
}
