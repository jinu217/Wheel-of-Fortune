using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapConnectionRenderer : MonoBehaviour
{
    [Tooltip("노드 연결선을 생성할 부모 RectTransform입니다.")]
    [SerializeField] private RectTransform lineContainer;
    [Tooltip("연결선에 사용할 색상입니다.")]
    [SerializeField] private Color lineColor = Color.white;
    [Tooltip("연결선의 굵기입니다.")]
    [Min(1f)] [SerializeField] private float lineWidth = 6f;

    private readonly List<GameObject> createdLines = new List<GameObject>();

    public void SetLineContainer(RectTransform container)
    {
        lineContainer = container;
    }

    public void Render(IReadOnlyList<InGameEventNode> nodes)
    {
        Clear();
        if (lineContainer == null || nodes == null)
        {
            return;
        }

        foreach (InGameEventNode fromNode in nodes)
        {
            if (fromNode == null)
            {
                continue;
            }

            RectTransform from = fromNode.transform as RectTransform;
            foreach (InGameEventNode toNode in fromNode.NextNodes)
            {
                RectTransform to = toNode == null ? null : toNode.transform as RectTransform;
                if (from != null && to != null)
                {
                    CreateLine(from.anchoredPosition, to.anchoredPosition);
                }
            }
        }
    }

    private void CreateLine(Vector2 from, Vector2 to)
    {
        GameObject lineObject = new GameObject("Map Connection", typeof(RectTransform), typeof(Image));
        RectTransform rect = lineObject.GetComponent<RectTransform>();
        rect.SetParent(lineContainer, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        Vector2 direction = to - from;
        rect.anchoredPosition = (from + to) * 0.5f;
        rect.sizeDelta = new Vector2(direction.magnitude, lineWidth);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        Image image = lineObject.GetComponent<Image>();
        image.color = lineColor;
        image.raycastTarget = false;
        rect.SetAsFirstSibling();
        createdLines.Add(lineObject);
    }

    private void Clear()
    {
        foreach (GameObject line in createdLines)
        {
            if (line != null)
            {
                Destroy(line);
            }
        }

        createdLines.Clear();
    }
}
