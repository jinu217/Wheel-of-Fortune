using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InnPanelUI : MonoBehaviour
{
    private const int ServiceCount = 2;

    [Tooltip("여관 서비스를 실행할 InnManager입니다.")]
    [SerializeField] private InnManager innManager;
    [Tooltip("클릭 가능한 여관 서비스 배경 이미지 2개입니다.")]
    [SerializeField] private Image[] serviceImages = new Image[ServiceCount];
    [Tooltip("각 이미지 안에 표시할 서비스 이름 텍스트 2개입니다.")]
    [SerializeField] private TMP_Text[] nameTexts = new TMP_Text[ServiceCount];
    [Tooltip("각 이미지 안에 표시할 서비스 내용 텍스트 2개입니다.")]
    [SerializeField] private TMP_Text[] descriptionTexts = new TMP_Text[ServiceCount];
    [Tooltip("각 이미지 안에 표시할 비용 텍스트 2개입니다.")]
    [SerializeField] private TMP_Text[] costTexts = new TMP_Text[ServiceCount];
    [Tooltip("여관 UI에 사용할 폰트입니다. 비어 있으면 현재 씬의 neodgm UI 폰트를 사용합니다.")]
    [SerializeField] private TMP_FontAsset innFont;

    private readonly Button[] serviceButtons = new Button[ServiceCount];
    private bool restPurchased;
    private bool feastPurchased;

    public void SetManager(InnManager manager)
    {
        innManager = manager;
    }

    public void Refresh()
    {
        if (innManager == null)
        {
            Debug.LogError("InnManager가 연결되지 않았습니다.", this);
            return;
        }

        EnsureUI();
        nameTexts[0].text = "충분한 휴식";
        descriptionTexts[0].text = "총 HP의 절반 회복\n(소수점 올림)";
        costTexts[0].text = $"{innManager.RestPrice} 골드";
        nameTexts[1].text = "최후의 만찬";
        descriptionTexts[1].text = $"{innManager.FeastDurationTurns}턴 동안\n공격력 +{innManager.FeastAttackBonus}, 방어력 +{innManager.FeastDefenseBonus}";
        costTexts[1].text = $"{innManager.FinalFeastPrice} 골드";
        serviceButtons[0].interactable = !restPurchased;
        serviceButtons[1].interactable = !feastPurchased;
    }

    public void OpenForVisit()
    {
        restPurchased = false;
        feastPurchased = false;
        Refresh();
    }

    private void EnsureUI()
    {
        if (!HasCompleteUI()) BuildRuntimeUI();
        for (int i = 0; i < ServiceCount; i++)
        {
            serviceButtons[i] = serviceImages[i].GetComponent<Button>();
            serviceButtons[i] ??= serviceImages[i].gameObject.AddComponent<Button>();
            serviceButtons[i].targetGraphic = serviceImages[i];
            serviceButtons[i].onClick.RemoveAllListeners();
        }
        serviceButtons[0].onClick.AddListener(BuyRest);
        serviceButtons[1].onClick.AddListener(BuyFinalFeast);
    }

    private bool HasCompleteUI()
    {
        if (serviceImages == null || nameTexts == null || descriptionTexts == null || costTexts == null
            || serviceImages.Length < ServiceCount || nameTexts.Length < ServiceCount
            || descriptionTexts.Length < ServiceCount || costTexts.Length < ServiceCount) return false;
        for (int i = 0; i < ServiceCount; i++)
            if (serviceImages[i] == null || nameTexts[i] == null || descriptionTexts[i] == null || costTexts[i] == null) return false;
        return true;
    }

    private void BuyRest()
    {
        if (restPurchased || innManager == null || !innManager.TryRest()) return;
        restPurchased = true;
        serviceButtons[0].interactable = false;
    }

    private void BuyFinalFeast()
    {
        if (feastPurchased || innManager == null || !innManager.TryFinalFeast()) return;
        feastPurchased = true;
        serviceButtons[1].interactable = false;
    }

    private void BuildRuntimeUI()
    {
        if (innFont == null)
        {
            TMP_Text sceneText = FindFirstObjectByType<TMP_Text>(FindObjectsInactive.Include);
            innFont = sceneText == null ? null : sceneText.font;
        }
        serviceImages = new Image[ServiceCount];
        nameTexts = new TMP_Text[ServiceCount];
        descriptionTexts = new TMP_Text[ServiceCount];
        costTexts = new TMP_Text[ServiceCount];
        for (int i = 0; i < ServiceCount; i++)
        {
            GameObject card = new GameObject($"Inn Service {i + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(transform, false);
            RectTransform rect = card.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2((i == 0 ? -1f : 1f) * 220f, 0f);
            rect.sizeDelta = new Vector2(380f, 430f);
            serviceImages[i] = card.GetComponent<Image>();
            serviceImages[i].color = new Color(0.20f, 0.17f, 0.13f, 1f);
            nameTexts[i] = CreateText("Name Text", card.transform, new Vector2(0f, 125f), new Vector2(340f, 70f), 32f);
            descriptionTexts[i] = CreateText("Description Text", card.transform, Vector2.zero, new Vector2(340f, 170f), 25f);
            costTexts[i] = CreateText("Cost Text", card.transform, new Vector2(0f, -140f), new Vector2(340f, 60f), 28f);
        }
    }

    private TMP_Text CreateText(string objectName, Transform parent, Vector2 position, Vector2 size, float fontSize)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.font = innFont;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        return text;
    }
}
