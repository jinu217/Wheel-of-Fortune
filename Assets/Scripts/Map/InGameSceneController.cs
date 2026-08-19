using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[DefaultExecutionOrder(-200)]
public class InGameSceneController : MonoBehaviour
{
    [Tooltip("일반 전투, 보스, 미믹을 선택할 몬스터 데이터베이스입니다.")]
    [SerializeField] private MonsterDatabase monsterDatabase;

    private const int MapWidth = 4;
    private const int FloorCount = 12;
    private const int CenterFloorIndex = 5;
    private const float VisibleMapHeight = 1500f;
    private const float SelectionFocusDuration = 0.25f;

    private InGameProgressionManager progression;
    private InGameEventCoordinator coordinator;
    [Tooltip("인게임 UI 전체에 사용할 TextMesh Pro 폰트 에셋입니다.")]
    [SerializeField] private TMP_FontAsset inGameFont;

    [Header("씬 UI 참조")]
    [Tooltip("IGS 씬에 저장된 인게임 UI Canvas입니다.")]
    [SerializeField] private GameObject inGameCanvas;
    [Tooltip("절차적으로 생성된 맵 노드가 들어갈 영역입니다.")]
    [SerializeField] private RectTransform mapContent;
    [Tooltip("배경과 모든 선택지를 함께 이동시키는 맵 전체 패널입니다.")]
    [SerializeField] private RectTransform mapPanel;
    [Tooltip("맵 중앙 정렬과 이동 범위를 계산할 표시 영역입니다.")]
    [SerializeField] private RectTransform mapViewport;
    [Tooltip("마우스 휠로 맵을 이동시키는 컴포넌트입니다.")]
    [SerializeField] private MapMouseScroll mapMouseScroll;
    [Tooltip("플레이어의 현재 능력치와 골드를 표시할 텍스트입니다.")]
    [SerializeField] private TMP_Text statusText;
    [Tooltip("보물상자, 주술사, 인과율의 신전, 생명의 샘, 가시 덤불 전용 패널을 관리합니다.")]
    [SerializeField] private RandomEventPanelController randomEventPanelController;

    [Header("맵 선택지 표시")]
    [Tooltip("IGS 맵에 표시되는 각 이벤트 선택지의 가로·세로 크기입니다.")]
    [SerializeField] private Vector2 eventNodeSize = new Vector2(140f, 140f);
    [Tooltip("같은 층에 있는 이벤트 선택지 사이의 좌우 간격입니다.")]
    [Min(1f)] [SerializeField] private float horizontalNodeSpacing = 260f;
    [Tooltip("층과 층 사이의 상하 간격입니다.")]
    [Min(1f)] [SerializeField] private float verticalNodeSpacing = 190f;
    [Tooltip("몬스터 전투 선택지에 표시할 이미지입니다.")]
    [SerializeField] private Sprite battleNodeImage;
    [Tooltip("보스 전투 선택지에 표시할 이미지입니다.")]
    [SerializeField] private Sprite bossNodeImage;
    [Tooltip("랜덤 이벤트 선택지에 표시할 이미지입니다.")]
    [SerializeField] private Sprite randomNodeImage;
    [Tooltip("상점·여관 선택지에 표시할 이미지입니다.")]
    [SerializeField] private Sprite shopInnNodeImage;
    [Tooltip("선택지 사이의 연결선에 반복해서 표시할 이미지입니다.")]
    [SerializeField] private Sprite connectionLineImage;
    [Tooltip("선택지 사이 연결선 이미지의 굵기입니다.")]
    [Min(1f)] [SerializeField] private float connectionLineWidth = 6f;
    [Tooltip("마우스 휠 한 칸당 맵이 이동하는 거리입니다.")]
    [Min(0f)] [SerializeField] private float mapScrollSpeed = 80f;
    [Tooltip("선택 가능한 선택지에 마우스를 올렸을 때 적용할 크기 배율입니다.")]
    [Min(0.01f)] [SerializeField] private float nodeHoverScale = 1.08f;
    [Tooltip("선택지를 클릭하고 있는 동안 적용할 크기 배율입니다.")]
    [Min(0.01f)] [SerializeField] private float nodePressedScale = 0.92f;
    [Tooltip("선택지 확대·축소가 완료되는 데 걸리는 시간입니다.")]
    [Min(0f)] [SerializeField] private float nodeScaleAnimationDuration = 0.1f;

    private float MapHeight => (FloorCount - 1) * verticalNodeSpacing + VisibleMapHeight;

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

