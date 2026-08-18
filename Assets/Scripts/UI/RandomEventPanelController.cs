using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RandomEventPanelController : MonoBehaviour
{
    [Header("이벤트 관리자")]
    [Tooltip("랜덤 이벤트의 추첨과 효과 적용을 담당합니다.")] [SerializeField] private RandomEventManager manager;

    [Header("보물상자")]
    [Tooltip("보물상자 이벤트 전체 패널입니다.")] [SerializeField] private GameObject treasurePanel;
    [Tooltip("클릭할 보물상자 이미지 버튼입니다.")] [SerializeField] private Button treasureChestButton;
    [Tooltip("보물상자 확률과 결과 안내입니다.")] [SerializeField] private TMP_Text treasureDescriptionText;

    [Header("주술사")]
    [Tooltip("주술사 이벤트 전체 패널입니다.")] [SerializeField] private GameObject shamanPanel;
    [Tooltip("20골드로 능력을 구매하는 이미지 버튼입니다.")] [SerializeField] private Button shamanPurchaseButton;
    [Tooltip("비용과 구매 결과 안내입니다.")] [SerializeField] private TMP_Text shamanDescriptionText;

    [Header("인과율의 신전")]
    [Tooltip("인과율의 신전 전체 패널입니다.")] [SerializeField] private GameObject causalityPanel;
    [Tooltip("우연과 능력을 삭제하는 왼쪽 이미지 버튼입니다.")] [SerializeField] private Button causalityDeleteButton;
    [Tooltip("우연과 능력을 획득하는 오른쪽 이미지 버튼입니다.")] [SerializeField] private Button causalityGainButton;
    [Tooltip("동적으로 생성되는 세부 선택지의 부모입니다.")] [SerializeField] private RectTransform causalityChoiceContainer;
    [Tooltip("세부 선택지를 만들 때 복제할 비활성 버튼입니다.")] [SerializeField] private Button causalityChoiceButtonTemplate;

    [Header("생명의 샘")]
    [Tooltip("생명의 샘 전체 패널입니다.")] [SerializeField] private GameObject lifeSpringPanel;
    [Tooltip("생명의 샘 확인 버튼입니다.")] [SerializeField] private Button lifeSpringConfirmButton;
    [Tooltip("회복 결과 안내입니다.")] [SerializeField] private TMP_Text lifeSpringDescriptionText;

    [Header("가시 덤불")]
    [Tooltip("가시 덤불 전체 패널입니다.")] [SerializeField] private GameObject thornBushPanel;
    [Tooltip("가시 덤불 확인 버튼입니다.")] [SerializeField] private Button thornBushConfirmButton;
    [Tooltip("피해와 아이템 획득 결과 안내입니다.")] [SerializeField] private TMP_Text thornBushDescriptionText;
    [Tooltip("획득한 아이템 이미지입니다.")] [SerializeField] private Image thornBushItemImage;

    private readonly List<Button> generatedChoices = new List<Button>();

    public void Configure(RandomEventManager randomEventManager, TMP_FontAsset unusedFont, Transform unusedParent)
    {
        if (manager != null) manager.RandomEventSelected -= Open;
        manager = randomEventManager;
        BindButtons();
        if (manager != null) manager.RandomEventSelected += Open;
        CloseAll();
    }

    private void OnDestroy() { if (manager != null) manager.RandomEventSelected -= Open; }

    private void BindButtons()
    {
        Bind(treasureChestButton, OpenTreasure);
        Bind(shamanPurchaseButton, PurchaseShamanAbility);
        Bind(causalityDeleteButton, ShowChanceRemovalChoices);
        Bind(causalityGainButton, ShowAbilityGainChoices);
        Bind(lifeSpringConfirmButton, () => lifeSpringPanel.SetActive(false));
        Bind(thornBushConfirmButton, () => thornBushPanel.SetActive(false));
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void Open(RandomEventType type)
    {
        CloseAll();
        GameObject panel = GetPanel(type);
        if (panel == null)
        {
            Debug.LogError($"{type} 전용 패널이 인스펙터에 연결되지 않았습니다.", this);
            return;
        }
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        if (type == RandomEventType.LifeSpring)
        {
            manager.UseLifeSpring();
            if (lifeSpringDescriptionText != null) lifeSpringDescriptionText.text = "HP가 전부 회복되었습니다.";
        }
        else if (type == RandomEventType.ThornBush)
        {
            bool received = manager.EnterThornBush();
            if (thornBushDescriptionText != null)
                thornBushDescriptionText.text = received ? "HP가 5 감소하고 무작위 아이템을 획득했습니다."
                    : "HP가 5 감소했습니다.\n인벤토리가 가득 차 아이템은 획득하지 못했습니다.";
            RefreshThornItemImage(received);
        }
    }

    private GameObject GetPanel(RandomEventType type)
    {
        switch (type)
        {
            case RandomEventType.TreasureChest: return treasurePanel;
            case RandomEventType.Shaman: return shamanPanel;
            case RandomEventType.CausalityShrine: return causalityPanel;
            case RandomEventType.LifeSpring: return lifeSpringPanel;
            case RandomEventType.ThornBush: return thornBushPanel;
            default: return null;
        }
    }

    private void OpenTreasure() { manager.OpenTreasureChest(); treasurePanel.SetActive(false); }

    private void PurchaseShamanAbility()
    {
        bool purchased = manager.TryUseShaman();
        if (shamanDescriptionText != null)
            shamanDescriptionText.text = purchased ? "무작위 능력을 획득했습니다." : "골드가 부족합니다. (필요 골드: 20)";
        if (purchased) shamanPanel.SetActive(false);
    }

    private void ShowChanceRemovalChoices()
    {
        ClearChoices();
        ChanceSystemManager chance = GameSessionManager.Instance.ChanceSystem;
        var owned = new List<ChanceEffectType>(chance.AcquiredChanceEffects);
        if (owned.Count == 0) { CreateChoice("삭제할 우연이 없습니다.", null); return; }
        foreach (ChanceEffectType value in owned)
        {
            ChanceEffectType selected = value;
            CreateChoice(chance.GetDefinition(value)?.Name ?? value.ToString(), () =>
            {
                chance.RemoveChanceEffect(selected);
                GameSessionManager.Instance.PlayerAbilities.RemoveRandomAbility();
                FinishCausality();
            });
        }
    }

    private void ShowAbilityGainChoices()
    {
        ClearChoices();
        GameSessionManager session = GameSessionManager.Instance;
        int floor = session.CurrentEventPosition.y + 1;
        session.ChanceSystem.AcquireRandomChance(floor);
        foreach (AbilityDefinition value in session.PlayerAbilities.GenerateChoices(floor, 3))
        {
            AbilityDefinition selected = value;
            CreateChoice($"{value.Name}\n{value.Description}", () =>
            {
                session.PlayerAbilities.Acquire(selected);
                FinishCausality();
            });
        }
    }

    private void CreateChoice(string label, UnityEngine.Events.UnityAction action)
    {
        if (causalityChoiceButtonTemplate == null || causalityChoiceContainer == null) return;
        Button choice = Instantiate(causalityChoiceButtonTemplate, causalityChoiceContainer);
        choice.gameObject.SetActive(true);
        TMP_Text text = choice.GetComponentInChildren<TMP_Text>(true);
        if (text != null) text.text = label;
        choice.onClick.RemoveAllListeners();
        choice.interactable = action != null;
        if (action != null) choice.onClick.AddListener(action);
        generatedChoices.Add(choice);
    }

    private void FinishCausality()
    {
        manager.CompleteCausalityShrine();
        causalityPanel.SetActive(false);
        ClearChoices();
    }

    private void ClearChoices()
    {
        foreach (Button choice in generatedChoices) if (choice != null) Destroy(choice.gameObject);
        generatedChoices.Clear();
    }

    private void RefreshThornItemImage(bool received)
    {
        if (thornBushItemImage == null) return;
        thornBushItemImage.enabled = false;
        if (!received || GameSessionManager.Instance?.PlayerInventory == null) return;
        IReadOnlyList<ItemData> items = GameSessionManager.Instance.PlayerInventory.Items;
        if (items.Count == 0 || items[items.Count - 1] == null) return;
        thornBushItemImage.sprite = items[items.Count - 1].ItemImage;
        thornBushItemImage.enabled = thornBushItemImage.sprite != null;
    }

    private void CloseAll()
    {
        ClearChoices();
        if (treasurePanel != null) treasurePanel.SetActive(false);
        if (shamanPanel != null) shamanPanel.SetActive(false);
        if (causalityPanel != null) causalityPanel.SetActive(false);
        if (lifeSpringPanel != null) lifeSpringPanel.SetActive(false);
        if (thornBushPanel != null) thornBushPanel.SetActive(false);
        if (causalityChoiceButtonTemplate != null) causalityChoiceButtonTemplate.gameObject.SetActive(false);
    }
}
