using UnityEngine;

/// <summary>
/// Konfiguruje ekran i kamerę dla formatu YouTube Shorts (9:16).
/// Dodaj do obiektu z kamerą lub GameManager.
/// </summary>
public class ScreenSetup : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameSettings settings;
    [SerializeField] private Camera targetCamera;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        
        SetupScreen();
        SetupCamera();
    }
    
    private void SetupScreen()
    {
        // Wymuś rozdzielczość tylko w renderze/buildzie (nie w edytorze)
        #if !UNITY_EDITOR
        if (settings != null && settings.forceResolution)
        {
            Screen.SetResolution(GameSettings.SCREEN_WIDTH, GameSettings.SCREEN_HEIGHT, FullScreenMode.Windowed);
        }
        #endif
    }
    
    private void SetupCamera()
    {
        if (targetCamera == null)
        {
            Debug.LogError("ScreenSetup: No camera found!");
            return;
        }
        
        // Ustaw kamerę jako ortograficzną
        targetCamera.orthographic = true;
        
        // Ustaw rozmiar ortograficzny
        float orthoSize = settings != null ? settings.cameraOrthoSize : GameSettings.WORLD_HEIGHT / 2f;
        targetCamera.orthographicSize = orthoSize;
        
        // Wycentruj kamerę
        targetCamera.transform.position = new Vector3(0, 0, -10);
        
        // Opcjonalnie: dodaj letterboxing dla nieprawidłowych proporcji
        AdjustCameraForAspectRatio();
    }
    
    private void AdjustCameraForAspectRatio()
    {
        float targetAspect = GameSettings.ASPECT_RATIO;
        float windowAspect = (float)Screen.width / Screen.height;
        float scaleHeight = windowAspect / targetAspect;
        
        if (scaleHeight < 1.0f)
        {
            // Dodaj paski na górze i dole (letterbox)
            Rect rect = targetCamera.rect;
            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;
            targetCamera.rect = rect;
        }
        else
        {
            // Dodaj paski po bokach (pillarbox)
            float scaleWidth = 1.0f / scaleHeight;
            Rect rect = targetCamera.rect;
            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;
            targetCamera.rect = rect;
        }
    }
    
#if UNITY_EDITOR
    private void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUILayout.BeginArea(new Rect(10, 120, 250, 150));
        GUILayout.Box("Screen Info");
        GUILayout.Label($"Target: {GameSettings.SCREEN_WIDTH}x{GameSettings.SCREEN_HEIGHT}");
        GUILayout.Label($"Current: {Screen.width}x{Screen.height}");
        GUILayout.Label($"Aspect: {(float)Screen.width / Screen.height:F3} (target: {GameSettings.ASPECT_RATIO:F3})");
        GUILayout.Label($"World Size: {GameSettings.WORLD_WIDTH:F2} x {GameSettings.WORLD_HEIGHT:F2}");
        
        if (targetCamera != null)
        {
            GUILayout.Label($"OrthoSize: {targetCamera.orthographicSize:F2}");
        }
        GUILayout.EndArea();
    }
    
    private void OnDrawGizmos()
    {
        // Rysuj granice widoku kamery
        float halfHeight = settings != null ? settings.cameraOrthoSize : 10f;
        float halfWidth = halfHeight * GameSettings.ASPECT_RATIO;
        
        Gizmos.color = Color.cyan;
        
        // Rysuj prostokąt widoku
        Vector3 topLeft = new Vector3(-halfWidth, halfHeight, 0);
        Vector3 topRight = new Vector3(halfWidth, halfHeight, 0);
        Vector3 bottomLeft = new Vector3(-halfWidth, -halfHeight, 0);
        Vector3 bottomRight = new Vector3(halfWidth, -halfHeight, 0);
        
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
        
        // Środkowe linie pomocnicze
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawLine(new Vector3(-halfWidth, 0, 0), new Vector3(halfWidth, 0, 0));
        Gizmos.DrawLine(new Vector3(0, -halfHeight, 0), new Vector3(0, halfHeight, 0));
    }
#endif
}

