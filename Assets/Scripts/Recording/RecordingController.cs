using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
#endif

/// <summary>
/// Kontroler nagrywania dla YouTube Shorts.
/// Klawisz F9 - Start/Stop nagrywania
/// Automatyczne ustawienia: 1080x1920, 60 FPS, MP4
/// </summary>
public class RecordingController : MonoBehaviour
{
    [Header("Recording Settings")]
    [Tooltip("Maksymalna długość nagrania w sekundach (0 = bez limitu)")]
    [SerializeField] private float maxRecordingDuration = 60f;
    
    // Klawisz F9 do nagrywania (hardcoded dla niezawodności)
    
    [Tooltip("Folder docelowy dla nagrań (względem projektu)")]
    [SerializeField] private string outputFolder = "Recordings";
    
    [Tooltip("Klatki na sekundę")]
    [SerializeField] private int targetFrameRate = 60;
    
    [Header("Collision Recording")]
    [SerializeField] private CollisionRecorder collisionRecorder;
    
    [Header("Status")]
    [SerializeField] private bool isRecording = false;
    
    private float recordingStartTime;
    private string currentRecordingName;
    
    private const float WEBM_READY_TIMEOUT = 8f;
    private const float WEBM_STABLE_TIME = 0.6f;
    private const long WEBM_MIN_BYTES = 10 * 1024;
    
#if UNITY_EDITOR
    private RecorderController recorderController;
    private RecorderControllerSettings controllerSettings;
#endif

    private void Start()
    {
        // Ustaw docelowy framerate
        Application.targetFrameRate = targetFrameRate;
        QualitySettings.vSyncCount = 0;
        
        // Stwórz folder na nagrania
        string fullPath = System.IO.Path.Combine(Application.dataPath, "..", outputFolder);
        if (!System.IO.Directory.Exists(fullPath))
        {
            System.IO.Directory.CreateDirectory(fullPath);
            Debug.Log($"[Recording] Created output folder: {fullPath}");
        }
        
        // Auto-create CollisionRecorder if not assigned
        if (collisionRecorder == null)
        {
            collisionRecorder = GetComponent<CollisionRecorder>();
            if (collisionRecorder == null)
            {
                collisionRecorder = gameObject.AddComponent<CollisionRecorder>();
            }
        }
        
#if UNITY_EDITOR
        SetupRecorder();
#endif
    }
    
    private void Update()
    {
        // Toggle nagrywania klawiszem F9 (nowy Input System)
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.f9Key.wasPressedThisFrame)
        {
            if (isRecording)
                StopRecording();
            else
                StartRecording();
        }
        
        // Auto-stop po osiągnięciu limitu czasu
        if (isRecording && maxRecordingDuration > 0)
        {
            float elapsed = Time.time - recordingStartTime;
            if (elapsed >= maxRecordingDuration)
            {
                Debug.Log($"[Recording] Auto-stop: reached {maxRecordingDuration}s limit");
                StopRecording();
            }
        }
    }

#if UNITY_EDITOR
    private void SetupRecorder()
    {
        // Stwórz ustawienia kontrolera
        controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        recorderController = new RecorderController(controllerSettings);
        
        // === KLUCZOWE: Stały framerate dla płynnego nagrywania ===
        // FrameRatePlayback.Constant zapewnia że każda klatka jest nagrana
        // nawet jeśli gra działa wolniej niż docelowy FPS
        controllerSettings.FrameRatePlayback = FrameRatePlayback.Constant;
        controllerSettings.FrameRate = targetFrameRate;
        controllerSettings.CapFrameRate = true;
        
        // Konfiguracja Movie Recorder dla YouTube Shorts
        var movieRecorder = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movieRecorder.name = "YouTube Shorts Recorder";
        movieRecorder.Enabled = true;
        
        // Rozdzielczość 1080x1920 (YouTube Shorts - pionowe 9:16)
        movieRecorder.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = GameSettings.SCREEN_WIDTH,
            OutputHeight = GameSettings.SCREEN_HEIGHT
        };
        
        // Encoder settings (Unity Recorder 5.x API)
        // WebM dla kompatybilności z Linux
        movieRecorder.EncoderSettings = new UnityEditor.Recorder.Encoder.CoreEncoderSettings
        {
            EncodingQuality = UnityEditor.Recorder.Encoder.CoreEncoderSettings.VideoEncodingQuality.High,
            Codec = UnityEditor.Recorder.Encoder.CoreEncoderSettings.OutputCodec.WEBM
        };
        
        // Ścieżka wyjściowa (WebM)
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        movieRecorder.OutputFile = System.IO.Path.Combine(
            Application.dataPath, 
            "..", 
            outputFolder, 
            $"BallGame_{timestamp}"
        );
        
        // Audio - włączone (freeze SFX w nagraniu)
        movieRecorder.AudioInputSettings.PreserveAudio = true;
        
        // Dodaj recorder do kontrolera
        controllerSettings.AddRecorderSettings(movieRecorder);
        controllerSettings.SetRecordModeToManual();
        
        Debug.Log($"[Recording] Recorder configured: {GameSettings.SCREEN_WIDTH}x{GameSettings.SCREEN_HEIGHT} @ {targetFrameRate}fps (Constant FrameRate)");
    }
