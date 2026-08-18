using System.Collections.Generic;
using UnityEngine;

public class InventoryUIController : MonoBehaviour
{
    [Tooltip("보유 아이템마다 생성할 인벤토리 슬롯 프리팹입니다.")] [SerializeField] private InventoryItemView itemViewPrefab;
    [Tooltip("생성된 인벤토리 슬롯이 배치될 부모 Transform입니다.")] [SerializeField] private Transform itemContainer;

    private readonly List<InventoryItemView> createdViews = new List<InventoryItemView>();
    private PlayerInventoryManager inventory;

    private void Start()
    {
        inventory = GameSessionManager.Instance == null
            ? null
            : GameSessionManager.Instance.PlayerInventory;

        if (inventory != null)
        {
            inventory.InventoryChanged += Refresh;
        }

        Refresh();
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.InventoryChanged -= Refresh;
        }
    }

    public void Refresh()
    {
        ClearViews();
        if (inventory == null || itemViewPrefab == null || itemContainer == null)
        {
            return;
        }

        for (int i = 0; i < inventory.Items.Count; i++)
        {
            ItemData item = inventory.Items[i];
            if (item == null)
            {
                continue;
            }

            InventoryItemView view = Instantiate(itemViewPrefab, itemContainer);
            view.Bind(inventory, item, i);
            createdViews.Add(view);
        }
    }

    private void ClearViews()
    {
        foreach (InventoryItemView view in createdViews)
        {
            if (view != null)
            {
                Destroy(view.gameObject);
            }
        }

        createdViews.Clear();
    }
}
