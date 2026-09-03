using UnityEngine;

/// <summary>
/// Typ kształtu spawnu - Ring lub Ellipse.
/// </summary>
public enum ShapeType
{
    Ring,
    Ellipse
}

/// <summary>
/// Tryb nagrywania.
/// </summary>
public enum RecordingMode
{
    None,           // Bez nagrywania
    Single,         // Pojedyncze nagranie (stary tryb, F9)
    Batch           // Batch recording z filtrowaniem (automatyczny start)
}

/// <summary>
/// Tryb gry.
/// </summary>
public enum GameMode
{
    Normal,         // Standardowa gra - 1 piłka
    Battle,         // Tryb bitwa - 2 piłki symetryczne
    DuelBreakout    // Tryb duel breakout - klasy piłek i wiele pierścieni HP
}

/// <summary>
/// Tryb zamrażania piłek.
/// </summary>
public enum FreezeMode
{
    Time,           // Zamrażanie po upływie czasu
    Bounces         // Zamrażanie po określonej liczbie odbić
}

/// <summary>
/// Klasy piłek dla trybu Duel Breakout.
/// </summary>
public enum BallClassType
{
    Speedy,
    Fibonacci,
    Fractal,
    Grower,
    Berserker,
    Sniper,
    Combo,
    Tank,
    Ricochet,
    FibSpeed,
    Kolos,
    Blitz
}

/// <summary>
/// Konfiguracja jednego pierścienia w trybie Duel Breakout.
/// </summary>
[System.Serializable]
public struct DuelRingConfig
{
    [Tooltip("Promien pierscienia")]
    public float radius;

    [Tooltip("Punkty zycia pierscienia")]
    public float hp;
}

/// <summary>
/// Konfiguracja parametrów gry - ScriptableObject do łatwej edycji w inspektorze.
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "Ball Engine/Game Settings")]
public class GameSettings : ScriptableObject
{
    // =============================================
    // STAŁE DLA YOUTUBE SHORTS (9:16)
    // =============================================
    
    /// <summary>Szerokość w pikselach dla YouTube Shorts</summary>
    public const int SCREEN_WIDTH = 1080;
    
    /// <summary>Wysokość w pikselach dla YouTube Shorts</summary>
    public const int SCREEN_HEIGHT = 1920;
    
    /// <summary>Proporcja ekranu (9:16 = 0.5625)</summary>
    public const float ASPECT_RATIO = 9f / 16f;
    
    /// <summary>Wysokość widoku kamery w jednostkach Unity (orthographicSize * 2)</summary>
    public const float WORLD_HEIGHT = 20f;
    
    /// <summary>Szerokość widoku kamery w jednostkach Unity</summary>
    public const float WORLD_WIDTH = WORLD_HEIGHT * ASPECT_RATIO; // = 11.25
    
    // =============================================
    
    [Header("Screen Settings")]
    [Tooltip("Rozmiar kamery ortograficznej (połowa wysokości widoku)")]
    public float cameraOrthoSize = 10f;
    
    [Tooltip("Wymuś rozdzielczość YouTube Shorts w buildzie")]
    public bool forceResolution = true;
    
    [Header("Game Mode")]
    [Tooltip("Tryb gry: Normal = 1 piłka, Battle = 2 piłki symetryczne, DuelBreakout = klasy piłek + pierścienie HP")]
    public GameMode gameMode = GameMode.Normal;

    [Header("Duel Breakout Settings")]
    [Tooltip("Klasa piłki gracza 1")]
    public BallClassType ballClass1 = BallClassType.Fibonacci;

    [Tooltip("Klasa piłki gracza 2")]
    public BallClassType ballClass2 = BallClassType.Speedy;

    [Tooltip("Liczba pierścieni do przebicia (1-5)")]
    [Range(1, 5)]
    public int duelRingCount = 3;

    [Tooltip("Konfiguracja pierścieni duel (radius + hp), od wewnętrznego do zewnętrznego")]
    public DuelRingConfig[] duelRings = new DuelRingConfig[]
    {
        new DuelRingConfig { radius = 120f, hp = 10f },
        new DuelRingConfig { radius = 200f, hp = 25f },
        new DuelRingConfig { radius = 280f, hp = 50f },
    };
    
    [Header("Shape Type")]
    [Tooltip("Typ kształtu spawnu (Ring lub Ellipse)")]
    public ShapeType shapeType = ShapeType.Ring;
    
    [Header("Ring Settings")]
    [Tooltip("Promień pierścienia w jednostkach Unity (4.5 = 80% szerokości ekranu)")]
    public float ringRadius = 4.5f;
    
    [Tooltip("Grubość linii pierścienia")]
    public float ringThickness = 0.3f;
    
