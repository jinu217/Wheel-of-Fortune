using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InnPanelUI : MonoBehaviour
{
    private const int ServiceCount = 2;

    [Tooltip("여관 서비스를 실행할 InnManager입니다.")]
    [SerializeField] private InnManager innManager;
    [Tooltip("각 이미지 안에 표시할 서비스 이름 텍스트 2개입니다.")]
    [SerializeField] private TMP_Text[] nameTexts = new TMP_Text[ServiceCount];
    [Tooltip("각 이미지 안에 표시할 서비스 내용 텍스트 2개입니다.")]
    [SerializeField] private TMP_Text[] descriptionTexts = new TMP_Text[ServiceCount];
    [Tooltip("각 가격 버튼 안에 표시할 비용 텍스트 2개입니다.")]
    [SerializeField] private TMP_Text[] costTexts = new TMP_Text[ServiceCount];
    [Tooltip("충분한 휴식과 최후의 만찬의 가격 버튼 2개입니다.")]
    [SerializeField] private Button[] priceButtons = new Button[ServiceCount];
    [Tooltip("플레이어가 현재 보유한 골드를 표시할 텍스트입니다.")]
    [SerializeField] private TMP_Text ownedGoldText;
    [Tooltip("여관을 닫고 이벤트를 완료하는 나가기 버튼입니다.")]
    [SerializeField] private Button exitButton;
    [Tooltip("여관 종료를 처리할 상점·여관 흐름 관리자입니다.")]
    [SerializeField] private ShopInnFlowController flowController;
    private TMP_FontAsset runtimeFont;
    private bool restPurchased;
    private bool feastPurchased;
    private bool servicePurchased;

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
        ownedGoldText.text = $"보유 골드 : {innManager.CurrentGold}";
        priceButtons[0].interactable = !servicePurchased && !restPurchased;
        priceButtons[1].interactable = !servicePurchased && !feastPurchased;
    }

    public void OpenForVisit()
    {
        restPurchased = false;
        feastPurchased = false;
        servicePurchased = false;
        Refresh();
    }

    private void EnsureUI()
    {
        if (exitButton == null)
        {
            Transform found = transform.Find("Close Inn");
            if (found != null) exitButton = found.GetComponent<Button>();
        }
        if (!HasCompleteUI()) BuildRuntimeUI();
        for (int i = 0; i < ServiceCount; i++)
        {
            priceButtons[i].onClick.RemoveAllListeners();
        }
        priceButtons[0].onClick.AddListener(BuyRest);
        priceButtons[1].onClick.AddListener(BuyFinalFeast);
        flowController ??= FindFirstObjectByType<ShopInnFlowController>();
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(() => flowController?.CloseInn());
        }
    }

    private bool HasCompleteUI()
    {
        if (nameTexts == null || descriptionTexts == null || costTexts == null
            || nameTexts.Length < ServiceCount
            || descriptionTexts.Length < ServiceCount || costTexts.Length < ServiceCount
            || priceButtons == null || priceButtons.Length < ServiceCount || ownedGoldText == null) return false;
        for (int i = 0; i < ServiceCount; i++)
            if (nameTexts[i] == null || descriptionTexts[i] == null
                || costTexts[i] == null || priceButtons[i] == null) return false;
        return true;
    }

    private void BuyRest()
    {
        if (servicePurchased || restPurchased || innManager == null || !innManager.TryRest()) return;
        restPurchased = true;
        servicePurchased = true;
        priceButtons[0].interactable = false;
        priceButtons[1].interactable = false;
        ownedGoldText.text = $"보유 골드 : {innManager.CurrentGold}";
    }

    private void BuyFinalFeast()
    {
        if (servicePurchased || feastPurchased || innManager == null || !innManager.TryFinalFeast()) return;
        feastPurchased = true;
        servicePurchased = true;
        priceButtons[0].interactable = false;
        priceButtons[1].interactable = false;
        ownedGoldText.text = $"보유 골드 : {innManager.CurrentGold}";
    }

    private void BuildRuntimeUI()
    {
        if (runtimeFont == null)
        {
            TMP_Text sceneText = FindFirstObjectByType<TMP_Text>(FindObjectsInactive.Include);
            runtimeFont = sceneText == null ? null : sceneText.font;
        }
        nameTexts = new TMP_Text[ServiceCount];
        descriptionTexts = new TMP_Text[ServiceCount];
        costTexts = new TMP_Text[ServiceCount];
        priceButtons = new Button[ServiceCount];
        ownedGoldText = CreateText("Owned Gold Text", transform, new Vector2(0f, 300f), new Vector2(600f, 70f), 30f);
        for (int i = 0; i < ServiceCount; i++)
        {
            GameObject card = new GameObject($"Inn Service {i + 1}", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(transform, false);
            RectTransform rect = card.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2((i == 0 ? -1f : 1f) * 220f, 0f);
            rect.sizeDelta = new Vector2(380f, 430f);
            card.GetComponent<Image>().color = new Color(0.20f, 0.17f, 0.13f, 1f);
            nameTexts[i] = CreateText("Name Text", card.transform, new Vector2(0f, 125f), new Vector2(340f, 70f), 32f);
            descriptionTexts[i] = CreateText("Description Text", card.transform, Vector2.zero, new Vector2(340f, 170f), 25f);
            GameObject priceObject = new GameObject("Price Button", typeof(RectTransform), typeof(Image), typeof(Button));
            priceObject.transform.SetParent(card.transform, false);
            RectTransform priceRect = priceObject.GetComponent<RectTransform>();
            priceRect.anchorMin = priceRect.anchorMax = new Vector2(0.5f, 0.5f);
            priceRect.anchoredPosition = new Vector2(0f, -150f); priceRect.sizeDelta = new Vector2(260f, 70f);
            priceObject.GetComponent<Image>().color = new Color(0.42f, 0.30f, 0.16f, 1f);
            priceButtons[i] = priceObject.GetComponent<Button>();
            costTexts[i] = CreateText("Cost Text", priceObject.transform, Vector2.zero, new Vector2(240f, 60f), 28f);
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
        text.font = runtimeFont;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        return text;
    }
}
