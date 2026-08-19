using System.Collections.Generic;
using UnityEngine;

public class GameSessionManager : MonoBehaviour
{
    public static GameSessionManager Instance { get; private set; }

    [Tooltip("씬이 바뀌어도 유지할 플레이어 능력치 관리자입니다.")]
    [SerializeField] private PlayerStatManager playerStats;
    [Tooltip("씬이 바뀌어도 유지할 플레이어 인벤토리 관리자입니다.")]
    [SerializeField] private PlayerInventoryManager playerInventory;
    [Tooltip("게임오버까지 유지할 플레이어 패시브 능력 관리자입니다.")]
    [SerializeField] private PlayerAbilityManager playerAbilities;
    [Tooltip("우연 발동 확률과 게임오버까지 유지되는 우연 효과를 관리합니다.")]
    [SerializeField] private ChanceSystemManager chanceSystem;
    [Header("기본 배경음악")]
    [Tooltip("게임 실행 중 모든 씬에서 반복 재생할 기본 배경음악입니다. 오디오는 나중에 지정해도 됩니다.")]
    [SerializeField] private AudioClip defaultBackgroundMusic;
    [Tooltip("기본 배경음악의 음량입니다.")]
    [Range(0f, 1f)] [SerializeField] private float backgroundMusicVolume = 0.5f;
    [Tooltip("이미 완료한 인게임 이벤트의 좌표 목록입니다.")]
    [SerializeField] private List<Vector2Int> completedEventPositions = new List<Vector2Int>();
    [Tooltip("현재 게임에서 이미 등장하여 다시 추첨하지 않을 일반 몬스터 목록입니다.")]
    [SerializeField] private List<MonsterData> encounteredMonsters = new List<MonsterData>();

    private Vector2Int currentEventPosition;
    private bool hasCurrentEvent;
    private AudioSource backgroundMusicSource;

    public PlayerStatManager PlayerStats => playerStats;
    public PlayerInventoryManager PlayerInventory => playerInventory;
    public PlayerAbilityManager PlayerAbilities => playerAbilities;
    public ChanceSystemManager ChanceSystem => chanceSystem;
    public MonsterData SelectedMonster { get; private set; }
    public BossData SelectedBoss { get; private set; }
    public Vector2Int CurrentEventPosition => currentEventPosition;
    public bool HasCurrentEvent => hasCurrentEvent;
    public bool? LastBattleWon { get; private set; }
    public int MapSeed { get; private set; }
    public string BattleReturnSceneName { get; private set; }
    public IReadOnlyList<MonsterData> EncounteredMonsters => encounteredMonsters;

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
        playerAbilities ??= GetComponentInChildren<PlayerAbilityManager>();
        playerAbilities ??= gameObject.AddComponent<PlayerAbilityManager>();
        chanceSystem ??= GetComponentInChildren<ChanceSystemManager>();
        chanceSystem ??= gameObject.AddComponent<ChanceSystemManager>();
        chanceSystem.SetPlayerData(playerStats, playerInventory, playerAbilities);
        ConfigureBackgroundMusic();
        DontDestroyOnLoad(transform.root.gameObject);
    }

    private void ConfigureBackgroundMusic()
    {
        backgroundMusicSource = gameObject.AddComponent<AudioSource>();
        backgroundMusicSource.playOnAwake = false;
        backgroundMusicSource.loop = true;
        backgroundMusicSource.volume = backgroundMusicVolume;
        backgroundMusicSource.clip = defaultBackgroundMusic;

        if (defaultBackgroundMusic != null)
            backgroundMusicSource.Play();
    }

    public void SetBackgroundMusic(AudioClip music)
    {
        defaultBackgroundMusic = music;
        if (backgroundMusicSource == null) ConfigureBackgroundMusic();
        backgroundMusicSource.clip = music;
        if (music != null) backgroundMusicSource.Play();
        else backgroundMusicSource.Stop();
    }

    public void SetBackgroundMusicVolume(float volume)
    {
        backgroundMusicVolume = Mathf.Clamp01(volume);
        if (backgroundMusicSource != null) backgroundMusicSource.volume = backgroundMusicVolume;
    }

    public void SetCurrentEvent(Vector2Int position)
    {
        currentEventPosition = position;
        hasCurrentEvent = true;
    }

    public void PrepareBattle(MonsterData monster, Vector2Int eventPosition, string returnSceneName = null)
    {
        SelectedMonster = monster;
        SelectedBoss = null;
        SetCurrentEvent(eventPosition);
        LastBattleWon = null;
        BattleReturnSceneName = returnSceneName;
        if (monster != null && !encounteredMonsters.Contains(monster))
            encounteredMonsters.Add(monster);
    }

    public void PrepareBossBattle(BossData boss, Vector2Int eventPosition, string returnSceneName = null)
    {
        SelectedBoss = boss;
        SelectedMonster = null;
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
        SelectedBoss = null;
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
        playerAbilities?.ResetForNewRun();
        chanceSystem?.ResetForNewRun();
        completedEventPositions.Clear();
        encounteredMonsters.Clear();
        hasCurrentEvent = false;
        SelectedMonster = null;
        SelectedBoss = null;
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
