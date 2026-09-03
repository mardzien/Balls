using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Zapisuje zdarzenia kolizji do plików JSON.
/// Używany do postprodukcji dźwięku - impact_intensity odpowiada głośności.
/// </summary>
public class CollisionRecorder : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Folder docelowy dla plików JSON (względem projektu)")]
    [SerializeField] private string outputFolder = "Recordings";
    
    private List<RawCollisionEvent> rawEvents = new List<RawCollisionEvent>();
    private bool isRecording = false;
    private int startFrame;
    private float startTime;
    private string currentFileName;
    
    /// <summary>
    /// Surowe dane kolizji (przed normalizacją).
    /// </summary>
    private class RawCollisionEvent
    {
        public int frameNumber;
        public float timestamp;
        public float velocity;
    }
    
    /// <summary>
    /// Struktura JSON - informacje o nagraniu.
    /// </summary>
    [System.Serializable]
    public class RecordingInfo
    {
        public int total_frames;
        public float duration;
    }
    
    /// <summary>
    /// Struktura JSON - pojedyncze zdarzenie kolizji.
    /// </summary>
    [System.Serializable]
    public class CollisionEvent
    {
        public int frame_number;
        public float timestamp;
        public float impact_intensity;
    }
    
    /// <summary>
    /// Główna struktura JSON.
    /// </summary>
    [System.Serializable]
    public class CollisionData
    {
        public RecordingInfo recording_info;
        public List<CollisionEvent> collision_events;
    }
    
    private void Awake()
    {
        // Upewnij się, że folder istnieje
        string fullPath = Path.Combine(Application.dataPath, "..", outputFolder);
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }
    }
    
    /// <summary>
    /// Rozpoczyna nagrywanie kolizji.
    /// </summary>
    /// <param name="fileName">Nazwa pliku bez rozszerzenia (np. "BallGame_2025-12-02_18-42-14")</param>
    public void StartRecording(string fileName)
    {
        if (isRecording)
        {
            Debug.LogWarning("[CollisionRecorder] Already recording!");
            return;
        }
        
        rawEvents.Clear();
        currentFileName = fileName;
        startFrame = Time.frameCount;
        startTime = Time.time;
        isRecording = true;
        
        Debug.Log($"[CollisionRecorder] Started recording collisions for: {fileName}");
    }
    
    /// <summary>
    /// Zatrzymuje nagrywanie i zapisuje plik JSON.
    /// </summary>
    public void StopRecording()
    {
        if (!isRecording)
        {
            Debug.LogWarning("[CollisionRecorder] Not recording!");
            return;
        }
        
        isRecording = false;
        
        int totalFrames = Time.frameCount - startFrame;
        float duration = Time.time - startTime;
        
        SaveToJson(totalFrames, duration);
        
        Debug.Log($"[CollisionRecorder] Stopped recording. Events: {rawEvents.Count}, Duration: {duration:F1}s");
    }
    
    /// <summary>
    /// Rejestruje zdarzenie kolizji.
    /// </summary>
    /// <param name="relativeVelocity">Prędkość względna kolizji (magnitude)</param>
    public void RecordCollision(float relativeVelocity)
    {
        if (!isRecording) return;
        
        rawEvents.Add(new RawCollisionEvent
        {
            frameNumber = Time.frameCount - startFrame,
            timestamp = Time.time - startTime,
            velocity = relativeVelocity
        });
    }
    
    /// <summary>
    /// Czy trwa nagrywanie?
    /// </summary>
    public bool IsRecording => isRecording;
    
    /// <summary>
    /// Normalizuje prędkość do zakresu [0.5, 1.0].
    /// </summary>
    private float NormalizeIntensity(float velocity, float maxVelocity)
    {
        if (maxVelocity <= 0f) return 0.5f;
        
        float normalized = Mathf.Clamp01(velocity / maxVelocity);
        // Mapowanie do zakresu [0.5, 1.0]
        return 0.5f + 0.5f * normalized;
    }
    
    /// <summary>
    /// Zapisuje dane do pliku JSON.
    /// </summary>
    private void SaveToJson(int totalFrames, float duration)
    {
        if (string.IsNullOrEmpty(currentFileName))
        {
            Debug.LogError("[CollisionRecorder] No filename set!");
            return;
        }
        
        // Znajdź maksymalną prędkość do normalizacji
        float maxVelocity = 0f;
        foreach (var evt in rawEvents)
        {
            if (evt.velocity > maxVelocity)
                maxVelocity = evt.velocity;
        }
        
        // Przygotuj dane JSON
        var data = new CollisionData
        {
            recording_info = new RecordingInfo
            {
                total_frames = totalFrames,
                duration = Mathf.Round(duration * 1000f) / 1000f // Zaokrąglij do 3 miejsc
            },
            collision_events = new List<CollisionEvent>()
        };
        
        // Konwertuj surowe zdarzenia na znormalizowane
        foreach (var raw in rawEvents)
        {
            data.collision_events.Add(new CollisionEvent
            {
                frame_number = raw.frameNumber,
                timestamp = Mathf.Round(raw.timestamp * 1000f) / 1000f,
                impact_intensity = Mathf.Round(NormalizeIntensity(raw.velocity, maxVelocity) * 100f) / 100f
            });
        }
        
        // Zapisz do pliku
        string json = JsonUtility.ToJson(data, true);
        string filePath = Path.Combine(Application.dataPath, "..", outputFolder, $"{currentFileName}.json");
        
        try
        {
            File.WriteAllText(filePath, json);
            Debug.Log($"[CollisionRecorder] Saved JSON to: {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CollisionRecorder] Failed to save JSON: {e.Message}");
        }
    }
    
    private void OnDestroy()
    {
        if (isRecording)
        {
            StopRecording();
        }
    }
}

