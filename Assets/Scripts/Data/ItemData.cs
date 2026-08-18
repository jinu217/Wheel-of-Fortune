using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Game Data/Item")]
public class ItemData : ScriptableObject
{
    [Tooltip("아이템을 구분하는 중복되지 않는 ID입니다.")]
    [SerializeField] private string itemId;
    [Tooltip("상점과 인벤토리에 표시할 아이템 이름입니다.")]
    [SerializeField] private string itemName;
    [Tooltip("아이템 사용 시 적용할 효과입니다.")]
    [SerializeField] private ItemEffectType effect;
    [Tooltip("회복, 버프 또는 코인 지급에 사용할 수치입니다.")]
    [SerializeField] private int value;
    [Tooltip("상점에서 아이템을 구매할 때 필요한 골드입니다.")]
    [Min(0)] [SerializeField] private int price;
    [Tooltip("상점과 전투 인벤토리 슬롯에 표시할 아이템 이미지입니다.")]
    [SerializeField] private Sprite itemImage;

    public string ItemId => itemId;
    public string ItemName => itemName;
    public ItemEffectType Effect => effect;
    public int Value => value;
    public int Price => price;
    public Sprite ItemImage => itemImage;
}

public enum ItemEffectType
{
    Heal,
    AttackBuff,
    DefenseBuff,
    AttackRouletteCoin,
    DefenseRouletteCoin
}
