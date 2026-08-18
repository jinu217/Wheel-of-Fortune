using System;
using System.Collections.Generic;
using UnityEngine;

public enum ChanceEffectType
{
    GreenTwoToRedStatsUp,
    RedToBlackGreenToWhite,
    GreenTwoToGoldStatsDown,
    RedGreenToYellowMaxHpDown,
    RandomOneToBlackStatsUp,
    GreenTwoToBlueStatsDown,
    AttackCoinUpDefenseCoinDown,
    SafeTwoToRedBlackMaxHpUpAbility,
    LoseGoldItemsGainTwoAbilities,
    GreenToLuckyAndUnluckyAllStatsUp
}

public enum ChanceRouletteMutationType
{
    GreenTwoToRed,
    RedToBlackGreenToWhite,
    GreenTwoToGold,
    RedGreenToYellow,
    RandomOneToBlack,
    GreenTwoToBlue,
    SafeTwoToRedBlack,
    GreenToLuckyAndUnlucky
}

[Serializable]
public sealed class ChanceEffectDefinition
{
    public ChanceEffectType Type { get; }
    public string Name { get; }
    public string Description { get; }

    public ChanceEffectDefinition(ChanceEffectType type, string name, string description)
    {
        Type = type;
        Name = name;
        Description = description;
    }
}

[Serializable]
public struct ChanceRouletteMutation
{
    public ChanceRouletteMutationType type;
    public int seed;
}

public class ChanceSystemManager : MonoBehaviour
{
    private const float BaseChance = 0.30f;
    private const float MissIncrease = 0.10f;

    private readonly List<ChanceEffectDefinition> catalog = new List<ChanceEffectDefinition>();
    private readonly List<ChanceRouletteMutation> rouletteMutations = new List<ChanceRouletteMutation>();
    private readonly List<ChanceEffectType> acquiredChanceEffects = new List<ChanceEffectType>();
    private float currentChance = BaseChance;
    private PlayerStatManager stats;
    private PlayerInventoryManager inventory;
    private PlayerAbilityManager abilities;

    public float CurrentChance => currentChance;
    public IReadOnlyList<ChanceRouletteMutation> RouletteMutations => rouletteMutations;
    public int AcquiredChanceCount => acquiredChanceEffects.Count;
    public IReadOnlyList<ChanceEffectType> AcquiredChanceEffects => acquiredChanceEffects;

    private void Awake()
    {
        BuildCatalog();
    }

    public void SetPlayerData(PlayerStatManager playerStats, PlayerInventoryManager playerInventory,
        PlayerAbilityManager abilityManager)
    {
        stats = playerStats;
        inventory = playerInventory;
        abilities = abilityManager;
    }

    public bool TryGenerateEvent(int floorNumber, out List<ChanceEffectDefinition> choices,
        out ChanceEffectDefinition selected)
    {
        choices = new List<ChanceEffectDefinition>();
        selected = null;
        if (floorNumber < 2) return false;

        bool triggered = UnityEngine.Random.value < currentChance;
        currentChance = triggered ? BaseChance : Mathf.Min(1f, currentChance + MissIncrease);
        if (!triggered) return false;

        List<ChanceEffectDefinition> pool = new List<ChanceEffectDefinition>(catalog);
        for (int i = 0; i < 3 && pool.Count > 0; i++)
        {
            int index = UnityEngine.Random.Range(0, pool.Count);
            choices.Add(pool[index]);
            pool.RemoveAt(index);
        }

        selected = choices[UnityEngine.Random.Range(0, choices.Count)];
        return true;
    }

