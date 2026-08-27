using System;
using UnityEngine;

public enum MonsterActionType
{
    Attack = 0,
    Defense = 1,
    Skill1 = 2,
    Skill2 = 3,
    // 기존 MonsterData 에셋의 직렬화 값 4를 반드시 유지해야 합니다.
    None = 4,
    Skill3 = 5,
    Skill4 = 6,
    Skill5 = 7
}

public class MonsterController : MonoBehaviour
{
    private MonsterData data;
    private BossData bossData;
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
    private bool isBossPhase2;

    public MonsterData Data => data;
    public BossData BossData => bossData;
    public bool HasCombatData => data != null || bossData != null;
    public bool IsBoss => bossData != null;
    public int CurrentHp => currentHp;
    public int MaxHp => data != null ? data.Hp : bossData == null ? 0 : bossData.Hp;
    public int Attack => BaseAttack + attackBonus;
    public int Defense => BaseDefense + defenseBonus;
    public int RewardCoin => data == null ? 0 : data.Coin;
    public Sprite DisplayImage => data != null ? data.MonsterImage : bossData == null ? null
        : isBossPhase2 && bossData.Phase2BossImage != null ? bossData.Phase2BossImage : bossData.BossImage;
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
        bossData = null;
        InitializeCombatState(data == null ? 0 : data.Hp);
    }

    public void Initialize(BossData selectedBossData)
    {
        data = null;
        bossData = selectedBossData;
        InitializeCombatState(bossData == null ? 0 : bossData.Hp);
    }

    private void InitializeCombatState(int hp)
    {
        currentHp = hp;
        attackBonus = 0;
        defenseBonus = 0;
        barrier = 0;
        isBossPhase2 = false;
        hasNextAction = HasCombatData;
        hasPreviousAction = false;
        punishPlayerFailure = false;
        suppressVictoryRewards = false;
        receivedDamageMultiplier = 1f;
        nextPatternIndex = 0;
        if (hasNextAction) nextAction = CreateNextAction();
        HpChanged?.Invoke(currentHp);
        NextActionChanged?.Invoke();
    }

    private int BaseAttack => data != null ? data.Atk : bossData == null ? 0
        : isBossPhase2 ? bossData.Phase2Atk : bossData.Atk;
    private int BaseDefense => data != null ? data.Def : bossData == null ? 0
        : isBossPhase2 ? bossData.Phase2Def : bossData.Def;

    public int TakeDamage(int rawDamage)
    {
        // 방어력은 방어 행동으로 획득하는 베리어의 기준일 뿐 피해를 직접 줄이지 않습니다.
        int damage = Mathf.CeilToInt(Mathf.Max(0, rawDamage) * receivedDamageMultiplier);
        int absorbed = Mathf.Min(barrier, damage);
        barrier -= absorbed;
        damage -= absorbed;
        currentHp = Mathf.Max(0, currentHp - damage);
        UpdateBossPhase();
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

        hasNextAction = HasCombatData;
        if (hasNextAction) nextAction = CreateNextAction();
        ActionExecuted?.Invoke(selected);
        NextActionChanged?.Invoke();
        return selected;
    }

    public int TakeDirectDamage(int damage)
    {
        int applied = Mathf.CeilToInt(Mathf.Max(0, damage) * receivedDamageMultiplier);
        currentHp = Mathf.Max(0, currentHp - applied);
        UpdateBossPhase();
        HpChanged?.Invoke(currentHp);
        return applied;
    }

    private MonsterActionType CreateNextAction()
    {
        if (!HasCombatData) return MonsterActionType.Attack;
        int patternCount = data != null ? data.ActionPatternCount
            : isBossPhase2 ? bossData.Phase2PatternCount : bossData.Phase1PatternCount;
        MonsterActionType selected = data != null ? data.GetActionPattern(nextPatternIndex)
            : ConvertBossAction(isBossPhase2 ? bossData.GetPhase2Action(nextPatternIndex)
                : bossData.GetPhase1Action(nextPatternIndex));
        nextPatternIndex = (nextPatternIndex + 1) % Mathf.Max(1, patternCount);
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
        if (data != null)
            return action == MonsterActionType.Skill1 ? data.Skill1
                : action == MonsterActionType.Skill2 ? data.Skill2 : null;
        if (bossData == null) return null;
        switch (action)
        {
            case MonsterActionType.Skill1: return bossData.Skill1;
            case MonsterActionType.Skill2: return bossData.Skill2;
            case MonsterActionType.Skill3: return bossData.Skill3;
            case MonsterActionType.Skill4: return bossData.Skill4;
            case MonsterActionType.Skill5: return bossData.Skill5;
            default: return null;
        }
    }

    private void ExecuteAction(MonsterActionType action, SkillInfo skill, PlayerStatManager playerStats,
        BattleManager battleManager)
    {
        switch (action)
        {
            case MonsterActionType.Attack:
                battleManager?.NotifyMonsterAttack();
                if (!playerStats.TryConsumeDodge()) playerStats.TakeDamage(Attack);
                break;
            case MonsterActionType.Defense:
                AddBarrier(Defense);
                break;
            case MonsterActionType.Skill1:
            case MonsterActionType.Skill2:
            case MonsterActionType.Skill3:
            case MonsterActionType.Skill4:
            case MonsterActionType.Skill5:
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
                currentHp = Mathf.Min(MaxHp, currentHp + Mathf.Max(0, skill.EffectValue));
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
                defenseBonus = Mathf.Max(-BaseDefense, defenseBonus - 1);
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
            case SkillEffectType.AttackByBarrier:
                battleManager?.NotifyMonsterAttack();
                if (!playerStats.TryConsumeDodge()) playerStats.TakeDamage(barrier);
                break;
            case SkillEffectType.RestAndHeal5:
                currentHp = Mathf.Min(MaxHp, currentHp + 5);
                HpChanged?.Invoke(currentHp);
                break;
            case SkillEffectType.StealPlayerBarrier:
                AddBarrier(playerStats.TakeAllBarrier());
                break;
            case SkillEffectType.Barrier3AndReducePlayerAttack1:
                AddBarrier(3);
                playerStats.AddBattleStatModifier(-1, 0);
                break;
            case SkillEffectType.GreenToRedOrTake30Damage:
                if (battleManager == null || !battleManager.ConvertOneGreenToRedByMonster())
                    TakeDirectDamage(30);
                break;
        }
    }

    private void AddBarrier(int amount)
    {
        barrier += Mathf.Max(0, amount);
        HpChanged?.Invoke(currentHp);
    }

    public Sprite GetActionImage(MonsterActionType action)
    {
        if (data != null)
        {
            switch (action)
            {
                case MonsterActionType.Attack: return data.AtkImage;
                case MonsterActionType.Defense: return data.DefImage;
                case MonsterActionType.Skill1: return data.Skill1Image;
                case MonsterActionType.Skill2: return data.Skill2Image;
                default: return null;
            }
        }
        if (bossData == null) return null;
        switch (action)
        {
            case MonsterActionType.Attack: return bossData.AtkImage;
            case MonsterActionType.Defense: return bossData.DefImage;
            case MonsterActionType.Skill1: return bossData.Skill1Image;
            case MonsterActionType.Skill2: return bossData.Skill2Image;
            case MonsterActionType.Skill3: return bossData.Skill3Image;
            case MonsterActionType.Skill4: return bossData.Skill4Image;
            case MonsterActionType.Skill5: return bossData.Skill5Image;
            default: return null;
        }
    }

    private void UpdateBossPhase()
    {
        if (bossData == null || isBossPhase2 || currentHp > bossData.Phase2StartHp) return;
        isBossPhase2 = true;
        nextPatternIndex = 0;
        hasNextAction = true;
        nextAction = CreateNextAction();
        NextActionChanged?.Invoke();
    }

    private static MonsterActionType ConvertBossAction(BossActionType action)
    {
        switch (action)
        {
            case BossActionType.Attack: return MonsterActionType.Attack;
            case BossActionType.Defense: return MonsterActionType.Defense;
            case BossActionType.Skill1: return MonsterActionType.Skill1;
            case BossActionType.Skill2: return MonsterActionType.Skill2;
            case BossActionType.Skill3: return MonsterActionType.Skill3;
            case BossActionType.Skill4: return MonsterActionType.Skill4;
            case BossActionType.Skill5: return MonsterActionType.Skill5;
            default: return MonsterActionType.None;
        }
    }
}
