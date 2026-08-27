using System;
using System.Collections.Generic;
using UnityEngine;

public enum AbilityGrade { Small, Moderate, Good }

public enum PassiveAbilityType
{
    Attack2, Defense1, MaxHp5, BattleEndHeal3, BadColorBarrier2,
    NoBarrierEndTurn5, NonGreenHeal2, BattleEndGold5, CheapInn, ChanceGrantsAbility,
    Attack3, Defense2, MaxHp10, BattleEndHeal5, Retaliate3,
    FirstAttackFailureAttack5, ShopDiscount20, SpinCountBarrier, GreenToBlue, AttackBarrierMultiplier,
    MaxHp20, Attack5, Defense3, PermanentAttackCoin, PermanentDefenseCoin,
    GreenToGold, BattleEndHeal10, LowHpAttack3, MonsterEndBarrierHeal5, BadColorPermanentAttack1
}

[Serializable]
public class AbilityDefinition
{
    public PassiveAbilityType Type { get; }
    public AbilityGrade Grade { get; }
    public string Name { get; }
    public string Description { get; }

    public AbilityDefinition(PassiveAbilityType type, AbilityGrade grade, string name, string description)
    {
        Type = type; Grade = grade; Name = name; Description = description;
    }
}

public class PlayerAbilityManager : MonoBehaviour
{
    private readonly HashSet<PassiveAbilityType> acquired = new HashSet<PassiveAbilityType>();
    private readonly List<AbilityDefinition> catalog = new List<AbilityDefinition>();
    private PlayerStatManager stats;

    public event Action AbilitiesChanged;

    private void Awake()
    {
        stats = GetComponent<PlayerStatManager>() ?? GetComponentInChildren<PlayerStatManager>();
        BuildCatalog();
    }

    public bool Has(PassiveAbilityType type) => acquired.Contains(type);
    public int AcquiredCount => acquired.Count;

    public List<AbilityDefinition> GenerateChoices(int floorNumber, int count)
    {
        List<AbilityDefinition> available = catalog.FindAll(item => !acquired.Contains(item.Type));
        List<AbilityDefinition> result = new List<AbilityDefinition>();
        while (result.Count < count && available.Count > 0)
        {
            AbilityGrade grade = RollGrade(floorNumber);
            List<AbilityDefinition> gradePool = available.FindAll(item => item.Grade == grade);
            if (gradePool.Count == 0) gradePool = available;
            AbilityDefinition selected = gradePool[UnityEngine.Random.Range(0, gradePool.Count)];
            result.Add(selected);
            available.Remove(selected);
        }
        return result;
    }

    public void Acquire(AbilityDefinition ability)
    {
        if (ability == null || !acquired.Add(ability.Type)) return;
        ApplyImmediateEffect(ability.Type);
        AbilitiesChanged?.Invoke();
    }

    public void AcquireRandomAbility(int floorNumber)
    {
        List<AbilityDefinition> choices = GenerateChoices(floorNumber, 1);
        if (choices.Count > 0) Acquire(choices[0]);
    }

    public bool RemoveRandomAbility()
    {
        if (acquired.Count == 0) return false;
        List<PassiveAbilityType> candidates = new List<PassiveAbilityType>(acquired);
        PassiveAbilityType selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        acquired.Remove(selected);
        ReverseImmediateEffect(selected);
        AbilitiesChanged?.Invoke();
        return true;
    }

    public void ResetForNewRun()
    {
        acquired.Clear();
        AbilitiesChanged?.Invoke();
    }

