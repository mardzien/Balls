using UnityEngine;

/// <summary>
/// Pierścień z luką - obraca się i ma kolizję dla kulki.
/// Używa LineRenderer do wizualizacji i EdgeCollider2D do fizyki.
/// Kolizja jest na wewnętrznej krawędzi pierścienia.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(EdgeCollider2D))]
public class Ring : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameSettings settings;
    
    private LineRenderer lineRenderer;
    private EdgeCollider2D edgeCollider;
    
    // Liczba segmentów do rysowania łuku (więcej = gładszy)
    private const int SEGMENTS_PER_DEGREE = 1;
    
    /// <summary>
    /// Aktualny kąt rotacji pierścienia w stopniach.
    /// </summary>
    public float CurrentAngle { get; private set; }
    
    /// <summary>
    /// Pozycja środka pierścienia w świecie.
    /// </summary>
    public Vector2 Center => transform.position;
    
    /// <summary>
    /// Wewnętrzny promień pierścienia (dla kolizji).
    /// </summary>
    public float InnerRadius => settings != null ? settings.ringRadius - settings.ringThickness / 2f : 3f;
    
    /// <summary>
    /// Zewnętrzny promień pierścienia.
    /// </summary>
    public float OuterRadius => settings != null ? settings.ringRadius + settings.ringThickness / 2f : 3.5f;
    
    /// <summary>
    /// Kąt początku luki w stopniach (względem aktualnej rotacji).
    /// </summary>
    public float GapStartAngle => 90f + CurrentAngle - settings.gapAngleDegrees / 2f;
    
    /// <summary>
    /// Kąt końca luki w stopniach (względem aktualnej rotacji).
    /// </summary>
    public float GapEndAngle => 90f + CurrentAngle + settings.gapAngleDegrees / 2f;
    
    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        edgeCollider = GetComponent<EdgeCollider2D>();
    }
    
    private void Start()
    {
        if (settings == null)
        {
            Debug.LogError("Ring: GameSettings nie jest przypisany!");
            return;
        }
        
        SetupPosition();
        SetupLineRenderer();
        GenerateRing();
    }
    
    private void Update()
    {
        // Obracaj pierścień
        CurrentAngle += settings.rotationSpeed * Time.deltaTime;
        CurrentAngle %= 360f;
        
        // Aktualizuj rotację obiektu
        transform.rotation = Quaternion.Euler(0, 0, CurrentAngle);
    }
    
    /// <summary>
    /// Inicjalizuje Ring z podanymi ustawieniami.
    /// </summary>
    public void Initialize(GameSettings gameSettings)
    {
        settings = gameSettings;
        
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        if (edgeCollider == null) edgeCollider = GetComponent<EdgeCollider2D>();
        
        SetupPosition();
        SetupLineRenderer();
        GenerateRing();
    }
    
    private void SetupPosition()
    {
        // Ustaw pozycję pierścienia (z offsetem pionowym)
        transform.position = new Vector3(0, settings.ringVerticalOffset, 0);
    }
    
    private void SetupLineRenderer()
    {
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = false;
        lineRenderer.startWidth = settings.ringThickness;
        lineRenderer.endWidth = settings.ringThickness;
        lineRenderer.startColor = settings.ringColor;
        lineRenderer.endColor = settings.ringColor;
        
        // Używamy domyślnego materiału sprites
        if (lineRenderer.material == null || lineRenderer.material.shader.name == "Hidden/InternalErrorShader")
        {
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }
        lineRenderer.material.color = settings.ringColor;
    }
    
    private void GenerateRing()
    {
        // Oblicz kąt łuku (360 - luka)
        float arcAngle = 360f - settings.gapAngleDegrees;
        int segments = Mathf.Max(32, Mathf.RoundToInt(arcAngle * SEGMENTS_PER_DEGREE));
        
        // Punkty dla LineRenderer (na środkowym promieniu)
        Vector3[] linePoints = new Vector3[segments + 1];
        
        // Punkty dla EdgeCollider (na ŚRODKOWYM promieniu - piłka "wchodzi" w połowę grubości pierścienia)
        Vector2[] colliderPoints = new Vector2[segments + 1];
        
        // Kolizja na środku pierścienia dla lepszego efektu wizualnego
        float collisionRadius = settings.ringRadius;
        
        // Rozpocznij od połowy luki (luka będzie na górze w pozycji startowej)
        // Kąt 90 stopni = góra
        float startAngle = 90f + settings.gapAngleDegrees / 2f;
        float angleStep = arcAngle / segments;
        
        for (int i = 0; i <= segments; i++)
        {
            float angle = (startAngle + i * angleStep) * Mathf.Deg2Rad;
            
            // LineRenderer - na środkowym promieniu
            float lx = Mathf.Cos(angle) * settings.ringRadius;
            float ly = Mathf.Sin(angle) * settings.ringRadius;
            linePoints[i] = new Vector3(lx, ly, 0);
            
            // EdgeCollider - na ŚRODKOWYM promieniu
            float cx = Mathf.Cos(angle) * collisionRadius;
            float cy = Mathf.Sin(angle) * collisionRadius;
            colliderPoints[i] = new Vector2(cx, cy);
        }
        
        lineRenderer.positionCount = linePoints.Length;
        lineRenderer.SetPositions(linePoints);
        
        edgeCollider.points = colliderPoints;
    }
    
    /// <summary>
    /// Sprawdza czy podany kąt (w stopniach) znajduje się w obszarze luki.
    /// </summary>
    public bool IsInGap(float angleDegrees)
    {
        // Normalizuj kąt do zakresu 0-360
        float normalizedAngle = ((angleDegrees % 360f) + 360f) % 360f;
        float normalizedGapStart = ((GapStartAngle % 360f) + 360f) % 360f;
        float normalizedGapEnd = ((GapEndAngle % 360f) + 360f) % 360f;
        
        // Obsłuż przypadek gdy luka przechodzi przez 0/360 stopni
        if (normalizedGapStart > normalizedGapEnd)
        {
            return normalizedAngle >= normalizedGapStart || normalizedAngle <= normalizedGapEnd;
        }
        
        return normalizedAngle >= normalizedGapStart && normalizedAngle <= normalizedGapEnd;
    }
    
    /// <summary>
    /// Zwraca promień pierścienia (środkowy).
    /// </summary>
    public float GetRadius()
    {
        return settings != null ? settings.ringRadius : 3f;
    }
    
    /// <summary>
    /// Generuje losową pozycję spawnu wewnątrz pierścienia (górna część).
    /// </summary>
    public Vector2 GetRandomSpawnPosition()
    {
        // Losowy kąt w górnej części (między spawnAngleMin a spawnAngleMax)
        float angle = Random.Range(settings.spawnAngleMin, settings.spawnAngleMax) * Mathf.Deg2Rad;
        
        // Losowa odległość od środka
        float distance = InnerRadius * settings.spawnRadiusPercent;
        
        float x = Center.x + Mathf.Cos(angle) * distance;
        float y = Center.y + Mathf.Sin(angle) * distance;
        
        return new Vector2(x, y);
    }
    
    /// <summary>
    /// Resetuje rotację pierścienia do pozycji startowej.
    /// </summary>
    public void ResetRotation()
    {
        CurrentAngle = 0f;
        transform.rotation = Quaternion.identity;
    }
    
    /// <summary>
    /// Ustawia kolor pierścienia.
    /// </summary>
    public void SetColor(Color color)
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
    /// Ustawia widoczność pierścienia (LineRenderer i EdgeCollider).
    /// </summary>
    public void SetVisible(bool visible)
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
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (settings == null) return;
        
        Vector3 center = transform.position;
        
        // Rysuj zewnętrzny promień
        Gizmos.color = Color.yellow;
        DrawWireCircle(center, settings.ringRadius + settings.ringThickness / 2f);
        
        // Rysuj wewnętrzny promień (kolizja)
        Gizmos.color = Color.green;
        DrawWireCircle(center, settings.ringRadius - settings.ringThickness / 2f);
        
        // Rysuj obszar luki
        Gizmos.color = Color.red;
        float gapStart = GapStartAngle * Mathf.Deg2Rad;
        float gapEnd = GapEndAngle * Mathf.Deg2Rad;
        
        Vector3 startDir = new Vector3(Mathf.Cos(gapStart), Mathf.Sin(gapStart), 0);
        Vector3 endDir = new Vector3(Mathf.Cos(gapEnd), Mathf.Sin(gapEnd), 0);
        
        Gizmos.DrawLine(center, center + startDir * settings.ringRadius);
        Gizmos.DrawLine(center, center + endDir * settings.ringRadius);
        
        // Rysuj obszar spawnu
        Gizmos.color = Color.cyan;
        float spawnRadius = (settings.ringRadius - settings.ringThickness / 2f) * settings.spawnRadiusPercent;
        float minAngle = settings.spawnAngleMin * Mathf.Deg2Rad;
        float maxAngle = settings.spawnAngleMax * Mathf.Deg2Rad;
        
        Vector3 minDir = new Vector3(Mathf.Cos(minAngle), Mathf.Sin(minAngle), 0);
        Vector3 maxDir = new Vector3(Mathf.Cos(maxAngle), Mathf.Sin(maxAngle), 0);
        
        Gizmos.DrawLine(center, center + minDir * spawnRadius);
        Gizmos.DrawLine(center, center + maxDir * spawnRadius);
    }
    
    private void DrawWireCircle(Vector3 center, float radius, int segments = 64)
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
