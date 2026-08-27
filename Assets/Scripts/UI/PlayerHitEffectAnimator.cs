using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHitEffectAnimator : MonoBehaviour
{
    [Tooltip("플레이어가 실제 피해를 받은 시점을 전달하는 전투 관리자입니다.")]
    [SerializeField] private BattleManager battleManager;
    [Tooltip("피격 애니메이션 프레임을 표시할 이미지입니다.")]
    [SerializeField] private Image effectImage;
    [Tooltip("MonsterAtk 폴더의 피격 이미지를 파일명 순서대로 연결한 프레임입니다.")]
    [SerializeField] private Sprite[] frames;
    [Tooltip("각 피격 이미지가 표시되는 시간입니다.")]
    [Min(0.01f)] [SerializeField] private float secondsPerFrame = 0.05f;
    [Tooltip("피격 애니메이션이 시작될 때 함께 재생할 효과음입니다.")]
    [SerializeField] private AudioClip effectSound;
    [Tooltip("피격 효과음의 음량입니다.")]
    [Range(0f, 1f)] [SerializeField] private float soundVolume = 1f;

    private Coroutine playRoutine;
    private AudioSource audioSource;

    private void OnEnable()
    {
        if (battleManager != null) battleManager.PlayerHit += Play;
        Hide();
    }

    private void OnDisable()
    {
        if (battleManager != null) battleManager.PlayerHit -= Play;
        if (playRoutine != null) StopCoroutine(playRoutine);
        playRoutine = null;
        Hide();
    }

    public void Play()
    {
        if (effectImage == null || frames == null || frames.Length == 0) return;
        if (playRoutine != null) StopCoroutine(playRoutine);
        PlaySound();
        playRoutine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        effectImage.gameObject.SetActive(true);
        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] == null) continue;
            effectImage.sprite = frames[i];
            yield return new WaitForSeconds(secondsPerFrame);
        }
        playRoutine = null;
        Hide();
    }

    private void Hide()
    {
        if (effectImage != null) effectImage.gameObject.SetActive(false);
    }

    private void PlaySound()
    {
        if (effectSound == null) return;
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.PlayOneShot(effectSound, soundVolume);
    }
}
