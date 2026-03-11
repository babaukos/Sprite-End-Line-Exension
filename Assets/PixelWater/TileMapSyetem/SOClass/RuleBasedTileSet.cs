using UnityEngine;
using System.Collections.Generic;


[CreateAssetMenu(fileName = "NewRuleTileSet", menuName = "RPG/Rule Based Tile Set")]
public class RuleBasedTileSet : ScriptableObject
{
    [System.Serializable]
    public class TileRule
    {
        public Sprite sprite;
        
        // 8 напрямків
        public bool up, down, left, right;
        public bool upLeft, upRight, downLeft, downRight;

        public bool IsMatch(bool u, bool d, bool l, bool r, bool ul, bool ur, bool dl, bool dr)
        {
            return up == u && down == d && left == l && right == r &&
                   upLeft == ul && upRight == ur && downLeft == dl && downRight == dr;
        }
    }

    public Sprite defaultSprite;
    public List<TileRule> rules = new List<TileRule>();

    public Sprite GetSprite(bool u, bool d, bool l, bool r, bool ul, bool ur, bool dl, bool dr)
    {
        for (int i = 0; i < rules.Count; i++)
        {
            if (rules[i] != null && rules[i].IsMatch(u, d, l, r, ul, ur, dl, dr))
                return rules[i].sprite;
        }
        return defaultSprite;
    }
}