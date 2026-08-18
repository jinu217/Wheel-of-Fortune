using System.Collections.Generic;
using UnityEngine;

public class GameSessionManager : MonoBehaviour
{
    public static GameSessionManager Instance { get; private set; }

    [Tooltip("씬이 바뀌어도 유지할 플레이어 능력치 관리자입니다.")]
    [SerializeField] private PlayerStatManager playerStats;
    [Tooltip("씬이 바뀌어도 유지할 플레이어 인벤토리 관리자입니다.")]
    [SerializeField] private PlayerInventoryManager playerInventory;
    [Tooltip("이미 완료한 인게임 이벤트의 좌표 목록입니다.")]
    [SerializeField] private List<Vector2Int> completedEventPositions = new List<Vector2Int>();

    private Vector2Int currentEventPosition;
    private bool hasCurrentEvent;

    public PlayerStatManager PlayerStats => playerStats;
    public PlayerInventoryManager PlayerInventory => playerInventory;
    public MonsterData SelectedMonster { get; private set; }
    public Vector2Int CurrentEventPosition => currentEventPosition;
    public bool HasCurrentEvent => hasCurrentEvent;
    public bool? LastBattleWon { get; private set; }
    public int MapSeed { get; private set; }
    public string BattleReturnSceneName { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        playerStats ??= GetComponentInChildren<PlayerStatManager>();
        playerInventory ??= GetComponentInChildren<PlayerInventoryManager>();
        DontDestroyOnLoad(transform.root.gameObject);
    }

    public void SetCurrentEvent(Vector2Int position)
    {
        currentEventPosition = position;
        hasCurrentEvent = true;
    }

    public void PrepareBattle(MonsterData monster, Vector2Int eventPosition, string returnSceneName = null)
    {
        SelectedMonster = monster;
        SetCurrentEvent(eventPosition);
        LastBattleWon = null;
        BattleReturnSceneName = returnSceneName;
    }

    public void FinishBattle(bool playerWon)
    {
        LastBattleWon = playerWon;
        if (playerWon)
        {
            MarkEventCompleted(currentEventPosition);
        }

        SelectedMonster = null;
    }

    public void MarkEventCompleted(Vector2Int position)
    {
        SetCurrentEvent(position);
        if (!completedEventPositions.Contains(position))
        {
            completedEventPositions.Add(position);
        }
    }

    public bool IsEventCompleted(Vector2Int position)
    {
        return completedEventPositions.Contains(position);
    }

    public void BeginNewRun()
    {
        playerStats?.ResetForNewRun();
        playerInventory?.ClearInventory();
        completedEventPositions.Clear();
        hasCurrentEvent = false;
        SelectedMonster = null;
        LastBattleWon = null;
        BattleReturnSceneName = null;
        MapSeed = Random.Range(1, int.MaxValue);
    }

    public int GetOrCreateMapSeed()
    {
        if (MapSeed == 0)
        {
            MapSeed = Random.Range(1, int.MaxValue);
        }

        return MapSeed;
    }
}
