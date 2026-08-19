using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleUIController : MonoBehaviour
{
    [Tooltip("표시할 전투 상태를 제공하는 BattleManager입니다.")]
    [SerializeField] private BattleManager battleManager;
    [Tooltip("HP와 행동 데이터를 제공하는 MonsterController입니다.")]
    [SerializeField] private MonsterController monster;
    [Tooltip("현재 몬스터의 기본 이미지를 표시할 UI Image입니다.")]
    [SerializeField] private Image monsterImage;
    [Tooltip("몬스터의 공격·방어·스킬 행동 이미지를 표시할 UI Image입니다.")]
    [SerializeField] private Image monsterActionImage;
    [Tooltip("플레이어의 현재 HP와 최대 HP를 표시할 텍스트입니다.")]
    [SerializeField] private TMP_Text playerHpText;
    [Tooltip("몬스터의 현재 HP와 최대 HP를 표시할 텍스트입니다.")]
    [SerializeField] private TMP_Text monsterHpText;
    [Tooltip("현재 플레이어 턴 또는 몬스터 턴을 표시할 텍스트입니다.")]
    [SerializeField] private TMP_Text turnText;
    [Tooltip("공격과 방어에 공통으로 사용하는 하나의 룰렛 버튼입니다.")]
    [SerializeField] private Button rouletteButton;
    [Tooltip("플레이어의 현재 HP 비율을 표시할 슬라이더입니다.")]
    [SerializeField] private Slider playerHpSlider;
    [Tooltip("몬스터의 현재 HP 비율을 표시할 슬라이더입니다.")]
    [SerializeField] private Slider monsterHpSlider;
    [Tooltip("현재 보유 골드를 표시할 텍스트입니다.")]
    [SerializeField] private TMP_Text goldText;
    [Tooltip("현재 누적된 배리어를 HP 옆에 표시할 텍스트입니다.")]
    [SerializeField] private TMP_Text barrierText;
    [Tooltip("몬스터의 누적 배리어를 HP와 분리해 표시할 텍스트입니다.")]
    [SerializeField] private TMP_Text monsterBarrierText;
    [Tooltip("공격 룰렛 코인 이미지입니다. 비어 있으면 붉은 기본 이미지로 표시합니다.")]
    [SerializeField] private Sprite attackCoinImage;
    [Tooltip("방어 룰렛 코인 이미지입니다. 비어 있으면 파란 기본 이미지로 표시합니다.")]
    [SerializeField] private Sprite defenseCoinImage;
    [Tooltip("공격 코인부터 방어 코인 순서로 모든 룰렛 코인 이미지가 배치될 하나의 부모입니다.")]
    [SerializeField] private RectTransform rouletteCoinContainer;
    [Tooltip("화면에 표시되는 룰렛 코인 이미지 한 개의 가로·세로 크기입니다.")]
    [SerializeField] private Vector2 rouletteCoinImageSize = new Vector2(64f, 64f);
    [Tooltip("룰렛 코인 이미지 중심 사이의 가로 간격입니다.")]
    [Min(0f)] [SerializeField] private float rouletteCoinSpacing = 72f;
    [Tooltip("최대 3개의 소모품 이미지를 표시할 인벤토리 슬롯입니다.")]
    [SerializeField] private Image[] inventorySlotImages = new Image[3];

    private PlayerStatManager playerStats;
    private PlayerInventoryManager inventory;
    private readonly List<Image> rouletteCoinImages = new List<Image>();
    private readonly Button[] inventorySlotButtons = new Button[3];

    private void OnEnable()
    {
        if (battleManager != null)
        {
            battleManager.TurnChanged += HandleTurnChanged;
            battleManager.MonsterTurnResolved += ShowMonsterAction;
            battleManager.PlayerRouletteResolved += HandleRouletteResolved;
        }

        if (monster != null)
        {
            monster.HpChanged += HandleMonsterHpChanged;
            monster.NextActionChanged += RefreshActionPreview;
        }
    }

    private void Start()
    {
        BindSingleRouletteButton();
        BuildMissingBattleHud();
        BindPlayerStats();
        BindInventory();
        RefreshAll();
    }

    private void OnDisable()
    {
        if (battleManager != null)
        {
            battleManager.TurnChanged -= HandleTurnChanged;
            battleManager.MonsterTurnResolved -= ShowMonsterAction;
            battleManager.PlayerRouletteResolved -= HandleRouletteResolved;
        }

        if (monster != null)
        {
            monster.HpChanged -= HandleMonsterHpChanged;
            monster.NextActionChanged -= RefreshActionPreview;
        }

        if (playerStats != null)
        {
            playerStats.StatsChanged -= RefreshPlayerUI;
        }

        if (inventory != null)
        {
            inventory.InventoryChanged -= RefreshInventory;
        }
    }

    private void RefreshAll()
    {
        if (monster != null && monster.Data != null)
        {
            if (monsterImage != null)
            {
                monsterImage.sprite = monster.Data.MonsterImage;
            }

            HandleMonsterHpChanged(monster.CurrentHp);
            EnsureActionPreviewImage();
            RefreshActionPreview();
        }

        RefreshPlayerUI();
        HandleTurnChanged(battleManager == null ? BattleTurn.None : battleManager.CurrentTurn);
    }

    private void RefreshPlayerUI()
    {
        if (playerStats == null)
        {
            return;
        }

        if (playerHpText != null)
        {
            playerHpText.gameObject.SetActive(true);
            playerHpText.text = $"{playerStats.CurrentHp} / {playerStats.MaxHp}";
        }

        if (playerHpSlider != null)
        {
            playerHpSlider.maxValue = Mathf.Max(1, playerStats.MaxHp);
            playerHpSlider.value = playerStats.CurrentHp;
        }

        RefreshCoinImages();

        if (goldText != null)
        {
            goldText.text = $"GOLD : {playerStats.Coin}";
        }

        if (barrierText != null)
        {
            barrierText.text = $"{playerStats.Barrier}";
        }

        RefreshRouletteButton();
    }

    private void BindSingleRouletteButton()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button.name == "Defense Spin")
            {
                button.gameObject.SetActive(false);
                continue;
            }

            if (rouletteButton == null && (button.name == "Roulette Spin" || button.name == "Attack Spin"))
            {
                rouletteButton = button;
            }
        }

    }

    private void RefreshRouletteButton()
    {
        if (rouletteButton == null || battleManager == null)
        {
            return;
        }

        rouletteButton.interactable = battleManager.CanSpinAttackRoulette || battleManager.CanSpinDefenseRoulette;
    }

    private void BindInventory()
    {
        inventory = GameSessionManager.Instance == null ? null : GameSessionManager.Instance.PlayerInventory;
        if (inventory != null)
        {
            inventory.InventoryChanged -= RefreshInventory;
            inventory.InventoryChanged += RefreshInventory;
        }

        RefreshInventory();
    }

    private void RefreshInventory()
    {
        for (int i = 0; i < inventorySlotImages.Length; i++)
        {
            if (inventorySlotImages[i] == null) continue;
            Sprite icon = inventory != null && i < inventory.Items.Count
                ? inventory.Items[i].ItemImage
                : null;
            inventorySlotImages[i].sprite = icon;
            inventorySlotImages[i].enabled = true;
            inventorySlotImages[i].preserveAspect = icon != null;
            inventorySlotImages[i].color = icon == null
                ? new Color(0.13f, 0.15f, 0.21f, 0.95f)
                : Color.white;
            if (i < inventorySlotButtons.Length)
            {
                int itemIndex = i;
                inventorySlotButtons[i] ??= inventorySlotImages[i].GetComponent<Button>();
                inventorySlotButtons[i] ??= inventorySlotImages[i].gameObject.AddComponent<Button>();
                inventorySlotButtons[i].targetGraphic = inventorySlotImages[i];
                inventorySlotButtons[i].onClick.RemoveAllListeners();
                inventorySlotButtons[i].onClick.AddListener(() => inventory?.UseItem(itemIndex));
                inventorySlotButtons[i].interactable = inventory != null && i < inventory.Items.Count;
            }
        }
    }

    private void BuildMissingBattleHud()
    {
        Transform root = transform;
        TMP_FontAsset font = turnText == null ? null : turnText.font;

        if (monsterBarrierText == null)
        {
            monsterBarrierText = CreateRuntimeText("Monster Barrier", root, font, 24, new Vector2(535f, 105f), new Vector2(100f, 45f));
            monsterBarrierText.color = new Color(0.35f, 0.75f, 1f, 1f);
        }

        if (barrierText == null)
        {
            barrierText = CreateRuntimeText("Barrier", root, font, 24, new Vector2(-65f, 105f), new Vector2(100f, 45f));
            barrierText.color = new Color(0.35f, 0.75f, 1f, 1f);
        }

        if (playerHpSlider == null)
        {
            playerHpSlider = CreateRuntimeSlider("Player HP Slider", root, new Vector2(-300f, 70f));
        }

        if (monsterHpSlider == null)
        {
            monsterHpSlider = CreateRuntimeSlider("Monster HP Slider", root, new Vector2(300f, 70f));
        }

        if (rouletteCoinContainer == null)
        {
            rouletteCoinContainer = CreateRuntimeUI("Roulette Coin Images", root).GetComponent<RectTransform>();
            SetRuntimeRect(rouletteCoinContainer, new Vector2(0f, -625f), new Vector2(640f, 44f));
        }

        if (goldText == null)
        {
            goldText = CreateRuntimeText("Gold", root, font, 24, new Vector2(-300f, 35f), new Vector2(400f, 40f));
        }

        if (inventorySlotImages == null || inventorySlotImages.Length != 3)
        {
            inventorySlotImages = new Image[3];
        }

        for (int i = 0; i < 3; i++)
        {
            if (inventorySlotImages[i] != null) continue;
            GameObject slot = CreateRuntimeUI($"Inventory Slot {i + 1}", root, typeof(Image));
            SetRuntimeRect(slot.GetComponent<RectTransform>(), new Vector2(-410f + i * 110f, -20f), new Vector2(100f, 70f));
            slot.GetComponent<Image>().color = new Color(0.13f, 0.15f, 0.21f, 0.95f);
            GameObject icon = CreateRuntimeUI("Item Icon", slot.transform, typeof(Image));
            StretchRuntime(icon.GetComponent<RectTransform>(), 6f);
            inventorySlotImages[i] = icon.GetComponent<Image>();
            inventorySlotImages[i].preserveAspect = true;
        }
    }

    private static GameObject CreateRuntimeUI(string name, Transform parent, params System.Type[] components)
    {
        System.Type[] types = new System.Type[components.Length + 1];
        types[0] = typeof(RectTransform);
        components.CopyTo(types, 1);
        GameObject result = new GameObject(name, types);
        result.transform.SetParent(parent, false);
        return result;
    }

    private static TMP_Text CreateRuntimeText(string name, Transform parent, TMP_FontAsset font, float size, Vector2 position, Vector2 dimensions)
    {
        GameObject result = CreateRuntimeUI(name, parent, typeof(TextMeshProUGUI));
        SetRuntimeRect(result.GetComponent<RectTransform>(), position, dimensions);
        TextMeshProUGUI text = result.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        return text;
    }

    private static Slider CreateRuntimeSlider(string name, Transform parent, Vector2 position)
    {
        GameObject sliderObject = CreateRuntimeUI(name, parent, typeof(Slider));
        SetRuntimeRect(sliderObject.GetComponent<RectTransform>(), position, new Vector2(400f, 28f));
        Slider slider = sliderObject.GetComponent<Slider>();
        GameObject background = CreateRuntimeUI("Background", sliderObject.transform, typeof(Image));
        StretchRuntime(background.GetComponent<RectTransform>());
        background.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f, 1f);
        GameObject fill = CreateRuntimeUI("Fill", sliderObject.transform, typeof(Image));
        StretchRuntime(fill.GetComponent<RectTransform>(), 3f);
        fill.GetComponent<Image>().color = new Color(0.25f, 0.82f, 0.35f, 1f);
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.targetGraphic = background.GetComponent<Image>();
        slider.transition = Selectable.Transition.None;
        return slider;
    }

    private void RefreshCoinImages()
    {
        if (playerStats == null || rouletteCoinContainer == null) return;
        int attackCount = playerStats.AttackRouletteCoins;
        int defenseCount = playerStats.DefenseRouletteCoins;
        int totalCount = attackCount + defenseCount;
        while (rouletteCoinImages.Count < totalCount)
        {
            GameObject coin = CreateRuntimeUI("Coin", rouletteCoinContainer, typeof(Image));
            Image image = coin.GetComponent<Image>();
            image.raycastTarget = false;
            rouletteCoinImages.Add(image);
        }
        for (int i = 0; i < rouletteCoinImages.Count; i++)
        {
            Image image = rouletteCoinImages[i];
            image.gameObject.SetActive(i < totalCount);
            if (i >= totalCount) continue;
            bool isAttackCoin = i < attackCount;
            image.sprite = isAttackCoin ? attackCoinImage : defenseCoinImage;
            image.color = image.sprite != null ? Color.white
                : isAttackCoin ? new Color(0.82f, 0.22f, 0.18f, 1f) : new Color(0.2f, 0.45f, 0.9f, 1f);
            image.preserveAspect = true;
            RectTransform coinRect = image.rectTransform;
            coinRect.anchorMin = coinRect.anchorMax = new Vector2(0f, 1f);
            coinRect.pivot = new Vector2(0f, 1f);
            coinRect.anchoredPosition = new Vector2(i * rouletteCoinSpacing, 0f);
            coinRect.sizeDelta = rouletteCoinImageSize;
        }
    }

    private static void SetRuntimeRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void StretchRuntime(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    private void HandleMonsterHpChanged(int hp)
    {
        if (monsterHpText != null && monster.Data != null)
        {
            monsterHpText.text = $"{hp} / {monster.Data.Hp}";
        }

        if (monsterBarrierText != null && monster.Data != null)
        {
            monsterBarrierText.text = $"{monster.Barrier}";
        }

        if (monsterHpSlider != null && monster.Data != null)
        {
            monsterHpSlider.maxValue = Mathf.Max(1, monster.Data.Hp);
            monsterHpSlider.value = hp;
        }

        if (monsterImage != null && monster.Data != null)
        {
            monsterImage.sprite = monster.Data.MonsterImage;
        }
    }

    private void HandleTurnChanged(BattleTurn turn)
    {
        BindPlayerStats();
        if (turnText != null)
        {
            turnText.text = turn == BattleTurn.Player ? "Player Turn"
                : turn == BattleTurn.Monster ? "Monster Turn" : string.Empty;
        }

        RefreshPlayerUI();
    }

    private void BindPlayerStats()
    {
        PlayerStatManager current = battleManager == null ? null : battleManager.PlayerStats;
        if (current == null || current == playerStats)
        {
            return;
        }

        if (playerStats != null)
        {
            playerStats.StatsChanged -= RefreshPlayerUI;
        }

        playerStats = current;
        playerStats.StatsChanged += RefreshPlayerUI;
    }

    private void ShowMonsterAction(MonsterActionType action)
    {
        if (monster == null || monster.Data == null)
        {
            return;
        }

        RefreshActionPreview();
    }

    private void EnsureActionPreviewImage()
    {
        if (monsterActionImage == null) return;
        monsterActionImage.preserveAspect = true;
        monsterActionImage.color = Color.white;
    }

    private void RefreshActionPreview()
    {
        if (monster == null || monster.Data == null || monsterActionImage == null) return;
        monsterActionImage.color = Color.white;
        Sprite sprite = monster.HasNextAction ? GetMonsterActionSprite(monster.NextAction) : null;
        monsterActionImage.sprite = sprite;
        monsterActionImage.gameObject.SetActive(sprite != null);
    }

    private Sprite GetMonsterActionSprite(MonsterActionType action)
    {
        switch (action)
        {
            case MonsterActionType.Attack: return monster.Data.AtkImage;
            case MonsterActionType.Defense: return monster.Data.DefImage;
            case MonsterActionType.Skill1: return monster.Data.Skill1Image;
            case MonsterActionType.Skill2: return monster.Data.Skill2Image;
            default: return null;
        }
    }

    private void HandleRouletteResolved(RouletteActionType action, RouletteSpinResult result)
    {
        RefreshAll();
    }
}
