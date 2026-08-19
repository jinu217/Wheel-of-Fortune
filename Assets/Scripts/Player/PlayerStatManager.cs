using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatManager : MonoBehaviour
{
    [Serializable]
    private class TimedStatModifier
    {
        public StatType statType;
        public int value;
        public int remainingTurns;
    }

    [Header("Base Stats")]
    [Tooltip("플레이어의 기본 최대 체력입니다.")] [Min(1)] [SerializeField] private int maxHp = 100;
    [Tooltip("게임 시작 시 플레이어의 현재 체력입니다.")] [Min(0)] [SerializeField] private int currentHp = 100;
    [Tooltip("플레이어의 기본 공격력입니다.")] [Min(0)] [SerializeField] private int attack = 20;
    [Tooltip("플레이어의 기본 방어력입니다.")] [Min(0)] [SerializeField] private int defense = 10;
    [Tooltip("게임 시작 시 보유한 골드입니다.")] [Min(0)] [SerializeField] private int coin;
    [Tooltip("현재 보유한 공격 룰렛 코인입니다.")] [Min(0)] [SerializeField] private int attackRouletteCoins = 1;
    [Tooltip("현재 보유한 방어 룰렛 코인입니다.")] [Min(0)] [SerializeField] private int defenseRouletteCoins = 1;
    [Tooltip("HP보다 먼저 피해를 흡수하는 누적 배리어 수치입니다.")] [Min(0)] [SerializeField] private int barrier;

    private readonly List<TimedStatModifier> timedModifiers = new List<TimedStatModifier>();
    private int initialMaxHp;
    private int initialAttack;
    private int initialDefense;
    private int initialCoin;
    private int battleAttackModifier;
    private int battleDefenseModifier;
    private int pendingDodges;
    private int permanentAttackCoinBonus;
    private int permanentDefenseCoinBonus;
    private int lowHpAttackBonus;

    public int MaxHp => maxHp + GetModifierTotal(StatType.MaxHp);
    public int CurrentHp => currentHp;
    public int Attack => attack + battleAttackModifier + GetModifierTotal(StatType.Attack)
        + (currentHp * 2 <= MaxHp ? lowHpAttackBonus : 0);
    public int Defense => defense + battleDefenseModifier + GetModifierTotal(StatType.Defense);
    public int Coin => coin;
    public int AttackRouletteCoins => attackRouletteCoins;
    public int DefenseRouletteCoins => defenseRouletteCoins;
    public int Barrier => barrier;
    public bool IsDead => currentHp <= 0;

    public event Action StatsChanged;
    public event Action PlayerDied;
    public event Action<int> DamageTaken;

    private void Awake()
    {
        initialMaxHp = maxHp;
        initialAttack = attack;
        initialDefense = defense;
        initialCoin = coin;
    }

    public void ResetForNewRun()
    {
        maxHp = initialMaxHp;
        attack = initialAttack;
        defense = initialDefense;
        coin = initialCoin;
        currentHp = maxHp;
        attackRouletteCoins = 1;
        defenseRouletteCoins = 1;
        barrier = 0;
        battleAttackModifier = 0;
        battleDefenseModifier = 0;
        pendingDodges = 0;
        permanentAttackCoinBonus = 0;
        permanentDefenseCoinBonus = 0;
        lowHpAttackBonus = 0;
        timedModifiers.Clear();
        StatsChanged?.Invoke();
    }

    public int TakeDamage(int rawDamage)
    {
        // 방어력은 방어 룰렛 성공 시 생성되는 배리어의 기준 수치이며,
        // 피격 피해를 상시 감소시키지는 않습니다.
        int damage = Mathf.Max(0, rawDamage);
        int absorbed = Mathf.Min(barrier, damage);
        barrier -= absorbed;
        damage -= absorbed;
        currentHp = Mathf.Max(0, currentHp - damage);
        StatsChanged?.Invoke();
        if (damage > 0) DamageTaken?.Invoke(damage);

        if (IsDead)
        {
            PlayerDied?.Invoke();
        }

        return damage;
    }

    public void Heal(int amount)
    {
        currentHp = Mathf.Clamp(currentHp + Mathf.Max(0, amount), 0, MaxHp);
        StatsChanged?.Invoke();
    }

    public void RestoreFullHp()
    {
        currentHp = MaxHp;
        StatsChanged?.Invoke();
    }

    public void AddBarrier(int amount)
    {
        barrier = Mathf.Max(0, barrier + Mathf.Max(0, amount));
        StatsChanged?.Invoke();
    }

    public int TakeAllBarrier()
    {
        int taken = barrier;
        barrier = 0;
        StatsChanged?.Invoke();
        return taken;
    }

    public void ResetBattleState()
    {
        battleAttackModifier = 0;
        battleDefenseModifier = 0;
        pendingDodges = 0;
        barrier = 0;
        StatsChanged?.Invoke();
    }

    public void AddBattleStatModifier(int attackAmount, int defenseAmount)
    {
        battleAttackModifier += attackAmount;
        battleDefenseModifier += defenseAmount;
        StatsChanged?.Invoke();
    }

    public void AddDodge(int count = 1)
    {
        pendingDodges += Mathf.Max(0, count);
        StatsChanged?.Invoke();
    }

    public bool TryConsumeDodge()
    {
        if (pendingDodges <= 0) return false;
        pendingDodges--;
        StatsChanged?.Invoke();
        return true;
    }

    public void AddPermanentStat(StatType statType, int amount)
    {
        switch (statType)
        {
            case StatType.MaxHp:
                int previousMaxHp = MaxHp;
                maxHp = Mathf.Max(1, maxHp + amount);
                int increasedMaxHp = Mathf.Max(0, MaxHp - previousMaxHp);
                currentHp = Mathf.Clamp(currentHp + increasedMaxHp, 0, MaxHp);
                break;
            case StatType.Attack:
                attack = Mathf.Max(0, attack + amount);
                break;
            case StatType.Defense:
                defense = Mathf.Max(0, defense + amount);
                break;
        }

        StatsChanged?.Invoke();
    }

    public void AddTimedModifier(StatType statType, int amount, int turns)
    {
        if (amount == 0 || turns <= 0)
        {
            return;
        }

        timedModifiers.Add(new TimedStatModifier
        {
            statType = statType,
            value = amount,
            remainingTurns = turns
        });
        StatsChanged?.Invoke();
    }

    public void AdvanceBuffTurn()
    {
        for (int i = timedModifiers.Count - 1; i >= 0; i--)
        {
            timedModifiers[i].remainingTurns--;
            if (timedModifiers[i].remainingTurns <= 0)
            {
                timedModifiers.RemoveAt(i);
            }
        }

        currentHp = Mathf.Min(currentHp, MaxHp);
        StatsChanged?.Invoke();
    }

    public void AddCoins(int amount)
    {
        coin = Mathf.Max(0, coin + amount);
        StatsChanged?.Invoke();
    }

    public bool TrySpendCoins(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (coin < amount)
        {
            return false;
        }

        coin -= amount;
        StatsChanged?.Invoke();
        return true;
    }

    public void ResetBattleRouletteCoins()
    {
        attackRouletteCoins = Mathf.Max(0, 1 + permanentAttackCoinBonus);
        defenseRouletteCoins = Mathf.Max(0, 1 + permanentDefenseCoinBonus);
        StatsChanged?.Invoke();
    }

    public bool TrySpendAttackRouletteCoin()
    {
        if (attackRouletteCoins <= 0)
        {
            return false;
        }

        attackRouletteCoins--;
        StatsChanged?.Invoke();
        return true;
    }

    public bool TrySpendDefenseRouletteCoin()
    {
        if (defenseRouletteCoins <= 0)
        {
            return false;
        }

        defenseRouletteCoins--;
        StatsChanged?.Invoke();
        return true;
    }

    public void AddAttackRouletteCoins(int amount)
    {
        attackRouletteCoins = Mathf.Max(0, attackRouletteCoins + amount);
        StatsChanged?.Invoke();
    }

    public void AddDefenseRouletteCoins(int amount)
    {
        defenseRouletteCoins = Mathf.Max(0, defenseRouletteCoins + amount);
        StatsChanged?.Invoke();
    }

    public void AddPermanentAttackRouletteCoins(int amount)
    {
        permanentAttackCoinBonus += amount;
        StatsChanged?.Invoke();
    }

    public void AddPermanentDefenseRouletteCoins(int amount)
    {
        permanentDefenseCoinBonus += amount;
        StatsChanged?.Invoke();
    }

    public void SetLowHpAttackBonus(int amount)
    {
        lowHpAttackBonus = Mathf.Max(0, amount);
        StatsChanged?.Invoke();
    }

    private int GetModifierTotal(StatType statType)
    {
        int total = 0;
        foreach (TimedStatModifier modifier in timedModifiers)
        {
            if (modifier.statType == statType)
            {
                total += modifier.value;
            }
        }

        return total;
    }

    private void OnValidate()
    {
        maxHp = Mathf.Max(1, maxHp);
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
    }
}
