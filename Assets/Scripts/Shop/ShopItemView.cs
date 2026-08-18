using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemView : MonoBehaviour
{
    [Tooltip("클릭해서 구매하는 아이템 이미지입니다.")] [SerializeField] private Image itemImage;
    [Tooltip("아이템 이름과 효과를 이름 줄바꿈 효과 형식으로 표시합니다.")] [SerializeField] private TMP_Text itemDescriptionText;
    [Tooltip("상품 가격을 표시할 텍스트입니다.")] [SerializeField] private TMP_Text priceText;
    private ShopManager shopManager;
    private int productIndex;
    private Button buyButton;

    public void Bind(ShopManager manager, ItemData item, int index)
    {
        shopManager = manager;
        productIndex = index;
        EnsureRuntimeUI();
        buyButton = itemImage == null ? null : itemImage.GetComponent<Button>();
        if (itemImage != null) buyButton ??= itemImage.gameObject.AddComponent<Button>();
        if (buyButton != null) buyButton.targetGraphic = itemImage;

        if (itemImage != null)
        {
            itemImage.gameObject.SetActive(true);
            itemImage.enabled = true;
            itemImage.sprite = null;
            itemImage.overrideSprite = null;
            itemImage.sprite = item.ItemImage;
            itemImage.overrideSprite = item.ItemImage;
            itemImage.color = Color.white;
            itemImage.canvasRenderer.SetAlpha(1f);
            itemImage.type = Image.Type.Simple;
            itemImage.preserveAspect = true;
            if (item.ItemImage == null)
                Debug.LogError($"{item.ItemName} ItemData에 Item Image가 연결되지 않았습니다.", item);
        }

        if (itemDescriptionText != null)
            itemDescriptionText.text = $"{item.ItemName}\n{GetEffectDescription(item)}";

        if (priceText != null)
        {
            priceText.text = $"{manager.GetPrice(item)} 골드";
        }

        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(Buy);
            buyButton.onClick.AddListener(Buy);
            buyButton.transition = Selectable.Transition.None;
            buyButton.interactable = !manager.IsSoldOut(index);
        }
    }

    private void EnsureRuntimeUI()
    {
        TMP_FontAsset font = FindFirstObjectByType<TMP_Text>(FindObjectsInactive.Include)?.font;
        if (itemImage == null)
        {
            itemImage = CreateImage("Item Image", new Vector2(0f, 90f), new Vector2(150f, 150f));
        }
        if (itemDescriptionText == null) itemDescriptionText = CreateText("Item Description", font, new Vector2(0f, -65f), new Vector2(250f, 125f), 24f);
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

    private static string GetEffectDescription(ItemData item)
    {
        switch (item.Effect)
        {
            case ItemEffectType.Heal: return $"사용 시 HP {item.Value} 회복";
            case ItemEffectType.AttackBuff: return $"1턴 동안 공격력 +{item.Value}";
            case ItemEffectType.DefenseBuff: return $"방어력 +{item.Value}";
            case ItemEffectType.AttackRouletteCoin: return "1회용 공격 코인";
            case ItemEffectType.DefenseRouletteCoin: return "1회용 방어 코인";
            case ItemEffectType.DodgeNextAttack: return "다음 공격 1회 회피";
            case ItemEffectType.DamageEnemy: return $"적에게 피해 {item.Value}";
            case ItemEffectType.DelayEnemyTurn: return "적 행동 1턴 지연";
            case ItemEffectType.Barrier: return $"방어 {item.Value} 획득";
            case ItemEffectType.Mushroom: return "3턴 공격 +3 / 방어 -1";
            case ItemEffectType.FateCoin: return "행운 75% / 불행 25%";
            case ItemEffectType.DoubleBattleGold: return "전투 골드 2배";
            default: return item.Effect.ToString();
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
