using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RouletteActionType
{
    Attack,
    Defense
}

public class BattleManager : MonoBehaviour
{
    private readonly struct PendingRouletteResult
    {
        public RouletteActionType ActionType { get; }
        public RouletteSpinResult Result { get; }

        public PendingRouletteResult(RouletteActionType actionType, RouletteSpinResult result)
        {
            ActionType = actionType;
            Result = result;
        }
    }

    [Tooltip("전투에 참여하는 플레이어의 능력치 관리자입니다. 전투 씬에서 자동 연결할 수 있습니다.")]
    [SerializeField] private PlayerStatManager playerStats;
    [Tooltip("플레이어 턴의 공격·방어 룰렛을 실행할 컴포넌트입니다.")]
    [SerializeField] private RouletteController roulette;
    [Tooltip("현재 몬스터의 능력치와 행동을 처리할 컴포넌트입니다.")]
    [SerializeField] private MonsterController monster;
    [Tooltip("몬스터 턴이 시작된 뒤 예고된 행동을 실행하기까지 기다리는 시간입니다.")]
    [Min(0f)] [SerializeField] private float monsterActionDisplaySeconds = 3f;

    private bool isResolvingRoulette;
    private readonly List<PendingRouletteResult> pendingRouletteResults = new List<PendingRouletteResult>();
    private int skippedMonsterTurns;
    private int battleGoldMultiplier = 1;
    private PlayerAbilityManager abilities;
    private int rouletteSpinsThisTurn;
    private bool firstAttackRouletteResolved;

    public BattleTurn CurrentTurn { get; private set; } = BattleTurn.None;
    public bool IsBattleActive => CurrentTurn != BattleTurn.None;
    public PlayerStatManager PlayerStats => playerStats;
    public MonsterController Monster => monster;
    public bool SuppressPostBattleRewards { get; private set; }
    public bool CanSpinAttackRoulette => CurrentTurn == BattleTurn.Player
        && !isResolvingRoulette
        && playerStats != null
        && playerStats.AttackRouletteCoins > 0;
    public bool CanSpinDefenseRoulette => CurrentTurn == BattleTurn.Player
        && !isResolvingRoulette
        && playerStats != null
        && playerStats.AttackRouletteCoins <= 0
        && playerStats.DefenseRouletteCoins > 0;

    public event Action<BattleTurn> TurnChanged;
    public event Action<RouletteActionType, RouletteSpinResult> PlayerRouletteResolved;
    public event Action<RouletteActionType, RouletteSpinResult> SpecialRouletteTriggered;
    public event Action<MonsterActionType> MonsterTurnResolved;
    public event Action<bool> BattleFinished;

    public void SetPlayerStats(PlayerStatManager stats)
    {
        playerStats = stats;
    }

    public void BeginBattle(MonsterData monsterData)
    {
        if (playerStats == null || roulette == null || monster == null || monsterData == null)
        {
            Debug.LogError("BattleManager references are not assigned.", this);
            return;
        }

        monster.Initialize(monsterData);
        abilities = GameSessionManager.Instance == null ? null : GameSessionManager.Instance.PlayerAbilities;
        playerStats.DamageTaken -= HandlePlayerDamageTaken;
        playerStats.DamageTaken += HandlePlayerDamageTaken;
        playerStats.ResetBattleState();
        skippedMonsterTurns = 0;
        battleGoldMultiplier = 1;
        firstAttackRouletteResolved = false;
        SuppressPostBattleRewards = false;
        BeginPlayerTurn();
    }

    public bool SpinAttackRoulette()
    {
        return TrySpinRoulette(RouletteActionType.Attack);
    }

    public void OnAttackRouletteButton()
    {
        OnRouletteButton();
    }

    public bool SpinDefenseRoulette()
    {
        return TrySpinRoulette(RouletteActionType.Defense);
    }

    public void OnDefenseRouletteButton()
    {
        OnRouletteButton();
    }

    public void OnRouletteButton()
    {
        if (playerStats == null)
        {
            return;
        }

        if (playerStats.AttackRouletteCoins > 0)
        {
            SpinAttackRoulette();
        }
        else if (playerStats.DefenseRouletteCoins > 0)
        {
            SpinDefenseRoulette();
        }
    }

    public bool EndPlayerTurn()
    {
        if (CurrentTurn != BattleTurn.Player
            || isResolvingRoulette
            || playerStats.AttackRouletteCoins > 0
            || playerStats.DefenseRouletteCoins > 0)
        {
            return false;
        }

        ResolveCompletedPlayerTurn();
        return true;
    }

