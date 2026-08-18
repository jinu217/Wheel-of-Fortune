using System.Collections.Generic;
using UnityEngine;

public class ShopCatalogUI : MonoBehaviour
{
    [Tooltip("표시할 상품 목록을 제공하는 상점 관리자입니다.")] [SerializeField] private ShopManager shopManager;
    [Tooltip("상품마다 생성할 상점 아이템 UI 프리팹입니다.")] [SerializeField] private ShopItemView itemViewPrefab;
    [Tooltip("생성된 상품 UI가 배치될 부모 Transform입니다.")] [SerializeField] private Transform itemContainer;

    private readonly List<ShopItemView> createdViews = new List<ShopItemView>();

    public void Refresh()
    {
        ClearViews();
        if (shopManager == null || itemContainer == null)
        {
            return;
        }

        foreach (int productIndex in shopManager.DisplayedProductIndexes)
        {
            ItemData item = shopManager.Products[productIndex];
            if (item == null)
            {
                continue;
            }

            ShopItemView view;
            if (itemViewPrefab != null)
            {
                view = Instantiate(itemViewPrefab, itemContainer);
            }
            else
            {
                GameObject card = new GameObject($"Shop Item {createdViews.Count + 1}", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button), typeof(ShopItemView));
                card.transform.SetParent(itemContainer, false);
                card.GetComponent<RectTransform>().sizeDelta = new Vector2(280f, 420f);
                card.GetComponent<UnityEngine.UI.Image>().color = new Color(0.16f, 0.18f, 0.24f, 1f);
                view = card.GetComponent<ShopItemView>();
            }
            view.Bind(shopManager, item, productIndex);
            createdViews.Add(view);
        }
    }

    private void ClearViews()
    {
        foreach (ShopItemView view in createdViews)
        {
            if (view != null)
            {
                Destroy(view.gameObject);
            }
        }

        createdViews.Clear();
    }
}