    private void ApplyImmediateEffect(PassiveAbilityType type)
    {
        if (stats == null) return;
        switch (type)
        {
            case PassiveAbilityType.Attack2: stats.AddPermanentStat(StatType.Attack, 2); break;
            case PassiveAbilityType.Defense1: stats.AddPermanentStat(StatType.Defense, 1); break;
            case PassiveAbilityType.MaxHp5: stats.AddPermanentStat(StatType.MaxHp, 5); break;
            case PassiveAbilityType.Attack3: stats.AddPermanentStat(StatType.Attack, 3); break;
            case PassiveAbilityType.Defense2: stats.AddPermanentStat(StatType.Defense, 2); break;
            case PassiveAbilityType.MaxHp10: stats.AddPermanentStat(StatType.MaxHp, 10); break;
            case PassiveAbilityType.MaxHp20: stats.AddPermanentStat(StatType.MaxHp, 20); break;
            case PassiveAbilityType.Attack5: stats.AddPermanentStat(StatType.Attack, 5); break;
            case PassiveAbilityType.Defense3: stats.AddPermanentStat(StatType.Defense, 3); break;
            case PassiveAbilityType.PermanentAttackCoin: stats.AddPermanentAttackRouletteCoins(1); break;
            case PassiveAbilityType.PermanentDefenseCoin: stats.AddPermanentDefenseRouletteCoins(1); break;
            case PassiveAbilityType.LowHpAttack3: stats.SetLowHpAttackBonus(3); break;
        }
    }

    private void ReverseImmediateEffect(PassiveAbilityType type)
    {
        if (stats == null) return;
        switch (type)
        {
            case PassiveAbilityType.Attack2: stats.AddPermanentStat(StatType.Attack, -2); break;
            case PassiveAbilityType.Defense1: stats.AddPermanentStat(StatType.Defense, -1); break;
            case PassiveAbilityType.MaxHp5: stats.AddPermanentStat(StatType.MaxHp, -5); break;
            case PassiveAbilityType.Attack3: stats.AddPermanentStat(StatType.Attack, -3); break;
            case PassiveAbilityType.Defense2: stats.AddPermanentStat(StatType.Defense, -2); break;
            case PassiveAbilityType.MaxHp10: stats.AddPermanentStat(StatType.MaxHp, -10); break;
            case PassiveAbilityType.MaxHp20: stats.AddPermanentStat(StatType.MaxHp, -20); break;
            case PassiveAbilityType.Attack5: stats.AddPermanentStat(StatType.Attack, -5); break;
            case PassiveAbilityType.Defense3: stats.AddPermanentStat(StatType.Defense, -3); break;
            case PassiveAbilityType.PermanentAttackCoin: stats.AddPermanentAttackRouletteCoins(-1); break;
            case PassiveAbilityType.PermanentDefenseCoin: stats.AddPermanentDefenseRouletteCoins(-1); break;
            case PassiveAbilityType.LowHpAttack3: stats.SetLowHpAttackBonus(0); break;
        }
    }

    private static AbilityGrade RollGrade(int floor)
    {
        floor = Mathf.Clamp(floor, 1, 10);
        int small;
        int moderate;
        if (floor <= 2) { small = 90; moderate = 10; }
        else if (floor <= 4) { small = 70; moderate = 20; }
        else if (floor <= 6) { small = 50; moderate = 30; }
        else if (floor <= 8) { small = 30; moderate = 40; }
        else { small = 10; moderate = 50; }
        int roll = UnityEngine.Random.Range(0, 100);
        return roll < small ? AbilityGrade.Small
            : roll < small + moderate ? AbilityGrade.Moderate : AbilityGrade.Good;
    }

