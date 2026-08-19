using System;
using System.Collections.Generic;
using UnityEngine;

public enum RandomEventType
{
    TreasureChest,
    Shaman,
    CausalityShrine,
    LifeSpring,
    ThornBush
}

public enum TreasureChestResult
{
    Gold,
    Mimic
}

public class RandomEventManager : MonoBehaviour
{
    [Tooltip("랜덤 이벤트 효과를 적용할 플레이어입니다.")] [SerializeField] private PlayerStatManager playerStats;
    [Tooltip("가시 덤불 아이템을 추가할 인벤토리입니다.")] [SerializeField] private PlayerInventoryManager inventory;
    [Tooltip("보물상자 미믹을 포함한 전체 몬스터 데이터베이스입니다.")]
    [SerializeField] private MonsterDatabase monsterDatabase;
    [Tooltip("가시 덤불에서 아이템을 추첨할 전체 아이템 데이터베이스입니다.")] [SerializeField] private ItemDatabase itemDatabase;

    [Header("Treasure Chest")]
    [Tooltip("보물상자에서 미믹이 나오지 않았을 때 얻는 골드입니다.")] [Min(0)] [SerializeField] private int treasureGold = 50;
    [Tooltip("보물상자 대신 미믹이 등장할 확률입니다. 0.25는 25%입니다.")] [Range(0f, 1f)] [SerializeField] private float mimicChance = 0.25f;

    [Header("Shaman")]
    [Tooltip("주술사에게 능력을 받을 때 지불할 골드입니다.")] [Min(0)] [SerializeField] private int shamanPrice = 20;

    [Header("Thorn Bush")]
    [Tooltip("가시 덤불 진입 시 플레이어가 받는 피해입니다.")] [Min(0)] [SerializeField] private int thornDamage = 5;

    public event Action<RandomEventType> RandomEventCompleted;
    public event Action<RandomEventType> RandomEventSelected;
    public event Action<MonsterData> MimicEncountered;
    public bool CanAffordShaman => playerStats != null && playerStats.Coin >= shamanPrice;
    public int ShamanPrice => shamanPrice;

    public RandomEventType SelectRandomEvent()
    {
        // Five enum values: every value has exactly a 1/5 (20%) chance.
        RandomEventType selected = (RandomEventType)UnityEngine.Random.Range(0, 5);
        RandomEventSelected?.Invoke(selected);
        return selected;
    }

    public void SetPlayerData(PlayerStatManager stats, PlayerInventoryManager playerInventory)
    {
        playerStats = stats;
        inventory = playerInventory;
    }

    public TreasureChestResult OpenTreasureChest()
    {
        MonsterData mimicMonster = monsterDatabase == null ? null : monsterDatabase.MimicMonster;
        if (UnityEngine.Random.value < mimicChance && mimicMonster != null)
        {
            return TreasureChestResult.Mimic;
        }

        playerStats.AddCoins(treasureGold);
        RandomEventCompleted?.Invoke(RandomEventType.TreasureChest);
        return TreasureChestResult.Gold;
    }

    public void StartMimicEncounter()
    {
        MonsterData mimicMonster = monsterDatabase == null ? null : monsterDatabase.MimicMonster;
        if (mimicMonster == null)
        {
            Debug.LogError("몬스터 데이터베이스에 미믹 데이터가 없습니다.", this);
            return;
        }

        MimicEncountered?.Invoke(mimicMonster);
    }

    public bool TryUseShaman(int floorNumber, out AbilityDefinition acquiredAbility)
    {
        acquiredAbility = null;
        PlayerAbilityManager abilities = GameSessionManager.Instance == null
            ? null
            : GameSessionManager.Instance.PlayerAbilities;
        if (playerStats == null || abilities == null) return false;

        List<AbilityDefinition> choices = abilities.GenerateChoices(floorNumber, 1);
        if (choices.Count == 0 || !playerStats.TrySpendCoins(shamanPrice))
        {
            return false;
        }

        acquiredAbility = choices[0];
        abilities.Acquire(acquiredAbility);
        return true;
    }

    public void CompleteShaman()
    {
        RandomEventCompleted?.Invoke(RandomEventType.Shaman);
    }

    public void CompleteCausalityShrine()
    {
        RandomEventCompleted?.Invoke(RandomEventType.CausalityShrine);
    }

    public void UseLifeSpring()
    {
        playerStats.RestoreFullHp();
        RandomEventCompleted?.Invoke(RandomEventType.LifeSpring);
    }

    public ItemData EnterThornBush()
    {
        playerStats.TakeDamage(thornDamage);
        ItemData acquiredItem = null;
        if (inventory != null && !inventory.IsFull && itemDatabase != null && itemDatabase.Count > 0)
        {
            ItemData item = itemDatabase.GetRandomItem();
            if (inventory.TryAddItem(item)) acquiredItem = item;
        }
        RandomEventCompleted?.Invoke(RandomEventType.ThornBush);
        return acquiredItem;
    }
}
