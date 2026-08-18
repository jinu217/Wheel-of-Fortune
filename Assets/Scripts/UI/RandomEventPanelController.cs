using UnityEngine;

public class RandomEventPanelController : MonoBehaviour
{
    [Tooltip("추첨된 랜덤 이벤트 종류를 전달하는 이벤트 관리자입니다.")] [SerializeField] private InGameEventCoordinator eventCoordinator;
    [Tooltip("랜덤 이벤트 실행과 완료 상태를 관리하는 컴포넌트입니다.")] [SerializeField] private RandomEventManager randomEventManager;
    [Tooltip("보물상자 이벤트가 선택됐을 때 열 패널입니다.")] [SerializeField] private GameObject treasureChestPanel;
    [Tooltip("주술사 이벤트가 선택됐을 때 열 패널입니다.")] [SerializeField] private GameObject shamanPanel;
    [Tooltip("인과율의 신전 이벤트가 선택됐을 때 열 패널입니다.")] [SerializeField] private GameObject causalityShrinePanel;
    [Tooltip("생명의 샘 이벤트가 선택됐을 때 열 패널입니다.")] [SerializeField] private GameObject lifeSpringPanel;
    [Tooltip("가시 덤불 이벤트가 선택됐을 때 열 패널입니다.")] [SerializeField] private GameObject thornBushPanel;

    private void OnEnable()
    {
        if (eventCoordinator != null)
        {
            eventCoordinator.RandomEventOpened += OpenPanel;
        }

        if (randomEventManager != null)
        {
            randomEventManager.RandomEventCompleted += CloseAllPanels;
        }
    }

    private void OnDisable()
    {
        if (eventCoordinator != null)
        {
            eventCoordinator.RandomEventOpened -= OpenPanel;
        }

        if (randomEventManager != null)
        {
            randomEventManager.RandomEventCompleted -= CloseAllPanels;
        }
    }

    private void OpenPanel(RandomEventType eventType)
    {
        SetAllPanels(false);
        switch (eventType)
        {
            case RandomEventType.TreasureChest:
                treasureChestPanel?.SetActive(true);
                break;
            case RandomEventType.Shaman:
                shamanPanel?.SetActive(true);
                break;
            case RandomEventType.CausalityShrine:
                causalityShrinePanel?.SetActive(true);
                break;
            case RandomEventType.LifeSpring:
                lifeSpringPanel?.SetActive(true);
                break;
            case RandomEventType.ThornBush:
                thornBushPanel?.SetActive(true);
                break;
        }
    }

    private void CloseAllPanels(RandomEventType eventType)
    {
        SetAllPanels(false);
    }

    private void SetAllPanels(bool active)
    {
        treasureChestPanel?.SetActive(active);
        shamanPanel?.SetActive(active);
        causalityShrinePanel?.SetActive(active);
        lifeSpringPanel?.SetActive(active);
        thornBushPanel?.SetActive(active);
    }
}
