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
    [SerializeField] private SpawnShape shape;
    [SerializeField] private RecordingController recordingController;
    [SerializeField] private BatchRecordingController batchRecordingController;
    
    // Backwards compatibility - jeśli w scenie jest stary Ring, użyj go
    [SerializeField] private Ring legacyRing;
    
    // Track current shape type for recreation
    private ShapeType currentShapeType;
    
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
    
    /// <summary>
    /// Aktualny kształt spawnu (Ring lub Ellipse).
    /// </summary>
    public SpawnShape Shape => shape;
    
    /// <summary>
    /// Backwards compatibility - zwraca Ring jeśli aktualny kształt to Ring.
    /// </summary>
    public Ring Ring => shape as Ring;
    
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
        
        // Backwards compatibility - użyj starego Ring jeśli jest przypisany
        if (shape == null && legacyRing != null)
        {
            shape = legacyRing;
        }
        
        // Utwórz nowy kształt jeśli nie ma żadnego
        if (shape == null)
        {
            shape = CreateShape(settings.shapeType);
        }
        
        currentShapeType = settings.shapeType;
        shape.Initialize(settings);
        
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
        
        // Find BatchRecordingController if exists
        if (batchRecordingController == null)
        {
            batchRecordingController = FindAnyObjectByType<BatchRecordingController>();
        }
    }
    
    /// <summary>
    /// Factory method - tworzy odpowiedni kształt na podstawie typu.
    /// </summary>
    private SpawnShape CreateShape(ShapeType shapeType)
    {
        string shapeName = shapeType.ToString();
        GameObject shapeObj = new GameObject(shapeName);
        shapeObj.AddComponent<LineRenderer>();
        shapeObj.AddComponent<EdgeCollider2D>();
        
        SpawnShape newShape = shapeType switch
        {
            ShapeType.Ring => shapeObj.AddComponent<Ring>(),
            ShapeType.Ellipse => shapeObj.AddComponent<EllipseShape>(),
            _ => shapeObj.AddComponent<Ring>()
        };
        
        Debug.Log($"[GameManager] Created shape: {shapeName}");
        return newShape;
    }
    
    private void StartNewRound()
    {
        roundCount++;
        ClearAllBalls();
        
        // Check if shape type changed (due to randomization) - recreate if needed
        if (settings.shapeType != currentShapeType)
        {
            RecreateShape();
        }
        
        // Update gravity in case it was randomized
        Physics2D.gravity = new Vector2(0, settings.gravity);
        
        shape.Initialize(settings);
        shape.ResetRotation();
        shape.SetVisible(true);
        shape.SetColor(settings.ringColor);
        
        SpawnNewBall();
        currentState = GameState.Playing;
        
        // Auto-start single recording (once per session) - only if mode is Single
        if (settings.recordingMode == RecordingMode.Single && 
            settings.autoStartRecording && 
            recordingController != null && 
            !hasRecordedThisSession)
        {
            hasRecordedThisSession = true;
            recordingController.StartRecording();
        }
        
        OnGameRestart?.Invoke();
    }
    
    /// <summary>
    /// Odtwarza kształt gdy zmieni się typ (np. Ring -> Ellipse).
    /// </summary>
    private void RecreateShape()
    {
        Debug.Log($"[GameManager] Shape type changed: {currentShapeType} -> {settings.shapeType}");
        
        // Destroy old shape
        if (shape != null)
        {
            Destroy(shape.gameObject);
        }
        
        // Create new shape
        shape = CreateShape(settings.shapeType);
        currentShapeType = settings.shapeType;
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
        
        Vector2 spawnPos = shape.GetRandomSpawnPosition();
        ball.transform.position = new Vector3(spawnPos.x, spawnPos.y, 0);
        
        ball.OnFrozen += OnBallFrozen;
        ball.OnBounce += OnBallBounce;
        
        allBalls.Add(ball);
        activeBall = ball;
        spawnGraceTimer = SPAWN_GRACE_PERIOD; // Reset grace period
    }
    
    private void OnBallBounce(float relativeVelocity)
    {
        // Przekaż zdarzenie kolizji do CollisionRecorder (standard recording)
        if (recordingController != null && recordingController.CollisionRecorder != null)
        {
            recordingController.CollisionRecorder.RecordCollision(relativeVelocity);
        }
        
        // Przekaż zdarzenie kolizji do BatchRecordingController (batch recording)
        if (batchRecordingController != null && batchRecordingController.CollisionRecorder != null)
        {
            batchRecordingController.CollisionRecorder.RecordCollision(relativeVelocity);
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
        
        Vector2 ballPosition = activeBall.transform.position;
        
        // Sprawdź czy piłka jest poza granicą kształtu
        if (shape.IsPointOutsideShape(ballPosition, settings.escapeBuffer))
        {
            Debug.Log($"[Game] Ball escaped!");
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
    /// Zatrzymuje nagrywanie pojedyncze (wywoływane przez efekty końcowe).
    /// Nie dotyczy batch recording - tam nagrania są zarządzane automatycznie.
    /// </summary>
    public void StopRecordingIfNeeded()
    {
        if (settings.recordingMode == RecordingMode.Single && 
            recordingController != null && 
            recordingController.IsRecording)
        {
            recordingController.StopRecording();
        }
    }
}
