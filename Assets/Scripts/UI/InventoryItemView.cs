using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemView : MonoBehaviour
{
    [Tooltip("소모품 이름을 표시할 텍스트입니다.")] [SerializeField] private TMP_Text itemNameText;
    [Tooltip("소모품 효과와 수치를 표시할 텍스트입니다.")] [SerializeField] private TMP_Text effectText;
    [Tooltip("아이템을 사용하고 인벤토리에서 제거할 버튼입니다.")] [SerializeField] private Button useButton;

    private PlayerInventoryManager inventory;
    private int itemIndex;

    public void Bind(PlayerInventoryManager playerInventory, ItemData item, int index)
    {
        inventory = playerInventory;
        itemIndex = index;

        if (itemNameText != null)
        {
            itemNameText.text = item.ItemName;
        }

        if (effectText != null)
        {
            effectText.text = $"{item.Effect} +{item.Value}";
        }

        if (useButton != null)
        {
            useButton.onClick.RemoveListener(UseItem);
            useButton.onClick.AddListener(UseItem);
        }
    }

    private void UseItem()
    {
        if (useButton != null)
        {
            useButton.interactable = false;
        }

        inventory?.UseItem(itemIndex);
    }

    private void OnDestroy()
    {
        if (useButton != null)
        {
            useButton.onClick.RemoveListener(UseItem);
        }
    }
}
