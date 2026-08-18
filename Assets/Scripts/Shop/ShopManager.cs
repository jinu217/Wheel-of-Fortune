using System;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [Tooltip("구매 골드를 차감할 플레이어 능력치 관리자입니다.")]
    [SerializeField] private PlayerStatManager playerStats;
    [Tooltip("구매한 아이템을 추가할 플레이어 인벤토리입니다.")]
    [SerializeField] private PlayerInventoryManager inventory;
    [Tooltip("상점에서 무작위로 추첨할 전체 ItemData 목록입니다.")]
    [SerializeField] private List<ItemData> products = new List<ItemData>();
    private readonly HashSet<int> soldProductIndexes = new HashSet<int>();
    private readonly List<int> displayedProductIndexes = new List<int>();

    public IReadOnlyList<ItemData> Products => products;
    public IReadOnlyList<int> DisplayedProductIndexes => displayedProductIndexes;
    public event Action<ItemData> ItemPurchased;

    public void SetPlayerData(PlayerStatManager stats, PlayerInventoryManager playerInventory)
    {
        playerStats = stats;
        inventory = playerInventory;
    }

    public bool TryBuy(int productIndex)
    {
        if (productIndex < 0 || productIndex >= products.Count || inventory == null || playerStats == null
            || soldProductIndexes.Contains(productIndex) || !displayedProductIndexes.Contains(productIndex))
        {
            return false;
        }

        ItemData item = products[productIndex];
        if (item == null || inventory.IsFull || !playerStats.TrySpendCoins(item.Price))
        {
            return false;
        }

        if (!inventory.TryAddItem(item))
        {
            playerStats.AddCoins(item.Price);
            return false;
        }

        soldProductIndexes.Add(productIndex);
        ItemPurchased?.Invoke(item);
        return true;
    }

    public bool IsSoldOut(int productIndex)
    {
        return soldProductIndexes.Contains(productIndex);
    }

    public void RefreshRandomStock()
    {
        soldProductIndexes.Clear();
        displayedProductIndexes.Clear();

        List<int> candidates = new List<int>();
        for (int i = 0; i < products.Count; i++)
        {
            if (products[i] != null) candidates.Add(i);
        }

        int displayCount = Mathf.Min(3, candidates.Count);
        for (int i = 0; i < displayCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, candidates.Count);
            displayedProductIndexes.Add(candidates[randomIndex]);
            candidates.RemoveAt(randomIndex);
        }
    }
}
