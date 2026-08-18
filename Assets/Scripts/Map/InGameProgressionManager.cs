using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class InGameProgressionManager : MonoBehaviour
{
    [Tooltip("게임 시작 시 선택할 수 있는 맨 아래 이벤트 노드 목록입니다.")]
    [SerializeField] private List<InGameEventNode> startingNodes = new List<InGameEventNode>();
    [Tooltip("현재 맵에 존재하는 모든 이벤트 노드 목록입니다.")]
    [SerializeField] private List<InGameEventNode> eventNodes = new List<InGameEventNode>();

    private readonly List<InGameEventNode> selectableNodes = new List<InGameEventNode>();

    public InGameEventNode CurrentNode { get; private set; }
    public bool IsEventRunning { get; private set; }
    public IReadOnlyList<InGameEventNode> SelectableNodes => selectableNodes;

    public event Action<InGameEventNode> EventStarted;
    public event Action<InGameEventNode> EventCompleted;
    public event Action RouteFinished;

    public void SetMap(IReadOnlyList<InGameEventNode> allNodes, IReadOnlyList<InGameEventNode> firstNodes)
    {
        eventNodes.Clear();
        startingNodes.Clear();

        if (allNodes != null)
        {
            eventNodes.AddRange(allNodes);
        }

        if (firstNodes != null)
        {
            startingNodes.AddRange(firstNodes);
        }
    }

    private void Awake()
    {
        foreach (InGameEventNode node in eventNodes)
        {
            node?.Initialize(this);
        }

        foreach (InGameEventNode startingNode in startingNodes)
        {
            if (startingNode != null && !eventNodes.Contains(startingNode))
            {
                eventNodes.Add(startingNode);
                startingNode.Initialize(this);
            }
        }

        RestoreRouteState();
    }

    private void RestoreRouteState()
    {
        GameSessionManager session = GameSessionManager.Instance;
        if (session == null)
        {
            UnlockStartingNodes();
            return;
        }

        foreach (InGameEventNode node in eventNodes)
        {
            if (node != null && session.IsEventCompleted(node.GridPosition))
            {
                node.MarkCompleted();
            }
        }

        if (!session.HasCurrentEvent)
        {
            UnlockStartingNodes();
            return;
        }

        InGameEventNode current = eventNodes.Find(
            node => node != null && node.GridPosition == session.CurrentEventPosition);
        CurrentNode = current;

        if (session.IsEventCompleted(session.CurrentEventPosition))
        {
            UnlockNextNodes(current);
        }
        else if (current != null)
        {
            UnlockOnly(current);
        }
    }

    public bool TryStartEvent(InGameEventNode node)
    {
        if (node == null || IsEventRunning || !selectableNodes.Contains(node) || node.IsCompleted)
        {
            return false;
        }

        LockAllNodes();
        CurrentNode = node;
        IsEventRunning = true;
        GameSessionManager.Instance?.SetCurrentEvent(node.GridPosition);
        EventStarted?.Invoke(node);
        return true;
    }

    // Battle, Shop, Inn, or Random event controller calls this when its event ends.
    public void CompleteCurrentEvent()
    {
        if (!IsEventRunning || CurrentNode == null)
        {
            return;
        }

        CurrentNode.MarkCompleted();
        GameSessionManager.Instance?.MarkEventCompleted(CurrentNode.GridPosition);
        IsEventRunning = false;
        EventCompleted?.Invoke(CurrentNode);
        UnlockNextNodes(CurrentNode);
    }

    private void UnlockNextNodes(InGameEventNode current)
    {
        selectableNodes.Clear();

        if (current == null)
        {
            RouteFinished?.Invoke();
            return;
        }

        foreach (InGameEventNode node in current.NextNodes)
        {
            if (node == null || node.IsCompleted || selectableNodes.Contains(node))
            {
                continue;
            }

            selectableNodes.Add(node);
            node.SetSelectable(true);
        }

        if (selectableNodes.Count == 0)
        {
            RouteFinished?.Invoke();
        }
    }

    private void UnlockOnly(InGameEventNode node)
    {
        LockAllNodes();
        if (node == null)
        {
            Debug.LogError("Starting event node is not assigned.", this);
            return;
        }

        selectableNodes.Add(node);
        node.SetSelectable(true);
    }

    private void UnlockStartingNodes()
    {
        LockAllNodes();
        foreach (InGameEventNode node in startingNodes)
        {
            if (node == null || node.IsCompleted)
            {
                continue;
            }

            selectableNodes.Add(node);
            node.SetSelectable(true);
        }

        if (selectableNodes.Count == 0)
        {
            Debug.LogError("At least one starting event node must be assigned.", this);
        }
    }

    private void LockAllNodes()
    {
        selectableNodes.Clear();
        foreach (InGameEventNode node in eventNodes)
        {
            node?.SetSelectable(false);
        }
    }
}
