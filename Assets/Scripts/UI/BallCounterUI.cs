using UnityEngine;
using TMPro;

/// <summary>
/// Globalny licznik UI wyświetlający wartość (czas lub odbicia) dla aktywnej piłki.
/// Umieszczony pod kształtem (pierścieniem/elipsą).
/// </summary>
public class BallCounterUI : MonoBehaviour
{
    private TextMeshPro textMesh;
    private GameSettings settings;
    private Ball currentBall;
    
    /// <summary>
    /// Inicjalizuje globalny licznik.
    /// </summary>
    /// <param name="gameSettings">Ustawienia gry</param>
    /// <param name="positionUnderShape">Pozycja pod kształtem</param>
    public void Initialize(GameSettings gameSettings, Vector3 positionUnderShape)
    {
        settings = gameSettings;
        
        SetupTextMesh();
        transform.position = positionUnderShape;
    }
    
    /// <summary>
    /// Ustawia aktywną piłkę do śledzenia.
    /// </summary>
    /// <param name="ball">Aktywna piłka</param>
    public void SetActiveBall(Ball ball)
    {
        currentBall = ball;
        UpdateDisplay();
    }
    
    private void SetupTextMesh()
    {
        try
        {
            // Tworzenie TextMeshPro
            textMesh = gameObject.AddComponent<TextMeshPro>();
            
            // Konfiguracja wyglądu - DUŻY rozmiar
            textMesh.fontSize = 12f; // Znacznie zwiększone dla lepszej widoczności
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.color = Color.white;
            textMesh.fontStyle = FontStyles.Bold;
            
            // Grubszy outline dla lepszej widoczności
            textMesh.outlineWidth = 0.4f;
            textMesh.outlineColor = Color.black;
            
            // Sortowanie - renderuj nad innymi elementami
            textMesh.sortingOrder = 100;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[BallCounterUI] Failed to create TextMeshPro: {e.Message}. Import TMP Essential Resources from Window > TextMeshPro > Import TMP Essential Resources");
            // Zniszcz ten obiekt jeśli TextMeshPro nie może być utworzone
            Destroy(gameObject);
        }
    }
    
    private void Update()
    {
        if (settings == null)
        {
            return;
        }
        
        // Pozycja jest zarządzana przez GameManager
        // Tylko aktualizuj wyświetlaną wartość
        UpdateDisplay();
    }
    
    private void UpdateDisplay()
    {
        if (textMesh == null || settings == null) return;
        
        // Jeśli nie ma aktywnej piłki, ukryj licznik
        if (currentBall == null || currentBall.IsFrozen)
        {
            textMesh.text = "";
            return;
        }
        
        int displayValue = 0;
        
        // Wybór wartości do wyświetlenia na podstawie trybu zamrażania
        if (settings.freezeMode == FreezeMode.Time)
        {
            // Wyświetl pozostały czas (obcięcie części po przecinku, nie zaokrąglanie)
            displayValue = Mathf.FloorToInt(currentBall.RemainingTime);
        }
        else if (settings.freezeMode == FreezeMode.Bounces)
        {
            // Wyświetl pozostałe odbicia
            displayValue = currentBall.RemainingBounces;
        }
        
        // Aktualizuj tekst
        textMesh.text = displayValue.ToString();
        
        // Opcjonalnie: zmień kolor gdy wartość jest niska
        if (displayValue <= 1)
        {
            textMesh.color = Color.red;
        }
        else if (displayValue <= 2)
        {
            textMesh.color = Color.yellow;
        }
        else
        {
            textMesh.color = Color.white;
        }
    }
    
    /// <summary>
    /// Ukrywa licznik.
    /// </summary>
    public void Hide()
    {
        if (textMesh != null)
        {
            textMesh.enabled = false;
        }
    }
    
    /// <summary>
    /// Pokazuje licznik.
    /// </summary>
    public void Show()
    {
        if (textMesh != null)
        {
            textMesh.enabled = true;
        }
    }
}
