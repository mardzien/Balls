using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
#endif

/// <summary>
/// Kontroler nagrywania batch dla YouTube Shorts.
/// Automatycznie nagrywa wiele rund, filtruje po długości,
/// usuwa za krótkie/długie nagrania i randomizuje parametry.
/// Konfiguracja w GameSettings (GameConfig).
/// Klawisz F10 - ręczny Start/Stop batch recording
/// </summary>
public class BatchRecordingController : MonoBehaviour
{
    [Header("Recording Settings")]
    [Tooltip("Folder docelowy dla nagrań (względem projektu)")]
    [SerializeField] private string outputFolder = "Recordings";
    
    [Tooltip("Klatki na sekundę")]
    [SerializeField] private int targetFrameRate = 60;
    
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ParameterRandomizer parameterRandomizer;
    [SerializeField] private CollisionRecorder collisionRecorder;
    
    [Header("Statistics (Read Only)")]
    [SerializeField] private int successfulRecordings = 0;
    [SerializeField] private int discardedRecordings = 0;
    [SerializeField] private int totalRoundsPlayed = 0;
    [SerializeField] private bool isBatchActive = false;
    [SerializeField] private bool isRecordingRound = false;
    
    // Settings from GameConfig (cached)
    private float batchDuration;
    private float minRecordingLength;
    private float maxRecordingLength;
    private bool enableParameterRandomization;
    
    private float batchStartTime;
    private float roundStartTime;
    private string currentRecordingName;
    private bool hasAutoStarted = false;
    
#if UNITY_EDITOR
    private RecorderController recorderController;
    private RecorderControllerSettings controllerSettings;
#endif

    private void Start()
    {
        Application.targetFrameRate = targetFrameRate;
        QualitySettings.vSyncCount = 0;
        
        // Ensure output folder exists
        string fullPath = Path.Combine(Application.dataPath, "..", outputFolder);
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
            Debug.Log($"[BatchRecording] Created output folder: {fullPath}");
        }
        
        // Auto-find references
        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
        }
        
        if (parameterRandomizer == null)
        {
            parameterRandomizer = GetComponent<ParameterRandomizer>();
            if (parameterRandomizer == null)
            {
                parameterRandomizer = gameObject.AddComponent<ParameterRandomizer>();
            }
        }
        
        if (collisionRecorder == null)
        {
            collisionRecorder = GetComponent<CollisionRecorder>();
            if (collisionRecorder == null)
            {
                collisionRecorder = gameObject.AddComponent<CollisionRecorder>();
            }
        }
        
        // Load settings from GameConfig
        LoadSettingsFromGameConfig();
        
        // Subscribe to game events
        if (gameManager != null)
        {
            gameManager.OnGameOverStart += OnGameOver;
            gameManager.OnGameRestart += OnRoundStart;
        }
        
#if UNITY_EDITOR
        SetupRecorder();
#endif
        
        // Auto-start if configured in GameSettings
        if (gameManager != null && gameManager.Settings != null)
        {
            var settings = gameManager.Settings;
            if (settings.recordingMode == RecordingMode.Batch && settings.autoStartRecording)
            {
                // Delay auto-start to next frame to ensure everything is initialized
                Invoke(nameof(AutoStartBatch), 0.1f);
            }
        }
    }
    
    private void LoadSettingsFromGameConfig()
    {
        if (gameManager == null || gameManager.Settings == null)
        {
            Debug.LogWarning("[BatchRecording] GameManager or Settings not found, using defaults");
            batchDuration = 1800f;
            minRecordingLength = 15f;
            maxRecordingLength = 40f;
            enableParameterRandomization = true;
            return;
        }
        
        var settings = gameManager.Settings;
        batchDuration = settings.batchDuration;
        minRecordingLength = settings.minRecordingLength;
        maxRecordingLength = settings.maxRecordingLength;
        enableParameterRandomization = settings.enableParameterRandomization;
        
        Debug.Log($"[BatchRecording] Loaded settings from GameConfig: " +
                  $"duration={batchDuration}s, filter={minRecordingLength}-{maxRecordingLength}s, " +
                  $"randomize={enableParameterRandomization}");
    }
    
    private void AutoStartBatch()
    {
        if (!hasAutoStarted && !isBatchActive)
        {
            hasAutoStarted = true;
            StartBatch();
        }
    }
    
    private void Update()
    {
        // Toggle batch recording with F10 (manual override)
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.f10Key.wasPressedThisFrame)
        {
            if (isBatchActive)
                StopBatch();
            else
                StartBatch();
        }
        
        // Check batch time limit
        if (isBatchActive && Time.time - batchStartTime >= batchDuration)
        {
            Debug.Log($"[BatchRecording] Batch time limit reached ({batchDuration}s)");
            StopBatch();
        }
    }

