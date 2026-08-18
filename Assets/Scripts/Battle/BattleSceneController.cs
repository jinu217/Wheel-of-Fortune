using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleSceneController : MonoBehaviour
{
    [Tooltip("전투 턴과 승패를 관리하는 BattleManager입니다.")]
    [SerializeField] private BattleManager battleManager;
    [Tooltip("전투에서 사용할 10칸 룰렛입니다.")]
    [SerializeField] private RouletteController rouletteController;
    [Tooltip("플레이어 턴에만 표시할 룰렛 UI 전체 패널입니다.")]
    [SerializeField] private GameObject roulettePanel;
    [Tooltip("전투 패배 시 표시할 결과 패널입니다.")]
    [SerializeField] private GameObject defeatPanel;
    [Tooltip("전투 승리 후 영구 능력치 3개 중 하나를 선택하게 하는 UI입니다. 비어 있으면 자동 생성합니다.")]
    [SerializeField] private BattleRewardSelectionUI rewardSelectionUI;
    [Tooltip("전투 종료 후 우연이 발동했을 때 표시할 경고 UI입니다. 비어 있으면 자동 생성합니다.")]
    [SerializeField] private ChanceEventUI chanceEventUI;
    [Tooltip("전투 결과를 표시한 뒤 인게임 씬으로 돌아가기까지 기다리는 시간입니다.")]
    [Min(0f)] [SerializeField] private float returnDelaySeconds = 1f;
    [Tooltip("전투 종료 후 돌아갈 인게임 씬 이름입니다.")]
    [SerializeField] private string inGameSceneName = "InGameScene";

    private void Start()
    {
        GameSessionManager session = GameSessionManager.Instance;
        if (session == null || session.SelectedMonster == null)
        {
            Debug.LogError("No battle data exists in GameSessionManager.", this);
            SceneManager.LoadScene(inGameSceneName);
            return;
        }

        battleManager.SetPlayerStats(session.PlayerStats);
        rouletteController.SetPlayerStats(session.PlayerStats);
        rouletteController.SetAbilityManager(session.PlayerAbilities);
        session.PlayerInventory?.SetBattleManager(battleManager);
        if (!rouletteController.ConfigureInitialSlots())
        {
            Debug.LogError("Roulette initial configuration must contain exactly 10 slots.", this);
        }

        defeatPanel?.SetActive(false);
        battleManager.TurnChanged += HandleTurnChanged;
        battleManager.BattleFinished += HandleBattleFinished;
        battleManager.BeginBattle(session.SelectedMonster);
    }

    private void OnDestroy()
    {
        if (battleManager != null)
        {
            battleManager.TurnChanged -= HandleTurnChanged;
            battleManager.BattleFinished -= HandleBattleFinished;
        }
    }

    private void HandleTurnChanged(BattleTurn turn)
    {
        if (roulettePanel != null)
        {
            roulettePanel.SetActive(turn == BattleTurn.Player);
        }
    }

    private void HandleBattleFinished(bool playerWon)
    {
        roulettePanel?.SetActive(false);
        GameSessionManager.Instance.FinishBattle(playerWon);

        if (playerWon)
        {
            defeatPanel?.SetActive(false);
            if (battleManager.SuppressPostBattleRewards)
            {
                StartCoroutine(ReturnToInGameRoutine());
                return;
            }
            ShowChanceOrReward();
            return;
        }

        defeatPanel?.SetActive(true);
        StartCoroutine(ReturnToInGameRoutine());
    }

    private void ShowChanceOrReward()
    {
        GameSessionManager session = GameSessionManager.Instance;
        int floor = session.CurrentEventPosition.y + 1;
        ChanceSystemManager chanceSystem = session.ChanceSystem;
        if (chanceSystem != null && chanceSystem.TryGenerateEvent(floor, out var choices, out var selected))
        {
            ShowRewardSelection(false);
            StartCoroutine(ShowChanceAfterRewardDelay(chanceSystem, choices, selected, floor));
            return;
        }

        ShowRewardSelection(true);
    }

    private IEnumerator ShowChanceAfterRewardDelay(ChanceSystemManager chanceSystem,
        System.Collections.Generic.List<ChanceEffectDefinition> choices,
        ChanceEffectDefinition selected, int floor)
    {
        yield return new WaitForSeconds(1f);
        chanceEventUI ??= GetComponent<ChanceEventUI>();
        chanceEventUI ??= gameObject.AddComponent<ChanceEventUI>();
        yield return StartCoroutine(chanceEventUI.ShowAndApplyRoutine(
            chanceSystem, choices, selected, floor,
            LoadInGameScene));
    }

    private void ShowRewardSelection(bool allowSelection)
    {
        rewardSelectionUI ??= GetComponent<BattleRewardSelectionUI>();
        rewardSelectionUI ??= gameObject.AddComponent<BattleRewardSelectionUI>();
        rewardSelectionUI.Show(GameSessionManager.Instance.PlayerStats,
            () => StartCoroutine(ReturnToInGameRoutine()), allowSelection);
    }

    private IEnumerator ReturnToInGameRoutine()
    {
        roulettePanel?.SetActive(false);

        if (returnDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(returnDelaySeconds);
        }

        LoadInGameScene();
    }

    private void LoadInGameScene()
    {
        string returnScene = GameSessionManager.Instance == null
            || string.IsNullOrEmpty(GameSessionManager.Instance.BattleReturnSceneName)
            ? inGameSceneName
            : GameSessionManager.Instance.BattleReturnSceneName;
        SceneManager.LoadScene(returnScene);
    }
}
