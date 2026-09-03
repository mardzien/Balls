using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Efekty końcowe gry - rozpad kulek i kształtu spawnu na cząsteczki.
/// Obsługuje typy kształtów Ring i Ellipse.
/// </summary>
public class GameOverEffect : MonoBehaviour
{
    [Header("Ball Fragment Settings")]
    [SerializeField] private int fragmentsPerBall = 16;
    [SerializeField] private float explosionForce = 6f;
    
    [Header("Shape Destruction Settings")]
    [SerializeField] private float shapeExplosionForce = 3f;
    [SerializeField] private Color shapeColorStart = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color shapeColorEnd = new Color(1f, 0f, 0f);
    
    private GameManager gameManager;
    private SpawnShape shape;
    private List<GameObject> fragments = new List<GameObject>();
    private List<GameObject> shapeParticles = new List<GameObject>();
    private bool isAnimating = false;
    private float animationTime = 0f;
    private float animationDuration = 2f;
    
    // Cached shape parameters (captured at animation start to avoid randomization issues)
    private ShapeType cachedShapeType;
    private float cachedRingRadius;
    private float cachedEllipseWidthRadius;
    private float cachedEllipseHeightRadius;
    
    private void Start()
    {
        gameManager = GetComponent<GameManager>() ?? FindAnyObjectByType<GameManager>();
        
        if (gameManager != null)
        {
            shape = gameManager.Shape;
            gameManager.OnGameOverStart += StartGameOverAnimation;
            gameManager.OnGameRestart += CleanupEffects;
        }
    }
    
    private void Update()
    {
        if (!isAnimating) return;
        
        animationTime += Time.deltaTime;
        float progress = animationTime / animationDuration;
        
        if (progress >= 1f)
        {
            isAnimating = false;
            gameManager?.StopRecordingIfNeeded();
            return;
        }
        
        AnimateParticles(progress);
    }
    
    private void StartGameOverAnimation()
    {
        isAnimating = true;
        animationTime = 0f;
        animationDuration = gameManager?.Settings?.gameOverAnimationDuration ?? 2f;
        
        if (shape == null) shape = gameManager?.Shape;
        
        // Cache shape parameters from the ACTUAL shape object (not settings!)
        // Settings may have been randomized for the next round already
        if (shape != null)
        {
            // Detect actual shape type from the object itself
            if (shape is Ring)
                cachedShapeType = ShapeType.Ring;
            else if (shape is EllipseShape)
                cachedShapeType = ShapeType.Ellipse;
            else
                cachedShapeType = ShapeType.Ring;
            
            // Use cached dimensions from shape (set when shape was initialized at round start)
            cachedRingRadius = shape.CachedRingRadius;
            cachedEllipseWidthRadius = shape.CachedEllipseWidthRadius;
            cachedEllipseHeightRadius = shape.CachedEllipseHeightRadius;
        }
        
        CreateBallFragments();
        CreateShapeParticles();
    }
    
    private void CreateBallFragments()
    {
        if (gameManager == null) return;
        
        foreach (var ball in gameManager.AllBalls)
        {
            if (ball != null && ball.IsFrozen)
            {
                CreateFragmentsFromBall(ball);
            }
        }
    }
    
