using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Style efektu ogonka piłki.
/// </summary>
public enum TrailStyle
{
    None,           // Bez ogonka
    Comet,          // Cienki blisko piłki, grubszy dalej
    FadingTrail,    // Jasny blisko, przezroczysty dalej
    ThinUniform     // Jednolita szerokość i jasność
}

/// <summary>
/// Zarządza efektem ogonka (TrailRenderer) na piłce.
/// </summary>
[RequireComponent(typeof(TrailRenderer))]
public class BallTrailEffect : MonoBehaviour
{
    private TrailRenderer trailRenderer;
    private Color baseColor = Color.white;
    private float ballRadius = 0.3f;
    
    // Comet particle system
    private float particleSpawnTimer = 0f;
    private const float PARTICLE_SPAWN_INTERVAL = 0.03f;
    private const int MAX_COMET_PARTICLES = 30;
    private static List<GameObject> cometParticles = new List<GameObject>();
    private bool isFrozen = false;
    private float trailTime = 0.25f;
    
    // Position history for accurate tail tracking
    private struct PositionRecord
    {
        public Vector3 position;
        public float time;
    }
    private Queue<PositionRecord> positionHistory = new Queue<PositionRecord>();
    private const float POSITION_RECORD_INTERVAL = 0.02f;
    private float lastRecordTime = 0f;
    
    /// <summary>
    /// Inicjalizuje efekt ogonka z podanymi parametrami.
    /// </summary>
    public void Initialize(TrailStyle style, Color color, float time, float trailWidth)
    {
        baseColor = color;
        ballRadius = trailWidth; // trailWidth jest bazowany na promieniu piłki
        trailTime = time;
        
        if (trailRenderer == null)
        {
            trailRenderer = GetComponent<TrailRenderer>();
        }
        
        // Clear position history
        positionHistory.Clear();
        lastRecordTime = 0f;
        
        if (style == TrailStyle.None)
        {
            trailRenderer.enabled = false;
            return;
        }
        
        trailRenderer.enabled = true;
        ConfigureTrailRenderer(style, time, trailWidth);
    }
    
    private void Update()
    {
        // Record position history for all styles (needed for comet particles)
        RecordPosition();
        
        // Spawn particles only for Comet style and only when not frozen
        if (currentStyle != TrailStyle.Comet || isFrozen) return;
        
        particleSpawnTimer += Time.deltaTime;
        
        if (particleSpawnTimer >= PARTICLE_SPAWN_INTERVAL)
        {
            particleSpawnTimer = 0f;
            SpawnCometParticle();
        }
    }
    
    private void RecordPosition()
    {
        float currentTime = Time.time;
        
        // Record position at intervals
        if (currentTime - lastRecordTime >= POSITION_RECORD_INTERVAL)
        {
            positionHistory.Enqueue(new PositionRecord
            {
                position = transform.position,
                time = currentTime
            });
            lastRecordTime = currentTime;
        }
        
        // Remove old positions (older than trailTime)
        while (positionHistory.Count > 0 && currentTime - positionHistory.Peek().time > trailTime)
        {
            positionHistory.Dequeue();
        }
    }
    
    /// <summary>
    /// Pobiera rzeczywistą pozycję końca ogona na podstawie historii pozycji.
    /// </summary>
    private Vector3? GetTailPosition()
    {
        if (positionHistory.Count < 2) return null;
        
        // Get the oldest position (tail end)
        PositionRecord oldest = positionHistory.Peek();
        Vector3 tailPos = oldest.position;
        
        // Check if tail is far enough from current position
        float distance = Vector3.Distance(tailPos, transform.position);
        if (distance < ballRadius * 0.3f) return null;
        
        return tailPos;
    }
    
    /// <summary>
    /// Ustawia stan zamrożenia - zamrożone piłki nie emitują drobin.
    /// </summary>
    public void SetFrozen(bool frozen)
    {
        isFrozen = frozen;
    }
    
    private void SpawnCometParticle()
    {
        // Clean up destroyed particles from list
        cometParticles.RemoveAll(p => p == null);
        
        // Limit particle count
        if (cometParticles.Count >= MAX_COMET_PARTICLES) return;
        
        // Get actual tail position from history
        Vector3? tailPos = GetTailPosition();
        if (!tailPos.HasValue) return;
        
        Vector3 tailPosition = tailPos.Value;
        
        // Random offset around tail end
        Vector2 offset = new Vector2(
            Random.Range(-ballRadius * 0.3f, ballRadius * 0.3f),
            Random.Range(-ballRadius * 0.3f, ballRadius * 0.3f)
        );
        
        Vector3 spawnPos = tailPosition + (Vector3)offset;
        
        GameObject particle = new GameObject("CometParticle");
        particle.transform.position = spawnPos;
        
        float size = ballRadius * Random.Range(0.15f, 0.35f);
        particle.transform.localScale = Vector3.one * size;
        
        SpriteRenderer sr = particle.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteUtility.GetSmallSprite();
        
        // Jasny kolor dla drobinek (jak koniec ogona)
        Color particleColor = baseColor * Random.Range(1.0f, 1.4f);
        particleColor.a = Random.Range(0.7f, 1f);
        sr.color = particleColor;
        sr.sortingOrder = -1; // Za piłką
        
        Rigidbody2D rb = particle.AddComponent<Rigidbody2D>();
        rb.gravityScale = Random.Range(1.0f, 2.0f);
        
        // Delikatny ruch w bok i w dół
        rb.linearVelocity = new Vector2(
            Random.Range(-1.5f, 1.5f),
            Random.Range(-1f, 0.5f)
        );
        
        cometParticles.Add(particle);
        
        // Auto-destroy
        float lifetime = Random.Range(0.4f, 0.8f);
        Destroy(particle, lifetime);
    }
    
