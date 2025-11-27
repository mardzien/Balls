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
    [SerializeField] private GameSettings settings;
    
    [Header("Ball Fragment Settings")]
    [Tooltip("Liczba fragmentów na kulkę")]
    [SerializeField] private int fragmentsPerBall = 6;
    
    [Tooltip("Siła eksplozji fragmentów")]
    [SerializeField] private float explosionForce = 5f;
    
    [Header("Ring Burn Settings")]
    [Tooltip("Kolor początkowy płomienia")]
    [SerializeField] private Color burnColorStart = new Color(1f, 0.5f, 0f);
    
    [Tooltip("Kolor końcowy płomienia")]
    [SerializeField] private Color burnColorEnd = new Color(1f, 0f, 0f);
    
    private List<GameObject> fragments = new List<GameObject>();
    private bool isAnimating = false;
    private float animationTime = 0f;
    private float animationDuration = 2f;
    
    private void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
        }
        
        if (ring == null)
        {
            ring = FindAnyObjectByType<Ring>();
        }
        
        if (settings == null)
        {
            settings = FindAnyObjectByType<GameSettings>();
            if (settings == null)
            {
                // Spróbuj znaleźć w ustawieniach
                var allSettings = Resources.FindObjectsOfTypeAll<GameSettings>();
                if (allSettings.Length > 0)
                {
                    settings = allSettings[0];
                }
            }
        }
        
        if (gameManager != null)
        {
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
        animationDuration = settings != null ? settings.gameOverAnimationDuration : 2f;
        
        // Stwórz fragmenty z zamrożonych kulek
        CreateBallFragments();
        
        Debug.Log("[Effect] Game Over animation started");
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
            float fragmentSize = ballRadius * 0.5f;
            fragment.transform.localScale = Vector3.one * fragmentSize;
            
            // Dodaj fizykę
            Rigidbody2D rb = fragment.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
            
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
    
    private void AnimateRingBurn(float progress)
    {
        if (ring == null) return;
        
        // Interpoluj kolor od burnColorStart do burnColorEnd
        Color currentColor = Color.Lerp(burnColorStart, burnColorEnd, progress);
        
        // Dodaj migotanie
        float flicker = 1f + Mathf.Sin(progress * 20f) * 0.1f;
        currentColor *= flicker;
        
        // Fade out na końcu
        if (progress > 0.7f)
        {
            float fadeProgress = (progress - 0.7f) / 0.3f;
            currentColor.a = 1f - fadeProgress;
        }
        
        ring.SetColor(currentColor);
    }
    
    private void AnimateFragments(float progress)
    {
        // Fade out fragmentów
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
        
        // Usuń wszystkie fragmenty
        foreach (var fragment in fragments)
        {
            if (fragment != null)
            {
                Destroy(fragment);
            }
        }
        fragments.Clear();
        
        // Przywróć kolor pierścienia
        if (ring != null && settings != null)
        {
            ring.SetColor(settings.ringColor);
        }
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