#if UNITY_EDITOR
    private void SetupRecorder()
    {
        controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        recorderController = new RecorderController(controllerSettings);
        
        controllerSettings.FrameRatePlayback = FrameRatePlayback.Constant;
        controllerSettings.FrameRate = targetFrameRate;
        controllerSettings.CapFrameRate = true;
        
        var movieRecorder = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movieRecorder.name = "Batch Recorder";
        movieRecorder.Enabled = true;
        
        movieRecorder.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = GameSettings.SCREEN_WIDTH,
            OutputHeight = GameSettings.SCREEN_HEIGHT
        };
        
        movieRecorder.EncoderSettings = new UnityEditor.Recorder.Encoder.CoreEncoderSettings
        {
            EncodingQuality = UnityEditor.Recorder.Encoder.CoreEncoderSettings.VideoEncodingQuality.High,
            Codec = UnityEditor.Recorder.Encoder.CoreEncoderSettings.OutputCodec.WEBM
        };
        
        movieRecorder.AudioInputSettings.PreserveAudio = false;
        
        controllerSettings.AddRecorderSettings(movieRecorder);
        controllerSettings.SetRecordModeToManual();
        
        Debug.Log($"[BatchRecording] Recorder configured: {GameSettings.SCREEN_WIDTH}x{GameSettings.SCREEN_HEIGHT} @ {targetFrameRate}fps");
    }
    
    private void UpdateRecorderOutputPath()
    {
        foreach (var rs in controllerSettings.RecorderSettings)
        {
            if (rs is MovieRecorderSettings mrs)
            {
                mrs.OutputFile = Path.Combine(
                    Application.dataPath,
                    "..",
                    outputFolder,
                    currentRecordingName
                );
                break;
            }
        }
    }
