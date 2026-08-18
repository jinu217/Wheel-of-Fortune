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

public enum CausalityChoice
{
    TwoAbilitiesAndChance,
    OneRandomAbility
}

[Serializable]
public struct RandomAbility
{
    [Tooltip("랜덤 능력의 표시 이름입니다.")] public string abilityName;
    [Tooltip("랜덤 능력이 증가시킬 플레이어 능력치입니다.")] public StatType statType;
    [Tooltip("랜덤 능력으로 증가하는 수치입니다.")] public int value;
}

public class RandomEventManager : MonoBehaviour
{
    [Tooltip("랜덤 이벤트 효과를 적용할 플레이어입니다.")] [SerializeField] private PlayerStatManager playerStats;
    [Tooltip("가시 덤불 아이템을 추가할 인벤토리입니다.")] [SerializeField] private PlayerInventoryManager inventory;
    [Tooltip("보물상자 미믹을 포함한 전체 몬스터 데이터베이스입니다.")]
    [SerializeField] private MonsterDatabase monsterDatabase;
    [Tooltip("가시 덤불에서 아이템을 추첨할 전체 아이템 데이터베이스입니다.")] [SerializeField] private ItemDatabase itemDatabase;
    [Tooltip("주술사와 인과율의 신전에서 얻을 수 있는 능력 목록입니다.")] [SerializeField] private List<RandomAbility> randomAbilities = new List<RandomAbility>();

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
    public event Action CausalityChanceTriggered;

    private ItemDatabase Database => itemDatabase;

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

    public void OpenTreasureChest()
    {
        MonsterData mimicMonster = monsterDatabase == null ? null : monsterDatabase.MimicMonster;
        if (UnityEngine.Random.value < mimicChance && mimicMonster != null)
        {
            MimicEncountered?.Invoke(mimicMonster);
            return;
        }

        playerStats.AddCoins(treasureGold);
        RandomEventCompleted?.Invoke(RandomEventType.TreasureChest);
    }

    public bool TryUseShaman()
    {
        if (!CanGrantAbility() || !playerStats.TrySpendCoins(shamanPrice))
        {
            return false;
        }

        GrantRandomAbility();
        RandomEventCompleted?.Invoke(RandomEventType.Shaman);
        return true;
    }

    public void UseCausalityShrine(CausalityChoice choice)
    {
        if (choice == CausalityChoice.TwoAbilitiesAndChance)
        {
            GrantRandomAbility();
            GrantRandomAbility();
            CausalityChanceTriggered?.Invoke();
        }
        else
        {
            GrantRandomAbility();
        }

        RandomEventCompleted?.Invoke(RandomEventType.CausalityShrine);
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

    public bool EnterThornBush()
    {
        playerStats.TakeDamage(thornDamage);
        bool added = false;
        if (inventory != null && !inventory.IsFull && Database != null && Database.Count > 0)
        {
            ItemData item = Database.GetRandomItem();
            added = inventory.TryAddItem(item);
        }
        RandomEventCompleted?.Invoke(RandomEventType.ThornBush);
        return added;
    }

    private bool CanGrantAbility()
    {
        return playerStats != null && randomAbilities.Count > 0;
    }

    private void GrantRandomAbility()
    {
        if (!CanGrantAbility())
        {
            return;
        }

        RandomAbility ability = randomAbilities[UnityEngine.Random.Range(0, randomAbilities.Count)];
        playerStats.AddPermanentStat(ability.statType, ability.value);
    }
}
