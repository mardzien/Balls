using UnityEngine;

/// <summary>
/// Konfiguracja parametrów gry - ScriptableObject do łatwej edycji w inspektorze.
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "Ball Engine/Game Settings")]
public class GameSettings : ScriptableObject
{
    [Header("Ring Settings")]
    [Tooltip("Promień pierścienia w jednostkach Unity")]
    public float ringRadius = 3f;
    
    [Tooltip("Grubość linii pierścienia")]
    public float ringThickness = 0.2f;
    
    [Tooltip("Kąt luki w stopniach (np. 30 = luka 30 stopni)")]
    [Range(10f, 90f)]
    public float gapAngleDegrees = 30f;
    
    [Tooltip("Prędkość obrotu pierścienia w stopniach na sekundę")]
    public float rotationSpeed = 45f;
    
    [Tooltip("Kolor pierścienia")]
    public Color ringColor = Color.white;
    
    [Header("Ball Settings")]
    [Tooltip("Promień kulki")]
    public float ballRadius = 0.15f;
    
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
    public float escapeBuffer = 0.5f;
}

