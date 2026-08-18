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
    [Tooltip("일반 몬스터, 보스, 미믹이 모두 등록된 몬스터 데이터베이스입니다.")]
    [SerializeField] private MonsterDatabase monsterDatabase;

    [Header("UI 배치")]
    [Tooltip("가로(X)와 세로(Y) 노드 사이 간격입니다.")]
    [SerializeField] private Vector2 nodeSpacing = new Vector2(160f, 180f);
    [Tooltip("노드 위치에 적용할 무작위 흔들림의 최대 크기입니다.")]
    [SerializeField] private Vector2 positionJitter = new Vector2(25f, 15f);
    [Tooltip("스크롤 영역 위아래에 추가할 여백입니다.")]
    [Min(0f)] [SerializeField] private float verticalPadding = 300f;

    [Header("2~10층 이벤트 비율")]
    [Tooltip("2~10층에서 몬스터 전투 노드가 나올 상대 비율입니다.")]
    [Min(0f)] [SerializeField] private float battleWeight = 0f;
    [Tooltip("2~10층에서 랜덤 이벤트 노드가 나올 상대 비율입니다.")]
    [Min(0f)] [SerializeField] private float randomWeight = 100f;
    [Tooltip("2~10층에서 상점·여관 노드가 나올 상대 비율입니다.")]
    [Min(0f)] [SerializeField] private float shopInnWeight = 0f;

    [Header("층별 가로 노드 개수 비율")]
    [Tooltip("한 층에 노드 1개가 생성될 상대 비율입니다.")]
    [Min(0f)] [SerializeField] private float oneNodeWeight = 1f;
    [Tooltip("한 층에 노드 2개가 생성될 상대 비율입니다.")]
    [Min(0f)] [SerializeField] private float twoNodeWeight = 1f;
    [Tooltip("한 층에 노드 3개가 생성될 상대 비율입니다.")]
    [Min(0f)] [SerializeField] private float threeNodeWeight = 1f;
    [Tooltip("한 층에 노드 4개가 생성될 상대 비율입니다.")]
    [Min(0f)] [SerializeField] private float fourNodeWeight = 1f;

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
            List<InGameEventNode> previousFloor = floor > 0 ? floors[floor - 1] : null;
            List<InGameEventType> floorTypes = CreateFloorEventTypes(floor, columns.Count, random, previousFloor);
            while (floorTypes.Count < columns.Count) floorTypes.Add(InGameEventType.Random);
            List<InGameEventNode> floorNodes = new List<InGameEventNode>();
            for (int i = 0; i < columns.Count; i++)
            {
                floorNodes.Add(GetOrCreateNode(new Vector2Int(columns[i], floor), floorTypes[i], random));
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

    private InGameEventNode GetOrCreateNode(Vector2Int position, InGameEventType type, System.Random random)
    {
        if (nodesByPosition.TryGetValue(position, out InGameEventNode existing))
        {
            return existing;
        }

        InGameEventNode node = Instantiate(nodePrefab, mapContent);
        node.name = $"Event Node ({position.x}, {position.y})";
        if (type == InGameEventType.Boss) node.ConfigureBoss(position, SelectBoss(random));
        else node.Configure(position, type, type == InGameEventType.Battle ? SelectMonster(random) : null);

        RectTransform rect = node.transform as RectTransform;
        if (rect != null)
        {
            float centeredX = (position.x - (MapWidth - 1) * 0.5f) * nodeSpacing.x;
            float jitterX = NextFloat(random, -positionJitter.x, positionJitter.x);
            float jitterY = NextFloat(random, -positionJitter.y, positionJitter.y);
            rect.anchoredPosition = new Vector2(
                centeredX + jitterX,
                verticalPadding * 0.5f + position.y * nodeSpacing.y + jitterY);
        }

        nodesByPosition.Add(position, node);
        return node;
    }

    private List<InGameEventType> CreateFloorEventTypes(int floor, int count, System.Random random, List<InGameEventNode> previousFloor)
    {
        List<InGameEventType> types = new List<InGameEventType>(count);
        if (floor == 0)
        {
            while (types.Count < count) types.Add(InGameEventType.Battle);
            return types;
        }
        if (floor == 10)
        {
            while (types.Count < count) types.Add(InGameEventType.ShopOrInn);
            return types;
        }
        if (floor == 11)
        {
            while (types.Count < count) types.Add(InGameEventType.Boss);
            return types;
        }

        while (types.Count < count)
        {
            types.Add(SelectWeightedMiddleEvent(random, floor != 9, previousFloor, types.Count));
        }
        return types;
    }

    private InGameEventType SelectWeightedMiddleEvent(System.Random random, bool allowShop, List<InGameEventNode> previousFloor, int choiceIndex)
    {
        float battle = AllCanConnect(previousFloor, InGameEventType.Battle) ? Mathf.Max(0f, battleWeight) : 0f;
        float randomEvent = AllCanConnect(previousFloor, InGameEventType.Random) ? Mathf.Max(0f, randomWeight) : 0f;
        float shopInn = allowShop && AllCanConnect(previousFloor, InGameEventType.ShopOrInn) ? Mathf.Max(0f, shopInnWeight) : 0f;
        float total = battle + randomEvent + shopInn;
        if (total <= 0f)
        {
            if (AllCanConnect(previousFloor, InGameEventType.Battle)) return InGameEventType.Battle;
            if (AllCanConnect(previousFloor, InGameEventType.Random)) return InGameEventType.Random;
            if (allowShop && AllCanConnect(previousFloor, InGameEventType.ShopOrInn)) return InGameEventType.ShopOrInn;
            return choiceIndex % 2 == 0 ? InGameEventType.Battle : InGameEventType.Random;
        }

        double roll = random.NextDouble() * total;
        if (roll < battle) return InGameEventType.Battle;
        if (roll < battle + randomEvent) return InGameEventType.Random;
        return InGameEventType.ShopOrInn;
    }

    private static bool AllCanConnect(List<InGameEventNode> sources, InGameEventType targetType)
    {
        if (sources == null) return true;
        foreach (InGameEventNode source in sources) if (!source.CanConnectTo(targetType)) return false;
        return true;
    }

    private MonsterData SelectMonster(System.Random random)
    {
        return monsterDatabase == null ? null : monsterDatabase.GetRandomRegular(random);
    }

    private BossData SelectBoss(System.Random random)
    {
        return monsterDatabase == null ? null : monsterDatabase.GetRandomBoss(random);
    }

    private List<int> CreateFloorColumns(System.Random random)
    {
        List<int> columns = new List<int> { 0, 1, 2, 3 };
        for (int i = columns.Count - 1; i > 0; i--)
        {
            int swap = random.Next(0, i + 1);
            (columns[i], columns[swap]) = (columns[swap], columns[i]);
        }
        int nodeCount = SelectFloorNodeCount(random);
        columns.RemoveRange(nodeCount, columns.Count - nodeCount);
        columns.Sort();
        return columns;
    }

    private int SelectFloorNodeCount(System.Random random)
    {
        float[] weights =
        {
            Mathf.Max(0f, oneNodeWeight), Mathf.Max(0f, twoNodeWeight),
            Mathf.Max(0f, threeNodeWeight), Mathf.Max(0f, fourNodeWeight)
        };
        float total = weights[0] + weights[1] + weights[2] + weights[3];
        if (total <= 0f) return random.Next(1, MapWidth + 1);

        double roll = random.NextDouble() * total;
        float cumulative = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (roll < cumulative) return i + 1;
        }
        return MapWidth;
    }

    private static void ConnectFloors(List<InGameEventNode> lower, List<InGameEventNode> upper)
    {
        foreach (InGameEventNode from in lower)
        {
            InGameEventNode nearest = FindNearestCompatibleTarget(from, upper);
            if (nearest != null) from.ConnectTo(nearest);
        }
        foreach (InGameEventNode target in upper)
        {
            InGameEventNode nearest = FindNearestCompatibleSource(lower, target);
            if (nearest != null) nearest.ConnectTo(target);
        }
    }

    private static InGameEventNode FindNearestCompatibleTarget(InGameEventNode source, List<InGameEventNode> targets)
    {
        InGameEventNode nearest = null;
        foreach (InGameEventNode target in targets)
            if (source.CanConnectTo(target.EventType) && (nearest == null || Mathf.Abs(target.GridPosition.x - source.GridPosition.x) < Mathf.Abs(nearest.GridPosition.x - source.GridPosition.x))) nearest = target;
        return nearest;
    }

    private static InGameEventNode FindNearestCompatibleSource(List<InGameEventNode> sources, InGameEventNode target)
    {
        InGameEventNode nearest = null;
        foreach (InGameEventNode source in sources)
            if (source.CanConnectTo(target.EventType) && (nearest == null || Mathf.Abs(source.GridPosition.x - target.GridPosition.x) < Mathf.Abs(nearest.GridPosition.x - target.GridPosition.x))) nearest = source;
        return nearest;
    }

    private static float NextFloat(System.Random random, float min, float max)
    {
        return Mathf.Lerp(min, max, (float)random.NextDouble());
    }
}
