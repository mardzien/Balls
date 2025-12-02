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
    
    private List<Ball> allBalls = new List<Ball>();
    private Ball activeBall;
    
    private enum GameState { Playing, GameOver }
    
    private GameState currentState = GameState.Playing;
    private float gameOverTimer = 0f;
    private float spawnGraceTimer = 0f; // Okres ochronny po spawnie
    private const float SPAWN_GRACE_PERIOD = 0.5f;
    private int roundCount = 0;
    private bool hasRecordedThisSession = false;
    
    // Events
    public event System.Action OnGameOverStart;
    public event System.Action OnGameRestart;
    
    // Public properties
    public int RoundCount => roundCount;
    public IReadOnlyList<Ball> AllBalls => allBalls;
    public bool IsGameOver => currentState == GameState.GameOver;
    public GameSettings Settings => settings;
    public Ring Ring => ring;
    
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
        if (currentState == GameState.Playing)
        {
            CheckForEscape();
        }
        else if (currentState == GameState.GameOver)
        {
            gameOverTimer -= Time.deltaTime;
            if (gameOverTimer <= 0f)
            {
                StartNewRound();
            }
        }
    }
    
    private void SetupGame()
    {
        Physics2D.gravity = new Vector2(0, settings.gravity);
        
        if (ring == null)
        {
            ring = CreateRing();
        }
        ring.Initialize(settings);
        
        // Auto-create components if needed
        if (FindAnyObjectByType<GameOverEffect>() == null)
        {
            gameObject.AddComponent<GameOverEffect>();
        }
        
        if (recordingController == null)
        {
            recordingController = FindAnyObjectByType<RecordingController>();
            if (recordingController == null)
            {
                recordingController = gameObject.AddComponent<RecordingController>();
            }
        }
    }
    
    private Ring CreateRing()
    {
        GameObject ringObj = new GameObject("Ring");
        ringObj.AddComponent<LineRenderer>();
        ringObj.AddComponent<EdgeCollider2D>();
        return ringObj.AddComponent<Ring>();
    }
    
    private void StartNewRound()
    {
        roundCount++;
        ClearAllBalls();
        
        ring.ResetRotation();
        ring.SetVisible(true);
        ring.SetColor(settings.ringColor);
        
        SpawnNewBall();
        currentState = GameState.Playing;
        
        // Auto-start recording (once per session)
        if (settings.autoRecording && recordingController != null && !hasRecordedThisSession)
        {
            hasRecordedThisSession = true;
            recordingController.StartRecording();
        }
        
        OnGameRestart?.Invoke();
    }
    
    private void SpawnNewBall()
    {
        GameObject ballObj = new GameObject($"Ball_{allBalls.Count}");
        
        SpriteRenderer sr = ballObj.AddComponent<SpriteRenderer>();
        ballObj.AddComponent<CircleCollider2D>();
        ballObj.AddComponent<Rigidbody2D>();
        Ball ball = ballObj.AddComponent<Ball>();
        
        sr.sprite = SpriteUtility.GetBallSprite();
        ball.Initialize(settings, settings.useRandomBallColors);
        
        Vector2 spawnPos = ring.GetRandomSpawnPosition();
        ball.transform.position = new Vector3(spawnPos.x, spawnPos.y, 0);
        
        ball.OnFrozen += OnBallFrozen;
        ball.OnBounce += OnBallBounce;
        
        allBalls.Add(ball);
        activeBall = ball;
        spawnGraceTimer = SPAWN_GRACE_PERIOD; // Reset grace period
    }
    
    private void OnBallBounce(float relativeVelocity)
    {
        // Przekaż zdarzenie kolizji do CollisionRecorder
        if (recordingController != null && recordingController.CollisionRecorder != null)
        {
            recordingController.CollisionRecorder.RecordCollision(relativeVelocity);
        }
    }
    
    private void OnBallFrozen(Ball frozenBall)
    {
        if (currentState != GameState.Playing) return;
        SpawnNewBall();
    }
    
    private void CheckForEscape()
    {
        if (activeBall == null || activeBall.IsFrozen) return;
        
        // Grace period po spawnie - nie sprawdzaj ucieczki przez pierwsze 0.5s
        if (spawnGraceTimer > 0f)
        {
            spawnGraceTimer -= Time.deltaTime;
            return;
        }
        
        Vector2 ringCenter = ring.Center;
        float distance = activeBall.GetDistanceFromCenter(ringCenter);
        float escapeThreshold = ring.InnerRadius + settings.escapeBuffer;
        
        if (distance > escapeThreshold)
        {
            Debug.Log($"[Game] Ball escaped! Distance: {distance:F2}, Threshold: {escapeThreshold:F2}");
            TriggerGameOver();
        }
    }
    
    private void TriggerGameOver()
    {
        if (currentState != GameState.Playing) return;
        
        currentState = GameState.GameOver;
        gameOverTimer = settings.gameOverAnimationDuration;
        
        OnGameOverStart?.Invoke();
        
        if (activeBall != null && !activeBall.IsFrozen)
        {
            activeBall.DisableFreezeTimer();
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
