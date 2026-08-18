using System.Collections;
using UnityEngine;

public class MapMouseScroll : MonoBehaviour
{
    [Tooltip("마우스 휠 입력으로 위아래 이동시킬 맵 전체 RectTransform입니다.")]
    [SerializeField] private RectTransform map;
    [Tooltip("현재 화면에 표시되는 맵 영역의 높이를 계산할 기준 RectTransform입니다.")]
    [SerializeField] private RectTransform rollet;

    [Tooltip("마우스 휠 한 칸당 맵이 이동하는 거리입니다.")]
    [SerializeField] private float scrollSpeed = 80f;
    [Tooltip("체크하면 Y=0을 맵의 맨 아래 시작 위치로 사용합니다. 아래쪽 피벗 맵에 사용합니다.")]
    [SerializeField] private bool bottomStartsAtZero;

    private float minScrollY;
    private float maxScrollY;
    private bool isAutoMoving;

    private void Start()
    {
        RecalculateRange(true);
    }

    public void Configure(RectTransform mapTransform, RectTransform visibleArea, float speed = 80f,
        bool useZeroAsBottom = false)
    {
        map = mapTransform;
        rollet = visibleArea;
        scrollSpeed = Mathf.Max(0f, speed);
        bottomStartsAtZero = useZeroAsBottom;
        RecalculateRange(true);
    }

    public void RecalculateRange(bool moveToBottom)
    {
        if (map == null || rollet == null) return;

        // 맵 전체 높이 - 화면 높이
        float maxScroll = Mathf.Max(0f, map.rect.height - rollet.rect.height);
        minScrollY = bottomStartsAtZero ? -maxScroll : 0f;
        maxScrollY = bottomStartsAtZero ? 0f : maxScroll;

        if (moveToBottom)
        {
            // 게임 시작 시 맵의 맨 아래쪽을 보여줌
            Vector2 position = map.anchoredPosition;
            position.y = maxScrollY;
            map.anchoredPosition = position;
        }
    }

    private void Update()
    {
        if (map == null || rollet == null || isAutoMoving) return;

        float scroll = Input.mouseScrollDelta.y;

        if (scroll == 0f)
            return;

        Vector2 position = map.anchoredPosition;

        // 휠을 위로 → 맵의 위쪽으로 이동
        // 휠을 아래로 → 맵의 아래쪽으로 이동
        position.y -= scroll * scrollSpeed;

        position.y = Mathf.Clamp(position.y, minScrollY, maxScrollY);

        map.anchoredPosition = position;
    }

    public void FocusOn(RectTransform target, float duration)
    {
        if (map == null || rollet == null || target == null) return;
        StopAllCoroutines();
        float scaleY = map.parent == null ? 1f : Mathf.Max(0.0001f, Mathf.Abs(map.parent.lossyScale.y));
        float targetCenterY = target.TransformPoint(target.rect.center).y;
        float visibleCenterY = rollet.TransformPoint(rollet.rect.center).y;
        float destinationY = Mathf.Clamp(
            map.anchoredPosition.y + (visibleCenterY - targetCenterY) / scaleY,
            minScrollY,
            maxScrollY);
        if (duration <= 0f)
        {
            Vector2 immediate = map.anchoredPosition;
            immediate.y = destinationY;
            map.anchoredPosition = immediate;
            return;
        }
        StartCoroutine(FocusRoutine(destinationY, duration));
    }

    public void MoveBy(float yDistance, float duration)
    {
        if (map == null) return;
        StopAllCoroutines();
        float destinationY = Mathf.Clamp(map.anchoredPosition.y + yDistance, minScrollY, maxScrollY);
        if (duration <= 0f)
        {
            Vector2 immediate = map.anchoredPosition;
            immediate.y = destinationY;
            map.anchoredPosition = immediate;
            return;
        }
        StartCoroutine(FocusRoutine(destinationY, duration));
    }

    private IEnumerator FocusRoutine(float destinationY, float duration)
    {
        isAutoMoving = true;
        Vector2 start = map.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            Vector2 position = start;
            position.y = Mathf.Lerp(start.y, destinationY, progress);
            map.anchoredPosition = position;
            yield return null;
        }
        Vector2 completed = map.anchoredPosition;
        completed.y = destinationY;
        map.anchoredPosition = completed;
        isAutoMoving = false;
    }
}
