using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemView : MonoBehaviour
{
    [Tooltip("상품 하나의 클릭 가능한 배경 이미지입니다.")] [SerializeField] private Image backgroundImage;
    [Tooltip("배경 이미지 안에 표시할 아이템 이미지입니다.")] [SerializeField] private Image itemImage;
    [Tooltip("상품 이름을 표시할 텍스트입니다.")] [SerializeField] private TMP_Text itemNameText;
    [Tooltip("상품 효과와 수치를 표시할 텍스트입니다.")] [SerializeField] private TMP_Text effectText;
    [Tooltip("상품 가격을 표시할 텍스트입니다.")] [SerializeField] private TMP_Text priceText;
    private ShopManager shopManager;
    private int productIndex;
    private Button buyButton;

    public void Bind(ShopManager manager, ItemData item, int index)
    {
        shopManager = manager;
        productIndex = index;
        EnsureRuntimeUI();
        backgroundImage ??= GetComponent<Image>();
        if (backgroundImage != null)
        {
            buyButton = backgroundImage.GetComponent<Button>();
            buyButton ??= backgroundImage.gameObject.AddComponent<Button>();
            buyButton.targetGraphic = backgroundImage;
        }

        if (itemImage != null)
        {
            itemImage.sprite = item.ItemImage;
            itemImage.preserveAspect = true;
            itemImage.enabled = item.ItemImage != null;
        }

        if (itemNameText != null)
        {
            itemNameText.text = item.ItemName;
        }

        if (effectText != null)
        {
            effectText.text = $"{GetEffectName(item.Effect)} +{item.Value}";
        }

        if (priceText != null)
        {
            priceText.text = $"{item.Price} 골드";
        }

        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(Buy);
            buyButton.onClick.AddListener(Buy);
            buyButton.interactable = !manager.IsSoldOut(index);
        }
    }

    private void EnsureRuntimeUI()
    {
        backgroundImage ??= GetComponent<Image>();
        TMP_FontAsset font = FindFirstObjectByType<TMP_Text>(FindObjectsInactive.Include)?.font;
        if (itemImage == null)
        {
            itemImage = CreateImage("Item Image", new Vector2(0f, 90f), new Vector2(150f, 150f));
        }
        if (itemNameText == null) itemNameText = CreateText("Item Name", font, new Vector2(0f, -15f), new Vector2(250f, 55f), 27f);
        if (effectText == null) effectText = CreateText("Effect", font, new Vector2(0f, -85f), new Vector2(250f, 70f), 22f);
        if (priceText == null) priceText = CreateText("Price", font, new Vector2(0f, -155f), new Vector2(250f, 50f), 24f);
    }

    private Image CreateImage(string objectName, Vector2 position, Vector2 size)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        child.transform.SetParent(transform, false);
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return child.GetComponent<Image>();
    }

    private TMP_Text CreateText(string objectName, TMP_FontAsset font, Vector2 position, Vector2 size, float fontSize)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        child.transform.SetParent(transform, false);
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TMP_Text text = child.GetComponent<TMP_Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        return text;
    }

    private static string GetEffectName(ItemEffectType effect)
    {
        switch (effect)
        {
            case ItemEffectType.Heal: return "HP 회복";
            case ItemEffectType.AttackBuff: return "공격력 증가";
            case ItemEffectType.DefenseBuff: return "방어력 증가";
            case ItemEffectType.AttackRouletteCoin: return "공격 코인";
            case ItemEffectType.DefenseRouletteCoin: return "방어 코인";
            default: return effect.ToString();
        }
    }

    private void Buy()
    {
        if (shopManager.TryBuy(productIndex) && buyButton != null)
        {
            buyButton.interactable = false;
        }
    }

    private void OnDestroy()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(Buy);
        }
    }
}
