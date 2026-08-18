using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleRewardSelectionUI : MonoBehaviour
{
    private const int ChoiceCount = 3;

    [Header("Reward Panel")]
    [Tooltip("전투 종료 후 활성화할 능력치 선택 패널입니다. 비어 있으면 자동 생성합니다.")]
    [SerializeField] private GameObject rewardPanel;
    [Tooltip("클릭해서 능력을 선택할 배경 Image 3개입니다.")]
    [SerializeField] private Image[] rewardImages = new Image[ChoiceCount];
    [Tooltip("추첨된 능력의 설명만 표시할 텍스트 3개입니다.")]
    [SerializeField] private TMP_Text[] rewardTexts = new TMP_Text[ChoiceCount];

    private TMP_FontAsset rewardFont;

    private readonly AbilityDefinition[] displayedRewards = new AbilityDefinition[ChoiceCount];
    private readonly Button[] rewardButtons = new Button[ChoiceCount];
    private PlayerAbilityManager abilityManager;
    private Action rewardSelected;
    private bool selectionCompleted;

    public void Show(PlayerStatManager stats, Action onRewardSelected, bool allowSelection = true)
    {
        abilityManager = GameSessionManager.Instance == null
            ? stats == null ? null : stats.GetComponentInParent<PlayerAbilityManager>()
            : GameSessionManager.Instance.PlayerAbilities;
        rewardSelected = onRewardSelected;
        selectionCompleted = false;
        EnsureRewardPanel();
        DrawThreeRewards();
        SetSelectionEnabled(allowSelection);
        rewardPanel.SetActive(true);
        rewardPanel.transform.SetAsLastSibling();
    }

    public void SetSelectionEnabled(bool enabled)
    {
        for (int i = 0; i < rewardButtons.Length; i++)
        {
            if (rewardButtons[i] != null)
                rewardButtons[i].interactable = enabled && displayedRewards[i] != null;
        }
    }

    private void DrawThreeRewards()
    {
        if (abilityManager == null) return;
        int floor = GameSessionManager.Instance == null ? 1 : GameSessionManager.Instance.CurrentEventPosition.y + 1;
        System.Collections.Generic.List<AbilityDefinition> candidates = abilityManager.GenerateChoices(floor, ChoiceCount);
        for (int i = 0; i < ChoiceCount; i++)
        {
            if (i >= candidates.Count)
            {
                displayedRewards[i] = null;
                rewardTexts[i].text = "획득 가능한 능력 없음";
                rewardButtons[i].interactable = false;
                rewardImages[i].gameObject.SetActive(false);
                continue;
            }
            AbilityDefinition option = candidates[i];
            displayedRewards[i] = option;
            rewardImages[i].gameObject.SetActive(true);
            rewardImages[i].sprite = null;
            rewardImages[i].color = option.Grade == AbilityGrade.Small ? new Color(0.32f, 0.36f, 0.42f, 1f)
                : option.Grade == AbilityGrade.Moderate ? new Color(0.20f, 0.42f, 0.62f, 1f)
                : new Color(0.72f, 0.52f, 0.10f, 1f);
            rewardTexts[i].text = option.Description;
            rewardButtons[i].interactable = true;
        }
    }

    private void SelectReward(int choiceIndex)
    {
        if (selectionCompleted || abilityManager == null || choiceIndex < 0 || choiceIndex >= ChoiceCount) return;
        AbilityDefinition selected = displayedRewards[choiceIndex];
        if (selected == null) return;
        selectionCompleted = true;
        foreach (Button button in rewardButtons) button.interactable = false;
        abilityManager.Acquire(selected);
        rewardPanel.SetActive(false);
        rewardSelected?.Invoke();
    }

    private void EnsureRewardPanel()
    {
        if (HasCompleteInspectorUI())
        {
            ApplyRewardFont();
            BindButtons();
        }
        else BuildRewardPanel();
    }

    private void ApplyRewardFont()
    {
        rewardFont = FindNeodgmFont();
        if (rewardFont == null || rewardTexts == null) return;

        foreach (TMP_Text rewardText in rewardTexts)
        {
            if (rewardText != null) rewardText.font = rewardFont;
        }
    }

    private TMP_FontAsset FindNeodgmFont()
    {
        if (rewardTexts != null)
        {
            foreach (TMP_Text rewardText in rewardTexts)
            {
                if (rewardText != null && rewardText.font != null
                    && rewardText.font.name.IndexOf("neodgm", StringComparison.OrdinalIgnoreCase) >= 0)
                    return rewardText.font;
            }
        }

        TMP_Text[] sceneTexts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (TMP_Text sceneText in sceneTexts)
        {
            if (sceneText.font != null
                && sceneText.font.name.IndexOf("neodgm", StringComparison.OrdinalIgnoreCase) >= 0)
                return sceneText.font;
        }

        return null;
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
        rewardFont = FindNeodgmFont();

        rewardPanel = CreateUIObject("Permanent Stat Reward Panel", canvas.transform, typeof(Image));
        StretchToParent(rewardPanel.GetComponent<RectTransform>());
        rewardPanel.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.06f, 0.96f);
        CreateText("Reward Title", rewardPanel.transform, "전투 승리!\n영구 능력 하나를 선택하세요", 42f,
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
