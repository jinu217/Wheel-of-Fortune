using UnityEngine;

public class ShopInnFlowController : MonoBehaviour
{
    [Tooltip("상점·여관 이벤트 시작과 완료를 전달할 이벤트 관리자입니다.")] [SerializeField] private InGameEventCoordinator eventCoordinator;
    [Tooltip("상품 재고를 관리하는 ShopManager입니다.")] [SerializeField] private ShopManager shopManager;
    [Tooltip("상점 상품 UI를 생성하는 컴포넌트입니다.")] [SerializeField] private ShopCatalogUI shopCatalog;
    [Tooltip("상점 이벤트에서 먼저 열리는 패널입니다.")] [SerializeField] private GameObject shopPanel;
    [Tooltip("상점을 닫은 뒤 자동으로 열리는 여관 패널입니다.")] [SerializeField] private GameObject innPanel;
    [Tooltip("여관의 두 서비스 이미지와 텍스트를 관리하는 UI입니다. 비어 있으면 여관 패널에 자동 추가합니다.")]
    [SerializeField] private InnPanelUI innPanelUI;
    [Tooltip("여관 회복과 최후의 만찬 효과를 처리하는 관리자입니다.")]
    [SerializeField] private InnManager innManager;

    private void OnEnable()
    {
        if (eventCoordinator != null)
        {
            eventCoordinator.ShopOrInnOpened += OpenShop;
        }
    }

    private void OnDisable()
    {
        if (eventCoordinator != null)
        {
            eventCoordinator.ShopOrInnOpened -= OpenShop;
        }
    }

    public void OpenShop()
    {
        shopManager ??= FindFirstObjectByType<ShopManager>();
        shopManager?.RefreshRandomStock();
        shopPanel?.SetActive(true);
        innPanel?.SetActive(false);
        shopCatalog?.Refresh();
    }

    public void CloseShopAndOpenInn()
    {
        shopPanel?.SetActive(false);
        innPanel?.SetActive(true);
        if (innPanel != null)
        {
            innManager ??= FindFirstObjectByType<InnManager>();
            innPanelUI ??= innPanel.GetComponent<InnPanelUI>();
            innPanelUI ??= innPanel.AddComponent<InnPanelUI>();
            innPanelUI.SetManager(innManager);
            innPanelUI.OpenForVisit();
        }
    }

    public void CloseInn()
    {
        innPanel?.SetActive(false);
        eventCoordinator?.CloseShopOrInn();
    }
}
