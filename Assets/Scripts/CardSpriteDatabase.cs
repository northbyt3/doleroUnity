using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Sprite Database")]
public class CardSpriteDatabase : ScriptableObject
{
    [System.Serializable]
    public struct CardKey
    {
        public int rank;
        public int suit;
        //0 = spades 1 = hearts 2 = diamonds 3 = clubs
    }

    [System.Serializable]
    public struct CardSpriteEntry
    {
        public CardKey key;
        public Sprite sprite;
    }

    public List<CardSpriteEntry> entries;

    private Dictionary<(int, int), Sprite> _lookup;

    public Sprite GetSprite(int rank, int suit)
    {
        if (_lookup == null)
        {
            _lookup = new Dictionary<(int, int), Sprite>();
            foreach (var e in entries)
                _lookup[(e.key.rank, e.key.suit)] = e.sprite;
        }
        return _lookup.TryGetValue((rank, suit), out var sprite) ? sprite : null;
    }
}
