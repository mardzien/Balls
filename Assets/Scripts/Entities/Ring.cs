using UnityEngine;

/// <summary>
/// Pierścień z luką - obraca się i ma kolizję dla kulki.
/// Używa LineRenderer do wizualizacji i EdgeCollider2D do fizyki.
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
    /// Kąt początku luki w stopniach (względem aktualnej rotacji).
    /// </summary>
    public float GapStartAngle => CurrentAngle - settings.gapAngleDegrees / 2f;
    
    /// <summary>
    /// Kąt końca luki w stopniach (względem aktualnej rotacji).
    /// </summary>
    public float GapEndAngle => CurrentAngle + settings.gapAngleDegrees / 2f;
    
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
        
        SetupLineRenderer();
        GenerateRing();
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
        
        // Punkty dla LineRenderer i EdgeCollider
        Vector3[] linePoints = new Vector3[segments + 1];
        Vector2[] colliderPoints = new Vector2[segments + 1];
        
        // Rozpocznij od połowy luki (luka będzie na górze w pozycji startowej)
        float startAngle = settings.gapAngleDegrees / 2f;
        float angleStep = arcAngle / segments;
        
        for (int i = 0; i <= segments; i++)
        {
            float angle = (startAngle + i * angleStep) * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * settings.ringRadius;
            float y = Mathf.Sin(angle) * settings.ringRadius;
            
            linePoints[i] = new Vector3(x, y, 0);
            colliderPoints[i] = new Vector2(x, y);
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
    /// Zwraca promień pierścienia.
    /// </summary>
    public float GetRadius()
    {
        return settings != null ? settings.ringRadius : 3f;
    }
    
    /// <summary>
    /// Resetuje rotację pierścienia do pozycji startowej.
    /// </summary>
    public void ResetRotation()
    {
        CurrentAngle = 0f;
        transform.rotation = Quaternion.identity;
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (settings == null) return;
        
        // Rysuj promień pierścienia
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, settings.ringRadius);
        
        // Rysuj obszar luki
        Gizmos.color = Color.red;
        float gapStart = GapStartAngle * Mathf.Deg2Rad;
        float gapEnd = GapEndAngle * Mathf.Deg2Rad;
        
        Vector3 startDir = new Vector3(Mathf.Cos(gapStart), Mathf.Sin(gapStart), 0);
        Vector3 endDir = new Vector3(Mathf.Cos(gapEnd), Mathf.Sin(gapEnd), 0);
        
        Gizmos.DrawLine(transform.position, transform.position + startDir * settings.ringRadius);
        Gizmos.DrawLine(transform.position, transform.position + endDir * settings.ringRadius);
    }
#endif
}

