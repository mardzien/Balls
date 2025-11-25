using UnityEngine;

/// <summary>
/// Główny manager gry - zarządza pętlą gry, spawnowaniem i restartami.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameSettings settings;
    
    [Header("Prefabs")]
    [SerializeField] private GameObject ringPrefab;
    [SerializeField] private GameObject ballPrefab;
    
    [Header("Scene References (optional - will be created if null)")]
    [SerializeField] private Ring ring;
    [SerializeField] private Ball ball;
    [SerializeField] private EscapeDetector escapeDetector;
    [SerializeField] private ScreenSetup screenSetup;
    
    private enum GameState
    {
        Playing,
        GameOver,
        Restarting
    }
    
    private GameState currentState = GameState.Playing;
    private float restartTimer = 0f;
    private int roundCount = 0;
    
    /// <summary>
    /// Aktualny numer rundy.
    /// </summary>
    public int RoundCount => roundCount;
    
    private void Start()
    {
        if (settings == null)
        {
            Debug.LogError("GameManager: GameSettings nie jest przypisany!");
            return;
        }
        
        SetupGame();
        StartNewRound();
    }
    
    private void Update()
    {
        switch (currentState)
        {
            case GameState.Playing:
                // Gra trwa - logika w EscapeDetector
                break;
                
            case GameState.GameOver:
                // Czekaj na restart
                restartTimer -= Time.deltaTime;
                if (restartTimer <= 0f)
                {
                    currentState = GameState.Restarting;
                    StartNewRound();
                }
                break;
                
            case GameState.Restarting:
                // Przejściowy stan podczas restartu
                break;
        }
    }
    
    private void SetupGame()
    {
        // Ustaw grawitację w Physics2D
        Physics2D.gravity = new Vector2(0, settings.gravity);
        
        // Skonfiguruj ekran i kamerę dla YouTube Shorts (9:16)
        SetupScreen();
        
        // Stwórz lub znajdź Ring
        if (ring == null)
        {
            if (ringPrefab != null)
            {
                GameObject ringObj = Instantiate(ringPrefab, Vector3.zero, Quaternion.identity);
                ringObj.name = "Ring";
                ring = ringObj.GetComponent<Ring>();
            }
            else
            {
                ring = CreateRing();
            }
        }
        ring.Initialize(settings);
        
        // Stwórz lub znajdź Ball
        if (ball == null)
        {
            if (ballPrefab != null)
            {
                Vector3 spawnPos = new Vector3(settings.ballSpawnOffset.x, settings.ballSpawnOffset.y, 0);
                GameObject ballObj = Instantiate(ballPrefab, spawnPos, Quaternion.identity);
                ballObj.name = "Ball";
                ball = ballObj.GetComponent<Ball>();
            }
            else
            {
                ball = CreateBall();
            }
        }
        ball.Initialize(settings);
        
        // Stwórz lub znajdź EscapeDetector
        if (escapeDetector == null)
        {
            escapeDetector = gameObject.AddComponent<EscapeDetector>();
        }
        escapeDetector.Initialize(settings, ring, ball);
        escapeDetector.OnBallEscaped += OnBallEscaped;
        
        // Subskrybuj event odbicia (do przyszłych zastosowań)
        ball.OnBounce += OnBallBounce;
    }
    
    private void SetupScreen()
    {
        // Dodaj ScreenSetup jeśli nie istnieje
        if (screenSetup == null)
        {
            screenSetup = gameObject.GetComponent<ScreenSetup>();
            if (screenSetup == null)
            {
                screenSetup = gameObject.AddComponent<ScreenSetup>();
            }
        }
        
        // Skonfiguruj kamerę
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = settings.cameraOrthoSize;
            mainCamera.transform.position = new Vector3(0, 0, -10);
            mainCamera.backgroundColor = Color.black;
        }
    }
    
    private Ring CreateRing()
    {
        GameObject ringObj = new GameObject("Ring");
        ringObj.AddComponent<LineRenderer>();
        ringObj.AddComponent<EdgeCollider2D>();
        Ring ringComponent = ringObj.AddComponent<Ring>();
        return ringComponent;
    }
    
    private Ball CreateBall()
    {
        GameObject ballObj = new GameObject("Ball");
        
        // Dodaj komponenty
        SpriteRenderer sr = ballObj.AddComponent<SpriteRenderer>();
        ballObj.AddComponent<CircleCollider2D>();
        ballObj.AddComponent<Rigidbody2D>();
        Ball ballComponent = ballObj.AddComponent<Ball>();
        
        // Stwórz prosty sprite koła
        sr.sprite = CreateCircleSprite();
        
        // Ustaw pozycję
        ballObj.transform.position = new Vector3(settings.ballSpawnOffset.x, settings.ballSpawnOffset.y, 0);
        
        return ballComponent;
    }
    
    private Sprite CreateCircleSprite()
    {
        // Stwórz prostą teksturę koła
        int size = 64;
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
    
    private void StartNewRound()
    {
        roundCount++;
        Debug.Log($"Starting round {roundCount}");
        
        // Reset pozycji kulki
        Vector2 spawnPosition = settings.ballSpawnOffset;
        ball.ResetBall(spawnPosition);
        
        // Reset pierścienia (opcjonalnie)
        ring.ResetRotation();
        
        // Reset detektora
        escapeDetector.Reset();
        
        // Zmień stan na Playing
        currentState = GameState.Playing;
    }
    
    private void OnBallEscaped()
    {
        if (currentState != GameState.Playing)
            return;
        
        Debug.Log($"Game Over! Round {roundCount} ended. Restarting in {settings.restartDelay}s...");
        
        currentState = GameState.GameOver;
        restartTimer = settings.restartDelay;
    }
    
    private void OnBallBounce()
    {
        // Tu można dodać logikę dla odbić (dźwięki, efekty, liczniki)
        // Debug.Log("Ball bounced!");
    }
    
    private void OnDestroy()
    {
        // Odsubskrybuj eventy
        if (escapeDetector != null)
        {
            escapeDetector.OnBallEscaped -= OnBallEscaped;
        }
        
        if (ball != null)
        {
            ball.OnBounce -= OnBallBounce;
        }
    }
    
    // Debug GUI usunięty - informacje w konsoli (Debug.Log)
}

