using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

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

    // Duel Breakout runtime state
    private int duelRingIndexP1 = 0;
    private int duelRingIndexP2 = 0;
    private float duelRingHpP1 = 0f;
    private float duelRingHpP2 = 0f;
    private float duelRingMaxHpP1 = 0f;
    private float duelRingMaxHpP2 = 0f;
    private int duelHitCountP1 = 0;
    private int duelHitCountP2 = 0;
    private bool duelInitialized = false;
    private TextMeshPro duelHudTop;
    private TextMeshPro duelHudBottom;
    private SpawnShape duelShapeBottom;
    private GameSettings duelSettingsTop;
    private GameSettings duelSettingsBottom;
    private const float DUEL_CENTER_OFFSET_Y = 5f;
    private readonly List<GameObject> duelVisualRingsTop = new List<GameObject>();
    private readonly List<GameObject> duelVisualRingsBottom = new List<GameObject>();
    
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

    private bool IsBattleMode()
    {
        return settings != null && settings.gameMode == GameMode.Battle;
    }

    private bool IsDuelBreakoutMode()
    {
        return settings != null && settings.gameMode == GameMode.DuelBreakout;
    }

    private int GetDuelConfiguredRingCount()
    {
        if (settings == null || settings.duelRings == null || settings.duelRings.Length == 0)
        {
            return 0;
        }

        return Mathf.Clamp(settings.duelRingCount, 1, settings.duelRings.Length);
    }

    private float ToWorldRadius(float configuredRadius)
    {
        // Duel config domyślnie pochodzi z px (120/200/280), a Unity działa w world units.
        if (configuredRadius > 20f)
        {
            return configuredRadius / 100f;
        }

        return configuredRadius;
    }

    private Color GetBallClassColor(BallClassType ballClass)
    {
        return ballClass switch
        {
            BallClassType.Speedy => new Color(0.3f, 0.9f, 1f),
            BallClassType.Fibonacci => new Color(1f, 0.9f, 0.2f),
            BallClassType.Fractal => new Color(0.8f, 0.5f, 1f),
            BallClassType.Grower => new Color(0.3f, 1f, 0.4f),
            BallClassType.Berserker => new Color(1f, 0.3f, 0.3f),
            BallClassType.Sniper => new Color(0.7f, 0.7f, 1f),
            BallClassType.Combo => new Color(1f, 0.5f, 0.2f),
            BallClassType.Tank => new Color(0.6f, 0.6f, 0.6f),
            BallClassType.Ricochet => new Color(0.9f, 0.9f, 1f),
            BallClassType.FibSpeed => new Color(1f, 0.8f, 0.5f),
            BallClassType.Kolos => new Color(0.4f, 0.9f, 0.4f),
            BallClassType.Blitz => new Color(1f, 0.7f, 0.2f),
            _ => Color.white,
        };
    }
    
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
        if (IsDuelBreakoutMode())
        {
            UpdateDuelHud();
        }

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

    private void EnsureDuelHud()
    {
        if (duelHudTop == null)
        {
            GameObject topObj = new GameObject("DuelHUD_Top");
            duelHudTop = topObj.AddComponent<TextMeshPro>();
            duelHudTop.fontSize = 8f;
            duelHudTop.alignment = TextAlignmentOptions.Center;
            duelHudTop.color = Color.white;
            duelHudTop.fontStyle = FontStyles.Bold;
            duelHudTop.outlineWidth = 0.25f;
            duelHudTop.outlineColor = Color.black;
            duelHudTop.sortingOrder = 200;
        }

        if (duelHudBottom == null)
        {
            GameObject bottomObj = new GameObject("DuelHUD_Bottom");
            duelHudBottom = bottomObj.AddComponent<TextMeshPro>();
            duelHudBottom.fontSize = 8f;
            duelHudBottom.alignment = TextAlignmentOptions.Center;
            duelHudBottom.color = Color.white;
            duelHudBottom.fontStyle = FontStyles.Bold;
            duelHudBottom.outlineWidth = 0.25f;
            duelHudBottom.outlineColor = Color.black;
            duelHudBottom.sortingOrder = 200;
        }
    }

    private void UpdateDuelHud()
    {
        if (!IsDuelBreakoutMode())
        {
            return;
        }

        EnsureDuelHud();

        if (shape == null)
        {
            return;
        }

        int ringCount = GetDuelConfiguredRingCount();
        int currentRingDisplayP1 = Mathf.Min(duelRingIndexP1 + 1, Mathf.Max(1, ringCount));
        int currentRingDisplayP2 = Mathf.Min(duelRingIndexP2 + 1, Mathf.Max(1, ringCount));

        if (duelHudTop != null)
        {
            duelHudTop.transform.position = new Vector3(0f, 8.8f, 0f);
            duelHudTop.text =
                $"P1 [{settings.ballClass1}]\\nRing {currentRingDisplayP1}/{ringCount}  HP {duelRingHpP1:0}/{duelRingMaxHpP1:0}";
        }

        if (duelHudBottom != null)
        {
            duelHudBottom.transform.position = new Vector3(0f, -8.8f, 0f);
            duelHudBottom.text =
                $"P2 [{settings.ballClass2}]\\nRing {currentRingDisplayP2}/{ringCount}  HP {duelRingHpP2:0}/{duelRingMaxHpP2:0}";
        }
    }

    private void SetDuelHudVisible(bool visible)
    {
        if (duelHudTop != null)
        {
            duelHudTop.gameObject.SetActive(visible);
        }
        if (duelHudBottom != null)
        {
            duelHudBottom.gameObject.SetActive(visible);
        }
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
        
        // DuelBreakout działa na pierścieniu
        if (IsDuelBreakoutMode())
        {
            settings.shapeType = ShapeType.Ring;
        }

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

        // Inicjalizacja state machine DuelBreakout po odświeżeniu shape
        if (IsDuelBreakoutMode())
        {
            InitializeDuelBreakoutRound();
            if (globalCounter != null) globalCounter.Hide();
            SetDuelHudVisible(true);
        }
        else
        {
            duelInitialized = false;
            if (globalCounter != null) globalCounter.Show();
            SetDuelHudVisible(false);
            if (duelShapeBottom != null) duelShapeBottom.SetVisible(false);
            ClearDuelVisualRings(0);
            ClearDuelVisualRings(1);
        }
        
        // Aktualizuj pozycję licznika (kształt mógł się zmienić)
        UpdateCounterPosition();
        
        // Cache spawn position for the round
        // In Battle mode, always use fixed symmetric positions
        bool useFixedPosition = settings.fixedSpawnPosition || IsBattleMode();
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
        if (IsBattleMode())
        {
            SpawnBattleBalls();
        }
        else if (IsDuelBreakoutMode())
        {
            SpawnDuelBreakoutBalls();
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
    /// Spawn 2 piłek w DuelBreakout:
    /// - P1 startuje w górnej połowie
    /// - P2 startuje w dolnej połowie
    /// </summary>
    private void SpawnDuelBreakoutBalls()
    {
        EnsureDuelShapes();

        // Wyczyść aktywne piłki duel
        if (activeBall != null)
        {
            activeBall.OnFrozen -= OnBallFrozen;
            activeBall.OnBounce -= OnBallBounce;
            Destroy(activeBall.gameObject);
            activeBall = null;
        }
        if (activeBall2 != null)
        {
            activeBall2.OnFrozen -= OnBallFrozen;
            activeBall2.OnBounce -= OnBallBounce;
            Destroy(activeBall2.gameObject);
            activeBall2 = null;
        }

        if (shape == null || duelShapeBottom == null)
        {
            return;
        }

        Vector2 centerTop = shape.Center;
        Vector2 centerBottom = duelShapeBottom.Center;
        float spawnOffsetTop = Mathf.Max(0.4f, GetDuelSettings(0).ringRadius * 0.35f);
        float spawnOffsetBottom = Mathf.Max(0.4f, GetDuelSettings(1).ringRadius * 0.35f);

        // P1 (góra)
        activeBall = CreateDuelBall(
            0,
            centerTop + new Vector2(0f, spawnOffsetTop),
            allBalls.Count
        );

        // P2 (dół)
        activeBall2 = CreateDuelBall(
            1,
            centerBottom + new Vector2(0f, -spawnOffsetBottom),
            allBalls.Count + 1
        );

        if (activeBall != null)
        {
            activeBall.DisableFreezeTimer();
            Rigidbody2D rb1 = activeBall.GetComponent<Rigidbody2D>();
            if (rb1 != null)
            {
                Vector2 dir = new Vector2(0.55f, 0.85f).normalized;
                rb1.linearVelocity = dir * GetDuelBaseSpeed(settings.ballClass1);
            }
        }

        if (activeBall2 != null)
        {
            activeBall2.DisableFreezeTimer();
            Rigidbody2D rb2 = activeBall2.GetComponent<Rigidbody2D>();
            if (rb2 != null)
            {
                Vector2 dir = new Vector2(-0.55f, -0.85f).normalized;
                rb2.linearVelocity = dir * GetDuelBaseSpeed(settings.ballClass2);
            }
        }

        spawnGraceTimer = SPAWN_GRACE_PERIOD;
    }

    private Ball CreateDuelBall(int playerId, Vector2 position, int index)
    {
        SpawnShape playerShape = GetDuelShape(playerId);
        GameSettings playerSettings = GetDuelSettings(playerId);
        BallClassType playerClass = GetPlayerBallClass(playerId);

        GameObject ballObj = new GameObject($"DuelBall_P{playerId + 1}_{index}");
        SpriteRenderer sr = ballObj.AddComponent<SpriteRenderer>();
        ballObj.AddComponent<CircleCollider2D>();
        ballObj.AddComponent<Rigidbody2D>();
        Ball ball = ballObj.AddComponent<Ball>();
        sr.sprite = SpriteUtility.GetBallSprite();

        Color color = GetBallClassColor(playerClass);
        ball.Initialize(playerSettings, color, playerSettings.ballFreezeTime, playerSettings.ballMaxBounces);
        ball.SetShape(playerShape);
        ball.DisableFreezeTimer();
        ballObj.transform.position = new Vector3(position.x, position.y, 0);

        ball.OnBounce += (v) => HandleDuelBreakoutHit(playerId, v);
        allBalls.Add(ball);
        return ball;
    }

    private void InitializeDuelBreakoutRound()
    {
        duelRingIndexP1 = 0;
        duelRingIndexP2 = 0;
        duelHitCountP1 = 0;
        duelHitCountP2 = 0;
        duelInitialized = true;
        EnsureDuelShapes();
        BuildDuelVisualRings(0);
        BuildDuelVisualRings(1);
        SetupDuelRingState(0, duelRingIndexP1);
        SetupDuelRingState(1, duelRingIndexP2);
    }

    private List<GameObject> GetDuelVisualRingList(int playerId)
    {
        return playerId == 0 ? duelVisualRingsTop : duelVisualRingsBottom;
    }

    private void ClearDuelVisualRings(int playerId)
    {
        List<GameObject> list = GetDuelVisualRingList(playerId);
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null)
            {
                Destroy(list[i]);
            }
        }
        list.Clear();
    }

    private void BuildDuelVisualRings(int playerId)
    {
        ClearDuelVisualRings(playerId);

        int count = GetDuelConfiguredRingCount();
        if (count <= 0)
        {
            return;
        }

        float centerY = playerId == 0 ? DUEL_CENTER_OFFSET_Y : -DUEL_CENTER_OFFSET_Y;
        List<GameObject> list = GetDuelVisualRingList(playerId);

        for (int i = 0; i < count; i++)
        {
            DuelRingConfig cfg = settings.duelRings[i];
            float radius = Mathf.Max(0.5f, ToWorldRadius(cfg.radius));

            GameObject ringObj = new GameObject($"DuelVisualRing_P{playerId + 1}_{i}");
            LineRenderer lr = ringObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.widthMultiplier = Mathf.Max(0.06f, settings.ringThickness * 0.8f);
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.numCapVertices = 4;
            lr.numCornerVertices = 4;

            const int segments = 96;
            lr.positionCount = segments;
            for (int s = 0; s < segments; s++)
            {
                float t = (float)s / segments * Mathf.PI * 2f;
                lr.SetPosition(s, new Vector3(Mathf.Cos(t) * radius, Mathf.Sin(t) * radius, 0f));
            }

            ringObj.transform.position = new Vector3(0f, centerY, 0f);
            ringObj.transform.SetParent(transform, true);
            list.Add(ringObj);
        }
    }

    private void EnsureDuelShapes()
    {
        if (duelSettingsTop == null)
        {
            duelSettingsTop = Instantiate(settings);
        }

        if (duelSettingsBottom == null)
        {
            duelSettingsBottom = Instantiate(settings);
        }

        if (duelShapeBottom == null)
        {
            duelShapeBottom = CreateShape(ShapeType.Ring);
        }
    }

    private SpawnShape GetDuelShape(int playerId)
    {
        return playerId == 0 ? shape : duelShapeBottom;
    }

    private GameSettings GetDuelSettings(int playerId)
    {
        return playerId == 0 ? duelSettingsTop : duelSettingsBottom;
    }

    private BallClassType GetPlayerBallClass(int playerId)
    {
        return playerId == 0 ? settings.ballClass1 : settings.ballClass2;
    }

    private void SetupDuelRingState(int playerId, int ringIndex)
    {
        EnsureDuelShapes();
        SpawnShape targetShape = GetDuelShape(playerId);
        GameSettings targetSettings = GetDuelSettings(playerId);

        int count = GetDuelConfiguredRingCount();
        if (count <= 0)
        {
            Debug.LogWarning("[DuelBreakout] Brak skonfigurowanych ringów. Używam fallback 1 ring.");
            targetSettings.ringRadius = 2.4f;
            targetSettings.ringColor = Color.green;
            if (playerId == 0)
            {
                duelRingMaxHpP1 = 10f;
                duelRingHpP1 = duelRingMaxHpP1;
            }
            else
            {
                duelRingMaxHpP2 = 10f;
                duelRingHpP2 = duelRingMaxHpP2;
            }
            return;
        }

        int clampedIndex = Mathf.Clamp(ringIndex, 0, count - 1);
        // Rozpad od wewnętrznego do zewnętrznego (naturalny breakout flow)
        int configIndex = clampedIndex;
        DuelRingConfig cfg = settings.duelRings[configIndex];
        float maxHp = Mathf.Max(1f, cfg.hp);

        if (playerId == 0)
        {
            duelRingMaxHpP1 = maxHp;
            duelRingHpP1 = maxHp;
        }
        else
        {
            duelRingMaxHpP2 = maxHp;
            duelRingHpP2 = maxHp;
        }

        targetSettings.ringRadius = Mathf.Max(0.5f, ToWorldRadius(cfg.radius));
        targetSettings.gapAngleDegrees = 0f;
        targetSettings.rotationSpeed = 0f;
        targetSettings.ringVerticalOffset = playerId == 0 ? DUEL_CENTER_OFFSET_Y : -DUEL_CENTER_OFFSET_Y;

        targetShape.Initialize(targetSettings);
        targetShape.ResetRotation();
        targetShape.SetVisible(true);
        // Defensive set in case shape initialized before Start() and gets repositioned later.
        targetShape.transform.position = new Vector3(0f, targetSettings.ringVerticalOffset, 0f);
        UpdateDuelRingColor(playerId);
        UpdateDuelVisualRingColors(playerId);

        Debug.Log($"[DuelBreakout] P{playerId + 1} Ring {clampedIndex + 1}/{count} | Radius={targetSettings.ringRadius:F2} | HP={maxHp:F0}");
    }

    private void UpdateDuelRingColor(int playerId)
    {
        SpawnShape targetShape = GetDuelShape(playerId);
        GameSettings targetSettings = GetDuelSettings(playerId);
        float currentHp = playerId == 0 ? duelRingHpP1 : duelRingHpP2;
        float maxHp = playerId == 0 ? duelRingMaxHpP1 : duelRingMaxHpP2;
        float ratio = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;
        Color ringColor = Color.Lerp(Color.red, Color.green, ratio);
        targetSettings.ringColor = ringColor;
        targetShape.SetColor(ringColor);
    }

    private void UpdateDuelVisualRingColors(int playerId)
    {
        List<GameObject> list = GetDuelVisualRingList(playerId);
        int activeIndex = playerId == 0 ? duelRingIndexP1 : duelRingIndexP2;

        float currentHp = playerId == 0 ? duelRingHpP1 : duelRingHpP2;
        float currentMaxHp = playerId == 0 ? duelRingMaxHpP1 : duelRingMaxHpP2;
        float ratio = currentMaxHp > 0f ? Mathf.Clamp01(currentHp / currentMaxHp) : 0f;
        Color activeColor = Color.Lerp(Color.red, Color.green, ratio);

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null)
            {
                continue;
            }

            LineRenderer lr = list[i].GetComponent<LineRenderer>();
            if (lr == null)
            {
                continue;
            }

            Color color;
            if (i < activeIndex)
            {
                // Już zniszczone
                color = new Color(0.25f, 0.25f, 0.25f, 0.25f);
            }
            else if (i == activeIndex)
            {
                // Aktywny ring
                color = new Color(activeColor.r, activeColor.g, activeColor.b, 1f);
            }
            else
            {
                // Kolejne ringi jeszcze przed zniszczeniem
                color = new Color(0.2f, 0.9f, 0.2f, 0.55f);
            }

            lr.startColor = color;
            lr.endColor = color;
            if (lr.material != null)
            {
                lr.material.color = color;
            }
        }
    }

    private IEnumerator PlayRingBreakAnimation(int playerId, int ringIndex)
    {
        List<GameObject> list = GetDuelVisualRingList(playerId);
        if (ringIndex < 0 || ringIndex >= list.Count)
        {
            yield break;
        }

        GameObject ringObj = list[ringIndex];
        if (ringObj == null)
        {
            yield break;
        }

        LineRenderer lr = ringObj.GetComponent<LineRenderer>();
        if (lr == null)
        {
            yield break;
        }

        Color startColor = lr.startColor;
        Vector3 startScale = ringObj.transform.localScale;
        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Lekki "pop" + wygaszanie alpha
            float pulse = 1f + 0.25f * t;
            ringObj.transform.localScale = new Vector3(
                startScale.x * pulse,
                startScale.y * pulse,
                startScale.z
            );

            Color c = Color.Lerp(startColor, Color.white, t * 0.6f);
            c.a = Mathf.Lerp(startColor.a, 0f, t);
            lr.startColor = c;
            lr.endColor = c;
            if (lr.material != null)
            {
                lr.material.color = c;
            }

            yield return null;
        }

        // Końcowy stan zniszczonego pierścienia
        ringObj.transform.localScale = startScale;
        Color destroyed = new Color(0.22f, 0.22f, 0.22f, 0.12f);
        lr.startColor = destroyed;
        lr.endColor = destroyed;
        if (lr.material != null)
        {
            lr.material.color = destroyed;
        }
    }

    private float GetDuelBaseSpeed(BallClassType ballClass)
    {
        return ballClass switch
        {
            BallClassType.Tank => 5.0f,
            BallClassType.Sniper => 6.5f,
            BallClassType.Kolos => 5.5f,
            _ => 8.0f,
        };
    }

    private float CalculateDuelDamage(BallClassType ballClass, float relativeVelocity, int playerId)
    {
        int hit = Mathf.Max(1, playerId == 0 ? duelHitCountP1 : duelHitCountP2);
        int playerRingIndex = playerId == 0 ? duelRingIndexP1 : duelRingIndexP2;

        switch (ballClass)
        {
            case BallClassType.Speedy:
                return 1f;
            case BallClassType.Fibonacci:
                return Fibonacci(hit);
            case BallClassType.Fractal:
                return 1f + (hit % 5 == 0 ? 1f : 0f);
            case BallClassType.Grower:
                return 1f + hit * 0.2f;
            case BallClassType.Berserker:
                return 2f + playerRingIndex * 1.5f;
            case BallClassType.Sniper:
                return Mathf.Max(1f, relativeVelocity * 0.1f);
            case BallClassType.Combo:
                return Mathf.Min(8f, 1f + hit * 0.25f);
            case BallClassType.Tank:
                return 8f;
            case BallClassType.Ricochet:
                return 1f + Mathf.Min(6f, hit * 0.1f);
            case BallClassType.FibSpeed:
                return Fibonacci(hit) + 0.5f;
            case BallClassType.Kolos:
                return 6f + hit * 0.15f;
            case BallClassType.Blitz:
                return 1.5f + Mathf.Min(5f, hit * 0.3f);
            default:
                return 1f;
        }
    }

    private int Fibonacci(int n)
    {
        if (n <= 2) return 1;
        int a = 1;
        int b = 1;
        for (int i = 3; i <= n; i++)
        {
            int next = a + b;
            a = b;
            b = next;
        }
        return b;
    }

    private void ApplyDuelBallClassEffects(int playerId, BallClassType ballClass)
    {
        Ball targetBall = playerId == 0 ? activeBall : activeBall2;
        if (targetBall == null)
        {
            return;
        }

        Rigidbody2D rb = targetBall.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            return;
        }

        switch (ballClass)
        {
            case BallClassType.Speedy:
            case BallClassType.FibSpeed:
            case BallClassType.Blitz:
                rb.linearVelocity *= 1.06f;
                break;
            case BallClassType.Grower:
            case BallClassType.Kolos:
                targetBall.transform.localScale *= 1.01f;
                break;
        }
    }

    private void HandleDuelBreakoutHit(int playerId, float relativeVelocity)
    {
        if (!duelInitialized)
        {
            InitializeDuelBreakoutRound();
        }

        BallClassType playerClass = GetPlayerBallClass(playerId);
        if (playerId == 0) duelHitCountP1++; else duelHitCountP2++;

        float damage = CalculateDuelDamage(playerClass, relativeVelocity, playerId);
        if (playerId == 0)
        {
            duelRingHpP1 = Mathf.Max(0f, duelRingHpP1 - damage);
        }
        else
        {
            duelRingHpP2 = Mathf.Max(0f, duelRingHpP2 - damage);
        }

        ApplyDuelBallClassEffects(playerId, playerClass);
        UpdateDuelRingColor(playerId);

        float hp = playerId == 0 ? duelRingHpP1 : duelRingHpP2;
        float maxHp = playerId == 0 ? duelRingMaxHpP1 : duelRingMaxHpP2;
        int hitCount = playerId == 0 ? duelHitCountP1 : duelHitCountP2;
        Debug.Log($"[DuelBreakout] P{playerId + 1} Hit {hitCount} | DMG={damage:F2} | RingHP={hp:F1}/{maxHp:F1}");

        // Log kolizji do recorderów
        if (recordingController != null && recordingController.CollisionRecorder != null)
        {
            recordingController.CollisionRecorder.RecordCollision(relativeVelocity);
        }
        if (batchRecordingController != null && batchRecordingController.CollisionRecorder != null)
        {
            batchRecordingController.CollisionRecorder.RecordCollision(relativeVelocity);
        }

        if (hp > 0f)
        {
            return;
        }

        int ringCount = GetDuelConfiguredRingCount();
        int currentRingIndex = playerId == 0 ? duelRingIndexP1 : duelRingIndexP2;
        StartCoroutine(PlayRingBreakAnimation(playerId, currentRingIndex));

        if (playerId == 0)
        {
            duelRingIndexP1++;
            if (duelRingIndexP1 >= ringCount)
            {
                Debug.Log("[DuelBreakout] P1 zniszczył wszystkie ringi.");
                TriggerGameOver();
                return;
            }
            SetupDuelRingState(0, duelRingIndexP1);
        }
        else
        {
            duelRingIndexP2++;
            if (duelRingIndexP2 >= ringCount)
            {
                Debug.Log("[DuelBreakout] P2 zniszczył wszystkie ringi.");
                TriggerGameOver();
                return;
            }
            SetupDuelRingState(1, duelRingIndexP2);
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
        
        if (IsBattleMode())
        {
            // Prevent duplicate respawning (both balls trigger this event)
            if (isRespawningBattleBalls) return;
            isRespawningBattleBalls = true;
            
            // In Battle mode, freeze both balls and respawn both
            FreezeBothBattleBalls();
            RespawnBattleBalls();
            
            isRespawningBattleBalls = false;
        }
        else if (IsDuelBreakoutMode())
        {
            SpawnDuelBreakoutBalls();
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
        if (IsDuelBreakoutMode())
        {
            // DuelBreakout kończy się po zniszczeniu wszystkich ringów, nie po escape.
            return;
        }

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
        if (IsBattleMode() && activeBall2 != null && !activeBall2.IsFrozen)
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
        ClearDuelVisualRings(0);
        ClearDuelVisualRings(1);
        if (duelShapeBottom != null) Destroy(duelShapeBottom.gameObject);
        if (duelSettingsTop != null) Destroy(duelSettingsTop);
        if (duelSettingsBottom != null) Destroy(duelSettingsBottom);
        if (duelHudTop != null) Destroy(duelHudTop.gameObject);
        if (duelHudBottom != null) Destroy(duelHudBottom.gameObject);
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
