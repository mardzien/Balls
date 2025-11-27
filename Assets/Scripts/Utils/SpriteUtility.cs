using UnityEngine;

/// <summary>
/// Narzędzia do tworzenia sprite'ów proceduralnych.
/// </summary>
public static class SpriteUtility
{
    // Cache dla sprite'ów o różnych rozmiarach
    private static Sprite cachedBallSprite;
    private static Sprite cachedSmallSprite;
    
    /// <summary>
    /// Tworzy lub zwraca z cache sprite koła dla piłki (64x64).
    /// </summary>
    public static Sprite GetBallSprite()
    {
        if (cachedBallSprite == null)
        {
            cachedBallSprite = CreateCircleSprite(64);
        }
        return cachedBallSprite;
    }
    
    /// <summary>
    /// Tworzy lub zwraca z cache mały sprite koła dla fragmentów (16x16).
    /// </summary>
    public static Sprite GetSmallSprite()
    {
        if (cachedSmallSprite == null)
        {
            cachedSmallSprite = CreateCircleSprite(16);
        }
        return cachedSmallSprite;
    }
    
    /// <summary>
    /// Tworzy sprite koła o podanym rozmiarze.
    /// </summary>
    public static Sprite CreateCircleSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        
        float center = size / 2f;
        float radius = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                
                texture.SetPixel(x, y, distance <= radius ? Color.white : Color.clear);
            }
        }
        
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        
        return Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            size
        );
    }
}

