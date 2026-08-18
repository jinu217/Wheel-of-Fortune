using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class PermanentStatRewardOption
{
    [Tooltip("보상 선택지에 표시할 이름입니다.")] public string rewardName = "능력치 상승";
    [Tooltip("보상 선택지에 표시할 이미지입니다.")] public Sprite rewardImage;
    [Tooltip("영구적으로 상승시킬 능력치입니다.")] public StatType statType = StatType.Attack;
    [Tooltip("선택한 능력치에 영구적으로 더할 수치입니다.")] public int amount = 1;
}

public class BattleRewardSelectionUI : MonoBehaviour
{
    private const int ChoiceCount = 3;

    [Header("Reward Data")]
    [Tooltip("전투 승리 시 무작위로 추첨할 전체 영구 능력치 보상 목록입니다.")]
    [SerializeField] private List<PermanentStatRewardOption> rewardOptions = new List<PermanentStatRewardOption>();
    [Header("Reward Panel")]
    [Tooltip("전투 종료 후 활성화할 능력치 선택 패널입니다. 비어 있으면 자동 생성합니다.")]
    [SerializeField] private GameObject rewardPanel;
    [Tooltip("클릭 가능한 선택지 배경 Image 3개입니다. 각 이미지 안에 보상 텍스트가 표시됩니다.")]
    [SerializeField] private Image[] rewardImages = new Image[ChoiceCount];
    [Tooltip("추첨된 선택지 이름과 상승 수치를 표시할 텍스트 3개입니다.")]
    [SerializeField] private TMP_Text[] rewardTexts = new TMP_Text[ChoiceCount];
    [Tooltip("보상 UI에 사용할 폰트입니다. 비어 있으면 현재 전투 UI의 폰트를 사용합니다.")]
    [SerializeField] private TMP_FontAsset rewardFont;

    private readonly PermanentStatRewardOption[] displayedRewards = new PermanentStatRewardOption[ChoiceCount];
    private readonly Button[] rewardButtons = new Button[ChoiceCount];
    private PlayerStatManager playerStats;
    private Action rewardSelected;
    private bool selectionCompleted;

    public void Show(PlayerStatManager stats, Action onRewardSelected)
    {
        playerStats = stats;
        rewardSelected = onRewardSelected;
        selectionCompleted = false;
        EnsureDefaultRewards();
        EnsureRewardPanel();
        DrawThreeRewards();
        rewardPanel.SetActive(true);
        rewardPanel.transform.SetAsLastSibling();
    }

    private void EnsureDefaultRewards()
    {
        int validRewardCount = rewardOptions.FindAll(option => option != null).Count;
        if (validRewardCount >= ChoiceCount) return;
        rewardOptions.Clear();
        rewardOptions.Add(CreateDefaultReward("최대 HP 증가", StatType.MaxHp, 10));
        rewardOptions.Add(CreateDefaultReward("공격력 증가", StatType.Attack, 2));
        rewardOptions.Add(CreateDefaultReward("방어력 증가", StatType.Defense, 2));
        rewardOptions.Add(CreateDefaultReward("최대 HP 크게 증가", StatType.MaxHp, 15));
        rewardOptions.Add(CreateDefaultReward("공격력 크게 증가", StatType.Attack, 3));
        rewardOptions.Add(CreateDefaultReward("방어력 크게 증가", StatType.Defense, 3));
    }

    private static PermanentStatRewardOption CreateDefaultReward(string rewardName, StatType statType, int amount)
    {
        return new PermanentStatRewardOption { rewardName = rewardName, statType = statType, amount = amount };
    }

