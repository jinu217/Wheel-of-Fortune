using UnityEngine;

public class InnManager : MonoBehaviour
{
    [Tooltip("회복과 버프를 적용하고 골드를 차감할 플레이어입니다.")]
    [SerializeField] private PlayerStatManager playerStats;
    [Tooltip("충분한 휴식에 필요한 골드입니다.")] [Min(0)] [SerializeField] private int restPrice = 5;
    [Tooltip("최후의 만찬에 필요한 골드입니다.")] [Min(0)] [SerializeField] private int finalFeastPrice = 10;
    [Tooltip("최후의 만찬으로 증가하는 공격력입니다.")] [SerializeField] private int feastAttackBonus = 3;
    [Tooltip("최후의 만찬으로 증가하는 방어력입니다.")] [SerializeField] private int feastDefenseBonus = 2;
    [Tooltip("최후의 만찬 효과가 유지되는 전투 턴 수입니다.")] [Min(1)] [SerializeField] private int feastDurationTurns = 3;

    public int RestPrice => restPrice;
    public int FinalFeastPrice => finalFeastPrice;
    public int FeastAttackBonus => feastAttackBonus;
    public int FeastDefenseBonus => feastDefenseBonus;
    public int FeastDurationTurns => feastDurationTurns;

    public void SetPlayerStats(PlayerStatManager stats)
    {
        playerStats = stats;
    }

    public bool TryRest()
    {
        if (playerStats == null || !playerStats.TrySpendCoins(restPrice))
        {
            return false;
        }

        playerStats.Heal(Mathf.CeilToInt(playerStats.MaxHp * 0.5f));
        return true;
    }

    public bool TryFinalFeast()
    {
        if (playerStats == null || !playerStats.TrySpendCoins(finalFeastPrice))
        {
            return false;
        }

        playerStats.AddTimedModifier(StatType.Attack, feastAttackBonus, feastDurationTurns);
        playerStats.AddTimedModifier(StatType.Defense, feastDefenseBonus, feastDurationTurns);
        return true;
    }
}
