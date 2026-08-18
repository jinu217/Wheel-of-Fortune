using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventoryManager : MonoBehaviour
{
    private const int MaxInventorySize = 3;

    [Tooltip("아이템 효과를 적용할 플레이어 능력치 관리자입니다.")]
    [SerializeField] private PlayerStatManager playerStats;
    [Tooltip("플레이어가 보유한 소모품 목록입니다. 최대 3개까지 보유합니다.")]
    [SerializeField] private List<ItemData> items = new List<ItemData>();

    public int Capacity => MaxInventorySize;
    public bool IsFull => items.Count >= MaxInventorySize;
    public IReadOnlyList<ItemData> Items => items;

    public event Action InventoryChanged;

    public void SetPlayerStats(PlayerStatManager stats)
    {
        playerStats = stats;
    }

    public bool TryAddItem(ItemData item)
    {
        if (item == null || IsFull)
        {
            return false;
        }

        items.Add(item);
        InventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveItem(ItemData item)
    {
        if (item == null || !items.Remove(item))
        {
            return false;
        }

        InventoryChanged?.Invoke();
        return true;
    }

    public bool UseItem(int itemIndex)
    {
        if (playerStats == null || itemIndex < 0 || itemIndex >= items.Count)
        {
            return false;
        }

        ItemData item = items[itemIndex];
        switch (item.Effect)
        {
            case ItemEffectType.Heal:
                playerStats.Heal(item.Value);
                break;
            case ItemEffectType.AttackBuff:
                playerStats.AddTimedModifier(StatType.Attack, item.Value, 1);
                break;
            case ItemEffectType.DefenseBuff:
                playerStats.AddTimedModifier(StatType.Defense, item.Value, 1);
                break;
            case ItemEffectType.AttackRouletteCoin:
                playerStats.AddAttackRouletteCoins(item.Value);
                break;
            case ItemEffectType.DefenseRouletteCoin:
                playerStats.AddDefenseRouletteCoins(item.Value);
                break;
        }

        items.RemoveAt(itemIndex);
        InventoryChanged?.Invoke();
        return true;
    }

    public void ClearInventory()
    {
        items.Clear();
        InventoryChanged?.Invoke();
    }
}
