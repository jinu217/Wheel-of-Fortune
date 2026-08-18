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
    private BattleManager battleManager;

    public int Capacity => MaxInventorySize;
    public bool IsFull => items.Count >= MaxInventorySize;
    public IReadOnlyList<ItemData> Items => items;

    public event Action InventoryChanged;

    public void SetPlayerStats(PlayerStatManager stats)
    {
        playerStats = stats;
    }

    public void SetBattleManager(BattleManager manager)
    {
        battleManager = manager;
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
        bool applied = true;
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
                if (!HasActiveBattle()) { applied = false; break; }
                playerStats.AddAttackRouletteCoins(item.Value);
                break;
            case ItemEffectType.DefenseRouletteCoin:
                if (!HasActiveBattle()) { applied = false; break; }
                playerStats.AddDefenseRouletteCoins(item.Value);
                break;
            case ItemEffectType.DodgeNextAttack:
                if (!HasActiveBattle()) { applied = false; break; }
                playerStats.AddDodge(1);
                break;
            case ItemEffectType.DamageEnemy:
                if (!HasActiveBattle()) { applied = false; break; }
                battleManager.DamageMonsterWithItem(item.Value);
                break;
            case ItemEffectType.DelayEnemyTurn:
                if (!HasActiveBattle()) { applied = false; break; }
                battleManager.DelayMonsterOneTurn();
                break;
            case ItemEffectType.Barrier:
                if (!HasActiveBattle()) { applied = false; break; }
                playerStats.AddBarrier(item.Value);
                break;
            case ItemEffectType.Mushroom:
                if (!HasActiveBattle()) { applied = false; break; }
                playerStats.AddTimedModifier(StatType.Attack, 3, 3);
                playerStats.AddTimedModifier(StatType.Defense, -1, 3);
                break;
            case ItemEffectType.FateCoin:
                applied = HasActiveBattle() && battleManager.UseFateCoin();
                break;
            case ItemEffectType.DoubleBattleGold:
                if (!HasActiveBattle()) { applied = false; break; }
                battleManager.DoubleBattleGold();
                break;
        }

        if (!applied) return false;
        items.RemoveAt(itemIndex);
        InventoryChanged?.Invoke();
        return true;
    }

    private bool HasActiveBattle()
    {
        return battleManager != null && battleManager.IsBattleActive;
    }

    public void ClearInventory()
    {
        items.Clear();
        InventoryChanged?.Invoke();
    }
}
