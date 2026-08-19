using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MonsterSpawnWeight
{
    [Tooltip("이 층 구간에서 등장할 몬스터입니다.")]
    public MonsterData monster;
    [Tooltip("몬스터의 상대 등장 확률입니다. 같은 구간의 전체 가중치 합을 기준으로 계산됩니다.")]
    [Min(0f)] public float weight;
}

[Serializable]
public class MonsterFloorSpawnTable
{
    [Tooltip("이 확률표가 적용되는 시작 층입니다.")]
    [Min(1)] public int minimumFloor = 1;
    [Tooltip("이 확률표가 적용되는 마지막 층입니다.")]
    [Min(1)] public int maximumFloor = 2;
    [Tooltip("해당 층 구간에 등장할 몬스터와 각각의 상대 확률입니다.")]
    public List<MonsterSpawnWeight> monsters = new List<MonsterSpawnWeight>();
}

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
    [Tooltip("일반 전투의 층 구간별 몬스터 등장 확률표입니다.")]
    [SerializeField] private List<MonsterFloorSpawnTable> floorSpawnTables = new List<MonsterFloorSpawnTable>();

    public IReadOnlyList<MonsterData> AllMonsters => allMonsters;
    public IReadOnlyList<MonsterData> RegularMonsters => regularMonsters;
    public IReadOnlyList<BossData> BossMonsters => bossMonsters;
    public MonsterData MimicMonster => mimicMonster;

    public MonsterData GetRandomRegular(System.Random random)
    {
        return GetRandom(regularMonsters.Count > 0 ? regularMonsters : allMonsters, random);
    }

    public MonsterData GetFloorWeightedRegular(
        int floorNumber,
        IReadOnlyList<MonsterData> encountered,
        System.Random random)
    {
        MonsterFloorSpawnTable table = floorSpawnTables.Find(value => value != null
            && floorNumber >= value.minimumFloor && floorNumber <= value.maximumFloor);
        if (table == null || table.monsters == null || table.monsters.Count == 0) return null;

        random ??= new System.Random(Environment.TickCount);
        List<(MonsterData monster, float weight)> candidates = new List<(MonsterData, float)>();
        float removedWeight = 0f;

        foreach (MonsterSpawnWeight entry in table.monsters)
        {
            if (entry == null || entry.monster == null || entry.weight <= 0f) continue;

            if (ContainsMonster(encountered, entry.monster)) removedWeight += entry.weight;
            else candidates.Add((entry.monster, entry.weight));
        }

        if (candidates.Count == 0) return null;

        // 제외된 몬스터의 확률을 남은 몬스터들에게 같은 양으로 분배합니다.
        float redistributedWeight = removedWeight / candidates.Count;
        float total = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            (MonsterData monster, float weight) candidate = candidates[i];
            candidate.weight += redistributedWeight;
            candidates[i] = candidate;
            total += candidate.weight;
        }

        double roll = random.NextDouble() * total;
        float cumulative = 0f;
        foreach ((MonsterData monster, float weight) candidate in candidates)
        {
            cumulative += candidate.weight;
            if (roll < cumulative) return candidate.monster;
        }
        return candidates[candidates.Count - 1].monster;
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

    private static bool ContainsMonster(IReadOnlyList<MonsterData> monsters, MonsterData target)
    {
        if (monsters == null) return false;
        for (int i = 0; i < monsters.Count; i++)
            if (monsters[i] == target) return true;
        return false;
    }

}
