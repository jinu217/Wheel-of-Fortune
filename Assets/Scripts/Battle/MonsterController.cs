using System;
using System.Collections.Generic;
using UnityEngine;

public enum MonsterActionType
{
    Attack,
    Defense,
    Skill1,
    Skill2
}

public class MonsterController : MonoBehaviour
{
    private MonsterData data;
    private int currentHp;
    private int attackBonus;
    private int defenseBonus;
    private int barrier;
    private int skill1Cooldown;
    private int skill2Cooldown;
    private MonsterActionType nextAction;
    private bool hasNextAction;

    public MonsterData Data => data;
    public int CurrentHp => currentHp;
    public int Attack => data == null ? 0 : data.Atk + attackBonus;
    public int Defense => data == null ? 0 : data.Def + defenseBonus;
    public int Barrier => barrier;
    public MonsterActionType NextAction => nextAction;
    public bool HasNextAction => hasNextAction;
    public bool IsDead => currentHp <= 0;

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
        skill1Cooldown = 0;
        skill2Cooldown = 0;
        hasNextAction = data != null;
        if (hasNextAction) nextAction = CreateNextAction();
        HpChanged?.Invoke(currentHp);
        NextActionChanged?.Invoke();
    }

    public int TakeDamage(int rawDamage)
    {
        int damage = Mathf.Max(0, rawDamage - Defense);
        int absorbed = Mathf.Min(barrier, damage);
        barrier -= absorbed;
        damage -= absorbed;
        currentHp = Mathf.Max(0, currentHp - damage);
        HpChanged?.Invoke(currentHp);
        return damage;
    }

    public MonsterActionType ExecuteTurn(PlayerStatManager playerStats)
    {
        MonsterActionType selected = hasNextAction ? nextAction : CreateNextAction();
        skill1Cooldown = Mathf.Max(0, skill1Cooldown - 1);
        skill2Cooldown = Mathf.Max(0, skill2Cooldown - 1);

        switch (selected)
        {
            case MonsterActionType.Attack:
                playerStats.TakeDamage(Attack);
                break;
            case MonsterActionType.Defense:
                barrier += Mathf.Max(0, Defense);
                HpChanged?.Invoke(currentHp);
                break;
            case MonsterActionType.Skill1:
                ExecuteSkill(data.Skill1, playerStats);
                skill1Cooldown = data.Skill1Turn;
                break;
            case MonsterActionType.Skill2:
                ExecuteSkill(data.Skill2, playerStats);
                skill2Cooldown = data.Skill2Turn;
                break;
        }

        hasNextAction = data != null;
        if (hasNextAction) nextAction = CreateNextAction();
        ActionExecuted?.Invoke(selected);
        NextActionChanged?.Invoke();
        return selected;
    }

    private MonsterActionType CreateNextAction()
    {
        List<MonsterActionType> availableActions = new List<MonsterActionType>
        {
            MonsterActionType.Attack,
            MonsterActionType.Defense
        };

        if (data.Skill1 != null && skill1Cooldown <= 0)
        {
            availableActions.Add(MonsterActionType.Skill1);
        }

        if (data.Skill2 != null && skill2Cooldown <= 0)
        {
            availableActions.Add(MonsterActionType.Skill2);
        }

        return availableActions[UnityEngine.Random.Range(0, availableActions.Count)];
    }

    private void ExecuteSkill(SkillInfo skill, PlayerStatManager playerStats)
    {
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
        }
    }
}
