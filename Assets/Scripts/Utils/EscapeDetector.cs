using UnityEngine;

/// <summary>
/// Wykrywa czy kulka uciekła przez lukę w pierścieniu.
/// Sprawdza odległość od środka i czy kulka jest w obszarze luki.
/// </summary>
public class EscapeDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameSettings settings;
    [SerializeField] private Ring ring;
    [SerializeField] private Ball ball;
    
    /// <summary>
    /// Event wywoływany gdy kulka ucieknie przez lukę.
    /// </summary>
    public event System.Action OnBallEscaped;
    
    private bool hasEscaped = false;
    
    /// <summary>
    /// Czy kulka już uciekła w tej rundzie.
    /// </summary>
    public bool HasEscaped => hasEscaped;
    
    /// <summary>
    /// Inicjalizuje detektor z referencjami.
    /// </summary>
    public void Initialize(GameSettings gameSettings, Ring ringRef, Ball ballRef)
    {
        settings = gameSettings;
        ring = ringRef;
        ball = ballRef;
        hasEscaped = false;
    }
    
    private void Update()
    {
        if (hasEscaped || ball == null || ring == null || settings == null)
            return;
        
        CheckForEscape();
    }
    
    private void CheckForEscape()
    {
        float distance = ball.GetDistanceFromCenter();
        float escapeThreshold = ring.GetRadius() + settings.escapeBuffer;
        
        // Sprawdź czy kulka przekroczyła próg ucieczki
        if (distance > escapeThreshold)
        {
            // Sprawdź czy kulka jest w obszarze luki
            float ballAngle = ball.GetAngleFromCenter();
            
            if (ring.IsInGap(ballAngle))
            {
                // Kulka uciekła przez lukę!
                hasEscaped = true;
                Debug.Log($"Ball escaped! Distance: {distance:F2}, Angle: {ballAngle:F1}°, Gap: {ring.GapStartAngle:F1}° - {ring.GapEndAngle:F1}°");
                OnBallEscaped?.Invoke();
            }
            else
            {
                // Kulka jest poza pierścieniem ale NIE przez lukę
                // To może oznaczać błąd w fizyce lub zbyt szybki ruch
                // Na razie traktujemy to też jako ucieczkę (do debugowania)
                Debug.LogWarning($"Ball escaped outside gap! Distance: {distance:F2}, Angle: {ballAngle:F1}°");
                hasEscaped = true;
                OnBallEscaped?.Invoke();
            }
        }
    }
    
    /// <summary>
    /// Resetuje stan detektora dla nowej rundy.
    /// </summary>
    public void Reset()
    {
        hasEscaped = false;
    }
    
    /// <summary>
    /// Zwraca aktualną odległość kulki od progu ucieczki.
    /// Wartość ujemna = kulka wewnątrz, dodatnia = kulka uciekła.
    /// </summary>
    public float GetDistanceToEscape()
    {
        if (ball == null || ring == null || settings == null)
            return 0f;
        
        float distance = ball.GetDistanceFromCenter();
        float escapeThreshold = ring.GetRadius() + settings.escapeBuffer;
        return distance - escapeThreshold;
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (settings == null || ring == null) return;
        
        // Rysuj próg ucieczki (tylko gdy zaznaczony)
        Gizmos.color = hasEscaped ? Color.red : Color.green;
        float escapeRadius = ring.GetRadius() + settings.escapeBuffer;
        
        // Rysuj okrąg progu ucieczki
        int segments = 64;
        Vector3 prevPoint = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 point = new Vector3(
                Mathf.Cos(angle) * escapeRadius,
                Mathf.Sin(angle) * escapeRadius,
                0f
            );
            
            if (i > 0)
            {
                Gizmos.DrawLine(prevPoint, point);
            }
            prevPoint = point;
        }
    }
#endif
}

