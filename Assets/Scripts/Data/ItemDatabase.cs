using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Game Data/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [Tooltip("게임에서 사용하는 모든 ItemData 목록입니다.")]
    [SerializeField] private List<ItemData> items = new List<ItemData>();

    public IReadOnlyList<ItemData> Items => items;
    public int Count => items.Count;

    public ItemData GetRandomItem()
    {
        if (items.Count == 0) return null;
        return items[Random.Range(0, items.Count)];
    }

    public ItemData GetById(string itemId)
    {
        return items.Find(item => item != null && item.ItemId == itemId);
    }
}
