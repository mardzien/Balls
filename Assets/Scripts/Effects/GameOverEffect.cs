using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Efekty końcowe gry - rozpad kulek i płonący pierścień.
/// </summary>
public class GameOverEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Ring ring;
    
    [Header("Ball Fragment Settings")]
    [Tooltip("Liczba fragmentów na kulkę")]
    [SerializeField] private int fragmentsPerBall = 16;
    
    [Tooltip("Siła eksplozji fragmentów")]
    [SerializeField] private float explosionForce = 6f;
    
    [Header("Ring Destruction Settings")]
    [Tooltip("Liczba cząsteczek pyłu z pierścienia")]
    [SerializeField] private int ringParticleCount = 100;
    
    [Tooltip("Siła rozpadu pierścienia")]
    [SerializeField] private float ringExplosionForce = 3f;
    
    [Tooltip("Kolor początkowy pierścienia przy rozpadzie")]
    [SerializeField] private Color ringColorStart = new Color(1f, 0.5f, 0f);
    
    [Tooltip("Kolor końcowy pierścienia przy rozpadzie")]
    [SerializeField] private Color ringColorEnd = new Color(1f, 0f, 0f);
    
    private List<GameObject> fragments = new List<GameObject>();
    private List<GameObject> ringParticles = new List<GameObject>();
    private bool isAnimating = false;
    private float animationTime = 0f;
    private float animationDuration = 2f;
    
    private void Awake()
    {
        // Znajdź GameManager jak najwcześniej
        if (gameManager == null)
        {
            gameManager = GetComponent<GameManager>();
        }
    }
    
    private void Start()
    {
        // Znajdź GameManager
        if (gameManager == null)
        {
            gameManager = GetComponent<GameManager>();
            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GameManager>();
            }
        }
        
        // Znajdź Ring
        if (ring == null)
        {
            ring = FindAnyObjectByType<Ring>();
        }
        
        // Subskrybuj eventy
        if (gameManager != null)
        {
            gameManager.OnGameOverStart += StartGameOverAnimation;
            gameManager.OnGameRestart += CleanupEffects;
            Debug.Log("[Effect] GameOverEffect subscribed to GameManager events");
        }
        else
        {
            Debug.LogError("[Effect] GameManager not found!");
        }
    }
    
    private void Update()
    {
        if (!isAnimating) return;
        
        animationTime += Time.deltaTime;
        float progress = animationTime / animationDuration;
        
        if (progress >= 1f)
        {
            // Koniec animacji - zatrzymaj nagrywanie
            isAnimating = false;
            gameManager?.StopRecordingIfNeeded();
            return;
        }
        
        // Animuj kolor pierścienia (płonący efekt)
        AnimateRingBurn(progress);
        
        // Animuj fragmenty (fade out)
        AnimateFragments(progress);
    }
    
    private void StartGameOverAnimation()
    {
        isAnimating = true;
        animationTime = 0f;
        
        var settings = gameManager?.Settings;
        animationDuration = settings != null ? settings.gameOverAnimationDuration : 2f;
        
        // Pobierz ring z GameManager jeśli nie ustawiony
        if (ring == null && gameManager != null)
        {
            ring = gameManager.Ring;
        }
        
        // Stwórz fragmenty z zamrożonych kulek
        CreateBallFragments();
        
        // Stwórz efekt rozpadu pierścienia
        CreateRingParticles();
        
        Debug.Log($"[Effect] Game Over animation started, duration: {animationDuration}s, frozen balls: {CountFrozenBalls()}");
    }
    
    private int CountFrozenBalls()
    {
        if (gameManager == null) return 0;
        int count = 0;
        foreach (var ball in gameManager.AllBalls)
        {
            if (ball != null && ball.IsFrozen) count++;
        }
        return count;
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
        
        // Ukryj oryginalną kulkę
        ball.gameObject.SetActive(false);
        
        // Stwórz fragmenty
        for (int i = 0; i < fragmentsPerBall; i++)
        {
            GameObject fragment = new GameObject($"Fragment_{fragments.Count}");
            fragment.transform.position = position;
            
            // Dodaj sprite
            SpriteRenderer sr = fragment.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite(16);
            sr.color = ballColor;
            
            // Mniejszy rozmiar
            float fragmentSize = ballRadius * 0.4f;
            fragment.transform.localScale = Vector3.one * fragmentSize;
            
            // Dodaj fizykę
            Rigidbody2D rb = fragment.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1.5f;
            
            // Losowy kierunek eksplozji
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
        
        // Ukryj oryginalny pierścień
        ring.SetVisible(false);
        
        // Pobierz parametry pierścienia
        float ringRadius = settings.ringRadius;
        float gapAngle = settings.gapAngleDegrees;
        float arcAngle = 360f - gapAngle;
        float startAngle = 90f + gapAngle / 2f;
        
        Color ringColor = settings.ringColor;
        
        // Stwórz cząsteczki wzdłuż pierścienia
        for (int i = 0; i < ringParticleCount; i++)
        {
            float progress = i / (float)ringParticleCount;
            float angle = (startAngle + progress * arcAngle) * Mathf.Deg2Rad;
            
            // Pozycja na pierścieniu
            float x = ring.Center.x + Mathf.Cos(angle) * ringRadius;
            float y = ring.Center.y + Mathf.Sin(angle) * ringRadius;
            
            GameObject particle = new GameObject($"RingParticle_{i}");
            particle.transform.position = new Vector3(x, y, 0);
            
            // Dodaj sprite
            SpriteRenderer sr = particle.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite(8);
            
            // Kolor z gradientem od pomarańczowego do czerwonego
            sr.color = Color.Lerp(ringColorStart, ringColorEnd, Random.Range(0f, 1f));
            
            // Losowy rozmiar cząsteczki
            float particleSize = settings.ringThickness * Random.Range(0.3f, 0.8f);
            particle.transform.localScale = Vector3.one * particleSize;
            
            // Dodaj fizykę
            Rigidbody2D rb = particle.AddComponent<Rigidbody2D>();
            rb.gravityScale = Random.Range(1.5f, 2.5f);
            
            // Kierunek eksplozji - głównie w dół i na zewnątrz
            Vector2 outward = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 downward = Vector2.down;
            Vector2 direction = (outward * 0.3f + downward * 0.7f).normalized;
            direction += new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(-0.2f, 0.2f));
            
            rb.linearVelocity = direction * ringExplosionForce * Random.Range(0.5f, 1.5f);
            rb.angularVelocity = Random.Range(-180f, 180f);
            
            ringParticles.Add(particle);
        }
    }
    
    private void AnimateRingBurn(float progress)
    {
        // Pierścień jest ukryty - animacja kolorów cząsteczek
        // Fade out cząsteczek pierścienia
        float alpha = Mathf.Lerp(1f, 0f, progress);
        
        foreach (var particle in ringParticles)
        {
            if (particle == null) continue;
            
            SpriteRenderer sr = particle.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }
        }
    }
    
    private void AnimateFragments(float progress)
    {
        // Fade out fragmentów kulek
        float alpha = 1f - progress;
        
        foreach (var fragment in fragments)
        {
            if (fragment == null) continue;
            
            SpriteRenderer sr = fragment.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }
        }
    }
    
    private void CleanupEffects()
    {
        isAnimating = false;
        
        // Usuń wszystkie fragmenty kulek
        foreach (var fragment in fragments)
        {
            if (fragment != null)
            {
                Destroy(fragment);
            }
        }
        fragments.Clear();
        
        // Usuń wszystkie cząsteczki pierścienia
        foreach (var particle in ringParticles)
        {
            if (particle != null)
            {
                Destroy(particle);
            }
        }
        ringParticles.Clear();
        
        // Przywróć widoczność i kolor pierścienia
        var settings = gameManager?.Settings;
        if (ring != null)
        {
            ring.SetVisible(true);
            if (settings != null)
            {
                ring.SetColor(settings.ringColor);
            }
        }
        
        Debug.Log("[Effect] Effects cleaned up");
    }
    
    private Sprite CreateCircleSprite(int size = 32)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        
        float center = size / 2f;
        float radius = size / 2f - 1f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                
                if (distance <= radius)
                {
                    texture.SetPixel(x, y, Color.white);
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        
        return Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            size
        );
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