    public void Apply(ChanceEffectDefinition effect, int floorNumber)
    {
        if (effect == null || stats == null) return;
        acquiredChanceEffects.Add(effect.Type);
        switch (effect.Type)
        {
            case ChanceEffectType.GreenTwoToRedStatsUp:
                AddMutation(ChanceRouletteMutationType.GreenTwoToRed);
                AddStats(3, 2, 0);
                break;
            case ChanceEffectType.RedToBlackGreenToWhite:
                AddMutation(ChanceRouletteMutationType.RedToBlackGreenToWhite);
                break;
            case ChanceEffectType.GreenTwoToGoldStatsDown:
                AddMutation(ChanceRouletteMutationType.GreenTwoToGold);
                AddStats(-3, -2, 0);
                break;
            case ChanceEffectType.RedGreenToYellowMaxHpDown:
                AddMutation(ChanceRouletteMutationType.RedGreenToYellow);
                AddStats(0, 0, -10);
                break;
            case ChanceEffectType.RandomOneToBlackStatsUp:
                AddMutation(ChanceRouletteMutationType.RandomOneToBlack);
                AddStats(2, 1, 5);
                break;
            case ChanceEffectType.GreenTwoToBlueStatsDown:
                AddMutation(ChanceRouletteMutationType.GreenTwoToBlue);
                AddStats(-2, -1, 0);
                break;
            case ChanceEffectType.AttackCoinUpDefenseCoinDown:
                stats.AddPermanentAttackRouletteCoins(1);
                stats.AddPermanentDefenseRouletteCoins(-1);
                break;
            case ChanceEffectType.SafeTwoToRedBlackMaxHpUpAbility:
                AddMutation(ChanceRouletteMutationType.SafeTwoToRedBlack);
                AddStats(0, 0, 20);
                abilities?.AcquireRandomAbility(floorNumber);
                break;
            case ChanceEffectType.LoseGoldItemsGainTwoAbilities:
                stats.AddCoins(-stats.Coin);
                inventory?.ClearInventory();
                abilities?.AcquireRandomAbility(floorNumber);
                abilities?.AcquireRandomAbility(floorNumber);
                break;
            case ChanceEffectType.GreenToLuckyAndUnluckyAllStatsUp:
                AddMutation(ChanceRouletteMutationType.GreenToLuckyAndUnlucky);
                AddStats(1, 1, 1);
                break;
        }

        if (abilities != null && abilities.Has(PassiveAbilityType.ChanceGrantsAbility))
            abilities.AcquireRandomAbility(floorNumber);
    }

    public void ResetForNewRun()
    {
        currentChance = BaseChance;
        rouletteMutations.Clear();
        acquiredChanceEffects.Clear();
    }

    public bool RemoveRandomAbilityIncludingChance()
    {
        int passiveCount = abilities == null ? 0 : abilities.AcquiredCount;
        int total = passiveCount + acquiredChanceEffects.Count;
        if (total <= 0) return false;
        int selected = UnityEngine.Random.Range(0, total);
        if (selected < passiveCount) return abilities.RemoveRandomAbility();
        RemoveChanceEffectAt(selected - passiveCount);
        return true;
    }

    public ChanceEffectDefinition GetDefinition(ChanceEffectType type)
    {
        return catalog.Find(item => item.Type == type);
    }

    public bool RemoveChanceEffect(ChanceEffectType type)
    {
        int index = acquiredChanceEffects.IndexOf(type);
        if (index < 0) return false;
        RemoveChanceEffectAt(index);
        return true;
    }

    public ChanceEffectDefinition AcquireRandomChance(int floorNumber)
    {
        if (catalog.Count == 0) return null;
        ChanceEffectDefinition selected = catalog[UnityEngine.Random.Range(0, catalog.Count)];
        Apply(selected, floorNumber);
        return selected;
    }

