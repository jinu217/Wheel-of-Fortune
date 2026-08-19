using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopCatalogUI : MonoBehaviour
{
    [Tooltip("표시할 상품 목록을 제공하는 상점 관리자입니다.")] [SerializeField] private ShopManager shopManager;
    [Tooltip("상점에 고정 배치된 아이템 UI 오브젝트 3개입니다.")] [SerializeField] private ShopItemView[] itemViews = new ShopItemView[3];
    [Tooltip("각 상품 구매 완료 시 아이템 위에 표시할 이미지 3개입니다. 상품 순서대로 연결합니다.")]
    [SerializeField] private Image[] purchaseCompletedImages = new Image[3];
    [Tooltip("플레이어가 현재 보유한 골드를 표시할 텍스트입니다.")] [SerializeField] private TMP_Text ownedGoldText;
    [Tooltip("상점을 닫고 여관을 여는 나가기 버튼입니다.")] [SerializeField] private Button exitButton;
    [Tooltip("상점 종료 후 여관을 열 흐름 관리자입니다.")] [SerializeField] private ShopInnFlowController flowController;

    private void OnEnable()
    {
        if (shopManager != null) shopManager.ItemPurchased += HandlePurchased;
        BindExitButton();
        RefreshGold();
    }

    private void OnDisable()
    {
        if (shopManager != null) shopManager.ItemPurchased -= HandlePurchased;
    }

    public void Refresh()
    {
        if (shopManager == null || itemViews == null || itemViews.Length < 3)
        {
            Debug.LogError("ShopCatalogUI의 Item Views에 상점 아이템 오브젝트 3개를 연결해야 합니다.", this);
            return;
        }
        BindExitButton();
        EnsureGoldText();
        RefreshGold();

        for (int slot = 0; slot < itemViews.Length; slot++)
        {
            ShopItemView view = itemViews[slot];
            if (view == null) continue;
            bool hasProduct = slot < shopManager.DisplayedProductIndexes.Count;
            view.gameObject.SetActive(hasProduct);
            if (!hasProduct) continue;
            int productIndex = shopManager.DisplayedProductIndexes[slot];
            ItemData item = shopManager.Products[productIndex];
            Image completedImage = purchaseCompletedImages != null && slot < purchaseCompletedImages.Length
                ? purchaseCompletedImages[slot]
                : null;
            if (item != null) view.Bind(shopManager, item, productIndex, completedImage);
        }
    }

    private void HandlePurchased(ItemData item) { RefreshGold(); }

    private void RefreshGold()
    {
        if (ownedGoldText != null) ownedGoldText.text = $"보유 골드 : {(shopManager == null ? 0 : shopManager.CurrentGold)}";
    }

    private void EnsureGoldText()
    {
        if (ownedGoldText != null) return;
        GameObject value = new GameObject("Owned Gold Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        value.transform.SetParent(transform, false);
        RectTransform rect = value.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 330f); rect.sizeDelta = new Vector2(600f, 70f);
        ownedGoldText = value.GetComponent<TMP_Text>();
        TMP_Text sceneText = FindFirstObjectByType<TMP_Text>(FindObjectsInactive.Include);
        ownedGoldText.font = sceneText == null ? null : sceneText.font;
        ownedGoldText.fontSize = 30f; ownedGoldText.alignment = TextAlignmentOptions.Center;
        ownedGoldText.color = Color.white;
    }

    private void BindExitButton()
    {
        flowController ??= FindFirstObjectByType<ShopInnFlowController>();
        if (exitButton == null)
        {
            Transform found = transform.Find("Close Shop");
            if (found != null) exitButton = found.GetComponent<Button>();
        }
        if (exitButton == null) return;
        exitButton.onClick.RemoveAllListeners();
        exitButton.onClick.AddListener(() => flowController?.CloseShopAndOpenInn());
    }

}
