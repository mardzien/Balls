using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Główny manager gry - zarządza pętlą gry, spawnowaniem kulek i sekwencją Game Over.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameSettings settings;
    
    [Header("Scene References")]
    [SerializeField] private Ring ring;
    [SerializeField] private RecordingController recordingController;
    
    // Lista wszystkich kulek (aktywne i zamrożone)
    private List<Ball> allBalls = new List<Ball>();
    private Ball activeBall; // Aktualnie aktywna (niezamrożona) kulka
    
    private enum GameState
    {
        Playing,
        GameOver,
        GameOverAnimation,
        Restarting
    }
    
    private GameState currentState = GameState.Playing;
    private float gameOverTimer = 0f;
    private int roundCount = 0;
    
    // Event do komunikacji z efektami końcowymi
    public event System.Action OnGameOverStart;
    public event System.Action OnGameRestart;
    
    /// <summary>
    /// Aktualny numer rundy.
    /// </summary>
    public int RoundCount => roundCount;
    
    /// <summary>
    /// Wszystkie kulki w grze.
    /// </summary>
    public IReadOnlyList<Ball> AllBalls => allBalls;
    
    /// <summary>
    /// Czy gra jest w trakcie animacji końcowej.
    /// </summary>
    public bool IsGameOver => currentState == GameState.GameOver || currentState == GameState.GameOverAnimation;
    
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
                CheckForEscape();
                break;
                
            case GameState.GameOver:
            case GameState.GameOverAnimation:
                gameOverTimer -= Time.deltaTime;
                if (gameOverTimer <= 0f)
                {
                    currentState = GameState.Restarting;
                    StartNewRound();
                }
                break;
                
            case GameState.Restarting:
                break;
        }
    }
    
    private void SetupGame()
    {
        // Ustaw grawitację w Physics2D
        Physics2D.gravity = new Vector2(0, settings.gravity);
        
        // Stwórz lub znajdź Ring
        if (ring == null)
        {
            ring = CreateRing();
        }
        ring.Initialize(settings);
        
        // Znajdź RecordingController
        if (recordingController == null)
        {
            recordingController = GetComponent<RecordingController>();
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
    
    private void StartNewRound()
    {
        roundCount++;
        Debug.Log($"[Game] Starting round {roundCount}");
        
        // Wyczyść wszystkie stare kulki
        ClearAllBalls();
        
        // Reset pierścienia
        ring.ResetRotation();
        ring.SetColor(settings.ringColor);
        
        // Spawn pierwszą kulkę
        SpawnNewBall();
        
        // Zmień stan na Playing
        currentState = GameState.Playing;
        
        // Auto-start nagrywania
        if (settings.autoRecording && recordingController != null && !recordingController.IsRecording)
        {
            recordingController.StartRecording();
        }
        
        OnGameRestart?.Invoke();
    }
    
    private void SpawnNewBall()
    {
        // Stwórz nową kulkę
        GameObject ballObj = new GameObject($"Ball_{allBalls.Count}");
        
        // Dodaj komponenty
        SpriteRenderer sr = ballObj.AddComponent<SpriteRenderer>();
        ballObj.AddComponent<CircleCollider2D>();
        ballObj.AddComponent<Rigidbody2D>();
        Ball ball = ballObj.AddComponent<Ball>();
        
        // Stwórz sprite koła
        sr.sprite = CreateCircleSprite();
        
        // Inicjalizuj kulkę
        ball.Initialize(settings, settings.useRandomBallColors);
        
        // Ustaw pozycję (losową w górnej części pierścienia)
        Vector2 spawnPos = ring.GetRandomSpawnPosition();
        ball.transform.position = new Vector3(spawnPos.x, spawnPos.y, 0);
        
        // Subskrybuj eventy
        ball.OnFrozen += OnBallFrozen;
        ball.OnBounce += OnBallBounce;
        
        // Dodaj do listy
        allBalls.Add(ball);
        activeBall = ball;
        
        Debug.Log($"[Game] Spawned ball at {spawnPos}");
    }
    
    private void OnBallFrozen(Ball frozenBall)
    {
        if (currentState != GameState.Playing) return;
        
        Debug.Log($"[Game] Ball frozen, spawning new one");
        
        // Spawn nową kulkę
        SpawnNewBall();
    }
    
    private void OnBallBounce()
    {
        // Tu można dodać efekty/dźwięki przy odbiciach
    }
    
    private void CheckForEscape()
    {
        if (activeBall == null || activeBall.IsFrozen) return;
        
        Vector2 ringCenter = ring.Center;
        float distance = activeBall.GetDistanceFromCenter(ringCenter);
        float escapeThreshold = ring.InnerRadius + settings.escapeBuffer;
        
        if (distance > escapeThreshold)
        {
            float ballAngle = activeBall.GetAngleFromCenter(ringCenter);
            
            if (ring.IsInGap(ballAngle))
            {
                // Kulka uciekła przez lukę!
                Debug.Log($"[Game] Ball escaped! Distance: {distance:F2}, Angle: {ballAngle:F1}°");
                TriggerGameOver();
            }
            else
            {
                // Kulka jest poza pierścieniem ale NIE przez lukę (błąd fizyki)
                Debug.LogWarning($"[Game] Ball escaped outside gap! Distance: {distance:F2}, Angle: {ballAngle:F1}°");
                TriggerGameOver();
            }
        }
    }
    
    private void TriggerGameOver()
    {
        if (currentState != GameState.Playing) return;
        
        currentState = GameState.GameOver;
        gameOverTimer = settings.gameOverAnimationDuration;
        
        Debug.Log($"[Game] Game Over! Round {roundCount} ended. Animation for {settings.gameOverAnimationDuration}s...");
        
        // Wywołaj event dla efektów końcowych
        OnGameOverStart?.Invoke();
        
        // Odmróź aktywną kulkę żeby mogła spaść
        if (activeBall != null && !activeBall.IsFrozen)
        {
            // Kulka już jest odmrożona, niech spada
        }
    }
    
    private void ClearAllBalls()
    {
        foreach (var ball in allBalls)
        {
            if (ball != null)
            {
                ball.OnFrozen -= OnBallFrozen;
                ball.OnBounce -= OnBallBounce;
                Destroy(ball.gameObject);
            }
        }
        allBalls.Clear();
        activeBall = null;
    }
    
    private Sprite CreateCircleSprite()
    {
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
    
    private void OnDestroy()
    {
        ClearAllBalls();
    }
    
    /// <summary>
    /// Zatrzymuje nagrywanie (wywoływane przez efekty końcowe).
    /// </summary>
    public void StopRecordingIfNeeded()
    {
        if (settings.autoRecording && recordingController != null && recordingController.IsRecording)
        {
            recordingController.StopRecording();
        }
    }
}