    /// <summary>
    /// Czyści wszystkie cząsteczki komety (np. przy restarcie gry).
    /// </summary>
    public static void ClearAllCometParticles()
    {
        foreach (var particle in cometParticles)
        {
            if (particle != null)
            {
                Destroy(particle);
            }
        }
        cometParticles.Clear();
    }
    
    /// <summary>
    /// Aktualizuje kolor ogonka (np. przy zamrożeniu piłki).
    /// </summary>
    public void UpdateColor(Color newColor)
    {
        baseColor = newColor;
        if (trailRenderer != null && trailRenderer.enabled)
        {
            ApplyColorGradient(GetCurrentStyle());
        }
    }
    
    /// <summary>
    /// Czyści ogonek (usuwa wszystkie punkty).
    /// </summary>
    public void ClearTrail()
    {
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }
    }
    
    private TrailStyle currentStyle = TrailStyle.None;
    
    private TrailStyle GetCurrentStyle() => currentStyle;
    
    private void ConfigureTrailRenderer(TrailStyle style, float trailTime, float trailWidth)
    {
        currentStyle = style;
        
        // Podstawowa konfiguracja
        trailRenderer.time = trailTime;
        trailRenderer.minVertexDistance = 0.05f;
        trailRenderer.autodestruct = false;
        trailRenderer.generateLightingData = false;
        trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trailRenderer.receiveShadows = false;
        
        // Użyj domyślnego materiału sprite
        trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
        
        // Konfiguracja specyficzna dla stylu
        switch (style)
        {
            case TrailStyle.Comet:
                ConfigureCometStyle(trailWidth);
                break;
            case TrailStyle.FadingTrail:
                ConfigureFadingStyle(trailWidth);
                break;
            case TrailStyle.ThinUniform:
                ConfigureUniformStyle(trailWidth);
                break;
        }
        
        ApplyColorGradient(style);
    }
    
    private void ConfigureCometStyle(float baseWidth)
    {
        // Kometa: cienki blisko piłki, grubszy dalej (120% na końcu)
        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0f, baseWidth * 0.2f);   // Bardzo cienki przy piłce
        widthCurve.AddKey(0.2f, baseWidth * 0.4f);
        widthCurve.AddKey(0.5f, baseWidth * 0.8f);
        widthCurve.AddKey(0.8f, baseWidth * 1.1f);
        widthCurve.AddKey(1f, baseWidth * 1.2f);   // 120% na końcu ogona
        
        trailRenderer.widthCurve = widthCurve;
    }
    
    private void ConfigureFadingStyle(float baseWidth)
    {
        // Zanikający: stała szerokość
        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0f, baseWidth);
        widthCurve.AddKey(1f, baseWidth * 0.8f);
        
        trailRenderer.widthCurve = widthCurve;
    }
    
    private void ConfigureUniformStyle(float baseWidth)
    {
        // Jednolity: stała szerokość
        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0f, baseWidth * 0.5f);
        widthCurve.AddKey(1f, baseWidth * 0.5f);
        
        trailRenderer.widthCurve = widthCurve;
    }
    
    private void ApplyColorGradient(TrailStyle style)
    {
        Gradient gradient = new Gradient();
        
        switch (style)
        {
            case TrailStyle.Comet:
                // Kometa: ciemny przy piłce, jasny na końcu ogona
                gradient.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(baseColor * 0.4f, 0f),   // Ciemny przy piłce
                        new GradientColorKey(baseColor * 0.7f, 0.3f),
                        new GradientColorKey(baseColor * 1.0f, 0.6f),
                        new GradientColorKey(baseColor * 1.2f, 1f)    // Jasny na końcu
                    },
                    new GradientAlphaKey[]
                    {
                        new GradientAlphaKey(0.3f, 0f),   // Przezroczysty przy piłce
                        new GradientAlphaKey(0.6f, 0.3f),
                        new GradientAlphaKey(0.9f, 0.7f),
                        new GradientAlphaKey(1f, 1f)      // Pełna widoczność na końcu
                    }
                );
                break;
                
            case TrailStyle.FadingTrail:
                // Zanikający: jasny przy piłce, przezroczysty dalej
                gradient.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(baseColor, 0f),
                        new GradientColorKey(baseColor, 1f)
                    },
                    new GradientAlphaKey[]
                    {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(0.6f, 0.3f),
                        new GradientAlphaKey(0.2f, 0.7f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
                break;
                
            case TrailStyle.ThinUniform:
                // Jednolity: stały kolor i przezroczystość
                gradient.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(baseColor, 0f),
                        new GradientColorKey(baseColor, 1f)
                    },
                    new GradientAlphaKey[]
                    {
                        new GradientAlphaKey(0.8f, 0f),
                        new GradientAlphaKey(0.8f, 0.8f),
                        new GradientAlphaKey(0.4f, 1f)
                    }
                );
                break;
                
            default:
                gradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(baseColor, 0f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f) }
                );
                break;
        }
        
        trailRenderer.colorGradient = gradient;
    }
}