    [Tooltip("Kąt luki w stopniach (np. 30 = luka 30 stopni)")]
    [Range(10f, 90f)]
    public float gapAngleDegrees = 30f;
    
    [Tooltip("Początkowa pozycja luki w stopniach (0=góra, 90=prawo, 180=dół, 270=lewo)")]
    [Range(0f, 360f)]
    public float gapInitialAngle = 0f;
    
    [Tooltip("Prędkość wędrowania luki w stopniach na sekundę")]
    public float rotationSpeed = 45f;
    
    [Tooltip("Kolor kształtu")]
    public Color ringColor = Color.white;
    
    [Tooltip("Minimalna jasność koloru kształtu (HSV Value) - dla randomizacji")]
    [Range(0.5f, 1f)]
    public float shapeColorMinBrightness = 0.8f;
    
    [Tooltip("Minimalne nasycenie koloru kształtu (HSV Saturation) - dla randomizacji")]
    [Range(0.5f, 1f)]
    public float shapeColorMinSaturation = 0.7f;
    
    [Tooltip("Przesunięcie kształtu w dół (jednostki Unity)")]
    public float ringVerticalOffset = -1.5f;
    
    [Header("Ellipse Settings")]
    [Tooltip("Promień poziomy elipsy (oś X)")]
    public float ellipseWidthRadius = 4f;
    
    [Tooltip("Promień pionowy elipsy (oś Y)")]
    public float ellipseHeightRadius = 7f;
    
    [Header("Ball Settings")]
    [Tooltip("Promień kulki (0.25 = dobrze widoczna)")]
    public float ballRadius = 0.25f;
    
    [Tooltip("Współczynnik odbicia (0-1, gdzie 1 = idealne odbicie)")]
    [Range(0f, 1f)]
    public float bounciness = 1f;
    
    [Tooltip("Tarcie kulki")]
    [Range(0f, 1f)]
    public float friction = 0.1f;
    
    [Header("Ball Freeze & Spawn")]
    [Tooltip("Tryb zamrażania: Time = po czasie, Bounces = po liczbie odbić")]
    public FreezeMode freezeMode = FreezeMode.Time;
    
    [Tooltip("Czas życia kulki przed zamrożeniem (sekundy) - używane gdy freezeMode = Time")]
    public float ballFreezeTime = 3f;
    
    [Tooltip("Maksymalna liczba odbić przed zamrożeniem - używane gdy freezeMode = Bounces")]
    public int ballMaxBounces = 4;
    
    [Tooltip("Czy włączyć inkrementację wartości dla kolejnych piłek (+1 dla każdej następnej)")]
    public bool enableFreezeIncrementation = false;
    
    [Tooltip("Krok inkrementacji dla kolejnych piłek (np. 1 = każda piłka +1)")]
    public float freezeIncrementStep = 1f;
    
    [Tooltip("Czy wyświetlać licznik wartości pod kształtem")]
    public bool showCounter = true;

    [Tooltip("Czy natychmiastowo zamrażać piłki przy dotknięciu kształtu")]
    public bool enableInstantFreezeOnShapeContact = true;

    [Tooltip("Czy włączać efekt wizualny przy zamrożeniu (szary + lodowa obwódka)")]
    public bool enableFreezeEffect = true;

    [Tooltip("Czy obracać zamarznięte piłki razem z ruchem luki")]
    public bool enableFrozenBallRotation = false;

    [Tooltip("Dźwięk odtwarzany przy natychmiastowym zamrożeniu po dotknięciu kształtu")]
    public AudioClip freezeCollisionClip;

    [Tooltip("Głośność dźwięku zamrożenia (0-1)")]
    [Range(0f, 1f)]
    public float freezeCollisionVolume = 1f;
    
    [Tooltip("Minimalna jasność koloru kulki (HSV Value)")]
    [Range(0.5f, 1f)]
    public float ballColorMinBrightness = 0.8f;
    
    [Tooltip("Minimalne nasycenie koloru kulki (HSV Saturation)")]
    [Range(0.5f, 1f)]
    public float ballColorMinSaturation = 0.7f;
    
    [Header("Physics")]
    [Tooltip("Siła grawitacji (wartość ujemna = w dół)")]
    public float gravity = -9.81f;
    
    [Header("Game Loop")]
    [Tooltip("Czas animacji końcowej przed restartem (sekundy)")]
    public float gameOverAnimationDuration = 2f;
    
    [Header("Recording")]
    [Tooltip("Tryb nagrywania: None = wyłączone, Single = pojedyncze (F9), Batch = automatyczne wiele rund")]
    public RecordingMode recordingMode = RecordingMode.Batch;
    
    [Tooltip("Czy automatycznie rozpocząć nagrywanie przy starcie gry")]
    public bool autoStartRecording = true;
    
