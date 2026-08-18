using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    //플레이어 기본 능력치
    [Header("Player Stats")]
    [Tooltip("레거시 플레이어 체력입니다. 새 시스템에서는 PlayerStatManager를 사용하세요.")]
    public int health = 100;
    [Tooltip("레거시 플레이어 공격력입니다. 새 시스템에서는 PlayerStatManager를 사용하세요.")]
    public int attack = 20;
    [Tooltip("레거시 플레이어 방어력입니다. 새 시스템에서는 PlayerStatManager를 사용하세요.")]
    public int defense = 10;
}
