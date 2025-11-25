using UnityEngine;

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
    
    [Header("Ring Settings")]
    [Tooltip("Promień pierścienia w jednostkach Unity (4.5 = 80% szerokości ekranu)")]
    public float ringRadius = 4.5f;
    
    [Tooltip("Grubość linii pierścienia")]
    public float ringThickness = 0.3f;
    
    [Tooltip("Kąt luki w stopniach (np. 30 = luka 30 stopni)")]
    [Range(10f, 90f)]
    public float gapAngleDegrees = 30f;
    
    [Tooltip("Prędkość obrotu pierścienia w stopniach na sekundę")]
    public float rotationSpeed = 45f;
    
    [Tooltip("Kolor pierścienia")]
    public Color ringColor = Color.white;
    
    [Header("Ball Settings")]
    [Tooltip("Promień kulki (0.25 = dobrze widoczna)")]
    public float ballRadius = 0.25f;
    
    [Tooltip("Współczynnik odbicia (0-1, gdzie 1 = idealne odbicie)")]
    [Range(0f, 1f)]
    public float bounciness = 0.8f;
    
    [Tooltip("Tarcie kulki")]
    [Range(0f, 1f)]
    public float friction = 0.1f;
    
    [Tooltip("Kolor kulki")]
    public Color ballColor = new Color(1f, 0.3f, 0.3f, 1f);
    
    [Header("Physics")]
    [Tooltip("Siła grawitacji (wartość ujemna = w dół)")]
    public float gravity = -9.81f;
    
    [Header("Game Loop")]
    [Tooltip("Opóźnienie przed restartem po ucieczce kulki (sekundy)")]
    public float restartDelay = 1f;
    
    [Tooltip("Pozycja startowa kulki względem środka pierścienia")]
    public Vector2 ballSpawnOffset = Vector2.zero;
    
    [Header("Escape Detection")]
    [Tooltip("Dodatkowy bufor dla detekcji ucieczki (dodawany do promienia pierścienia)")]
    public float escapeBuffer = 0.6f;
    
    /// <summary>
    /// Resetuje wszystkie wartości do zalecanych dla YouTube Shorts.
    /// W Unity: PPM na asset -> "Reset to Recommended"
    /// </summary>
    [ContextMenu("Reset to Recommended Values")]
    public void ResetToRecommended()
    {
        cameraOrthoSize = 10f;
        ringRadius = 4.5f;
        ringThickness = 0.3f;
        gapAngleDegrees = 30f;
        rotationSpeed = 45f;
        ringColor = Color.white;
        ballRadius = 0.25f;
        bounciness = 0.8f;
        friction = 0.1f;
        ballColor = new Color(1f, 0.3f, 0.3f, 1f);
        gravity = -9.81f;
        restartDelay = 1f;
        ballSpawnOffset = Vector2.zero;
        escapeBuffer = 0.6f;
        forceResolution = true;
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("GameSettings: Zresetowano do zalecanych wartości dla YouTube Shorts!");
        #endif
    }
}

