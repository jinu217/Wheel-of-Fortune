using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameEventCoordinator : MonoBehaviour
{
    [Tooltip("이벤트 선택과 다음 경로 활성화를 관리하는 컴포넌트입니다.")]
    [SerializeField] private InGameProgressionManager progression;
    [Tooltip("랜덤 이벤트 추첨과 결과 처리를 담당하는 컴포넌트입니다.")]
    [SerializeField] private RandomEventManager randomEventManager;
    [Tooltip("상점 상품과 구매를 관리하는 컴포넌트입니다.")]
    [SerializeField] private ShopManager shopManager;
    [Tooltip("여관 회복과 버프 구매를 관리하는 컴포넌트입니다.")]
    [SerializeField] private InnManager innManager;
    [Tooltip("전투 이벤트 선택 시 이동할 씬 이름입니다.")]
    [SerializeField] private string monsterBattleSceneName = "MonsterBattleScene";
    [Tooltip("12층 보스 이벤트 선택 시 이동할 씬 이름입니다.")]
    [SerializeField] private string bossBattleSceneName = "BossBattleScene";
    [Tooltip("노드 선택 후 맵 이동 연출과 0.5초 대기를 포함해 이벤트를 실행하기까지 기다리는 시간입니다.")]
    [Min(0f)] [SerializeField] private float eventStartDelay = 0.75f;

    public event Action<RandomEventType> RandomEventOpened;
    public event Action ShopOrInnOpened;

    public void SetRuntimeReferences(
        InGameProgressionManager progressionManager,
        RandomEventManager randomManager,
        ShopManager shop,
        InnManager inn)
    {
        if (progression != null)
        {
            progression.EventStarted -= HandleEventStarted;
        }

        if (randomEventManager != null)
        {
            randomEventManager.RandomEventCompleted -= HandleRandomEventCompleted;
            randomEventManager.MimicEncountered -= HandleMimicEncountered;
            randomEventManager.RandomEventSelected -= HandleRandomEventSelected;
        }

        progression = progressionManager;
        randomEventManager = randomManager;
        shopManager = shop;
        innManager = inn;

        if (isActiveAndEnabled && progression != null)
        {
            progression.EventStarted -= HandleEventStarted;
            progression.EventStarted += HandleEventStarted;
        }

        if (isActiveAndEnabled && randomEventManager != null)
        {
            randomEventManager.RandomEventCompleted += HandleRandomEventCompleted;
            randomEventManager.MimicEncountered += HandleMimicEncountered;
            randomEventManager.RandomEventSelected += HandleRandomEventSelected;
        }
    }

    private void Start()
    {
        GameSessionManager session = GameSessionManager.Instance;
        if (session != null && randomEventManager != null)
        {
            randomEventManager.SetPlayerData(session.PlayerStats, session.PlayerInventory);
        }

        if (session != null)
        {
            shopManager?.SetPlayerData(session.PlayerStats, session.PlayerInventory);
            innManager?.SetPlayerStats(session.PlayerStats);
        }
    }

    private void OnEnable()
    {
        if (progression != null)
        {
            progression.EventStarted += HandleEventStarted;
        }

        if (randomEventManager != null)
        {
            randomEventManager.RandomEventCompleted += HandleRandomEventCompleted;
            randomEventManager.MimicEncountered += HandleMimicEncountered;
            randomEventManager.RandomEventSelected += HandleRandomEventSelected;
        }
    }

    private void OnDisable()
    {
        if (progression != null)
        {
            progression.EventStarted -= HandleEventStarted;
        }

        if (randomEventManager != null)
        {
            randomEventManager.RandomEventCompleted -= HandleRandomEventCompleted;
            randomEventManager.MimicEncountered -= HandleMimicEncountered;
            randomEventManager.RandomEventSelected -= HandleRandomEventSelected;
        }
    }

    public void CloseShopOrInn()
    {
        if (progression != null && progression.CurrentNode != null
            && progression.CurrentNode.EventType == InGameEventType.ShopOrInn)
        {
            progression.CompleteCurrentEvent();
        }
    }

    private void HandleEventStarted(InGameEventNode node)
    {
        StartCoroutine(HandleEventStartedRoutine(node));
    }

    private IEnumerator HandleEventStartedRoutine(InGameEventNode node)
    {
        if (eventStartDelay > 0f) yield return new WaitForSeconds(eventStartDelay);
        switch (node.EventType)
        {
            case InGameEventType.Battle:
                LoadBattle(node.MonsterData, node.GridPosition);
                break;
            case InGameEventType.Random:
                randomEventManager.SelectRandomEvent();
                break;
            case InGameEventType.ShopOrInn:
                ShopOrInnOpened?.Invoke();
                break;
            case InGameEventType.Boss:
                LoadBossBattle(node.BossData, node.GridPosition);
                break;
        }
    }

    private void HandleMimicEncountered(MonsterData mimic)
    {
        if (progression.CurrentNode != null)
        {
            LoadBattle(mimic, progression.CurrentNode.GridPosition);
        }
    }

    private void LoadBattle(MonsterData monster, Vector2Int eventPosition, string battleSceneName = null)
    {
        if (monster == null || GameSessionManager.Instance == null)
        {
            Debug.LogError("Battle data or GameSessionManager is missing.", this);
            return;
        }

        GameSessionManager.Instance.PrepareBattle(
            monster,
            eventPosition,
            SceneManager.GetActiveScene().name);
        SceneManager.LoadScene(string.IsNullOrEmpty(battleSceneName) ? monsterBattleSceneName : battleSceneName);
    }

    private void LoadBossBattle(BossData boss, Vector2Int eventPosition)
    {
        if (boss == null || GameSessionManager.Instance == null)
        {
            Debug.LogError("BossData 또는 GameSessionManager가 없습니다.", this);
            return;
        }
        GameSessionManager.Instance.PrepareBossBattle(
            boss, eventPosition, SceneManager.GetActiveScene().name);
        SceneManager.LoadScene(bossBattleSceneName);
    }

    private void HandleRandomEventCompleted(RandomEventType eventType)
    {
        progression.CompleteCurrentEvent();
    }

    private void HandleRandomEventSelected(RandomEventType eventType)
    {
        RandomEventOpened?.Invoke(eventType);
    }
}
