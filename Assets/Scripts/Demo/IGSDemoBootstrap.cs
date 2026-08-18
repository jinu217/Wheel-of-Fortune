using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[DefaultExecutionOrder(-200)]
public class IGSDemoBootstrap : MonoBehaviour
{
    [Tooltip("데모 전투 노드에서 공통으로 사용할 몬스터 데이터입니다.")]
    [SerializeField] private MonsterData demoMonster;
    [Tooltip("12층 데모 보스 노드에서 사용할 몬스터 데이터입니다. 비어 있으면 일반 데모 몬스터를 사용합니다.")]
    [SerializeField] private MonsterData demoBoss;

    private const int DemoMapWidth = 4;
    private const int DemoFloorCount = 12;

    private InGameProgressionManager progression;
    private InGameEventCoordinator coordinator;
    private GameObject eventPanel;
    [Tooltip("데모 UI 전체에 사용할 TextMesh Pro 폰트 에셋입니다.")]
    [SerializeField] private TMP_FontAsset demoFont;

    private TMP_Text eventTitle;
    private ScrollRect mapScrollRect;
    private TMP_Text statusText;

    private void Awake()
    {
        GameObject preview = GameObject.Find("IGS Demo Preview Canvas");
        if (preview != null)
        {
            preview.SetActive(false);
        }

        if (GameObject.Find("IGS Demo Canvas") != null)
        {
            return;
        }

        EnsureGameSession();
        progression = FindFirstObjectByType<InGameProgressionManager>();
        coordinator = FindFirstObjectByType<InGameEventCoordinator>();
        RandomEventManager randomManager = FindFirstObjectByType<RandomEventManager>();
        ShopManager shopManager = FindFirstObjectByType<ShopManager>();
        InnManager innManager = FindFirstObjectByType<InnManager>();

        if (progression == null || coordinator == null)
        {
            Debug.LogError("IGS demo requires InGameProgressionManager and InGameEventCoordinator.", this);
            return;
        }

        coordinator.SetRuntimeReferences(progression, randomManager, shopManager, innManager);
        coordinator.RandomEventOpened += type => ShowEventPanel($"랜덤 이벤트: {GetRandomEventName(type)}");

        CreateEventSystemIfMissing();
        BuildDemoCanvas();
        BuildDemoMap();
    }

    private void Start()
    {
        if (mapScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            mapScrollRect.verticalNormalizedPosition = 0f;
        }

        RefreshStatus();
    }

