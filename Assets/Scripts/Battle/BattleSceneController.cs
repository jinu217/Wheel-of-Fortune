using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    [Tooltip("패배 후 기록을 초기화하고 메인 화면으로 이동하는 버튼입니다. 비어 있으면 패배 패널 안에 자동 생성합니다.")]
    [SerializeField] private Button mainMenuButton;
    [Tooltip("패배 후 돌아갈 메인 화면 씬 이름입니다.")]
    [SerializeField] private string gameStartSceneName = "GameStartScene";
    [Tooltip("전투 승리 후 영구 능력치 3개 중 하나를 선택하게 하는 UI입니다. 비어 있으면 자동 생성합니다.")]
    [SerializeField] private BattleRewardSelectionUI rewardSelectionUI;
    [Tooltip("전투 종료 후 우연이 발동했을 때 표시할 경고 UI입니다. 비어 있으면 자동 생성합니다.")]
    [SerializeField] private ChanceEventUI chanceEventUI;
    [Tooltip("전투 결과를 표시한 뒤 인게임 씬으로 돌아가기까지 기다리는 시간입니다.")]
    [Min(0f)] [SerializeField] private float returnDelaySeconds = 1f;
    [Tooltip("전투 종료 후 돌아갈 인게임 씬 이름입니다.")]
    [SerializeField] private string inGameSceneName = "InGameScene";
    [Tooltip("활성화하면 일반 MonsterData가 아니라 전달받은 BossData만 사용합니다.")]
    [SerializeField] private bool useBossData;

    private void Start()
    {
        GameSessionManager session = GameSessionManager.Instance;
        bool missingBattleData = session == null
            || useBossData && session.SelectedBoss == null
            || !useBossData && session.SelectedMonster == null;
        if (missingBattleData)
        {
            Debug.LogError(useBossData
                ? "BossBattleScene에 전달된 BossData가 없습니다."
                : "MonsterBattleScene에 전달된 MonsterData가 없습니다.", this);
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
        ConfigureDefeatPanel();
        battleManager.TurnChanged += HandleTurnChanged;
        battleManager.BattleFinished += HandleBattleFinished;
        if (useBossData) battleManager.BeginBattle(session.SelectedBoss);
        else battleManager.BeginBattle(session.SelectedMonster);
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
    }

    public void ReturnToMainMenu()
    {
        GameSessionManager.Instance?.BeginNewRun();
        SceneManager.LoadScene(gameStartSceneName);
    }

    private void ConfigureDefeatPanel()
    {
        if (defeatPanel == null) return;

        if (mainMenuButton == null)
            mainMenuButton = defeatPanel.GetComponentInChildren<Button>(true);
        if (mainMenuButton == null)
            mainMenuButton = CreateMainMenuButton();
        if (mainMenuButton == null) return;

        mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
        mainMenuButton.onClick.AddListener(ReturnToMainMenu);
    }

    private Button CreateMainMenuButton()
    {
        GameObject buttonObject = new GameObject("Main Menu Button", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.layer = defeatPanel.layer;
        buttonObject.transform.SetParent(defeatPanel.transform, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, -90f);
        buttonRect.sizeDelta = new Vector2(320f, 80f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.18f, 0.18f, 0.22f, 1f);

        GameObject textObject = new GameObject("Text", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.layer = defeatPanel.layer;
        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        TMP_Text panelText = defeatPanel.GetComponentInChildren<TMP_Text>(true);
        if (panelText != null) text.font = panelText.font;
        text.text = "메인화면으로";
        text.fontSize = 28f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        return button;
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
            HandleChanceCompleted));
    }

    private void HandleChanceCompleted()
    {
        PlayerAbilityManager abilities = GameSessionManager.Instance == null
            ? null : GameSessionManager.Instance.PlayerAbilities;
        if (abilities != null && abilities.Has(PassiveAbilityType.ChanceGrantsAbility))
        {
            chanceEventUI?.Hide();
            rewardSelectionUI?.SetSelectionEnabled(true);
            return;
        }

        LoadInGameScene();
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