    private bool TrySpinRoulette(RouletteActionType actionType)
    {
        if (CurrentTurn != BattleTurn.Player || isResolvingRoulette || roulette.IsSpinning)
        {
            return false;
        }

        // 플레이어 턴은 공격 코인을 모두 사용한 뒤 방어 코인을 사용하는 순서로 진행합니다.
        if (actionType == RouletteActionType.Defense && playerStats.AttackRouletteCoins > 0)
        {
            return false;
        }

        bool spent = actionType == RouletteActionType.Attack
            ? playerStats.TrySpendAttackRouletteCoin()
            : playerStats.TrySpendDefenseRouletteCoin();

        if (!spent)
        {
            return false;
        }

        isResolvingRoulette = true;
        bool started = roulette.SpinAnimatedWithoutApplying(
            result => ResolvePlayerRoulette(actionType, result));

        if (!started)
        {
            RefundRouletteCoin(actionType);
            isResolvingRoulette = false;
        }

        return started;
    }

    private void ResolvePlayerRoulette(RouletteActionType actionType, RouletteSpinResult result)
    {
        isResolvingRoulette = false;
        pendingRouletteResults.Add(new PendingRouletteResult(actionType, result));
        rouletteSpinsThisTurn++;

        if (abilities != null && result.EffectData != null)
        {
            RouletteEffectType effect = result.EffectData.Effect;
            bool badColor = effect == RouletteEffectType.Failure || effect == RouletteEffectType.SelfDebuff;
            if (effect != RouletteEffectType.Success && abilities.Has(PassiveAbilityType.NonGreenHeal2)) playerStats.Heal(2);
            if (badColor && abilities.Has(PassiveAbilityType.BadColorBarrier2)) playerStats.AddBarrier(2);
            if (badColor && abilities.Has(PassiveAbilityType.BadColorPermanentAttack1)) playerStats.AddPermanentStat(StatType.Attack, 1);
            if (actionType == RouletteActionType.Attack && !firstAttackRouletteResolved)
            {
                firstAttackRouletteResolved = true;
                if (effect == RouletteEffectType.Failure && abilities.Has(PassiveAbilityType.FirstAttackFailureAttack5))
                    playerStats.AddBattleStatModifier(5, 0);
            }
        }

        if (result.EffectData != null)
        {
            monster.HandlePlayerRouletteResult(actionType, result.EffectData.Effect, playerStats);
            if (playerStats.IsDead)
            {
                FinishBattle(false);
                return;
            }
        }

        // 추가 회전은 턴 종료 조건에 영향을 주므로 코인 반환만 즉시 처리합니다.
        if (result.EffectData != null && result.EffectData.Effect == RouletteEffectType.ExtraSpin)
        {
            RefundRouletteCoin(actionType);
        }

        PlayerRouletteResolved?.Invoke(actionType, result);

        if (playerStats.AttackRouletteCoins <= 0 && playerStats.DefenseRouletteCoins <= 0)
        {
            ResolveCompletedPlayerTurn();
        }
    }

    private void ResolveCompletedPlayerTurn()
    {
        foreach (PendingRouletteResult pending in pendingRouletteResults)
        {
            ApplyBattleRouletteEffect(pending.ActionType, pending.Result, false);
        }

        pendingRouletteResults.Clear();

        if (abilities != null)
        {
            if (playerStats.Barrier <= 0 && abilities.Has(PassiveAbilityType.NoBarrierEndTurn5)) playerStats.AddBarrier(5);
            if (abilities.Has(PassiveAbilityType.SpinCountBarrier)) playerStats.AddBarrier(rouletteSpinsThisTurn * 2);
        }

        if (monster.IsDead)
        {
            CompleteVictory();
            return;
        }

        StartCoroutine(ExecuteMonsterTurnRoutine());
    }

    private void BeginPlayerTurn()
    {
        pendingRouletteResults.Clear();
        rouletteSpinsThisTurn = 0;
        playerStats.ResetBattleRouletteCoins();
        CurrentTurn = BattleTurn.Player;
        TurnChanged?.Invoke(CurrentTurn);
    }

    private IEnumerator ExecuteMonsterTurnRoutine()
    {
        CurrentTurn = BattleTurn.Monster;
        TurnChanged?.Invoke(CurrentTurn);

        // 다음 행동 이미지를 보여준 상태로 기다린 뒤 실제 행동을 적용합니다.
        if (monsterActionDisplaySeconds > 0f)
        {
            yield return new WaitForSeconds(monsterActionDisplaySeconds);
        }

        if (skippedMonsterTurns > 0)
        {
            skippedMonsterTurns--;
            ApplyMonsterTurnEndAbilities();
            playerStats.AdvanceBuffTurn();
            BeginPlayerTurn();
            yield break;
        }

        MonsterActionType action = monster.ExecuteTurn(playerStats, this);
        MonsterTurnResolved?.Invoke(action);

        if (playerStats.IsDead)
        {
            FinishBattle(false);
            yield break;
        }

        if (monster.IsDead)
        {
            CompleteVictory();
            yield break;
        }

        ApplyMonsterTurnEndAbilities();
        playerStats.AdvanceBuffTurn();
        BeginPlayerTurn();
    }

