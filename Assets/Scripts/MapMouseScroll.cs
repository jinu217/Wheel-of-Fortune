using UnityEngine;

public class MapMouseScroll : MonoBehaviour
{
    [SerializeField] private RectTransform map;
    [SerializeField] private RectTransform rollet;

    [SerializeField] private float scrollSpeed = 200f;

    private float maxScroll;

    private void Start()
    {
        // 맵 전체 높이 - 화면 높이
        maxScroll = Mathf.Max(0f, map.rect.height - rollet.rect.height);

        // 게임 시작 시 맵의 맨 아래쪽을 보여줌
        Vector2 position = map.anchoredPosition;
        position.y = maxScroll;
        map.anchoredPosition = position;
    }

    private void Update()
    {
        float scroll = Input.mouseScrollDelta.y;

        if (scroll == 0f)
            return;

        Vector2 position = map.anchoredPosition;

        // 휠을 위로 → 맵의 위쪽으로 이동
        // 휠을 아래로 → 맵의 아래쪽으로 이동
        position.y -= scroll * scrollSpeed;

        position.y = Mathf.Clamp(
            position.y,
            0f,
            maxScroll
        );

        map.anchoredPosition = position;
    }
}