#endif

    /// <summary>
    /// Rozpoczyna nagrywanie.
    /// </summary>
    public void StartRecording()
    {
        if (isRecording)
        {
            Debug.LogWarning("[Recording] Already recording!");
            return;
        }
        
        // Generuj nazwę pliku (wspólną dla wideo i JSON)
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        currentRecordingName = $"BallGame_{timestamp}";
        
#if UNITY_EDITOR
        if (recorderController == null)
        {
            SetupRecorder();
        }
        
        // Aktualizuj nazwę pliku z aktualnym timestamp
        MovieRecorderSettings movieRecorder = null;
        foreach (var rs in controllerSettings.RecorderSettings)
        {
            if (rs is MovieRecorderSettings mrs)
            {
                movieRecorder = mrs;
                break;
            }
        }
        if (movieRecorder != null)
        {
            movieRecorder.OutputFile = System.IO.Path.Combine(
                Application.dataPath, 
                "..", 
                outputFolder, 
                currentRecordingName
            );
        }
        
        recorderController.PrepareRecording();
        recorderController.StartRecording();
#endif
        
        // Start collision recording
        if (collisionRecorder != null)
        {
            collisionRecorder.StartRecording(currentRecordingName);
        }
        
        isRecording = true;
        recordingStartTime = Time.time;
        
        Debug.Log($"[Recording] ▶ Started recording to: {outputFolder}/{currentRecordingName}");
    }
    
    /// <summary>
    /// Zatrzymuje nagrywanie.
    /// </summary>
    public void StopRecording()
    {
        if (!isRecording)
        {
            Debug.LogWarning("[Recording] Not recording!");
            return;
        }
        
#if UNITY_EDITOR
        recorderController.StopRecording();
#endif
        
        // Stop collision recording
        if (collisionRecorder != null && collisionRecorder.IsRecording)
        {
            collisionRecorder.StopRecording();
        }

        float duration = Time.time - recordingStartTime;
        isRecording = false;
        StartCoroutine(FinalizeRecording(currentRecordingName, duration));
    }
    
    /// <summary>
    /// Czy trwa nagrywanie?
    /// </summary>
    public bool IsRecording => isRecording;
    
    /// <summary>
    /// Aktualny czas nagrywania w sekundach.
    /// </summary>
    public float RecordingTime => isRecording ? Time.time - recordingStartTime : 0f;
    
    /// <summary>
    /// Referencja do CollisionRecorder (do rejestrowania zdarzeń kolizji).
    /// </summary>
    public CollisionRecorder CollisionRecorder => collisionRecorder;
    
    private void OnDestroy()
    {
        if (isRecording)
        {
            StopRecording();
        }
    }
    
    private IEnumerator FinalizeRecording(string recordingName, float duration)
    {
        yield return WaitForWebMReady(recordingName);
        Debug.Log($"[Recording] ⏹ Stopped recording. Duration: {duration:F1}s");
        Debug.Log($"[Recording] Files saved to: {outputFolder}/{recordingName}.*");
    }
    
    private IEnumerator WaitForWebMReady(string recordingName)
    {
        string basePath = Path.Combine(Application.dataPath, "..", outputFolder);
        string webmPath = Path.Combine(basePath, $"{recordingName}.webm");
        
        float elapsed = 0f;
        float stableTimer = 0f;
        long lastSize = -1;
        const float checkInterval = 0.25f;
        
        while (elapsed < WEBM_READY_TIMEOUT)
        {
            if (File.Exists(webmPath))
            {
                long size = new FileInfo(webmPath).Length;
                if (size == lastSize && size > 0)
                {
                    stableTimer += checkInterval;
                    if (stableTimer >= WEBM_STABLE_TIME)
                    {
                        break;
                    }
                }
                else
                {
                    stableTimer = 0f;
                    lastSize = size;
                }
            }
            
            yield return new WaitForSecondsRealtime(checkInterval);
            elapsed += checkInterval;
        }
        
        if (!File.Exists(webmPath) || new FileInfo(webmPath).Length < WEBM_MIN_BYTES)
        {
            Debug.LogWarning($"[Recording] WebM may be incomplete: {webmPath}");
        }
    }
    
    // Usunięty OnGUI - status nagrywania w konsoli
    // Aby uniknąć wyświetlania tekstu na nagraniu
}