    private void BuildCatalog()
    {
        if (catalog.Count > 0) return;
        Add(PassiveAbilityType.Attack2, AbilityGrade.Small, "작은 힘", "영구적으로 공격력 +2");
        Add(PassiveAbilityType.Defense1, AbilityGrade.Small, "작은 수호", "영구적으로 방어력 +1");
        Add(PassiveAbilityType.MaxHp5, AbilityGrade.Small, "작은 생명", "영구적으로 최대 체력 +5");
        Add(PassiveAbilityType.BattleEndHeal3, AbilityGrade.Small, "잔잔한 회복", "전투 종료 시 체력 3 회복");
        Add(PassiveAbilityType.BadColorBarrier2, AbilityGrade.Small, "불운 대비", "빨강·검정 칸이 나오면 방어 +2");
        Add(PassiveAbilityType.NoBarrierEndTurn5, AbilityGrade.Small, "빈틈 보완", "내 턴 종료 시 방어가 없으면 방어 +5");
        Add(PassiveAbilityType.NonGreenHeal2, AbilityGrade.Small, "색다른 치유", "초록색이 아니면 체력 2 회복");
        Add(PassiveAbilityType.BattleEndGold5, AbilityGrade.Small, "잔돈 수집", "전투 종료 시 골드 +5");
        Add(PassiveAbilityType.CheapInn, AbilityGrade.Small, "여관 단골", "휴식 0골드, 만찬 5골드");
        Add(PassiveAbilityType.ChanceGrantsAbility, AbilityGrade.Small, "우연의 선물", "우연 발생 후 능력 선택지에서 1개 추가 획득");
        Add(PassiveAbilityType.Attack3, AbilityGrade.Moderate, "단단한 힘", "영구적으로 공격력 +3");
        Add(PassiveAbilityType.Defense2, AbilityGrade.Moderate, "단단한 수호", "영구적으로 방어력 +2");
        Add(PassiveAbilityType.MaxHp10, AbilityGrade.Moderate, "튼튼한 생명", "영구적으로 최대 체력 +10");
        Add(PassiveAbilityType.BattleEndHeal5, AbilityGrade.Moderate, "전투 호흡", "전투 종료 시 체력 5 회복");
        Add(PassiveAbilityType.Retaliate3, AbilityGrade.Moderate, "가시 갑옷", "피해를 받으면 적에게 피해 3");
        Add(PassiveAbilityType.FirstAttackFailureAttack5, AbilityGrade.Moderate, "실패의 분노", "첫 공격 실패 시 현재 전투 공격력 +5");
        Add(PassiveAbilityType.ShopDiscount20, AbilityGrade.Moderate, "흥정", "상점 가격 20% 할인");
        Add(PassiveAbilityType.SpinCountBarrier, AbilityGrade.Moderate, "회전 방벽", "내 턴 룰렛 횟수 ×2 방어 획득");
        Add(PassiveAbilityType.GreenToBlue, AbilityGrade.Moderate, "푸른 개조", "초록색 1개를 파란색으로 변경");
        Add(PassiveAbilityType.AttackBarrierMultiplier, AbilityGrade.Moderate, "방벽 파쇄", "적 방어 보유 시 공격 피해 1.5배");
        Add(PassiveAbilityType.MaxHp20, AbilityGrade.Good, "거대한 생명", "영구적으로 최대 체력 +20");
        Add(PassiveAbilityType.Attack5, AbilityGrade.Good, "거대한 힘", "영구적으로 공격력 +5");
        Add(PassiveAbilityType.Defense3, AbilityGrade.Good, "거대한 수호", "영구적으로 방어력 +3");
        Add(PassiveAbilityType.PermanentAttackCoin, AbilityGrade.Good, "공격의 운명", "영구적으로 공격 코인 +1");
        Add(PassiveAbilityType.PermanentDefenseCoin, AbilityGrade.Good, "방어의 운명", "영구적으로 방어 코인 +1");
        Add(PassiveAbilityType.GreenToGold, AbilityGrade.Good, "황금 개조", "초록색 1개를 황금색으로 변경");
        Add(PassiveAbilityType.BattleEndHeal10, AbilityGrade.Good, "완전한 회복", "전투 종료 시 체력 10 회복");
        Add(PassiveAbilityType.LowHpAttack3, AbilityGrade.Good, "위기 본능", "체력이 절반 이하일 때 공격력 +3");
        Add(PassiveAbilityType.MonsterEndBarrierHeal5, AbilityGrade.Good, "수호의 치유", "상대 턴 종료 시 방어가 남으면 체력 +5");
        Add(PassiveAbilityType.BadColorPermanentAttack1, AbilityGrade.Good, "불운의 성장", "빨강·검정 칸이 나오면 영구 공격력 +1");
    }

    private void Add(PassiveAbilityType type, AbilityGrade grade, string name, string description)
    {
        catalog.Add(new AbilityDefinition(type, grade, name, description));
    }
}