    [Header("Batch Recording Settings")]
    [Tooltip("Całkowity czas sesji batch w sekundach (1800 = 30 minut)")]
    public float batchDuration = 1800f;
    
    [Tooltip("Minimalna długość nagrania w sekundach")]
    public float minRecordingLength = 15f;
    
    [Tooltip("Maksymalna długość nagrania w sekundach (rundy dłuższe są automatycznie przerywane)")]
    public float maxRecordingLength = 50f;
    
    [Tooltip("Włącz randomizację parametrów między rundami")]
    public bool enableParameterRandomization = true;
    
    [Header("Spawn Settings")]
    [Tooltip("Czy używać stałej pozycji spawnu dla całej rundy (wszystkie piłki startują z tego samego miejsca)")]
    public bool fixedSpawnPosition = true;
    
    [Tooltip("Minimalny kąt spawnu (stopnie, 0=prawo, 90=góra)")]
    [Range(0f, 180f)]
    public float spawnAngleMin = 45f;
    
    [Tooltip("Maksymalny kąt spawnu (stopnie)")]
    [Range(0f, 180f)]
    public float spawnAngleMax = 135f;
    
    [Tooltip("Odległość spawnu od środka (procent promienia)")]
    [Range(0.1f, 0.9f)]
    public float spawnRadiusPercent = 0.5f;
    
    [Header("Escape Detection")]
    [Tooltip("Dodatkowy bufor dla detekcji ucieczki (dodawany do promienia kształtu)")]
    public float escapeBuffer = 0.6f;
    
    [Header("Trail Effect (Ogonek)")]
    [Tooltip("Styl ogonka piłki")]
    public TrailStyle trailStyle = TrailStyle.FadingTrail;
    
    [Tooltip("Czas życia śladu (sekundy)")]
    [Range(0.1f, 1f)]
    public float trailTime = 0.25f;
    
    [Tooltip("Bazowa szerokość ogonka (mnożnik promienia piłki)")]
    [Range(0.2f, 2f)]
    public float trailWidthMultiplier = 0.8f;
    
    [Header("Game Over Effects")]
    [Tooltip("Liczba cząsteczek pyłu pierścienia przy Game Over")]
    [Range(50, 300)]
    public int ringParticleCount = 150;
    
    /// <summary>
    /// Resetuje wszystkie wartości do zalecanych dla YouTube Shorts.
    /// </summary>
    [ContextMenu("Reset to Recommended Values")]
    public void ResetToRecommended()
    {
        // Screen
        cameraOrthoSize = 10f;
        forceResolution = true;
        
        // Game Mode
        gameMode = GameMode.Normal;

        // Duel Breakout
        ballClass1 = BallClassType.Fibonacci;
        ballClass2 = BallClassType.Speedy;
        duelRingCount = 3;
        duelRings = new DuelRingConfig[]
        {
            new DuelRingConfig { radius = 120f, hp = 10f },
            new DuelRingConfig { radius = 200f, hp = 25f },
            new DuelRingConfig { radius = 280f, hp = 50f },
        };
        
        // Shape Type
        shapeType = ShapeType.Ring;
        
        // Ring
        ringRadius = 5f;
        ringThickness = 0.15f;
        gapAngleDegrees = 30f;
        gapInitialAngle = 0f;
        rotationSpeed = 45f;
        ringColor = Color.white;
        shapeColorMinBrightness = 0.8f;
        shapeColorMinSaturation = 0.7f;
        ringVerticalOffset = -1.5f;
        
        // Ellipse
        ellipseWidthRadius = 4f;
        ellipseHeightRadius = 7f;
        
        // Ball
        ballRadius = 0.25f;
        bounciness = 1f;
        friction = 0.1f;
        ballFreezeTime = 3f;
        ballColorMinBrightness = 0.8f;
        ballColorMinSaturation = 0.7f;
        
        // Physics
        gravity = -9.81f;
        
        // Game Loop
        gameOverAnimationDuration = 2f;
        
        // Recording
        recordingMode = RecordingMode.Batch;
        autoStartRecording = true;
        batchDuration = 1800f;
        minRecordingLength = 15f;
        maxRecordingLength = 50f;
        enableParameterRandomization = true;
        
        // Spawn
        fixedSpawnPosition = true;
        spawnAngleMin = 45f;
        spawnAngleMax = 135f;
        spawnRadiusPercent = 0.5f;
        
        // Escape
        escapeBuffer = 0.6f;
        
        // Trail Effect
        trailStyle = TrailStyle.FadingTrail;
        trailTime = 0.25f;
        trailWidthMultiplier = 0.8f;
        
        // Game Over Effects
        ringParticleCount = 150;
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("GameSettings: Zresetowano do zalecanych wartości!");
        #endif
    }
}
