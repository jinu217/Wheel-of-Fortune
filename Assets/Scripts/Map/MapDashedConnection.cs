using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapDashedConnection : MonoBehaviour
{
    [Header("References")]
    [Tooltip("노드들이 들어있는 부모. 현재 구조에서는 Map을 넣으면 됩니다.")]
    [SerializeField] private RectTransform nodeContainer;

    [Tooltip("연결선들이 생성될 부모 Lines")]
    [SerializeField] private RectTransform lineContainer;


    [Header("Row Settings")]
    [Tooltip("Y좌표가 이 정도 차이 안이면 같은 줄로 판단합니다.")]
    [SerializeField] private float rowTolerance = 40f;


    [Header("Connection Settings")]
    [Tooltip("바로 윗줄 노드와 연결할 수 있는 최대 거리")]
    [SerializeField] private float maxConnectionDistance = 210f;

    [Tooltip("한 노드가 바로 윗줄과 연결할 최대 개수")]
    [Range(1, 2)]
    [SerializeField] private int maxConnectionsPerNode = 2;


    [Header("Line Settings")]
    [SerializeField] private Color lineColor = Color.white;

    [Tooltip("실선의 굵기")]
    [SerializeField] private float lineWidth = 5f;


    private readonly List<GameObject> createdLines =
        new List<GameObject>();


    private void Start()
    {
        GenerateConnections();
    }


    [ContextMenu("Generate Connections")]
    public void GenerateConnections()
    {
        ClearConnections();

        if (nodeContainer == null || lineContainer == null)
        {
            Debug.LogWarning(
                "Node Container 또는 Line Container가 설정되지 않았습니다."
            );

            return;
        }

        // 1. 실제 노드만 가져오기
        List<RectTransform> nodes = GetNodes();

        if (nodes.Count == 0)
        {
            Debug.LogWarning("노드를 찾지 못했습니다.");
            return;
        }

        // 2. Y좌표를 기준으로 줄(Row) 만들기
        List<List<RectTransform>> rows = CreateRows(nodes);

        // 3. 아래 → 위 순서로 정렬
        rows.Sort((rowA, rowB) =>
        {
            float yA = GetAverageY(rowA);
            float yB = GetAverageY(rowB);

            return yA.CompareTo(yB);
        });

        // 4. 반드시 현재 줄 → 바로 윗줄만 연결
        for (int i = 0; i < rows.Count - 1; i++)
        {
            List<RectTransform> currentRow = rows[i];
            List<RectTransform> upperRow = rows[i + 1];

            ConnectRows(currentRow, upperRow);
        }
    }
    private List<RectTransform> GetNodes()
    {
        List<RectTransform> nodes = new List<RectTransform>();

        foreach (Transform child in nodeContainer)
        {
            // Lines 오브젝트는 절대 노드로 사용하지 않음
            if (child == lineContainer)
                continue;

            // 이름이 Lines인 경우에도 제외
            if (child.name == "Lines")
                continue;

            // 비활성화된 오브젝트 제외
            if (!child.gameObject.activeInHierarchy)
                continue;

            RectTransform rect =
                child.GetComponent<RectTransform>();

            Image image =
                child.GetComponent<Image>();

            // Image 컴포넌트가 있는 UI만 노드 취급
            // 따라서 빈 RectTransform인 Lines는 자동 제외됨
            if (rect != null && image != null)
            {
                nodes.Add(rect);
            }
        }

        return nodes;
    }
    private List<List<RectTransform>> CreateRows(
        List<RectTransform> nodes)
    {
        List<List<RectTransform>> rows =
            new List<List<RectTransform>>();

        // Y가 낮은 순서로 우선 정렬
        nodes.Sort((a, b) =>
            a.anchoredPosition.y.CompareTo(
                b.anchoredPosition.y
            )
        );

        foreach (RectTransform node in nodes)
        {
            bool added = false;

            foreach (List<RectTransform> row in rows)
            {
                float rowY = GetAverageY(row);

                float difference =
                    Mathf.Abs(
                        node.anchoredPosition.y - rowY
                    );

                // Y 차이가 작으면 같은 줄
                if (difference <= rowTolerance)
                {
                    row.Add(node);
                    added = true;
                    break;
                }
            }

            // 어느 줄에도 속하지 않으면 새로운 줄
            if (!added)
            {
                List<RectTransform> newRow =
                    new List<RectTransform>();

                newRow.Add(node);

                rows.Add(newRow);
            }
        }

        return rows;
    }


    private float GetAverageY(List<RectTransform> row)
    {
        if (row.Count == 0)
            return 0f;

        float total = 0f;

        foreach (RectTransform node in row)
        {
            total += node.anchoredPosition.y;
        }

        return total / row.Count;
    }
    private void ConnectRows(
        List<RectTransform> currentRow,
        List<RectTransform> upperRow)
    {
        foreach (RectTransform currentNode in currentRow)
        {
            List<RectTransform> candidates =
                new List<RectTransform>();

            foreach (RectTransform upperNode in upperRow)
            {
                float distance =
                    Vector2.Distance(
                        currentNode.anchoredPosition,
                        upperNode.anchoredPosition
                    );

                // 너무 멀면 연결 안 함
                if (distance > maxConnectionDistance)
                    continue;

                candidates.Add(upperNode);
            }


            // 가까운 순서로 정렬
            candidates.Sort((a, b) =>
            {
                float distanceA =
                    Vector2.Distance(
                        currentNode.anchoredPosition,
                        a.anchoredPosition
                    );

                float distanceB =
                    Vector2.Distance(
                        currentNode.anchoredPosition,
                        b.anchoredPosition
                    );

                return distanceA.CompareTo(distanceB);
            });


            int connectionCount =
                Mathf.Min(
                    maxConnectionsPerNode,
                    candidates.Count
                );


            for (int i = 0;
                 i < connectionCount;
                 i++)
            {
                CreateSolidLine(
                    currentNode,
                    candidates[i]
                );
            }
        }
    }
    private void CreateSolidLine(
        RectTransform startNode,
        RectTransform endNode)
    {
        Vector2 start =
            ConvertPosition(startNode);

        Vector2 end =
            ConvertPosition(endNode);

        Vector2 direction =
            end - start;

        float distance =
            direction.magnitude;

        if (distance <= 0f)
            return;


        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;


        GameObject lineObject =
            new GameObject(
                "ConnectionLine",
                typeof(RectTransform),
                typeof(Image)
            );


        lineObject.transform.SetParent(
            lineContainer,
            false
        );


        RectTransform lineRect =
            lineObject.GetComponent<RectTransform>();


        // 시작점과 끝점의 중앙에 배치
        lineRect.anchoredPosition =
            (start + end) * 0.5f;


        // 하나의 Image를 길게 늘려 실선 생성
        lineRect.sizeDelta =
            new Vector2(
                distance,
                lineWidth
            );


        lineRect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );


        Image lineImage =
            lineObject.GetComponent<Image>();

        lineImage.color = lineColor;

        // UI 클릭 방해 방지
        lineImage.raycastTarget = false;


        createdLines.Add(lineObject);
    }
    private Vector2 ConvertPosition(RectTransform node)
    {
        Vector3 worldPosition =
            node.position;

        Vector3 localPosition =
            lineContainer.InverseTransformPoint(
                worldPosition
            );

        return new Vector2(
            localPosition.x,
            localPosition.y
        );
    }

    [ContextMenu("Clear Connections")]
    public void ClearConnections()
    {
        for (int i =
             lineContainer != null
                 ? lineContainer.childCount - 1
                 : -1;
             i >= 0;
             i--)
        {
            Transform child =
                lineContainer.GetChild(i);

            if (child.name == "ConnectionLine")
            {
                Destroy(child.gameObject);
            }
        }

        createdLines.Clear();
    }
}