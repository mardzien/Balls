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
    
    [Header("Status")]
    [SerializeField] private bool isRecording = false;
    
    private float recordingStartTime;
    
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
        // Uwaga: rozszerzenie .webm zostanie dodane automatycznie przez Recorder
        
        // Audio
        movieRecorder.AudioInputSettings.PreserveAudio = true;
        
        // Dodaj recorder do kontrolera
        controllerSettings.AddRecorderSettings(movieRecorder);
        controllerSettings.SetRecordModeToManual();
        controllerSettings.FrameRate = targetFrameRate;
        
        Debug.Log($"[Recording] Recorder configured: {GameSettings.SCREEN_WIDTH}x{GameSettings.SCREEN_HEIGHT} @ {targetFrameRate}fps");
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
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            movieRecorder.OutputFile = System.IO.Path.Combine(
                Application.dataPath, 
                "..", 
                outputFolder, 
                $"BallGame_{timestamp}"
            );
        }
        
        recorderController.PrepareRecording();
        recorderController.StartRecording();
        
        isRecording = true;
        recordingStartTime = Time.time;
        
        Debug.Log($"[Recording] ▶ Started recording to: {outputFolder}/");
#else
        Debug.LogWarning("[Recording] Recording is only available in Unity Editor!");
#endif
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
        
        float duration = Time.time - recordingStartTime;
        Debug.Log($"[Recording] ⏹ Stopped recording. Duration: {duration:F1}s");
        Debug.Log($"[Recording] File saved to: {outputFolder}/");
#endif
        
        isRecording = false;
    }
    
    /// <summary>
    /// Czy trwa nagrywanie?
    /// </summary>
    public bool IsRecording => isRecording;
    
    /// <summary>
    /// Aktualny czas nagrywania w sekundach.
    /// </summary>
    public float RecordingTime => isRecording ? Time.time - recordingStartTime : 0f;
    
    private void OnDestroy()
    {
        if (isRecording)
        {
            StopRecording();
        }
    }
    
#if UNITY_EDITOR
    private void OnGUI()
    {
        // Pokaż status nagrywania
        if (isRecording)
        {
            float elapsed = Time.time - recordingStartTime;
            string remaining = maxRecordingDuration > 0 
                ? $" / {maxRecordingDuration:F0}s" 
                : "";
            
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = 14;
            style.normal.textColor = Color.red;
            style.fontStyle = FontStyle.Bold;
            
            GUI.Box(new Rect(10, 10, 200, 30), $"● REC {elapsed:F1}s{remaining}", style);
        }
        else
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 12;
            style.normal.textColor = Color.gray;
            
            GUI.Label(new Rect(10, 10, 200, 20), $"Press F9 to record", style);
        }
    }
#endif
}

