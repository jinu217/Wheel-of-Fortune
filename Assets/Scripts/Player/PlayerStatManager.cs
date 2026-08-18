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

    public int MaxHp => maxHp + GetModifierTotal(StatType.MaxHp);
    public int CurrentHp => currentHp;
    public int Attack => attack + GetModifierTotal(StatType.Attack);
    public int Defense => defense + GetModifierTotal(StatType.Defense);
    public int Coin => coin;
    public int AttackRouletteCoins => attackRouletteCoins;
    public int DefenseRouletteCoins => defenseRouletteCoins;
    public int Barrier => barrier;
    public bool IsDead => currentHp <= 0;

    public event Action StatsChanged;
    public event Action PlayerDied;

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
        timedModifiers.Clear();
        StatsChanged?.Invoke();
    }

    public int TakeDamage(int rawDamage)
    {
        int damage = Mathf.Max(0, rawDamage - Defense);
        int absorbed = Mathf.Min(barrier, damage);
        barrier -= absorbed;
        damage -= absorbed;
        currentHp = Mathf.Max(0, currentHp - damage);
        StatsChanged?.Invoke();

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

    public void AddPermanentStat(StatType statType, int amount)
    {
        switch (statType)
        {
            case StatType.MaxHp:
                maxHp = Mathf.Max(1, maxHp + amount);
                currentHp = Mathf.Min(currentHp, MaxHp);
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
        attackRouletteCoins = 1;
        defenseRouletteCoins = 1;
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
