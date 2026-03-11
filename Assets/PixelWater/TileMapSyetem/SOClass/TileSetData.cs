using UnityEngine;

[CreateAssetMenu(fileName = "NewTileSet", menuName = "RPG/TileSet")]
public class TileSetData : ScriptableObject
{
    public enum TileSetType { Simple, Standard16, T_Shape }
    [Header("Settings")]
    public TileSetType setType = TileSetType.Standard16;

    // Базові
    public Sprite center;    
    public Sprite isolated;  

    // Сторони та кути
    public Sprite topEdge, bottomEdge, leftEdge, rightEdge;
    public Sprite topLeftCorner, topRightCorner, bottomLeftCorner, bottomRightCorner;

    // Проходи
    public Sprite verticalPass, horizontalPass; 

    // Трійники
    public Sprite tUpShape, tDownShape, tLeftShape, tRightShape; 

    public Sprite GetSprite(bool hasUp, bool hasDown, bool hasLeft, bool hasRight)
    {
        int mask = 0;
        if (hasUp)    mask += 1;
        if (hasDown)  mask += 2;
        if (hasLeft)  mask += 4;
        if (hasRight) mask += 8;

        // Логіка вибору спрайту залишається універсальною, 
        // просто якщо спрайт не призначений (null), повертаємо center
        Sprite result = GetRawSprite(mask);
        return result != null ? result : center;
    }

    private Sprite GetRawSprite(int mask)
    {
        switch (mask)
        {
            case 0:  return isolated;
            case 1:  return bottomEdge;
            case 2:  return topEdge;
            case 3:  return verticalPass;
            case 4:  return rightEdge;
            case 5:  return bottomRightCorner;
            case 6:  return topRightCorner;
            case 7:  return tRightShape;
            case 8:  return leftEdge;
            case 9:  return bottomLeftCorner;
            case 10: return topLeftCorner;
            case 11: return tLeftShape;
            case 12: return horizontalPass;
            case 13: return tDownShape;
            case 14: return tUpShape;
            case 15: return center;
            default: return center;
        }
    }
}