    private void Awake()
    {
        EnsureGameSession();
        progression = FindFirstObjectByType<InGameProgressionManager>();
        coordinator = FindFirstObjectByType<InGameEventCoordinator>();
        RandomEventManager randomManager = FindFirstObjectByType<RandomEventManager>();
        ShopManager shopManager = FindFirstObjectByType<ShopManager>();
        InnManager innManager = FindFirstObjectByType<InnManager>();

        if (progression == null || coordinator == null)
        {
            Debug.LogError("IGS requires InGameProgressionManager and InGameEventCoordinator.", this);
            return;
        }

        coordinator.SetRuntimeReferences(progression, randomManager, shopManager, innManager);

        CreateEventSystemIfMissing();
        if (inGameCanvas == null || mapContent == null || mapPanel == null || mapViewport == null)
        {
            if (inGameCanvas != null) Destroy(inGameCanvas);
            BuildCanvas();
        }
        if (randomManager != null)
        {
            if (randomEventPanelController == null)
                randomEventPanelController = inGameCanvas.GetComponentInChildren<RandomEventPanelController>(true);
            if (randomEventPanelController != null)
                randomEventPanelController.Configure(randomManager, inGameFont, inGameCanvas.transform);
            else
                Debug.LogError("IGS 씬의 Random Event Panels가 인스펙터에 연결되지 않았습니다.", this);
        }
        UpdateMapLayout();
        mapMouseScroll?.Configure(mapPanel, mapViewport, mapScrollSpeed, true);
        GameSessionManager.Instance.PlayerStats.StatsChanged += RefreshStatus;
        progression.EventStarted += HandleNodeSelected;
        BuildMap();
    }

    private void Start()
    {
        RefreshStatus();
        StartCoroutine(FocusSelectableRowNextFrame());
    }

    private void OnDestroy()
    {
        if (GameSessionManager.Instance?.PlayerStats != null)
            GameSessionManager.Instance.PlayerStats.StatsChanged -= RefreshStatus;
        if (progression != null) progression.EventStarted -= HandleNodeSelected;
    }

    private System.Collections.IEnumerator FocusSelectableRowNextFrame()
    {
        yield return null;
        if (progression == null || progression.SelectableNodes.Count == 0) yield break;
        RectTransform target = progression.SelectableNodes[0].transform as RectTransform;
        mapMouseScroll?.FocusOn(target, 0f);
    }

    private void HandleNodeSelected(InGameEventNode node)
    {
        if (node != null)
            mapMouseScroll?.MoveBy(-verticalNodeSpacing, SelectionFocusDuration);
    }

    private void EnsureGameSession()
    {
        if (GameSessionManager.Instance != null)
        {
            return;
        }

        GameObject sessionObject = new GameObject("GameSession");
        PlayerStatManager stats = sessionObject.AddComponent<PlayerStatManager>();
        PlayerInventoryManager inventory = sessionObject.AddComponent<PlayerInventoryManager>();
        inventory.SetPlayerStats(stats);
        sessionObject.AddComponent<GameSessionManager>();
    }

