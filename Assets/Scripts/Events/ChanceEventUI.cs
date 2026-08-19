using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChanceEventUI : MonoBehaviour
{
    private const int ChoiceCount = 3;

    [Tooltip("우연 발동 시 능력 선택지 위에 표시할 우연 패널입니다. 비어 있으면 자동 생성합니다.")]
    [SerializeField] private GameObject warningPanel;
    [Tooltip("자동 추첨 후보를 표시할 배경 이미지 3개입니다.")]
    [SerializeField] private Image[] choiceImages = new Image[ChoiceCount];
    [Tooltip("자동 추첨 후보의 효과 설명만 표시할 텍스트 3개입니다.")]
    [SerializeField] private TMP_Text[] choiceTexts = new TMP_Text[ChoiceCount];
    [Tooltip("후보 3개를 보여준 뒤 자동 선택하기까지 기다리는 시간입니다.")]
    [Min(0f)] [SerializeField] private float selectionDelay = 1.5f;
    [Tooltip("우연이 자동 선택된 뒤 인게임 씬으로 이동하기 전까지 결과를 보여주는 시간입니다.")]
    [Min(0f)] [SerializeField] private float resultDelay = 3f;

    public void ShowAndApply(ChanceSystemManager system, List<ChanceEffectDefinition> choices,
        ChanceEffectDefinition selected, int floorNumber, Action onCompleted)
    {
        EnsureUI();
        StartCoroutine(ShowRoutine(system, choices, selected, floorNumber, onCompleted));
    }

    public IEnumerator ShowAndApplyRoutine(ChanceSystemManager system,
        List<ChanceEffectDefinition> choices, ChanceEffectDefinition selected,
        int floorNumber, Action onCompleted)
    {
        EnsureUI();
        yield return ShowRoutine(system, choices, selected, floorNumber, onCompleted);
    }

    private IEnumerator ShowRoutine(ChanceSystemManager system, List<ChanceEffectDefinition> choices,
        ChanceEffectDefinition selected, int floorNumber, Action onCompleted)
    {
        warningPanel.SetActive(true);
        warningPanel.transform.SetAsLastSibling();
        for (int i = 0; i < ChoiceCount; i++)
        {
            bool valid = choices != null && i < choices.Count;
            choiceImages[i].gameObject.SetActive(valid);
            if (!valid) continue;
            choiceImages[i].color = new Color(0.16f, 0.18f, 0.24f, 1f);
            choiceTexts[i].text = choices[i].Description;
        }

        if (selectionDelay > 0f) yield return new WaitForSeconds(selectionDelay);
        for (int i = 0; i < choices.Count && i < ChoiceCount; i++)
        {
            bool isSelected = choices[i] == selected;
            choiceImages[i].color = isSelected
                ? new Color(0.72f, 0.52f, 0.10f, 1f) : new Color(0.08f, 0.09f, 0.12f, 0.82f);
            choiceTexts[i].color = isSelected ? Color.white : new Color(0.45f, 0.45f, 0.45f, 1f);
        }

        system.Apply(selected, floorNumber);
        if (resultDelay > 0f) yield return new WaitForSeconds(resultDelay);
        onCompleted?.Invoke();
    }

    private void EnsureUI()
    {
        if (warningPanel != null && choiceImages != null && choiceTexts != null
            && choiceImages.Length >= ChoiceCount && choiceTexts.Length >= ChoiceCount)
        {
            bool complete = true;
            for (int i = 0; i < ChoiceCount; i++)
                complete &= choiceImages[i] != null && choiceTexts[i] != null;
            if (complete) return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Chance Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        TMP_FontAsset font = FindRidibatangFont();
        warningPanel = CreateObject("Chance Panel", canvas.transform, typeof(Image));
        RectTransform panelRect = warningPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        warningPanel.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.08f, 0.97f);
        CreateText("Chance Title", warningPanel.transform, "우연 - 운명의 장난질", 48f,
            new Vector2(0f, 330f), new Vector2(1200f, 100f), font);

        choiceImages = new Image[ChoiceCount];
        choiceTexts = new TMP_Text[ChoiceCount];
        for (int i = 0; i < ChoiceCount; i++)
        {
            GameObject card = CreateObject($"Chance Choice {i + 1}", warningPanel.transform, typeof(Image));
            RectTransform rect = card.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2((i - 1) * 390f, -20f);
            rect.sizeDelta = new Vector2(340f, 390f);
            choiceImages[i] = card.GetComponent<Image>();
            choiceImages[i].raycastTarget = false;
            choiceTexts[i] = CreateText("Chance Text", card.transform, string.Empty, 25f,
                Vector2.zero, new Vector2(300f, 340f), font);
            choiceTexts[i].raycastTarget = false;
        }
        warningPanel.SetActive(false);
    }

    private static TMP_FontAsset FindRidibatangFont()
    {
        TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (TMP_Text text in texts)
            if (text.font != null && text.font.name.IndexOf("RIDIBatang", StringComparison.OrdinalIgnoreCase) >= 0)
                return text.font;
        return null;
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, float size,
        Vector2 position, Vector2 dimensions, TMP_FontAsset font)
    {
        GameObject obj = CreateObject(name, parent, typeof(TextMeshProUGUI));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        TMP_Text text = obj.GetComponent<TMP_Text>();
        text.text = value;
        text.font = font;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        return text;
    }

    private static GameObject CreateObject(string name, Transform parent, params Type[] components)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        foreach (Type component in components) obj.AddComponent(component);
        return obj;
    }
}
