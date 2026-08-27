using System.Collections;
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
    [Tooltip("결과에 따라 스프라이트가 변경될 보물상자 이미지입니다.")] [SerializeField] private Image treasureChestImage;
    [Tooltip("50골드 획득 시 보물상자 이미지 대신 표시할 이미지입니다.")] [SerializeField] private Sprite treasureGoldResultSprite;
    [Tooltip("미믹 전투 진입 전에 보물상자 이미지 대신 표시할 이미지입니다.")] [SerializeField] private Sprite treasureMimicResultSprite;
    [Tooltip("결과 이미지를 보여준 뒤 패널을 닫거나 미믹 전투로 이동하기까지의 시간입니다.")] [Min(0f)] [SerializeField] private float treasureResultDuration = 3f;
    [Tooltip("보물상자 확률과 결과 안내입니다.")] [SerializeField] private TMP_Text treasureDescriptionText;

    [Header("주술사")]
    [Tooltip("주술사 이벤트 전체 패널입니다.")] [SerializeField] private GameObject shamanPanel;
    [Tooltip("20골드로 능력을 구매하는 이미지 버튼입니다.")] [SerializeField] private Button shamanPurchaseButton;
    [Tooltip("비용과 구매 결과 안내입니다.")] [SerializeField] private TMP_Text shamanDescriptionText;
    [Tooltip("구매한 능력 이름과 설명을 화면에 표시하는 시간입니다.")]
    [Min(0f)] [SerializeField] private float shamanAbilityDisplaySeconds = 3f;

    [Header("생명의 샘")]
    [Tooltip("생명의 샘 전체 패널입니다.")] [SerializeField] private GameObject lifeSpringPanel;
    [Tooltip("생명의 샘 확인 버튼입니다.")] [SerializeField] private Button lifeSpringConfirmButton;
    [Tooltip("회복 결과 안내입니다.")] [SerializeField] private TMP_Text lifeSpringDescriptionText;
    [Tooltip("생명의 샘 결과를 표시한 뒤 패널이 자동으로 닫히기까지의 시간입니다.")]
    [Min(0f)] [SerializeField] private float lifeSpringCloseDelay = 2f;

    [Header("가시 덤불")]
    [Tooltip("가시 덤불 전체 패널입니다.")] [SerializeField] private GameObject thornBushPanel;
    [Tooltip("가시 덤불 확인 버튼입니다.")] [SerializeField] private Button thornBushConfirmButton;
    [Tooltip("피해와 아이템 획득 결과 안내입니다.")] [SerializeField] private TMP_Text thornBushDescriptionText;
    [Tooltip("획득한 아이템 이미지입니다.")] [SerializeField] private Image thornBushItemImage;
    [Tooltip("가시 덤불 결과를 표시한 뒤 패널이 자동으로 닫히기까지의 시간입니다.")]
    [Min(0f)] [SerializeField] private float thornBushCloseDelay = 2f;

    private Sprite initialTreasureSprite;
    private Coroutine treasureResultRoutine;
    private Coroutine shamanResultRoutine;
    private Coroutine eventCloseRoutine;
    private TMP_Text shamanButtonText;

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
        if (treasureChestImage == null && treasureChestButton != null)
            treasureChestImage = treasureChestButton.targetGraphic as Image;
        if (treasureChestImage != null && initialTreasureSprite == null)
            initialTreasureSprite = treasureChestImage.sprite;

        Bind(treasureChestButton, OpenTreasure);
        Bind(shamanPurchaseButton, PurchaseShamanAbility);
        Bind(lifeSpringConfirmButton, UseLifeSpring);
        Bind(thornBushConfirmButton, EnterThornBush);

        if (shamanPurchaseButton != null)
        {
            shamanButtonText = shamanPurchaseButton.GetComponentInChildren<TMP_Text>(true);
        }
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
        if (type == RandomEventType.TreasureChest) ResetTreasurePanel();
        else if (type == RandomEventType.Shaman) ResetShamanPanel();
        else if (type == RandomEventType.LifeSpring) ResetLifeSpringPanel();
        else if (type == RandomEventType.ThornBush) ResetThornBushPanel();
    }

    private GameObject GetPanel(RandomEventType type)
    {
        switch (type)
        {
            case RandomEventType.TreasureChest: return treasurePanel;
            case RandomEventType.Shaman: return shamanPanel;
            case RandomEventType.LifeSpring: return lifeSpringPanel;
            case RandomEventType.ThornBush: return thornBushPanel;
            default: return null;
        }
    }

    private void OpenTreasure()
    {
        if (treasureResultRoutine != null || manager == null) return;

        TreasureChestResult result = manager.OpenTreasureChest();
        treasureChestButton.interactable = false;
        if (treasureChestImage != null)
        {
            Sprite resultSprite = result == TreasureChestResult.Mimic
                ? treasureMimicResultSprite
                : treasureGoldResultSprite;
            if (resultSprite != null) treasureChestImage.sprite = resultSprite;
        }

        if (treasureDescriptionText != null)
            treasureDescriptionText.text = result == TreasureChestResult.Mimic
                ? "미믹이 나타났다!"
                : "50골드를 획득했습니다.";

        treasureResultRoutine = StartCoroutine(FinishTreasureResult(result));
    }

    private IEnumerator FinishTreasureResult(TreasureChestResult result)
    {
        if (treasureResultDuration > 0f) yield return new WaitForSeconds(treasureResultDuration);
        treasureResultRoutine = null;

        if (result == TreasureChestResult.Mimic)
        {
            manager.StartMimicEncounter();
            yield break;
        }

        if (treasurePanel != null) treasurePanel.SetActive(false);
    }

    private void ResetTreasurePanel()
    {
        if (treasureResultRoutine != null)
        {
            StopCoroutine(treasureResultRoutine);
            treasureResultRoutine = null;
        }
        if (treasureChestImage != null) treasureChestImage.sprite = initialTreasureSprite;
        if (treasureChestButton != null) treasureChestButton.interactable = true;
    }

    private void PurchaseShamanAbility()
    {
        if (shamanResultRoutine != null || manager == null) return;
        GameSessionManager session = GameSessionManager.Instance;
        int floor = session == null ? 1 : session.CurrentEventPosition.y + 1;
        bool purchased = manager.TryUseShaman(floor, out AbilityDefinition ability);
        if (shamanDescriptionText != null)
            shamanDescriptionText.text = purchased
                ? $"{ability.Name}\n{ability.Description}"
                : "획득할 수 있는 능력이 없습니다.";
        if (!purchased)
        {
            if (shamanButtonText != null) shamanButtonText.text = "나가기";
            Bind(shamanPurchaseButton, ExitShaman);
            return;
        }

        if (shamanButtonText != null) shamanButtonText.text = "나가기";
        Bind(shamanPurchaseButton, ExitShaman);
        shamanResultRoutine = StartCoroutine(ClearShamanAbilityAfterDelay());
    }

    private IEnumerator ClearShamanAbilityAfterDelay()
    {
        if (shamanAbilityDisplaySeconds > 0f) yield return new WaitForSeconds(shamanAbilityDisplaySeconds);
        shamanResultRoutine = null;
        if (shamanDescriptionText != null) shamanDescriptionText.text = string.Empty;
    }

    private void ExitShaman()
    {
        manager?.CompleteShaman();
        if (shamanPanel != null) shamanPanel.SetActive(false);
    }

    private void UseLifeSpring()
    {
        if (eventCloseRoutine != null || manager == null) return;
        manager.UseLifeSpring();
        lifeSpringConfirmButton.interactable = false;
        if (lifeSpringDescriptionText != null) lifeSpringDescriptionText.text = "HP가 전부 회복되었습니다.";
        eventCloseRoutine = StartCoroutine(CloseEventPanelAfterDelay(lifeSpringPanel, lifeSpringCloseDelay));
    }

    private void EnterThornBush()
    {
        if (eventCloseRoutine != null || manager == null) return;
        ItemData acquiredItem = manager.EnterThornBush();
        thornBushConfirmButton.interactable = false;
        if (thornBushDescriptionText != null)
            thornBushDescriptionText.text = acquiredItem != null
                ? $"HP가 5 감소했습니다.\n{acquiredItem.ItemName}을 획득했습니다."
                : "HP가 5 감소했습니다.\n인벤토리가 가득 차 아이템은 획득하지 못했습니다.";
        RefreshThornItemImage(acquiredItem);
        eventCloseRoutine = StartCoroutine(CloseEventPanelAfterDelay(thornBushPanel, thornBushCloseDelay));
    }

    private IEnumerator CloseEventPanelAfterDelay(GameObject panel, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        eventCloseRoutine = null;
        if (panel != null) panel.SetActive(false);
    }

    private void RefreshThornItemImage(ItemData acquiredItem)
    {
        if (thornBushItemImage == null) return;
        thornBushItemImage.enabled = false;
        if (acquiredItem == null) return;
        thornBushItemImage.sprite = acquiredItem.ItemImage;
        thornBushItemImage.enabled = thornBushItemImage.sprite != null;
    }

    private void ResetShamanPanel()
    {
        if (shamanResultRoutine != null) StopCoroutine(shamanResultRoutine);
        shamanResultRoutine = null;
        bool canAfford = manager != null && manager.CanAffordShaman;
        if (shamanDescriptionText != null)
            shamanDescriptionText.text = $"{(manager == null ? 20 : manager.ShamanPrice)}골드를 지불하고 무작위 능력을 얻습니다.";
        if (shamanButtonText != null) shamanButtonText.text = canAfford ? "구매" : "골드 부족";
        Bind(shamanPurchaseButton, canAfford ? PurchaseShamanAbility : ExitShaman);
        if (shamanPurchaseButton != null) shamanPurchaseButton.interactable = true;
    }

    private void ResetLifeSpringPanel()
    {
        StopEventCloseRoutine();
        if (lifeSpringDescriptionText != null) lifeSpringDescriptionText.text = "이미지를 누르면 HP를 전부 회복합니다.";
        if (lifeSpringConfirmButton != null) lifeSpringConfirmButton.interactable = true;
    }

    private void ResetThornBushPanel()
    {
        StopEventCloseRoutine();
        if (thornBushDescriptionText != null) thornBushDescriptionText.text = "이미지를 누르면 HP가 5 감소하고 무작위 아이템을 얻습니다.";
        if (thornBushConfirmButton != null) thornBushConfirmButton.interactable = true;
        RefreshThornItemImage(null);
    }

    private void StopEventCloseRoutine()
    {
        if (eventCloseRoutine != null) StopCoroutine(eventCloseRoutine);
        eventCloseRoutine = null;
    }

    private void CloseAll()
    {
        if (treasurePanel != null) treasurePanel.SetActive(false);
        if (shamanPanel != null) shamanPanel.SetActive(false);
        if (lifeSpringPanel != null) lifeSpringPanel.SetActive(false);
        if (thornBushPanel != null) thornBushPanel.SetActive(false);
    }
}
