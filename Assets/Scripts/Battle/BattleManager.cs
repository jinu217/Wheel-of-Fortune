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
    [Tooltip("흰색 버프와 검은색 디버프가 유지되는 기본 턴 수입니다.")]
    [Min(1)] [SerializeField] private int buffDurationTurns = 3;
    [Tooltip("몬스터 행동 이미지를 화면에 보여주는 시간입니다.")]
    [Min(0f)] [SerializeField] private float monsterActionDisplaySeconds = 1f;

    private bool isResolvingRoulette;
    private readonly List<PendingRouletteResult> pendingRouletteResults = new List<PendingRouletteResult>();

    public BattleTurn CurrentTurn { get; private set; } = BattleTurn.None;
    public bool IsBattleActive => CurrentTurn != BattleTurn.None;
    public PlayerStatManager PlayerStats => playerStats;
    public MonsterController Monster => monster;
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

        if (monster.IsDead)
        {
            playerStats.AddCoins(monster.Data.Coin);
            FinishBattle(true);
            return;
        }

        StartCoroutine(ExecuteMonsterTurnRoutine());
    }

    private void BeginPlayerTurn()
    {
        pendingRouletteResults.Clear();
        playerStats.ResetBattleRouletteCoins();
        CurrentTurn = BattleTurn.Player;
        TurnChanged?.Invoke(CurrentTurn);
    }

    private IEnumerator ExecuteMonsterTurnRoutine()
    {
        CurrentTurn = BattleTurn.Monster;
        TurnChanged?.Invoke(CurrentTurn);

        MonsterActionType action = monster.ExecuteTurn(playerStats);
        MonsterTurnResolved?.Invoke(action);

        if (monsterActionDisplaySeconds > 0f)
        {
            yield return new WaitForSeconds(monsterActionDisplaySeconds);
        }

        if (playerStats.IsDead)
        {
            FinishBattle(false);
            yield break;
        }

        playerStats.AdvanceBuffTurn();
        BeginPlayerTurn();
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
                ApplySuccess(actionType, value, 1);
                break;
            case RouletteEffectType.GreatSuccess:
                ApplySuccess(actionType, value, 2);
                break;
            case RouletteEffectType.ExtraSpin:
                ApplySuccess(actionType, value, 1);
                if (refundExtraSpin)
                {
                    RefundRouletteCoin(actionType);
                }
                break;
            case RouletteEffectType.SelfBuff:
                playerStats.AddTimedModifier(
                    actionType == RouletteActionType.Attack ? StatType.Attack : StatType.Defense,
                    value,
                    buffDurationTurns);
                break;
            case RouletteEffectType.Heal:
                playerStats.Heal(value);
                break;
            case RouletteEffectType.Failure:
                break;
            case RouletteEffectType.SelfDebuff:
                playerStats.AddTimedModifier(
                    actionType == RouletteActionType.Attack ? StatType.Attack : StatType.Defense,
                    -value,
                    buffDurationTurns);
                break;
            case RouletteEffectType.Special:
                SpecialRouletteTriggered?.Invoke(actionType, result);
                break;
        }
    }

    private void ApplySuccess(RouletteActionType actionType, int value, int multiplier)
    {
        if (actionType == RouletteActionType.Attack)
        {
            monster.TakeDamage(playerStats.Attack * multiplier + value);
        }
        else
        {
            playerStats.AddBarrier(value * multiplier);
        }
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
        pendingRouletteResults.Clear();
        CurrentTurn = BattleTurn.None;
        TurnChanged?.Invoke(CurrentTurn);
        BattleFinished?.Invoke(playerWon);
    }
}
