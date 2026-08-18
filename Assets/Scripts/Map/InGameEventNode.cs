using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum InGameEventType
{
    Battle,
    Random,
    ShopOrInn,
    Boss
}

public class InGameEventNode : MonoBehaviour
{
    [Tooltip("경로 탐색에 사용할 이벤트 노드의 X, Y 좌표입니다.")]
    [SerializeField] private Vector2Int gridPosition;
    [Tooltip("클릭했을 때 실행할 이벤트 종류입니다.")]
    [SerializeField] private InGameEventType eventType;
    [Tooltip("전투 이벤트에서 등장할 몬스터 데이터입니다.")]
    [SerializeField] private MonsterData monsterData;
    [Tooltip("랜덤 이벤트 세부 종류입니다. 현재는 클릭 시 20% 확률로 자동 추첨합니다.")]
    [SerializeField] private RandomEventType randomEventType;
    [Tooltip("이 이벤트 노드를 클릭하는 UI 버튼입니다.")]
    [SerializeField] private Button button;
    [Tooltip("이 이벤트를 완료한 뒤 이동할 수 있는 다음 노드 목록입니다.")]
    [SerializeField] private List<InGameEventNode> nextNodes = new List<InGameEventNode>();
    [Tooltip("이벤트 종류를 표시할 아이콘 Image입니다.")]
    [SerializeField] private Image eventIcon;
    [Tooltip("몬스터 전투 노드에 표시할 아이콘입니다.")]
    [SerializeField] private Sprite battleIcon;
    [Tooltip("랜덤 이벤트 노드에 표시할 아이콘입니다.")]
    [SerializeField] private Sprite randomIcon;
    [Tooltip("상점·여관 노드에 표시할 아이콘입니다.")]
    [SerializeField] private Sprite shopInnIcon;
    [Tooltip("12층 보스 노드에 표시할 아이콘입니다.")]
    [SerializeField] private Sprite bossIcon;

    private InGameProgressionManager progressionManager;

    public Vector2Int GridPosition => gridPosition;
    public InGameEventType EventType => eventType;
    public MonsterData MonsterData => monsterData;
    public RandomEventType RandomEventType => randomEventType;
    public IReadOnlyList<InGameEventNode> NextNodes => nextNodes;
    public bool IsCompleted { get; private set; }

    public void Initialize(InGameProgressionManager manager)
    {
        progressionManager = manager;
        SetSelectable(false);

        if (button != null)
        {
            button.onClick.RemoveListener(OnClicked);
            button.onClick.AddListener(OnClicked);
        }

        RefreshIcon();
    }

    public void Configure(Vector2Int position, InGameEventType type, MonsterData monster = null)
    {
        gridPosition = position;
        eventType = type;
        monsterData = monster;
        nextNodes.Clear();
        IsCompleted = false;
        RefreshIcon();
    }

    public void SetRuntimeUI(Button nodeButton, Image iconImage)
    {
        button = nodeButton;
        eventIcon = iconImage;
    }

    public void ConnectTo(InGameEventNode nextNode)
    {
        if (nextNode != null && nextNode != this && !nextNodes.Contains(nextNode))
        {
            nextNodes.Add(nextNode);
        }
    }

    public void SetSelectable(bool selectable)
    {
        if (button != null)
        {
            button.interactable = selectable && !IsCompleted;
        }
    }

    public void MarkCompleted()
    {
        IsCompleted = true;
        SetSelectable(false);
    }

    private void OnClicked()
    {
        progressionManager?.TryStartEvent(this);
    }

    private void RefreshIcon()
    {
        if (eventIcon == null)
        {
            return;
        }

        switch (eventType)
        {
            case InGameEventType.Battle:
                eventIcon.sprite = battleIcon;
                break;
            case InGameEventType.Random:
                eventIcon.sprite = randomIcon;
                break;
            case InGameEventType.ShopOrInn:
                eventIcon.sprite = shopInnIcon;
                break;
            case InGameEventType.Boss:
                eventIcon.sprite = bossIcon;
                break;
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnClicked);
        }
    }
}
