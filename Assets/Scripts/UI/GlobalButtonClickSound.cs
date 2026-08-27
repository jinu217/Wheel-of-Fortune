using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>모든 씬의 UI 버튼에 동일한 클릭 효과음을 자동 적용합니다.</summary>
public sealed class GlobalButtonClickSound : MonoBehaviour
{
    private const string ClickSoundResourcePath = "클릭 사운드";
    private const float ScanInterval = 0.25f;

    private static GlobalButtonClickSound instance;
    private AudioSource audioSource;
    private AudioClip clickSound;
    private float nextScanTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstance()
    {
        if (instance != null) return;

        GameObject soundObject = new GameObject(nameof(GlobalButtonClickSound));
        instance = soundObject.AddComponent<GlobalButtonClickSound>();
        DontDestroyOnLoad(soundObject);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        clickSound = Resources.Load<AudioClip>(ClickSoundResourcePath);
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        AttachToAllButtons();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextScanTime) return;
        nextScanTime = Time.unscaledTime + ScanInterval;
        AttachToAllButtons();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (instance == this) instance = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachToAllButtons();
    }

    private static void AttachToAllButtons()
    {
        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.isLoaded) continue;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Button[] buttons = root.GetComponentsInChildren<Button>(true);
                foreach (Button button in buttons)
                {
                    if (button != null && button.GetComponent<GlobalButtonClickSoundReceiver>() == null)
                        button.gameObject.AddComponent<GlobalButtonClickSoundReceiver>();
                }
            }
        }
    }

    internal static void Play()
    {
        if (instance == null || instance.clickSound == null || instance.audioSource == null) return;
        instance.audioSource.PlayOneShot(instance.clickSound);
    }
}

/// <summary>Button의 기존 onClick 설정을 변경하지 않고 포인터 클릭만 감지합니다.</summary>
public sealed class GlobalButtonClickSoundReceiver : MonoBehaviour, IPointerClickHandler
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (button != null && button.IsActive() && button.IsInteractable())
            GlobalButtonClickSound.Play();
    }
}
