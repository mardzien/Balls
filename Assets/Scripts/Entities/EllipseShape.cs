using UnityEngine;

/// <summary>
/// Elipsa z luką - obraca się i ma kolizję dla kulki.
/// Używa LineRenderer do wizualizacji i EdgeCollider2D do fizyki.
/// Elipsa jest zorientowana pionowo (wysokość > szerokość).
/// Dziedziczy z SpawnShape.
/// </summary>
public class EllipseShape : SpawnShape
{
    // Liczba segmentów do rysowania (więcej = gładszy)
    private const int TOTAL_SEGMENTS = 360;
    
    private float _currentAngle;
    
    /// <summary>
    /// Aktualny kąt rotacji elipsy w stopniach.
    /// </summary>
    public override float CurrentAngle 
    { 
        get => _currentAngle; 
        protected set => _currentAngle = value; 
    }
    
    /// <summary>
    /// Promień poziomy elipsy (oś X) - używa heightRadius dla pionowej orientacji.
    /// </summary>
    private float RadiusX => settings != null ? settings.ellipseHeightRadius : 3f;
    
    /// <summary>
    /// Promień pionowy elipsy (oś Y) - używa widthRadius dla pionowej orientacji.
    /// </summary>
    private float RadiusY => settings != null ? settings.ellipseWidthRadius : 5f;
    
    /// <summary>
    /// Wewnętrzny promień elipsy (średni minus grubość).
    /// Używany do spawnu piłek.
    /// </summary>
    public override float InnerRadius
    {
        get
        {
            if (settings == null) return 3f;
            float avgRadius = (RadiusX + RadiusY) / 2f;
            return avgRadius - settings.ringThickness / 2f;
        }
    }
    
    /// <summary>
    /// Zewnętrzny promień elipsy (większy promień plus grubość).
    /// Używany do detekcji ucieczki.
    /// </summary>
    public override float OuterRadius
    {
        get
        {
            if (settings == null) return 5.5f;
            float maxRadius = Mathf.Max(RadiusX, RadiusY);
            return maxRadius + settings.ringThickness / 2f;
        }
    }
    
    /// <summary>
    /// Kąt początku luki w stopniach.
    /// </summary>
    public override float GapStartAngle => GetEffectiveGapBaseAngle() + CurrentAngle - settings.gapAngleDegrees / 2f;
    
    /// <summary>
    /// Kąt końca luki w stopniach.
    /// </summary>
    public override float GapEndAngle => GetEffectiveGapBaseAngle() + CurrentAngle + settings.gapAngleDegrees / 2f;
    
    /// <summary>
    /// Zwraca promień elipsy (średni).
    /// </summary>
    public override float GetRadius()
    {
        if (settings == null) return 4f;
        return (RadiusX + RadiusY) / 2f;
    }
    
    /// <summary>
    /// Generuje losową pozycję spawnu wewnątrz elipsy (górna część).
    /// </summary>
    public override Vector2 GetRandomSpawnPosition()
    {
        float angle = Random.Range(settings.spawnAngleMin, settings.spawnAngleMax) * Mathf.Deg2Rad;
        
        // Oblicz punkt na elipsie dla tego kąta
        float ellipseX = RadiusX * Mathf.Cos(angle);
        float ellipseY = RadiusY * Mathf.Sin(angle);
        
        // Oblicz odległość od środka do punktu na elipsie
        float ellipseRadius = Mathf.Sqrt(ellipseX * ellipseX + ellipseY * ellipseY);
        
        // Skaluj do wewnętrznego promienia
        float spawnDistance = (ellipseRadius - settings.ringThickness / 2f) * settings.spawnRadiusPercent;
        
        float x = Center.x + Mathf.Cos(angle) * spawnDistance;
        float y = Center.y + Mathf.Sin(angle) * spawnDistance;
        
        return new Vector2(x, y);
    }
    
    /// <summary>
    /// Generuje geometrię elipsy (LineRenderer i EdgeCollider).
    /// </summary>
    protected override void GenerateShape()
    {
        GenerateEllipse();
    }
    