    private void RemoveChanceEffectAt(int index)
    {
        if (index < 0 || index >= acquiredChanceEffects.Count) return;
        ChanceEffectType type = acquiredChanceEffects[index];
        acquiredChanceEffects.RemoveAt(index);
        switch (type)
        {
            case ChanceEffectType.GreenTwoToRedStatsUp:
                RemoveMutation(ChanceRouletteMutationType.GreenTwoToRed); AddStats(-3, -2, 0); break;
            case ChanceEffectType.RedToBlackGreenToWhite:
                RemoveMutation(ChanceRouletteMutationType.RedToBlackGreenToWhite); break;
            case ChanceEffectType.GreenTwoToGoldStatsDown:
                RemoveMutation(ChanceRouletteMutationType.GreenTwoToGold); AddStats(3, 2, 0); break;
            case ChanceEffectType.RedGreenToYellowMaxHpDown:
                RemoveMutation(ChanceRouletteMutationType.RedGreenToYellow); AddStats(0, 0, 10); break;
            case ChanceEffectType.RandomOneToBlackStatsUp:
                RemoveMutation(ChanceRouletteMutationType.RandomOneToBlack); AddStats(-2, -1, -5); break;
            case ChanceEffectType.GreenTwoToBlueStatsDown:
                RemoveMutation(ChanceRouletteMutationType.GreenTwoToBlue); AddStats(2, 1, 0); break;
            case ChanceEffectType.AttackCoinUpDefenseCoinDown:
                stats.AddPermanentAttackRouletteCoins(-1); stats.AddPermanentDefenseRouletteCoins(1); break;
            case ChanceEffectType.SafeTwoToRedBlackMaxHpUpAbility:
                RemoveMutation(ChanceRouletteMutationType.SafeTwoToRedBlack); AddStats(0, 0, -20); break;
            case ChanceEffectType.GreenToLuckyAndUnluckyAllStatsUp:
                RemoveMutation(ChanceRouletteMutationType.GreenToLuckyAndUnlucky); AddStats(-1, -1, -1); break;
        }
    }

    private void RemoveMutation(ChanceRouletteMutationType type)
    {
        int index = rouletteMutations.FindIndex(item => item.type == type);
        if (index >= 0) rouletteMutations.RemoveAt(index);
    }

    private void AddStats(int attack, int defense, int maxHp)
    {
        if (attack != 0) stats.AddPermanentStat(StatType.Attack, attack);
        if (defense != 0) stats.AddPermanentStat(StatType.Defense, defense);
        if (maxHp != 0) stats.AddPermanentStat(StatType.MaxHp, maxHp);
    }

    private void AddMutation(ChanceRouletteMutationType type)
    {
        rouletteMutations.Add(new ChanceRouletteMutation
        {
            type = type,
            seed = UnityEngine.Random.Range(1, int.MaxValue)
        });
    }

    private void BuildCatalog()
    {
        if (catalog.Count > 0) return;
        Add(ChanceEffectType.GreenTwoToRedStatsUp, "뒤틀린 투지", "초록 2칸 → 빨강 2칸\n공격력 +3, 방어력 +2");
        Add(ChanceEffectType.RedToBlackGreenToWhite, "엇갈린 색", "빨강 1칸 → 검정, 초록 1칸 → 흰색");
        Add(ChanceEffectType.GreenTwoToGoldStatsDown, "황금의 대가", "초록 2칸 → 황금\n공격력 -3, 방어력 -2");
        Add(ChanceEffectType.RedGreenToYellowMaxHpDown, "생명의 교환", "빨강·초록 각 1칸 → 노랑\n최대 체력 -10");
        Add(ChanceEffectType.RandomOneToBlackStatsUp, "검은 성장", "무작위 1칸 → 검정\n공격력 +2, 방어력 +1, 최대 체력 +5");
        Add(ChanceEffectType.GreenTwoToBlueStatsDown, "푸른 대가", "초록 2칸 → 파랑\n공격력 -2, 방어력 -1");
        Add(ChanceEffectType.AttackCoinUpDefenseCoinDown, "공수 교환", "공격 코인 +1, 방어 코인 -1");
        Add(ChanceEffectType.SafeTwoToRedBlackMaxHpUpAbility, "위험한 생명", "안전한 2칸 → 빨강·검정\n최대 체력 +20, 능력 1개 획득");
        Add(ChanceEffectType.LoseGoldItemsGainTwoAbilities, "빈손의 각성", "모든 골드와 아이템 상실\n능력 2개 획득");
        Add(ChanceEffectType.GreenToLuckyAndUnluckyAllStatsUp, "운명의 양면", "초록 2칸이 행운·불행 색으로 변경\n모든 능력치 +1");
    }

    private void Add(ChanceEffectType type, string name, string description)
    {
        catalog.Add(new ChanceEffectDefinition(type, name, description));
    }
}
