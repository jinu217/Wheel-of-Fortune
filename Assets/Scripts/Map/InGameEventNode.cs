using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum InGameEventType
{
    Battle,
    Random,
    ShopOrInn,
    Boss
}

public class InGameEventNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    private float hoverScale = 1.08f;
    private float pressedScale = 0.92f;
    private float scaleDuration = 0.1f;
    [Tooltip("경로 탐색에 사용할 이벤트 노드의 X, Y 좌표입니다.")]
    [SerializeField] private Vector2Int gridPosition;
    [Tooltip("클릭했을 때 실행할 이벤트 종류입니다.")]
    [SerializeField] private InGameEventType eventType;
    [Tooltip("전투 이벤트에서 등장할 몬스터 데이터입니다.")]
    [SerializeField] private MonsterData monsterData;
    [Tooltip("보스 이벤트에서 등장할 보스 데이터입니다.")]
    [SerializeField] private BossData bossData;
    [Tooltip("이 이벤트 노드를 클릭하는 UI 버튼입니다.")]
    [SerializeField] private Button button;
    [Tooltip("이 이벤트를 완료한 뒤 이동할 수 있는 다음 노드 목록입니다.")]
    [SerializeField] private List<InGameEventNode> nextNodes = new List<InGameEventNode>();

    private InGameProgressionManager progressionManager;
    private Coroutine scaleRoutine;
    private bool pointerInside;

    public Vector2Int GridPosition => gridPosition;
    public InGameEventType EventType => eventType;
    public MonsterData MonsterData => monsterData;
    public BossData BossData => bossData;
    public IReadOnlyList<InGameEventNode> NextNodes => nextNodes;
    public int ConsecutiveTypeCount { get; private set; } = 1;
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

    }

    public void Configure(Vector2Int position, InGameEventType type, MonsterData monster = null)
    {
        gridPosition = position;
        eventType = type;
        monsterData = monster;
        bossData = null;
        nextNodes.Clear();
        IsCompleted = false;
        ConsecutiveTypeCount = 1;
    }

    public void ConfigureBoss(Vector2Int position, BossData boss)
    {
        gridPosition = position;
        eventType = InGameEventType.Boss;
        monsterData = null;
        bossData = boss;
        nextNodes.Clear();
        IsCompleted = false;
        ConsecutiveTypeCount = 1;
    }

    public void SetRuntimeUI(Button nodeButton)
    {
        button = nodeButton;
    }

    public void SetAnimationSettings(float hover, float pressed, float duration)
    {
        hoverScale = Mathf.Max(0.01f, hover);
        pressedScale = Mathf.Max(0.01f, pressed);
        scaleDuration = Mathf.Max(0f, duration);
    }

    public void ConnectTo(InGameEventNode nextNode)
    {
        if (nextNode != null && nextNode != this && !nextNodes.Contains(nextNode)
            && CanConnectTo(nextNode.EventType))
        {
            nextNodes.Add(nextNode);
            int nextStreak = eventType == nextNode.EventType ? ConsecutiveTypeCount + 1 : 1;
            nextNode.ConsecutiveTypeCount = Mathf.Max(nextNode.ConsecutiveTypeCount, nextStreak);
        }
    }

    public bool CanConnectTo(InGameEventType nextType)
    {
        if (eventType != nextType) return true;
        if (eventType == InGameEventType.ShopOrInn) return ConsecutiveTypeCount < 1;
        if (eventType == InGameEventType.Random || eventType == InGameEventType.Battle) return ConsecutiveTypeCount < 2;
        return false;
    }

    public void SetSelectable(bool selectable)
    {
        if (button != null)
        {
            button.interactable = selectable && !IsCompleted;
            if (!button.interactable)
            {
                pointerInside = false;
                AnimateScale(1f);
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button == null || !button.interactable) return;
        pointerInside = true;
        AnimateScale(hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        AnimateScale(1f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button == null || !button.interactable) return;
        AnimateScale(pressedScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (button == null || !button.interactable)
        {
            AnimateScale(1f);
            return;
        }
        AnimateScale(pointerInside ? hoverScale : 1f);
    }

    private void AnimateScale(float targetScale)
    {
        if (!isActiveAndEnabled) return;
        if (scaleRoutine != null) StopCoroutine(scaleRoutine);
        scaleRoutine = StartCoroutine(ScaleRoutine(targetScale));
    }

    private IEnumerator ScaleRoutine(float targetScale)
    {
        Vector3 start = transform.localScale;
        Vector3 target = Vector3.one * targetScale;
        float elapsed = 0f;
        if (scaleDuration <= 0f)
        {
            transform.localScale = target;
            scaleRoutine = null;
            yield break;
        }
        while (elapsed < scaleDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(start, target, Mathf.Clamp01(elapsed / scaleDuration));
            yield return null;
        }
        transform.localScale = target;
        scaleRoutine = null;
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

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnClicked);
        }
    }
}
