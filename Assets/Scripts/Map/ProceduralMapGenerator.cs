using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class ProceduralMapGenerator : MonoBehaviour
{
    [Tooltip("생성된 노드와 시작 노드를 전달할 진행 관리자입니다.")]
    [SerializeField] private InGameProgressionManager progressionManager;
    [Tooltip("생성할 InGameEventNode UI 프리팹입니다.")]
    [SerializeField] private InGameEventNode nodePrefab;
    [Tooltip("생성된 맵 노드를 배치할 UI RectTransform입니다.")]
    [SerializeField] private RectTransform mapContent;
    [Tooltip("노드 사이의 연결선을 그릴 컴포넌트입니다.")]
    [SerializeField] private MapConnectionRenderer connectionRenderer;
    [Tooltip("전투 노드에 무작위로 배정할 몬스터 데이터 목록입니다.")]
    [SerializeField] private List<MonsterData> monsterPool = new List<MonsterData>();
    [Tooltip("12층 보스 노드에 무작위로 배정할 보스 데이터 목록입니다. 비어 있으면 일반 몬스터 목록을 사용합니다.")]
    [SerializeField] private List<MonsterData> bossPool = new List<MonsterData>();

    [Header("UI 배치")]
    [Tooltip("가로(X)와 세로(Y) 노드 사이 간격입니다.")]
    [SerializeField] private Vector2 nodeSpacing = new Vector2(160f, 180f);
    [Tooltip("노드 위치에 적용할 무작위 흔들림의 최대 크기입니다.")]
    [SerializeField] private Vector2 positionJitter = new Vector2(25f, 15f);
    [Tooltip("스크롤 영역 위아래에 추가할 여백입니다.")]
    [Min(0f)] [SerializeField] private float verticalPadding = 300f;

    private const int MapWidth = 4;
    private const int FloorCount = 12;

    private readonly Dictionary<Vector2Int, InGameEventNode> nodesByPosition =
        new Dictionary<Vector2Int, InGameEventNode>();

    private void Awake()
    {
        Generate();
    }

    public void Generate()
    {
        if (progressionManager == null || nodePrefab == null || mapContent == null)
        {
            Debug.LogError("ProceduralMapGenerator references are not assigned.", this);
            return;
        }

        nodesByPosition.Clear();
        int seed = GameSessionManager.Instance == null
            ? Environment.TickCount
            : GameSessionManager.Instance.GetOrCreateMapSeed();
        System.Random random = new System.Random(seed);

        List<List<InGameEventNode>> floors = new List<List<InGameEventNode>>();
        for (int floor = 0; floor < FloorCount; floor++)
        {
            List<int> columns = CreateFloorColumns(random);
            List<InGameEventNode> floorNodes = new List<InGameEventNode>();
            foreach (int x in columns)
            {
                floorNodes.Add(GetOrCreateNode(new Vector2Int(x, floor), random));
            }
            floors.Add(floorNodes);
            if (floor > 0) ConnectFloors(floors[floor - 1], floorNodes);
        }

        List<InGameEventNode> allNodes = new List<InGameEventNode>(nodesByPosition.Values);
        allNodes.Sort((left, right) => left.GridPosition.y != right.GridPosition.y
            ? left.GridPosition.y.CompareTo(right.GridPosition.y)
            : left.GridPosition.x.CompareTo(right.GridPosition.x));
        List<InGameEventNode> starts = allNodes.FindAll(node => node.GridPosition.y == 0);

        mapContent.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            (FloorCount - 1) * nodeSpacing.y + verticalPadding);
        progressionManager.SetMap(allNodes, starts);
        connectionRenderer?.Render(allNodes);
    }

    private InGameEventNode GetOrCreateNode(Vector2Int position, System.Random random)
    {
        if (nodesByPosition.TryGetValue(position, out InGameEventNode existing))
        {
            return existing;
        }

        InGameEventNode node = Instantiate(nodePrefab, mapContent);
        node.name = $"Event Node ({position.x}, {position.y})";
        InGameEventType type = SelectEventType(position.y);
        MonsterData monster = type == InGameEventType.Battle ? SelectMonster(random)
            : type == InGameEventType.Boss ? SelectBoss(random) : null;
        node.Configure(position, type, monster);

        RectTransform rect = node.transform as RectTransform;
        if (rect != null)
        {
            float centeredX = (position.x - (MapWidth - 1) * 0.5f) * nodeSpacing.x;
            float jitterX = NextFloat(random, -positionJitter.x, positionJitter.x);
            float jitterY = NextFloat(random, -positionJitter.y, positionJitter.y);
            rect.anchoredPosition = new Vector2(
                centeredX + jitterX,
                position.y * nodeSpacing.y + jitterY);
        }

        nodesByPosition.Add(position, node);
        return node;
    }

    private static InGameEventType SelectEventType(int floor)
    {
        if (floor == 0)
        {
            return InGameEventType.Battle;
        }

        if (floor <= 9) return InGameEventType.Random;
        if (floor == 10) return InGameEventType.ShopOrInn;
        return InGameEventType.Boss;
    }

    private MonsterData SelectMonster(System.Random random)
    {
        return monsterPool.Count == 0 ? null : monsterPool[random.Next(0, monsterPool.Count)];
    }

    private MonsterData SelectBoss(System.Random random)
    {
        return bossPool.Count > 0 ? bossPool[random.Next(0, bossPool.Count)] : SelectMonster(random);
    }

    private static List<int> CreateFloorColumns(System.Random random)
    {
        List<int> columns = new List<int> { 0, 1, 2, 3 };
        for (int i = columns.Count - 1; i > 0; i--)
        {
            int swap = random.Next(0, i + 1);
            (columns[i], columns[swap]) = (columns[swap], columns[i]);
        }
        int nodeCount = random.Next(1, MapWidth + 1);
        columns.RemoveRange(nodeCount, columns.Count - nodeCount);
        columns.Sort();
        return columns;
    }

    private static void ConnectFloors(List<InGameEventNode> lower, List<InGameEventNode> upper)
    {
        foreach (InGameEventNode from in lower)
        {
            InGameEventNode nearest = upper[0];
            foreach (InGameEventNode candidate in upper)
                if (Mathf.Abs(candidate.GridPosition.x - from.GridPosition.x) < Mathf.Abs(nearest.GridPosition.x - from.GridPosition.x)) nearest = candidate;
            from.ConnectTo(nearest);
        }
        foreach (InGameEventNode target in upper)
        {
            InGameEventNode nearest = lower[0];
            foreach (InGameEventNode candidate in lower)
                if (Mathf.Abs(candidate.GridPosition.x - target.GridPosition.x) < Mathf.Abs(nearest.GridPosition.x - target.GridPosition.x)) nearest = candidate;
            nearest.ConnectTo(target);
        }
    }

    private static float NextFloat(System.Random random, float min, float max)
    {
        return Mathf.Lerp(min, max, (float)random.NextDouble());
    }
}
