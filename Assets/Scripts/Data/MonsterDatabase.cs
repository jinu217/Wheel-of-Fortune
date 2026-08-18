using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MonsterDatabase", menuName = "Game Data/Monster Database")]
public class MonsterDatabase : ScriptableObject
{
    [Tooltip("프로젝트에서 사용하는 모든 몬스터 데이터 목록입니다.")]
    [SerializeField] private List<MonsterData> allMonsters = new List<MonsterData>();
    [Tooltip("일반 전투 노드에서 무작위로 선택할 몬스터 목록입니다.")]
    [SerializeField] private List<MonsterData> regularMonsters = new List<MonsterData>();
    [Tooltip("12층 보스 전투에서 무작위로 선택할 보스 데이터 목록입니다.")]
    [SerializeField] private List<BossData> bossMonsters = new List<BossData>();
    [Tooltip("보물상자 이벤트에서 등장할 미믹 몬스터입니다.")]
    [SerializeField] private MonsterData mimicMonster;

    public IReadOnlyList<MonsterData> AllMonsters => allMonsters;
    public IReadOnlyList<MonsterData> RegularMonsters => regularMonsters;
    public IReadOnlyList<BossData> BossMonsters => bossMonsters;
    public MonsterData MimicMonster => mimicMonster;

    public MonsterData GetRandomRegular(System.Random random)
    {
        return GetRandom(regularMonsters.Count > 0 ? regularMonsters : allMonsters, random);
    }

    public BossData GetRandomBoss(System.Random random)
    {
        if (bossMonsters == null || bossMonsters.Count == 0) return null;
        random ??= new System.Random(Environment.TickCount);
        for (int attempt = 0; attempt < bossMonsters.Count; attempt++)
        {
            BossData selected = bossMonsters[random.Next(0, bossMonsters.Count)];
            if (selected != null) return selected;
        }
        return bossMonsters.Find(boss => boss != null);
    }

    private static MonsterData GetRandom(List<MonsterData> source, System.Random random)
    {
        if (source == null || source.Count == 0) return null;
        random ??= new System.Random(Environment.TickCount);
        for (int attempt = 0; attempt < source.Count; attempt++)
        {
            MonsterData selected = source[random.Next(0, source.Count)];
            if (selected != null) return selected;
        }
        return source.Find(monster => monster != null);
    }
}
