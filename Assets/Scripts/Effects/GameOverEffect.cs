using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Efekty końcowe gry - rozpad kulek i pierścienia na cząsteczki.
/// </summary>
public class GameOverEffect : MonoBehaviour
{
    [Header("Ball Fragment Settings")]
    [SerializeField] private int fragmentsPerBall = 16;
    [SerializeField] private float explosionForce = 6f;
    
    [Header("Ring Destruction Settings")]
    [SerializeField] private float ringExplosionForce = 3f;
    [SerializeField] private Color ringColorStart = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color ringColorEnd = new Color(1f, 0f, 0f);
    
    private GameManager gameManager;
    private Ring ring;
    private List<GameObject> fragments = new List<GameObject>();
    private List<GameObject> ringParticles = new List<GameObject>();
    private bool isAnimating = false;
    private float animationTime = 0f;
    private float animationDuration = 2f;
    
    private void Start()
    {
        gameManager = GetComponent<GameManager>() ?? FindAnyObjectByType<GameManager>();
        
        if (gameManager != null)
        {
            ring = gameManager.Ring;
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
        
        if (ring == null) ring = gameManager?.Ring;
        
        CreateBallFragments();
        CreateRingParticles();
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
    
    private void CreateRingParticles()
    {
        if (ring == null) return;
        
        var settings = gameManager?.Settings;
        if (settings == null) return;
        
        ring.SetVisible(false);
        
        float ringRadius = settings.ringRadius;
        float gapAngle = settings.gapAngleDegrees;
        float arcAngle = 360f - gapAngle;
        float startAngle = 90f + gapAngle / 2f;
        int particleCount = settings.ringParticleCount;
        
        for (int i = 0; i < particleCount; i++)
        {
            float progress = i / (float)particleCount;
            float angle = (startAngle + progress * arcAngle) * Mathf.Deg2Rad;
            
            float x = ring.Center.x + Mathf.Cos(angle) * ringRadius;
            float y = ring.Center.y + Mathf.Sin(angle) * ringRadius;
            Vector3 position = new Vector3(x, y, 0);
            
            Color color = Color.Lerp(ringColorStart, ringColorEnd, Random.Range(0f, 1f));
            float size = settings.ringThickness * Random.Range(0.3f, 0.8f);
            
            GameObject particle = CreateParticle(position, color, size);
            
            Rigidbody2D rb = particle.GetComponent<Rigidbody2D>();
            rb.gravityScale = Random.Range(1.5f, 2.5f);
            
            Vector2 outward = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 direction = (outward * 0.3f + Vector2.down * 0.7f).normalized;
            direction += new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(-0.2f, 0.2f));
            
            rb.linearVelocity = direction * ringExplosionForce * Random.Range(0.5f, 1.5f);
            rb.angularVelocity = Random.Range(-180f, 180f);
            
            ringParticles.Add(particle);
        }
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
        
        foreach (var particle in ringParticles)
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
        DestroyAll(ringParticles);
        
        // Wyczyść cząsteczki komety
        BallTrailEffect.ClearAllCometParticles();
        
        if (ring != null)
        {
            ring.SetVisible(true);
            if (gameManager?.Settings != null)
            {
                ring.SetColor(gameManager.Settings.ringColor);
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