    private void EnsureGameSession()
    {
        if (GameSessionManager.Instance != null)
        {
            return;
        }

        GameObject sessionObject = new GameObject("GameSession (Demo)");
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

    private void BuildDemoCanvas()
    {
        GameObject canvasObject = new GameObject(
            "IGS Demo Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject background = CreateUIObject("Background", canvasObject.transform, typeof(Image));
        Stretch(background.GetComponent<RectTransform>());
        background.GetComponent<Image>().color = new Color(0.055f, 0.07f, 0.11f, 1f);

        GameObject title = CreateText("Title", canvasObject.transform, "운명의 수레바퀴 - 데모 경로", 42);
        SetRect(title.GetComponent<RectTransform>(), new Vector2(0f, 840f), new Vector2(900f, 80f));

        statusText = CreateText("Player Status", canvasObject.transform, string.Empty, 28)
            .GetComponent<TMP_Text>();
        SetRect(statusText.rectTransform, new Vector2(0f, 775f), new Vector2(900f, 55f));
        GameSessionManager.Instance.PlayerStats.StatsChanged += RefreshStatus;

        GameObject scrollObject = CreateUIObject("Map Scroll View", canvasObject.transform, typeof(Image), typeof(ScrollRect));
        RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
        SetRect(scrollRect, new Vector2(0f, -40f), new Vector2(980f, 1500f));
        scrollObject.GetComponent<Image>().color = new Color(0.09f, 0.11f, 0.17f, 0.96f);
        mapScrollRect = scrollObject.GetComponent<ScrollRect>();

        GameObject viewport = CreateUIObject("Viewport", scrollObject.transform, typeof(Image), typeof(Mask));
        Stretch(viewport.GetComponent<RectTransform>(), 20f);
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        GameObject content = CreateUIObject("Map Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0f);
        contentRect.anchorMax = new Vector2(0.5f, 0f);
        contentRect.pivot = new Vector2(0.5f, 0f);
        contentRect.sizeDelta = new Vector2(900f, (DemoFloorCount - 1) * 190f + 260f);
        contentRect.anchoredPosition = Vector2.zero;
        mapScrollRect.viewport = viewport.GetComponent<RectTransform>();
        mapScrollRect.content = contentRect;
        mapScrollRect.horizontal = false;
        mapScrollRect.vertical = true;
        mapScrollRect.movementType = ScrollRect.MovementType.Clamped;

        BuildEventPanel(canvasObject.transform);
    }

    private void BuildDemoMap()
    {
        RectTransform content = mapScrollRect.content;
        System.Random random = new System.Random(GameSessionManager.Instance.GetOrCreateMapSeed());
        List<List<InGameEventNode>> floors = new List<List<InGameEventNode>>();
        for (int floor = 0; floor < DemoFloorCount; floor++)
        {
            List<int> columns = CreateFloorColumns(random);
            List<InGameEventNode> floorNodes = new List<InGameEventNode>();
            foreach (int x in columns)
            {
                Vector2Int position = new Vector2Int(x, floor);
                floorNodes.Add(CreateNode(content, position, SelectType(floor)));
            }
            floors.Add(floorNodes);
            if (floor > 0) ConnectFloors(floors[floor - 1], floorNodes);
        }

        List<InGameEventNode> allNodes = new List<InGameEventNode>();
        foreach (List<InGameEventNode> floor in floors) allNodes.AddRange(floor);
        progression.SetMap(allNodes, floors[0]);

        MapConnectionRenderer renderer = gameObject.AddComponent<MapConnectionRenderer>();
        renderer.SetLineContainer(content);
        renderer.Render(allNodes);
    }

    private InGameEventNode CreateNode(RectTransform parent, Vector2Int position, InGameEventType type)
    {
        GameObject nodeObject = CreateUIObject(
            $"Demo Node ({position.x}, {position.y})",
            parent,
            typeof(Image),
            typeof(Button));
        RectTransform rect = nodeObject.GetComponent<RectTransform>();
        float x = (position.x - (DemoMapWidth - 1) * 0.5f) * 190f;
        float y = 100f + position.y * 190f;
        SetRect(rect, new Vector2(x, y), new Vector2(92f, 92f));

        Image image = nodeObject.GetComponent<Image>();
        image.color = GetNodeColor(type);
        Button button = nodeObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.disabledColor = new Color(0.22f, 0.22f, 0.24f, 0.65f);
        colors.highlightedColor = Color.white;
        button.colors = colors;

        InGameEventNode node = nodeObject.AddComponent<InGameEventNode>();
        node.SetRuntimeUI(button, image);
        MonsterData monster = type == InGameEventType.Battle ? demoMonster
            : type == InGameEventType.Boss ? (demoBoss == null ? demoMonster : demoBoss) : null;
        node.Configure(position, type, monster);

        string label = type == InGameEventType.Battle ? "몬스터"
            : type == InGameEventType.Random ? "랜덤"
            : type == InGameEventType.ShopOrInn ? "상점·여관" : "보스";
        GameObject textObject = CreateText("Label", nodeObject.transform, label, 22);
        Stretch(textObject.GetComponent<RectTransform>());
        return node;
    }

    private void BuildEventPanel(Transform canvas)
    {
        eventPanel = CreateUIObject("Demo Event Panel", canvas, typeof(Image));
        SetRect(eventPanel.GetComponent<RectTransform>(), Vector2.zero, new Vector2(760f, 420f));
        eventPanel.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.2f, 0.98f);

        eventTitle = CreateText("Event Title", eventPanel.transform, "이벤트", 38)
            .GetComponent<TMP_Text>();
        SetRect(eventTitle.rectTransform, new Vector2(0f, 80f), new Vector2(680f, 120f));

        GameObject completeButton = CreateButton("Complete Button", eventPanel.transform, "이벤트 완료");
        SetRect(completeButton.GetComponent<RectTransform>(), new Vector2(0f, -95f), new Vector2(320f, 90f));
        completeButton.GetComponent<Button>().onClick.AddListener(() =>
        {
            eventPanel.SetActive(false);
            progression.CompleteCurrentEvent();
        });
        eventPanel.SetActive(false);
    }

    private void ShowEventPanel(string title)
    {
        eventTitle.text = title;
        eventPanel.SetActive(true);
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

    private static InGameEventType SelectType(int floor)
    {
        if (floor == 0)
        {
            return InGameEventType.Battle;
        }

        if (floor <= 9) return InGameEventType.Random;
        if (floor == 10) return InGameEventType.ShopOrInn;
        return InGameEventType.Boss;
    }

    private static Color GetNodeColor(InGameEventType type)
    {
        return type == InGameEventType.Battle ? new Color(0.72f, 0.2f, 0.18f, 1f)
            : type == InGameEventType.Random ? new Color(0.48f, 0.25f, 0.72f, 1f)
            : type == InGameEventType.ShopOrInn ? new Color(0.85f, 0.62f, 0.16f, 1f)
            : new Color(0.55f, 0.05f, 0.08f, 1f);
    }

    private static List<int> CreateFloorColumns(System.Random random)
    {
        List<int> columns = new List<int> { 0, 1, 2, 3 };
        for (int i = columns.Count - 1; i > 0; i--)
        {
            int swap = random.Next(0, i + 1);
            (columns[i], columns[swap]) = (columns[swap], columns[i]);
        }
        int nodeCount = random.Next(1, DemoMapWidth + 1);
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
        text.font = demoFont;
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

    private static string GetRandomEventName(RandomEventType type)
    {
        switch (type)
        {
            case RandomEventType.TreasureChest: return "보물상자";
            case RandomEventType.Shaman: return "주술사";
            case RandomEventType.CausalityShrine: return "인과율의 신전";
            case RandomEventType.LifeSpring: return "생명의 샘";
            case RandomEventType.ThornBush: return "가시 덤불";
            default: return type.ToString();
        }
    }
}
