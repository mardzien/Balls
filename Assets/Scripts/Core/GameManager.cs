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
    
    // Track current shape type for recreation
    private ShapeType currentShapeType;
    
    private List<Ball> allBalls = new List<Ball>();
    private Ball activeBall;
    private BallCounterUI globalCounter; // Globalny licznik UI
    
    private enum GameState { Playing, GameOver }
    
    private GameState currentState = GameState.Playing;
    private float gameOverTimer = 0f;
    private float spawnGraceTimer = 0f; // Okres ochronny po spawnie
    private const float SPAWN_GRACE_PERIOD = 0.5f;
    private int roundCount = 0;
    private bool hasRecordedThisSession = false;
    private int ballSpawnIndex = 0; // Globalny licznik spawnowanych piłek (dla inkrementacji)
    
    // Fixed spawn position for round (when fixedSpawnPosition is enabled)
    private Vector2 cachedSpawnPosition;
    private bool hasValidCachedSpawnPosition = false;
    
    // Battle mode fields
    private Ball activeBall2;                    // Second active ball (Battle mode)
    private Vector2 cachedSpawnPosition2;        // Symmetric spawn position (Battle mode)
    private Color battleColor1;                  // Color for ball 1 (Battle mode)
    private Color battleColor2;                  // Color for ball 2 (complementary, Battle mode)
    private bool isRespawningBattleBalls = false; // Flag to prevent duplicate respawning
    
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
        
        // Find or create BatchRecordingController for Batch mode
        if (batchRecordingController == null)
        {
            batchRecordingController = FindAnyObjectByType<BatchRecordingController>();
            if (batchRecordingController == null && settings.recordingMode == RecordingMode.Batch)
            {
                batchRecordingController = gameObject.AddComponent<BatchRecordingController>();
            }
        }
        
        // Create global counter UI
        CreateGlobalCounter();
    }
    
    /// <summary>
    /// Tworzy globalny licznik UI pod kształtem.
    /// </summary>
    private void CreateGlobalCounter()
    {
        if (globalCounter != null) return; // Już istnieje
        
        // Nie twórz licznika jeśli wyłączony w ustawieniach
        if (!settings.showCounter) return;
        
        GameObject counterObj = new GameObject("GlobalCounter");
        globalCounter = counterObj.AddComponent<BallCounterUI>();
        
        // Oblicz początkową pozycję
        Vector3 positionUnderShape = CalculateCounterPosition();
        globalCounter.Initialize(settings, positionUnderShape);
    }
    
    /// <summary>
    /// Aktualizuje pozycję globalnego licznika (np. gdy kształt się zmieni).
    /// </summary>
    private void UpdateCounterPosition()
    {
        if (globalCounter == null || shape == null) return;
        
        Vector3 newPosition = CalculateCounterPosition();
        globalCounter.transform.position = newPosition;
    }
    
    /// <summary>
    /// Oblicza pozycję licznika pod dolną krawędzią kształtu.
    /// </summary>
    private Vector3 CalculateCounterPosition()
    {
        if (shape == null) return Vector3.zero;
        
        // Pozycja dynamicznie obliczona - tuż pod dolną krawędzią kształtu
        float bottomEdge = shape.Center.y - shape.OuterRadius;
        float counterOffset = -1.5f; // Małe przesunięcie w dół od krawędzi
        return new Vector3(shape.Center.x, bottomEdge + counterOffset, 0);
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
        ballSpawnIndex = 0; // Reset licznika piłek dla nowej rundy
        ClearAllBalls();
        
        // IMPORTANT: Invoke OnGameRestart FIRST so randomization happens BEFORE spawning
        // This ensures the ball and all components use the correct settings for THIS round
        OnGameRestart?.Invoke();
        
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
        
        // Aktualizuj pozycję licznika (kształt mógł się zmienić)
        UpdateCounterPosition();
        
        // Cache spawn position for the round
        // In Battle mode, always use fixed positions (symmetric)
        bool useFixedPosition = settings.fixedSpawnPosition || settings.gameMode == GameMode.Battle;
        if (useFixedPosition)
        {
            cachedSpawnPosition = shape.GetRandomSpawnPosition();
            // Symmetric position (mirror on Y axis) - relative to shape center
            Vector2 center = shape.Center;
            Vector2 relativePos = cachedSpawnPosition - center;
            cachedSpawnPosition2 = center + new Vector2(-relativePos.x, relativePos.y);
            hasValidCachedSpawnPosition = true;
        }
        else
        {
            hasValidCachedSpawnPosition = false;
        }
        
        // Spawn balls based on game mode
        if (settings.gameMode == GameMode.Battle)
        {
            SpawnBattleBalls();
        }
        else
        {
            SpawnNewBall();
        }
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
        
        // Oblicz wartości dla tej piłki (z inkrementacją lub bez)
        float freezeTime = settings.ballFreezeTime;
        int maxBounces = settings.ballMaxBounces;
        
        if (settings.enableFreezeIncrementation)
        {
            if (settings.freezeMode == FreezeMode.Time)
            {
                freezeTime += ballSpawnIndex * settings.freezeIncrementStep;
            }
            else if (settings.freezeMode == FreezeMode.Bounces)
            {
                maxBounces += Mathf.RoundToInt(ballSpawnIndex * settings.freezeIncrementStep);
            }
        }
        
        ball.Initialize(settings, null, freezeTime, maxBounces);
        ball.SetShape(shape);
        ballSpawnIndex++; // Inkrementuj globalny licznik
        
        // Use cached position if fixedSpawnPosition is enabled, otherwise generate new random position
        Vector2 spawnPos = (settings.fixedSpawnPosition && hasValidCachedSpawnPosition) 
            ? cachedSpawnPosition 
            : shape.GetRandomSpawnPosition();
        ball.transform.position = new Vector3(spawnPos.x, spawnPos.y, 0);
        
        ball.OnFrozen += OnBallFrozen;
        ball.OnBounce += OnBallBounce;
        
        allBalls.Add(ball);
        activeBall = ball;
        spawnGraceTimer = SPAWN_GRACE_PERIOD; // Reset grace period
        
        // Ustaw aktywną piłkę w globalnym liczniku
        if (globalCounter != null)
        {
            globalCounter.SetActiveBall(activeBall);
        }
    }
    
    /// <summary>
    /// Spawnuje dwie piłki w symetrycznych pozycjach z kolorami komplementarnymi (tryb Battle).
    /// </summary>
    private void SpawnBattleBalls()
    {
        // Generate complementary colors
        float hue1 = Random.Range(0f, 1f);
        float hue2 = (hue1 + 0.5f) % 1f; // Complementary color (180° shift)
        float saturation = Random.Range(settings.ballColorMinSaturation, 1f);
        float brightness = Random.Range(settings.ballColorMinBrightness, 1f);
        
        battleColor1 = Color.HSVToRGB(hue1, saturation, brightness);
        battleColor2 = Color.HSVToRGB(hue2, saturation, brightness);
        
        // Spawn ball 1
        activeBall = CreateBattleBall(cachedSpawnPosition, battleColor1, 0);
        
        // Spawn ball 2 (symmetric position)
        activeBall2 = CreateBattleBall(cachedSpawnPosition2, battleColor2, 1);
        
        // Inkrementuj ballSpawnIndex tylko raz (obie piłki mają te same wartości)
        ballSpawnIndex++;
        
        spawnGraceTimer = SPAWN_GRACE_PERIOD;
        
        // Ustaw aktywną piłkę w globalnym liczniku (śledzi pierwszą piłkę)
        if (globalCounter != null)
        {
            globalCounter.SetActiveBall(activeBall);
        }
    }
    
    /// <summary>
    /// Tworzy pojedynczą piłkę dla trybu Battle z określonym kolorem i pozycją.
    /// </summary>
    private Ball CreateBattleBall(Vector2 position, Color color, int index)
    {
        GameObject ballObj = new GameObject($"BattleBall_{index}");
        
        SpriteRenderer sr = ballObj.AddComponent<SpriteRenderer>();
        ballObj.AddComponent<CircleCollider2D>();
        ballObj.AddComponent<Rigidbody2D>();
        Ball ball = ballObj.AddComponent<Ball>();
        
        sr.sprite = SpriteUtility.GetBallSprite();
        
        // Oblicz wartości dla tej piłki (z inkrementacją lub bez)
        // W trybie Battle obie piłki mają te same wartości (ten sam ballSpawnIndex)
        float freezeTime = settings.ballFreezeTime;
        int maxBounces = settings.ballMaxBounces;
        
        if (settings.enableFreezeIncrementation)
        {
            if (settings.freezeMode == FreezeMode.Time)
            {
                freezeTime += ballSpawnIndex * settings.freezeIncrementStep;
            }
            else if (settings.freezeMode == FreezeMode.Bounces)
            {
                maxBounces += Mathf.RoundToInt(ballSpawnIndex * settings.freezeIncrementStep);
            }
        }
        
        ball.Initialize(settings, color, freezeTime, maxBounces); // Pass specific color and limits
        ball.SetShape(shape);
        
        ballObj.transform.position = new Vector3(position.x, position.y, 0);
        
        ball.OnFrozen += OnBallFrozen;
        ball.OnBounce += OnBallBounce;
        
        allBalls.Add(ball);
        return ball;
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
        
        if (settings.gameMode == GameMode.Battle)
        {
            // Prevent duplicate respawning (both balls trigger this event)
            if (isRespawningBattleBalls) return;
            isRespawningBattleBalls = true;
            
            // In Battle mode, freeze both balls and respawn both
            FreezeBothBattleBalls();
            RespawnBattleBalls();
            
            isRespawningBattleBalls = false;
        }
        else
        {
            SpawnNewBall();
        }
    }
    
    /// <summary>
    /// Zamraża obie piłki w trybie Battle.
    /// </summary>
    private void FreezeBothBattleBalls()
    {
        if (activeBall != null && !activeBall.IsFrozen)
        {
            activeBall.Freeze();
        }
        if (activeBall2 != null && !activeBall2.IsFrozen)
        {
            activeBall2.Freeze();
        }
    }
    
    /// <summary>
    /// Respawnuje obie piłki w trybie Battle z ich początkowych pozycji.
    /// </summary>
    private void RespawnBattleBalls()
    {
        // Spawn new pair of balls with the same colors
        activeBall = CreateBattleBall(cachedSpawnPosition, battleColor1, allBalls.Count);
        activeBall2 = CreateBattleBall(cachedSpawnPosition2, battleColor2, allBalls.Count);
        spawnGraceTimer = SPAWN_GRACE_PERIOD;
        
        // Ustaw aktywną piłkę w globalnym liczniku (śledzi pierwszą piłkę)
        if (globalCounter != null)
        {
            globalCounter.SetActiveBall(activeBall);
        }
    }
    
    private void CheckForEscape()
    {
        // Grace period po spawnie - nie sprawdzaj ucieczki przez pierwsze 0.5s
        if (spawnGraceTimer > 0f)
        {
            spawnGraceTimer -= Time.deltaTime;
            return;
        }
        
        // Check ball 1
        if (activeBall != null && !activeBall.IsFrozen)
        {
            Vector2 ballPosition = activeBall.transform.position;
            if (shape.IsPointOutsideShape(ballPosition, settings.escapeBuffer))
            {
                Debug.Log($"[Game] Ball 1 escaped!");
                TriggerGameOver();
                return;
            }
        }
        
        // Check ball 2 (Battle mode only)
        if (settings.gameMode == GameMode.Battle && activeBall2 != null && !activeBall2.IsFrozen)
        {
            Vector2 ball2Position = activeBall2.transform.position;
            if (shape.IsPointOutsideShape(ball2Position, settings.escapeBuffer))
            {
                Debug.Log($"[Game] Ball 2 escaped!");
                TriggerGameOver();
                return;
            }
        }
    }
    
    
    private void TriggerGameOver()
    {
        if (currentState != GameState.Playing) return;
        
        currentState = GameState.GameOver;
        gameOverTimer = settings.gameOverAnimationDuration;
        
        OnGameOverStart?.Invoke();
        
        // Disable freeze timer for active balls
        if (activeBall != null && !activeBall.IsFrozen)
        {
            activeBall.DisableFreezeTimer();
        }
        if (activeBall2 != null && !activeBall2.IsFrozen)
        {
            activeBall2.DisableFreezeTimer();
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
        activeBall2 = null;
    }
    
    private void OnDestroy()
    {
        ClearAllBalls();
    }
    
    /// <summary>
    /// Wymusza natychmiastowy restart rundy (bez animacji game over).
    /// Używane przez BatchRecordingController gdy runda trwa za długo.
    /// </summary>
    public void ForceRestart()
    {
        currentState = GameState.Playing;
        StartNewRound();
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
