using System;
using UnityEngine;

public enum BattleTurn
{
    None,
    Player,
    Monster
}

public class GameManager : MonoBehaviour
{
    [Tooltip("현재 진행 중인 전투 턴입니다. 현재 전투 구조에서는 BattleManager가 직접 관리합니다.")]
    [SerializeField] private BattleTurn currentTurn = BattleTurn.None;

    public BattleTurn CurrentTurn => currentTurn;
    public bool IsBattleActive => currentTurn != BattleTurn.None;

    public event Action<BattleTurn> TurnChanged;
    public event Action BattleEnded;

    public void StartBattle()
    {
        SetTurn(BattleTurn.Player);
    }

    public bool EndPlayerTurn()
    {
        if (currentTurn != BattleTurn.Player)
        {
            return false;
        }

        SetTurn(BattleTurn.Monster);
        return true;
    }

    public bool EndMonsterTurn()
    {
        if (currentTurn != BattleTurn.Monster)
        {
            return false;
        }

        SetTurn(BattleTurn.Player);
        return true;
    }

    public void EndBattle()
    {
        SetTurn(BattleTurn.None);
        BattleEnded?.Invoke();
    }

    private void SetTurn(BattleTurn nextTurn)
    {
        currentTurn = nextTurn;
        TurnChanged?.Invoke(currentTurn);
    }
}