    private void CreateFragmentsFromBall(Ball ball)
    {
        Vector3 position = ball.transform.position;
        Color ballColor = ball.BallColor;
        float ballRadius = ball.transform.localScale.x / 2f;
        
        ball.gameObject.SetActive(false);
        
        for (int i = 0; i < fragmentsPerBall; i++)
        {
            GameObject fragment = CreateParticle(position, ballColor, ballRadius * 0.4f);
            
            Rigidbody2D rb = fragment.GetComponent<Rigidbody2D>();
            rb.gravityScale = 1.5f;
            
            float angle = (i / (float)fragmentsPerBall) * 360f + Random.Range(-30f, 30f);
            Vector2 direction = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );
            
            rb.linearVelocity = direction * explosionForce * Random.Range(0.5f, 1.5f);
            rb.angularVelocity = Random.Range(-360f, 360f);
            
            fragments.Add(fragment);
        }
    }
    
    private void CreateShapeParticles()
    {
        if (shape == null) return;
        
        var settings = gameManager?.Settings;
        if (settings == null) return;
        
        shape.SetVisible(false);
        
        // Generuj cząsteczki wzdłuż obwodu kształtu
        int particleCount = settings.ringParticleCount;
        float gapAngle = settings.gapAngleDegrees;
        float arcAngle = 360f - gapAngle;
        
        // Bazowy kąt luki (uwzględnia wędrującą lukę)
        float startAngle = 90f + shape.GapAngle + gapAngle / 2f;
        
        for (int i = 0; i < particleCount; i++)
        {
            float progress = i / (float)particleCount;
            float localAngle = startAngle + progress * arcAngle;
            
            // Pobierz punkt na kształcie w lokalnych współrzędnych (używa cached parametrów)
            Vector2 localPoint = GetPointOnShape(localAngle);
            
            // Transformuj przez rotację kształtu
            Vector3 worldPoint = shape.transform.TransformPoint(new Vector3(localPoint.x, localPoint.y, 0));
            
            // Oblicz kierunek normalny (od środka na zewnątrz)
            Vector2 normal = (worldPoint - shape.transform.position).normalized;
            float normalAngle = Mathf.Atan2(normal.y, normal.x);
            
            CreateShapeParticle(worldPoint, normalAngle);
        }
    }
    
    /// <summary>
    /// Zwraca punkt na kształcie dla danego kąta (w lokalnych współrzędnych kształtu).
    /// Używa cached parametrów zapisanych na początku animacji.
    /// </summary>
    private Vector2 GetPointOnShape(float angleDegrees)
    {
        float angleRad = angleDegrees * Mathf.Deg2Rad;
        
        switch (cachedShapeType)
        {
            case ShapeType.Ring:
                return new Vector2(
                    cachedRingRadius * Mathf.Cos(angleRad),
                    cachedRingRadius * Mathf.Sin(angleRad)
                );
                
            case ShapeType.Ellipse:
                // Elipsa pionowa - widthRadius na X (mniejszy), heightRadius na Y (większy)
                return new Vector2(
                    cachedEllipseWidthRadius * Mathf.Cos(angleRad),
                    cachedEllipseHeightRadius * Mathf.Sin(angleRad)
                );
                
            default:
                return new Vector2(
                    cachedRingRadius * Mathf.Cos(angleRad),
                    cachedRingRadius * Mathf.Sin(angleRad)
                );
        }
    }
    
    private void CreateShapeParticle(Vector3 position, float normalAngle)
    {
        var settings = gameManager?.Settings;
        if (settings == null) return;
        
        Color color = Color.Lerp(shapeColorStart, shapeColorEnd, Random.Range(0f, 1f));
        float size = settings.ringThickness * Random.Range(0.3f, 0.8f);
        
        GameObject particle = CreateParticle(position, color, size);
        
        Rigidbody2D rb = particle.GetComponent<Rigidbody2D>();
        rb.gravityScale = Random.Range(1.5f, 2.5f);
        
        Vector2 outward = new Vector2(Mathf.Cos(normalAngle), Mathf.Sin(normalAngle));
        Vector2 direction = (outward * 0.3f + Vector2.down * 0.7f).normalized;
        direction += new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(-0.2f, 0.2f));
        
        rb.linearVelocity = direction * shapeExplosionForce * Random.Range(0.5f, 1.5f);
        rb.angularVelocity = Random.Range(-180f, 180f);
        
        shapeParticles.Add(particle);
    }
    
    private GameObject CreateParticle(Vector3 position, Color color, float size)
    {
        GameObject particle = new GameObject("Particle");
        particle.transform.position = position;
        particle.transform.localScale = Vector3.one * size;
        
        SpriteRenderer sr = particle.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteUtility.GetSmallSprite();
        sr.color = color;
        
        particle.AddComponent<Rigidbody2D>();
        
        return particle;
    }
    
    private void AnimateParticles(float progress)
    {
        float alpha = 1f - progress;
        
        foreach (var fragment in fragments)
        {
            SetAlpha(fragment, alpha);
        }
        
        foreach (var particle in shapeParticles)
        {
            SetAlpha(particle, alpha);
        }
    }
    
    private void SetAlpha(GameObject obj, float alpha)
    {
        if (obj == null) return;
        
        var sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }
    
    private void CleanupEffects()
    {
        isAnimating = false;
        
        DestroyAll(fragments);
        DestroyAll(shapeParticles);
        
        // Wyczyść cząsteczki komety
        BallTrailEffect.ClearAllCometParticles();
        
        if (shape != null)
        {
            shape.SetVisible(true);
            if (gameManager?.Settings != null)
            {
                shape.SetColor(gameManager.Settings.ringColor);
            }
        }
    }
    
    private void DestroyAll(List<GameObject> list)
    {
        foreach (var obj in list)
        {
            if (obj != null) Destroy(obj);
        }
        list.Clear();
    }
    
    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnGameOverStart -= StartGameOverAnimation;
            gameManager.OnGameRestart -= CleanupEffects;
        }
        CleanupEffects();
    }
}