    private void DrawThreeRewards()
    {
        List<PermanentStatRewardOption> candidates = rewardOptions.FindAll(option => option != null);
        for (int i = 0; i < ChoiceCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, candidates.Count);
            PermanentStatRewardOption option = candidates[randomIndex];
            candidates.RemoveAt(randomIndex);
            displayedRewards[i] = option;
            rewardImages[i].sprite = option.rewardImage;
            rewardImages[i].preserveAspect = true;
            rewardImages[i].color = option.rewardImage == null ? new Color(0.35f, 0.37f, 0.43f, 1f) : Color.white;
            rewardTexts[i].text = $"{option.rewardName}\n{GetStatName(option.statType)} +{option.amount}";
            rewardButtons[i].interactable = true;
        }
    }

    private void SelectReward(int choiceIndex)
    {
        if (selectionCompleted || playerStats == null || choiceIndex < 0 || choiceIndex >= ChoiceCount) return;
        PermanentStatRewardOption selected = displayedRewards[choiceIndex];
        if (selected == null) return;
        selectionCompleted = true;
        foreach (Button button in rewardButtons) button.interactable = false;
        playerStats.AddPermanentStat(selected.statType, selected.amount);
        rewardPanel.SetActive(false);
        rewardSelected?.Invoke();
    }

    private void EnsureRewardPanel()
    {
        if (HasCompleteInspectorUI()) BindButtons();
        else BuildRewardPanel();
    }

    private bool HasCompleteInspectorUI()
    {
        if (rewardPanel == null || rewardImages == null || rewardTexts == null
            || rewardImages.Length < ChoiceCount || rewardTexts.Length < ChoiceCount) return false;
        for (int i = 0; i < ChoiceCount; i++)
            if (rewardImages[i] == null || rewardTexts[i] == null) return false;
        return true;
    }

    private void BindButtons()
    {
        for (int i = 0; i < ChoiceCount; i++)
        {
            int index = i;
            rewardButtons[i] = rewardImages[i].GetComponent<Button>();
            if (rewardButtons[i] == null) rewardButtons[i] = rewardImages[i].gameObject.AddComponent<Button>();
            rewardButtons[i].onClick.RemoveAllListeners();
            rewardButtons[i].onClick.AddListener(() => SelectReward(index));
            rewardButtons[i].targetGraphic = rewardImages[i];
        }
    }

    private void BuildRewardPanel()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Battle Reward Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }
        if (rewardFont == null)
        {
            TMP_Text sceneText = FindFirstObjectByType<TMP_Text>(FindObjectsInactive.Include);
            rewardFont = sceneText == null ? null : sceneText.font;
        }

        rewardPanel = CreateUIObject("Permanent Stat Reward Panel", canvas.transform, typeof(Image));
        StretchToParent(rewardPanel.GetComponent<RectTransform>());
        rewardPanel.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.06f, 0.96f);
        CreateText("Reward Title", rewardPanel.transform, "전투 승리!\n영구 능력치 하나를 선택하세요", 42f,
            new Vector2(0f, 290f), new Vector2(1000f, 150f));

        rewardImages = new Image[ChoiceCount];
        rewardTexts = new TMP_Text[ChoiceCount];
        for (int i = 0; i < ChoiceCount; i++)
        {
            GameObject buttonObject = CreateUIObject($"Reward Choice {i + 1}", rewardPanel.transform, typeof(Image), typeof(Button));
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2((i - 1) * 360f, -20f);
            rect.sizeDelta = new Vector2(300f, 360f);
            buttonObject.GetComponent<Image>().color = new Color(0.18f, 0.20f, 0.26f, 1f);
            rewardButtons[i] = buttonObject.GetComponent<Button>();

            rewardImages[i] = buttonObject.GetComponent<Image>();
            rewardTexts[i] = CreateText("Reward Text", buttonObject.transform, string.Empty, 27f,
                Vector2.zero, new Vector2(270f, 300f));
        }
        BindButtons();
        rewardPanel.SetActive(false);
    }

    private static string GetStatName(StatType statType)
    {
        switch (statType)
        {
            case StatType.MaxHp: return "최대 HP";
            case StatType.Attack: return "공격력";
            case StatType.Defense: return "방어력";
            default: return statType.ToString();
        }
    }

    private TMP_Text CreateText(string name, Transform parent, string content, float size, Vector2 position, Vector2 dimensions)
    {
        GameObject textObject = CreateUIObject(name, parent, typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = content;
        text.font = rewardFont;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        return text;
    }

    private static GameObject CreateUIObject(string name, Transform parent, params Type[] components)
    {
        GameObject result = new GameObject(name, typeof(RectTransform));
        result.transform.SetParent(parent, false);
        foreach (Type component in components) result.AddComponent(component);
        return result;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