    private void GenerateEllipse()
    {
        // Oblicz kąt łuku (360 - luka)
        float arcAngle = 360f - settings.gapAngleDegrees;
        int segments = Mathf.Max(32, Mathf.RoundToInt(arcAngle));
        
        // Punkty dla LineRenderer
        Vector3[] linePoints = new Vector3[segments + 1];
        
        // Punkty dla EdgeCollider
        Vector2[] colliderPoints = new Vector2[segments + 1];
        
        // Bazowy kąt luki (uwzględnia wędrującą lukę)
        float gapBaseAngle = settings.enableTravelingGap ? GapAngle : 0f;
        
        // Rozpocznij od połowy luki (luka będzie na górze w pozycji startowej)
        float startAngle = 90f + gapBaseAngle + settings.gapAngleDegrees / 2f;
        float angleStep = arcAngle / segments;
        
        for (int i = 0; i <= segments; i++)
        {
            float angleDeg = startAngle + i * angleStep;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            
            // Parametryczne równanie elipsy (pionowa orientacja)
            float x = RadiusX * Mathf.Cos(angleRad);
            float y = RadiusY * Mathf.Sin(angleRad);
            
            linePoints[i] = new Vector3(x, y, 0);
            colliderPoints[i] = new Vector2(x, y);
        }
        
        lineRenderer.positionCount = linePoints.Length;
        lineRenderer.SetPositions(linePoints);
        
        edgeCollider.points = colliderPoints;
    }
    
    /// <summary>
    /// Oblicza promień elipsy dla danego kąta.
    /// </summary>
    public override float GetRadiusAtAngle(float angleDegrees)
    {
        if (settings == null) return 4f;
        float angleRad = angleDegrees * Mathf.Deg2Rad;
        float x = RadiusX * Mathf.Cos(angleRad);
        float y = RadiusY * Mathf.Sin(angleRad);
        return Mathf.Sqrt(x * x + y * y);
    }
    
    /// <summary>
    /// Zwraca punkt na elipsie dla danego kąta (w lokalnych współrzędnych).
    /// </summary>
    public Vector2 GetPointOnEllipse(float angleDegrees)
    {
        float angleRad = angleDegrees * Mathf.Deg2Rad;
        return new Vector2(RadiusX * Mathf.Cos(angleRad), RadiusY * Mathf.Sin(angleRad));
    }

#if UNITY_EDITOR
    /// <summary>
    /// Rysuje gizmos pomocnicze w edytorze.
    /// </summary>
    protected override void DrawGizmos()
    {
        Vector3 center = transform.position;
        
        // Rysuj elipsę zewnętrzną
        Gizmos.color = Color.yellow;
        DrawWireEllipse(center, RadiusX + settings.ringThickness / 2f, 
                        RadiusY + settings.ringThickness / 2f);
        
        // Rysuj elipsę wewnętrzną
        Gizmos.color = Color.green;
        DrawWireEllipse(center, RadiusX - settings.ringThickness / 2f, 
                        RadiusY - settings.ringThickness / 2f);
        
        // Rysuj obszar luki
        Gizmos.color = Color.red;
        float gapStart = GapStartAngle * Mathf.Deg2Rad;
        float gapEnd = GapEndAngle * Mathf.Deg2Rad;
        
        Vector3 startPoint = new Vector3(
            RadiusX * Mathf.Cos(gapStart),
            RadiusY * Mathf.Sin(gapStart), 0);
        Vector3 endPoint = new Vector3(
            RadiusX * Mathf.Cos(gapEnd),
            RadiusY * Mathf.Sin(gapEnd), 0);
        
        Gizmos.DrawLine(center, center + startPoint);
        Gizmos.DrawLine(center, center + endPoint);
    }
    
    private void DrawWireEllipse(Vector3 center, float radiusX, float radiusY, int segments = 64)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radiusX, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 point = center + new Vector3(
                Mathf.Cos(angle) * radiusX, 
                Mathf.Sin(angle) * radiusY, 0);
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }
    }
#endif
}