    private void CreateEventSystemIfMissing()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem.transform.SetAsLastSibling();
    }

    private void BuildCanvas()
    {
        GameObject canvasObject = new GameObject(
            "InGame Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        inGameCanvas = canvasObject;
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        float screenAspect = Screen.height <= 0 ? 1080f / 1920f : (float)Screen.width / Screen.height;
        float referenceAspect = scaler.referenceResolution.x / scaler.referenceResolution.y;
        scaler.matchWidthOrHeight = screenAspect >= referenceAspect ? 1f : 0f;

        GameObject background = CreateUIObject("Background", canvasObject.transform, typeof(Image));
        Stretch(background.GetComponent<RectTransform>());
        background.GetComponent<Image>().color = new Color(0.055f, 0.07f, 0.11f, 1f);

        GameObject title = CreateText("Title", canvasObject.transform, "운명의 수레바퀴", 42);
        SetRect(title.GetComponent<RectTransform>(), new Vector2(0f, 840f), new Vector2(900f, 80f));

        statusText = CreateText("Player Status", canvasObject.transform, string.Empty, 28)
            .GetComponent<TMP_Text>();
        SetRect(statusText.rectTransform, new Vector2(0f, 775f), new Vector2(900f, 55f));

        GameObject scrollObject = CreateUIObject("Map Scroll Area", canvasObject.transform);
        RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
        SetRect(scrollRect, new Vector2(0f, -40f), new Vector2(980f, VisibleMapHeight));
        mapViewport = scrollRect;

        float mapHeight = MapHeight;
        GameObject mapPanel = CreateUIObject("Map Panel", scrollObject.transform, typeof(Image));
        RectTransform mapPanelRect = mapPanel.GetComponent<RectTransform>();
        this.mapPanel = mapPanelRect;
        mapPanelRect.anchorMin = new Vector2(0.5f, 0f);
        mapPanelRect.anchorMax = new Vector2(0.5f, 0f);
        mapPanelRect.pivot = new Vector2(0.5f, 0f);
        mapPanelRect.sizeDelta = new Vector2(980f, mapHeight);
        mapPanelRect.anchoredPosition = Vector2.zero;
        mapPanel.GetComponent<Image>().color = new Color(0.09f, 0.11f, 0.17f, 0.96f);

        GameObject content = CreateUIObject("Map Content", mapPanel.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0f);
        contentRect.anchorMax = new Vector2(0.5f, 0f);
        contentRect.pivot = new Vector2(0.5f, 0f);
        contentRect.sizeDelta = new Vector2(960f, mapHeight);
        contentRect.anchoredPosition = Vector2.zero;
        mapContent = contentRect;
        mapMouseScroll = scrollObject.AddComponent<MapMouseScroll>();
        mapMouseScroll.Configure(mapPanelRect, mapViewport, mapScrollSpeed, true);

    }

    private void UpdateMapLayout()
    {
        float mapHeight = MapHeight;
        if (mapViewport != null)
            mapViewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, VisibleMapHeight);
        if (mapPanel != null)
            mapPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, mapHeight);
        if (mapContent != null)
            mapContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, mapHeight);
    }

    private void BuildMap()
    {
        RectTransform content = mapContent;
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
        System.Random random = new System.Random(GameSessionManager.Instance.GetOrCreateMapSeed());
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
                Vector2Int position = new Vector2Int(columns[i], floor);
                floorNodes.Add(CreateNode(content, position, floorTypes[i]));
            }
            floors.Add(floorNodes);
            if (floor > 0) ConnectFloors(floors[floor - 1], floorNodes);
        }

        List<InGameEventNode> allNodes = new List<InGameEventNode>();
        foreach (List<InGameEventNode> floor in floors) allNodes.AddRange(floor);
        progression.SetMap(allNodes, floors[0]);

        MapConnectionRenderer renderer = gameObject.AddComponent<MapConnectionRenderer>();
        renderer.Configure(content, connectionLineImage, connectionLineWidth);
        renderer.Render(allNodes);

    }

    private InGameEventNode CreateNode(RectTransform parent, Vector2Int position, InGameEventType type)
    {
        GameObject nodeObject = CreateUIObject(
            $"Event Node ({position.x}, {position.y})",
            parent,
            typeof(Image),
            typeof(Button));
        RectTransform rect = nodeObject.GetComponent<RectTransform>();
        float x = (position.x - (MapWidth - 1) * 0.5f) * horizontalNodeSpacing;
        // 노드 Anchor가 패널 중앙(0.5, 0.5)이므로 6번째 가로줄을 Y=0에 둡니다.
        float y = (position.y - CenterFloorIndex) * verticalNodeSpacing;
        SetRect(rect, new Vector2(x, y), eventNodeSize);

        Image image = nodeObject.GetComponent<Image>();
        image.sprite = GetNodeSprite(type);
        image.preserveAspect = true;
        image.color = image.sprite == null ? Color.clear : Color.white;
        Button button = nodeObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.82f, 0.42f, 1f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        colors.selectedColor = new Color(1f, 0.9f, 0.58f, 1f);
        colors.disabledColor = new Color(0.52f, 0.52f, 0.52f, 1f);
        colors.colorMultiplier = 1.15f;
        button.colors = colors;

        InGameEventNode node = nodeObject.AddComponent<InGameEventNode>();
        node.SetRuntimeUI(button);
        node.SetAnimationSettings(nodeHoverScale, nodePressedScale, nodeScaleAnimationDuration);
        System.Random monsterRandom = new System.Random(GameSessionManager.Instance.GetOrCreateMapSeed()
            ^ position.x * 397 ^ position.y * 7919);
        if (type == InGameEventType.Boss)
            node.ConfigureBoss(position, monsterDatabase?.GetRandomBoss(monsterRandom));
        else
            node.Configure(position, type, type == InGameEventType.Battle
                ? monsterDatabase?.GetFloorWeightedRegular(
                    position.y + 1,
                    GameSessionManager.Instance.EncounteredMonsters,
                    monsterRandom)
                : null);

        return node;
    }

    private void RefreshStatus()
    {
        if (statusText == null || GameSessionManager.Instance == null)
        {
            return;
        }

        PlayerStatManager stats = GameSessionManager.Instance.PlayerStats;
        statusText.text = $"HP {stats.CurrentHp}/{stats.MaxHp}   ATK {stats.Attack}   DEF {stats.Defense}   GOLD {stats.Coin}";
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

    private Sprite GetNodeSprite(InGameEventType type)
    {
        return type == InGameEventType.Battle ? battleNodeImage
            : type == InGameEventType.Random ? randomNodeImage
            : type == InGameEventType.ShopOrInn ? shopInnNodeImage
            : bossNodeImage;
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

    private GameObject CreateButton(string name, Transform parent, string label)
    {
        GameObject buttonObject = CreateUIObject(name, parent, typeof(Image), typeof(Button));
        buttonObject.GetComponent<Image>().color = new Color(0.22f, 0.5f, 0.36f, 1f);
        GameObject text = CreateText("Text", buttonObject.transform, label, 28);
        Stretch(text.GetComponent<RectTransform>());
        return buttonObject;
    }

    private GameObject CreateText(string name, Transform parent, string value, int fontSize)
    {
        GameObject textObject = CreateUIObject(name, parent, typeof(TextMeshProUGUI));
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = inGameFont;
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        return textObject;
    }

    private static GameObject CreateUIObject(string name, Transform parent, params Type[] components)
    {
        List<Type> types = new List<Type> { typeof(RectTransform) };
        types.AddRange(components);
        GameObject result = new GameObject(name, types.ToArray());
        result.transform.SetParent(parent, false);
        return result;
    }

    private static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

}
