using System;
using UnityEngine;

public enum MonsterActionType
{
    Attack,
    Defense,
    Skill1,
    Skill2,
    None
}

public class MonsterController : MonoBehaviour
{
    private MonsterData data;
    private int currentHp;
    private int attackBonus;
    private int defenseBonus;
    private int barrier;
    private MonsterActionType nextAction;
    private bool hasNextAction;
    private MonsterActionType previousAction;
    private bool hasPreviousAction;
    private bool punishPlayerFailure;
    private bool suppressVictoryRewards;
    private float receivedDamageMultiplier = 1f;
    private int nextPatternIndex;

    public MonsterData Data => data;
    public int CurrentHp => currentHp;
    public int Attack => data == null ? 0 : data.Atk + attackBonus;
    public int Defense => data == null ? 0 : data.Def + defenseBonus;
    public int Barrier => barrier;
    public MonsterActionType NextAction => nextAction;
    public bool HasNextAction => hasNextAction;
    public bool IsDead => currentHp <= 0;
    public bool SuppressVictoryRewards => suppressVictoryRewards;

    public event Action<int> HpChanged;
    public event Action<MonsterActionType> ActionExecuted;
    public event Action NextActionChanged;

    public void Initialize(MonsterData monsterData)
    {
        data = monsterData;
        currentHp = data == null ? 0 : data.Hp;
        attackBonus = 0;
        defenseBonus = 0;
        barrier = 0;
        hasNextAction = data != null;
        hasPreviousAction = false;
        punishPlayerFailure = false;
        suppressVictoryRewards = false;
        receivedDamageMultiplier = 1f;
        nextPatternIndex = 0;
        if (hasNextAction) nextAction = CreateNextAction();
        HpChanged?.Invoke(currentHp);
        NextActionChanged?.Invoke();
    }

    public int TakeDamage(int rawDamage)
    {
        int damage = Mathf.CeilToInt(Mathf.Max(0, rawDamage - Defense) * receivedDamageMultiplier);
        int absorbed = Mathf.Min(barrier, damage);
        barrier -= absorbed;
        damage -= absorbed;
        currentHp = Mathf.Max(0, currentHp - damage);
        HpChanged?.Invoke(currentHp);
        return damage;
    }

    public MonsterActionType ExecuteTurn(PlayerStatManager playerStats, BattleManager battleManager)
    {
        MonsterActionType selected = hasNextAction ? nextAction : CreateNextAction();
        SkillInfo selectedSkill = GetSkill(selected);
        ExecuteAction(selected, selectedSkill, playerStats, battleManager);
        if (selectedSkill == null || selectedSkill.EffectType != SkillEffectType.RepeatPreviousActionTwice)
        {
            previousAction = selected;
            hasPreviousAction = true;
        }

        hasNextAction = data != null;
        if (hasNextAction) nextAction = CreateNextAction();
        ActionExecuted?.Invoke(selected);
        NextActionChanged?.Invoke();
        return selected;
    }

    public int TakeDirectDamage(int damage)
    {
        int applied = Mathf.CeilToInt(Mathf.Max(0, damage) * receivedDamageMultiplier);
        currentHp = Mathf.Max(0, currentHp - applied);
        HpChanged?.Invoke(currentHp);
        return applied;
    }

    private MonsterActionType CreateNextAction()
    {
        if (data == null) return MonsterActionType.Attack;
        MonsterActionType selected = data.GetActionPattern(nextPatternIndex);
        nextPatternIndex = (nextPatternIndex + 1) % data.ActionPatternCount;
        return selected;
    }

    public void HandlePlayerRouletteResult(RouletteActionType actionType, RouletteEffectType effect,
        PlayerStatManager playerStats)
    {
        if (!punishPlayerFailure || effect != RouletteEffectType.Failure) return;
        if (actionType == RouletteActionType.Attack) playerStats.TakeDamage(10);
        else
        {
            barrier += 10;
            HpChanged?.Invoke(currentHp);
        }
    }

    private SkillInfo GetSkill(MonsterActionType action)
    {
        return action == MonsterActionType.Skill1 ? data.Skill1
            : action == MonsterActionType.Skill2 ? data.Skill2 : null;
    }

    private void ExecuteAction(MonsterActionType action, SkillInfo skill, PlayerStatManager playerStats,
        BattleManager battleManager)
    {
        switch (action)
        {
            case MonsterActionType.Attack:
                if (!playerStats.TryConsumeDodge()) playerStats.TakeDamage(Attack);
                break;
            case MonsterActionType.Defense:
                AddBarrier(Defense);
                break;
            case MonsterActionType.Skill1:
            case MonsterActionType.Skill2:
                ExecuteSkill(skill, playerStats, battleManager);
                break;
        }
    }

    private void ExecuteSkill(SkillInfo skill, PlayerStatManager playerStats, BattleManager battleManager)
    {
        if (skill == null) return;
        switch (skill.EffectType)
        {
            case SkillEffectType.Damage:
                playerStats.TakeDamage(skill.EffectValue);
                break;
            case SkillEffectType.Heal:
                currentHp = Mathf.Min(data.Hp, currentHp + Mathf.Max(0, skill.EffectValue));
                HpChanged?.Invoke(currentHp);
                break;
            case SkillEffectType.AttackBuff:
                attackBonus += Mathf.Max(0, skill.EffectValue);
                break;
            case SkillEffectType.DefenseBuff:
                defenseBonus += Mathf.Max(0, skill.EffectValue);
                break;
            case SkillEffectType.ReducePlayerNextAttack:
                playerStats.AddTimedModifier(StatType.Attack, -1, 2);
                break;
            case SkillEffectType.VanishAndDeleteAbility:
                playerStats.TakeDamage(10);
                GameSessionManager.Instance?.ChanceSystem?.RemoveRandomAbilityIncludingChance();
                suppressVictoryRewards = true;
                currentHp = 0;
                HpChanged?.Invoke(currentHp);
                break;
            case SkillEffectType.MonsterAttackUp3:
                attackBonus += 3;
                break;
            case SkillEffectType.MonsterAttackUp2DefenseDown1:
                attackBonus += 2;
                defenseBonus = Mathf.Max(-data.Def, defenseBonus - 1);
                break;
            case SkillEffectType.HeavyAttackAndStealGold:
                playerStats.TakeDamage(10);
                playerStats.AddCoins(-Mathf.Min(5, playerStats.Coin));
                break;
            case SkillEffectType.PunishPlayerFailure:
                punishPlayerFailure = true;
                break;
            case SkillEffectType.RepeatPreviousActionTwice:
                if (hasPreviousAction)
                {
                    SkillInfo previousSkill = GetSkill(previousAction);
                    ExecuteAction(previousAction, previousSkill, playerStats, battleManager);
                    if (!playerStats.IsDead && !IsDead)
                        ExecuteAction(previousAction, previousSkill, playerStats, battleManager);
                }
                break;
            case SkillEffectType.Barrier5AndAttack5:
                AddBarrier(5);
                playerStats.TakeDamage(5);
                break;
            case SkillEffectType.RestOneTurn:
                break;
            case SkillEffectType.ChargeBarrier5:
                AddBarrier(5);
                break;
            case SkillEffectType.ChargeOverload:
                receivedDamageMultiplier = 2f;
                break;
            case SkillEffectType.RandomAttackDefenseUp:
                int amount = UnityEngine.Random.Range(1, 7);
                attackBonus += amount;
                defenseBonus += amount;
                break;
        }
    }

    private void AddBarrier(int amount)
    {
        barrier += Mathf.Max(0, amount);
        HpChanged?.Invoke(currentHp);
    }
}