    private void ApplyMonsterTurnEndAbilities()
    {
        if (abilities != null && playerStats.Barrier > 0
            && abilities.Has(PassiveAbilityType.MonsterEndBarrierHeal5))
        {
            playerStats.Heal(5);
        }
    }

    private void ApplyBattleRouletteEffect(
        RouletteActionType actionType,
        RouletteSpinResult result,
        bool refundExtraSpin = true)
    {
        if (result.EffectData == null)
        {
            return;
        }

        int value = Mathf.Abs(result.Value);
        switch (result.EffectData.Effect)
        {
            case RouletteEffectType.Success:
                ApplySuccess(actionType, 1f);
                break;
            case RouletteEffectType.GreatSuccess:
                ApplySuccess(actionType, 1.5f);
                break;
            case RouletteEffectType.ExtraSpin:
                ApplySuccess(actionType, 1f);
                if (refundExtraSpin)
                {
                    RefundRouletteCoin(actionType);
                }
                break;
            case RouletteEffectType.SelfBuff:
                playerStats.AddBattleStatModifier(1, 1);
                break;
            case RouletteEffectType.Heal:
                playerStats.Heal(10);
                break;
            case RouletteEffectType.Failure:
                break;
            case RouletteEffectType.SelfDebuff:
                playerStats.AddBattleStatModifier(-1, -1);
                break;
            case RouletteEffectType.Special:
                SpecialRouletteTriggered?.Invoke(actionType, result);
                break;
        }
    }

    private void ApplySuccess(RouletteActionType actionType, float multiplier)
    {
        if (actionType == RouletteActionType.Attack)
        {
            if (abilities != null && monster.Barrier > 0 && abilities.Has(PassiveAbilityType.AttackBarrierMultiplier)) multiplier *= 1.5f;
            monster.TakeDamage(Mathf.CeilToInt(playerStats.Attack * multiplier));
        }
        else
        {
            playerStats.AddBarrier(Mathf.CeilToInt(playerStats.Defense * multiplier));
        }
    }

    public void DamageMonsterWithItem(int damage)
    {
        if (IsBattleActive) monster.TakeDirectDamage(damage);
    }

    public void DelayMonsterOneTurn()
    {
        if (IsBattleActive) skippedMonsterTurns++;
    }

    public void DoubleBattleGold()
    {
        if (IsBattleActive) battleGoldMultiplier = 2;
    }

    public bool UseFateCoin()
    {
        return CurrentTurn == BattleTurn.Player && !roulette.IsSpinning && roulette.ConfigureFateCoinSpin();
    }

    private void RefundRouletteCoin(RouletteActionType actionType)
    {
        if (actionType == RouletteActionType.Attack)
        {
            playerStats.AddAttackRouletteCoins(1);
        }
        else
        {
            playerStats.AddDefenseRouletteCoins(1);
        }
    }

    private void FinishBattle(bool playerWon)
    {
        if (playerWon && !SuppressPostBattleRewards && abilities != null)
        {
            if (abilities.Has(PassiveAbilityType.BattleEndHeal3)) playerStats.Heal(3);
            if (abilities.Has(PassiveAbilityType.BattleEndHeal5)) playerStats.Heal(5);
            if (abilities.Has(PassiveAbilityType.BattleEndHeal10)) playerStats.Heal(10);
            if (abilities.Has(PassiveAbilityType.BattleEndGold5)) playerStats.AddCoins(5);
        }
        pendingRouletteResults.Clear();
        CurrentTurn = BattleTurn.None;
        TurnChanged?.Invoke(CurrentTurn);
        BattleFinished?.Invoke(playerWon);
    }

    private void CompleteVictory()
    {
        SuppressPostBattleRewards = monster != null && monster.SuppressVictoryRewards;
        if (!SuppressPostBattleRewards)
            playerStats.AddCoins(monster.Data.Coin * battleGoldMultiplier);
        FinishBattle(true);
    }

    private void HandlePlayerDamageTaken(int damage)
    {
        if (damage > 0 && abilities != null && abilities.Has(PassiveAbilityType.Retaliate3) && monster != null && !monster.IsDead)
            monster.TakeDirectDamage(3);
    }

    private void OnDestroy()
    {
        if (playerStats != null) playerStats.DamageTaken -= HandlePlayerDamageTaken;
    }
}