#endif

    /// <summary>
    /// Rozpoczyna sesję batch recording.
    /// </summary>
    public void StartBatch()
    {
        if (isBatchActive)
        {
            Debug.LogWarning("[BatchRecording] Batch already active!");
            return;
        }
        
        // Reload settings in case they changed
        LoadSettingsFromGameConfig();
        
        isBatchActive = true;
        batchStartTime = Time.time;
        successfulRecordings = 0;
        discardedRecordings = 0;
        totalRoundsPlayed = 0;
        
        Debug.Log($"[BatchRecording] ▶ Batch started. Duration: {batchDuration}s, Filter: {minRecordingLength}-{maxRecordingLength}s");
        
        // If game is already playing, start recording current round
        if (gameManager != null && !gameManager.IsGameOver)
        {
            StartRoundRecording();
        }
    }
    
    /// <summary>
    /// Zatrzymuje sesję batch recording.
    /// </summary>
    public void StopBatch()
    {
        if (!isBatchActive)
        {
            Debug.LogWarning("[BatchRecording] Batch not active!");
            return;
        }
        
        // Stop current recording if active
        if (isRecordingRound)
        {
            StopRoundRecording(false); // Discard partial recording
        }
        
        isBatchActive = false;
        
        float totalTime = Time.time - batchStartTime;
        Debug.Log($"[BatchRecording] ⏹ Batch stopped.");
        Debug.Log($"[BatchRecording] === STATISTICS ===");
        Debug.Log($"[BatchRecording] Total time: {totalTime:F1}s");
        Debug.Log($"[BatchRecording] Rounds played: {totalRoundsPlayed}");
        Debug.Log($"[BatchRecording] Successful recordings: {successfulRecordings}");
        Debug.Log($"[BatchRecording] Discarded recordings: {discardedRecordings}");
        Debug.Log($"[BatchRecording] Success rate: {(totalRoundsPlayed > 0 ? (successfulRecordings * 100f / totalRoundsPlayed) : 0):F1}%");
    }
    
    /// <summary>
    /// Wywoływane gdy runda się kończy (piłka uciekła).
    /// </summary>
    private void OnGameOver()
    {
        if (!isBatchActive || !isRecordingRound) return;
        
        float roundDuration = Time.time - roundStartTime;
        totalRoundsPlayed++;
        
        bool isValidLength = roundDuration >= minRecordingLength && roundDuration <= maxRecordingLength;
        
        if (isValidLength)
        {
            Debug.Log($"[BatchRecording] ✓ Round {totalRoundsPlayed}: {roundDuration:F1}s - KEPT");
            StopRoundRecording(true);
            successfulRecordings++;
        }
        else
        {
            string reason = roundDuration < minRecordingLength ? "too short" : "too long";
            Debug.Log($"[BatchRecording] ✗ Round {totalRoundsPlayed}: {roundDuration:F1}s - DISCARDED ({reason})");
            StopRoundRecording(false);
            discardedRecordings++;
        }
    }
    
    /// <summary>
    /// Wywoływane gdy nowa runda się zaczyna.
    /// </summary>
    private void OnRoundStart()
    {
        if (!isBatchActive) return;
        
        // Randomize parameters before new round
        if (enableParameterRandomization && parameterRandomizer != null && gameManager != null)
        {
            parameterRandomizer.RandomizeAll(gameManager.Settings);
            
            // Re-initialize the shape with new settings
            if (gameManager.Shape != null)
            {
                gameManager.Shape.Initialize(gameManager.Settings);
            }
        }
        
        // Start recording new round
        StartRoundRecording();
    }
    
    private void StartRoundRecording()
    {
        if (isRecordingRound) return;
        
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        currentRecordingName = $"Batch_{timestamp}";
        roundStartTime = Time.time;
        isRecordingRound = true;
        
#if UNITY_EDITOR
        if (recorderController == null)
        {
            SetupRecorder();
        }
        
        UpdateRecorderOutputPath();
        recorderController.PrepareRecording();
        recorderController.StartRecording();
#endif
        
        if (collisionRecorder != null)
        {
            collisionRecorder.StartRecording(currentRecordingName);
        }
        
        Debug.Log($"[BatchRecording] Recording round: {currentRecordingName}");
    }
    
    private void StopRoundRecording(bool keepFiles)
    {
        if (!isRecordingRound) return;
        
#if UNITY_EDITOR
        recorderController?.StopRecording();
#endif
        
        if (collisionRecorder != null && collisionRecorder.IsRecording)
        {
            collisionRecorder.StopRecording();
        }
        
        isRecordingRound = false;
        
        if (!keepFiles && !string.IsNullOrEmpty(currentRecordingName))
        {
            DeleteRecordingFiles(currentRecordingName);
        }
    }
    
    private void DeleteRecordingFiles(string fileName)
    {
        string basePath = Path.Combine(Application.dataPath, "..", outputFolder);
        
        // Delete WebM file
        string webmPath = Path.Combine(basePath, $"{fileName}.webm");
        if (File.Exists(webmPath))
        {
            try
            {
                File.Delete(webmPath);
                Debug.Log($"[BatchRecording] Deleted: {webmPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[BatchRecording] Failed to delete {webmPath}: {e.Message}");
            }
        }
        
        // Delete JSON file
        string jsonPath = Path.Combine(basePath, $"{fileName}.json");
        if (File.Exists(jsonPath))
        {
            try
            {
                File.Delete(jsonPath);
                Debug.Log($"[BatchRecording] Deleted: {jsonPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[BatchRecording] Failed to delete {jsonPath}: {e.Message}");
            }
        }
    }
    
    // Public properties
    public bool IsBatchActive => isBatchActive;
    public bool IsRecordingRound => isRecordingRound;
    public int SuccessfulRecordings => successfulRecordings;
    public int DiscardedRecordings => discardedRecordings;
    public float BatchTimeRemaining => isBatchActive ? Mathf.Max(0, batchDuration - (Time.time - batchStartTime)) : 0f;
    public CollisionRecorder CollisionRecorder => collisionRecorder;
    
    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnGameOverStart -= OnGameOver;
            gameManager.OnGameRestart -= OnRoundStart;
        }
        
        if (isBatchActive)
        {
            StopBatch();
        }
    }
}
