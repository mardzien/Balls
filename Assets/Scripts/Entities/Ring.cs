using UnityEngine;

/// <summary>
/// Pierścień z luką - obraca się i ma kolizję dla kulki.
/// Używa LineRenderer do wizualizacji i EdgeCollider2D do fizyki.
/// Kolizja jest na środkowym promieniu pierścienia.
/// Dziedziczy z SpawnShape.
/// </summary>
public class Ring : SpawnShape
{
    // Liczba segmentów do rysowania łuku (więcej = gładszy)
    private const int SEGMENTS_PER_DEGREE = 1;
    
    private float _currentAngle;
    
    /// <summary>
    /// Aktualny kąt rotacji pierścienia w stopniach.
    /// </summary>
    public override float CurrentAngle 
    { 
        get => _currentAngle; 
        protected set => _currentAngle = value; 
    }
    
    /// <summary>
    /// Wewnętrzny promień pierścienia (wizualna wewnętrzna krawędź).
    /// </summary>
    public override float InnerRadius => settings != null ? settings.ringRadius - settings.ringThickness / 2f : 3f;
    
    /// <summary>
    /// Zewnętrzny promień pierścienia.
    /// </summary>
    public override float OuterRadius => settings != null ? settings.ringRadius + settings.ringThickness / 2f : 3.5f;
    
    /// <summary>
    /// Kąt początku luki w stopniach (względem aktualnej rotacji).
    /// </summary>
    public override float GapStartAngle => GetEffectiveGapBaseAngle() + CurrentAngle - settings.gapAngleDegrees / 2f;
    
    /// <summary>
    /// Kąt końca luki w stopniach (względem aktualnej rotacji).
    /// </summary>
    public override float GapEndAngle => GetEffectiveGapBaseAngle() + CurrentAngle + settings.gapAngleDegrees / 2f;
    
    /// <summary>
    /// Zwraca promień pierścienia (środkowy).
    /// </summary>
    public override float GetRadius()
    {
        return settings != null ? settings.ringRadius : 3f;
    }
    
    /// <summary>
    /// Generuje losową pozycję spawnu wewnątrz pierścienia (górna część).
    /// </summary>
    public override Vector2 GetRandomSpawnPosition()
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
    /// Generuje geometrię pierścienia (LineRenderer i EdgeCollider).
    /// </summary>
    protected override void GenerateShape()
    {
        GenerateRing();
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
        
        // Bazowy kąt luki (uwzględnia wędrującą lukę)
        float gapBaseAngle = settings.enableTravelingGap ? GapAngle : 0f;
        
        // Rozpocznij od połowy luki (luka będzie na górze w pozycji startowej)
        // Kąt 90 stopni = góra
        float startAngle = 90f + gapBaseAngle + settings.gapAngleDegrees / 2f;
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

#if UNITY_EDITOR
    /// <summary>
    /// Rysuje gizmos pomocnicze w edytorze.
    /// </summary>
    protected override void DrawGizmos()
    {
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
#endif
}
