using System;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [Tooltip("구매 골드를 차감할 플레이어 능력치 관리자입니다.")]
    [SerializeField] private PlayerStatManager playerStats;
    [Tooltip("구매한 아이템을 추가할 플레이어 인벤토리입니다.")]
    [SerializeField] private PlayerInventoryManager inventory;
    [Tooltip("상점에서 사용할 모든 아이템이 등록된 데이터베이스입니다.")]
    [SerializeField] private ItemDatabase itemDatabase;
    private readonly HashSet<int> soldProductIndexes = new HashSet<int>();
    private readonly List<int> displayedProductIndexes = new List<int>();

    public ItemDatabase Database => itemDatabase;
    public IReadOnlyList<ItemData> Products => itemDatabase == null ? System.Array.Empty<ItemData>() : itemDatabase.Items;
    public IReadOnlyList<int> DisplayedProductIndexes => displayedProductIndexes;
    public int CurrentGold => playerStats == null ? 0 : playerStats.Coin;
    public event Action<ItemData> ItemPurchased;

    public int GetPrice(ItemData item)
    {
        if (item == null) return 0;
        PlayerAbilityManager abilities = GameSessionManager.Instance == null ? null : GameSessionManager.Instance.PlayerAbilities;
        return abilities != null && abilities.Has(PassiveAbilityType.ShopDiscount20)
            ? Mathf.CeilToInt(item.Price * 0.8f) : item.Price;
    }

    public void SetPlayerData(PlayerStatManager stats, PlayerInventoryManager playerInventory)
    {
        playerStats = stats;
        inventory = playerInventory;
    }

    public bool TryBuy(int productIndex)
    {
        IReadOnlyList<ItemData> products = Products;
        if (productIndex < 0 || productIndex >= products.Count || inventory == null || playerStats == null
            || soldProductIndexes.Contains(productIndex) || !displayedProductIndexes.Contains(productIndex))
        {
            return false;
        }

        ItemData item = products[productIndex];
        int price = GetPrice(item);
        if (item == null || inventory.IsFull || !playerStats.TrySpendCoins(price))
        {
            return false;
        }

        if (!inventory.TryAddItem(item))
        {
            playerStats.AddCoins(price);
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

        if (itemDatabase == null)
        {
            Debug.LogError("ShopManager에 Assets/Data/Items/ItemDatabase.asset을 연결해야 합니다.", this);
            return;
        }

        IReadOnlyList<ItemData> products = Products;
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
