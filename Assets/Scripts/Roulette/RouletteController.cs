using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct RouletteColorCount
{
    [Tooltip("룰렛 칸에 적용할 색상입니다.")]
    public Color color;
    [Tooltip("이 색상을 배치할 룰렛 칸 개수입니다. 전체 합계는 10이어야 합니다.")]
    [Range(0, 10)] public int count;
}

public struct RouletteSpinResult
{
    public int SlotIndex { get; }
    public RouletteEffectData EffectData { get; }
    public int Value { get; }

    public RouletteSpinResult(int slotIndex, RouletteEffectData effectData, int value)
    {
        SlotIndex = slotIndex;
        EffectData = effectData;
        Value = value;
    }
}

public class RouletteController : MonoBehaviour
{
    private const int SlotCount = 10;

    [Header("10 slots arranged in a circle")]
    [Tooltip("원형으로 배치한 룰렛 칸 이미지 10개입니다.")]
    [SerializeField] private Image[] slotImages = new Image[SlotCount];
    [Tooltip("룰렛 색상과 일치하는 효과 데이터 목록입니다.")]
    [SerializeField] private List<RouletteEffectData> availableEffects = new List<RouletteEffectData>();
    [Tooltip("룰렛 결과를 적용할 플레이어입니다. 전투 씬에서 자동 연결할 수 있습니다.")]
    [SerializeField] private PlayerStatManager playerStats;
    [Tooltip("전투 시작 시 룰렛 10칸에 적용할 색상별 개수입니다. 합계는 반드시 10이어야 합니다.")]
    [SerializeField] private List<RouletteColorCount> initialConfiguration = new List<RouletteColorCount>
    {
        new RouletteColorCount { color = Color.green, count = 9 },
        new RouletteColorCount { color = Color.red, count = 1 }
    };
    [Header("Spin Animation")]
    [Tooltip("회전 애니메이션을 적용할 룰렛 UI Transform입니다.")]
    [SerializeField] private RectTransform wheelTransform;
    [Tooltip("룰렛 회전 시간의 최소값(X)과 최대값(Y)입니다.")]
    [SerializeField] private Vector2 spinDurationRange = new Vector2(3f, 5f);
    [Tooltip("룰렛의 가속과 감속 형태를 결정하는 애니메이션 곡선입니다.")]
    [SerializeField] private AnimationCurve spinCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private readonly RouletteEffectData[] appliedSlots = new RouletteEffectData[SlotCount];

    public event Action<int, RouletteEffectData, int> ResultSelected;
    public bool IsSpinning { get; private set; }

    public void SetPlayerStats(PlayerStatManager stats)
    {
        playerStats = stats;
    }

    public bool ConfigureInitialSlots()
    {
        return Configure(initialConfiguration);
    }

    public bool Configure(IReadOnlyList<RouletteColorCount> colorCounts)
    {
        if (colorCounts == null || slotImages == null || slotImages.Length != SlotCount)
        {
            return false;
        }

        int requestedCount = 0;
        foreach (RouletteColorCount setting in colorCounts)
        {
            requestedCount += setting.count;
        }

        if (requestedCount != SlotCount)
        {
            Debug.LogError("Roulette slot counts must add up to exactly 10.", this);
            return false;
        }

        int slotIndex = 0;
        foreach (RouletteColorCount setting in colorCounts)
        {
            RouletteEffectData effect = FindEffectByColor(setting.color);
            if (effect == null)
            {
                Debug.LogError($"No RouletteEffectData matches color {setting.color}.", this);
                return false;
            }

            for (int i = 0; i < setting.count; i++)
            {
                appliedSlots[slotIndex] = effect;
                slotImages[slotIndex].color = effect.Color;
                slotIndex++;
            }
        }

        return true;
    }

    public RouletteSpinResult Spin()
    {
        return SpinInternal(true);
    }

    public RouletteSpinResult SpinWithoutApplying()
    {
        return SpinInternal(false);
    }

    public bool SpinAnimatedWithoutApplying(Action<RouletteSpinResult> onCompleted = null)
    {
        if (IsSpinning)
        {
            return false;
        }

        StartCoroutine(SpinRoutine(false, onCompleted));
        return true;
    }

    private RouletteSpinResult SpinInternal(bool applyToPlayer)
    {
        int selectedIndex = UnityEngine.Random.Range(0, SlotCount);
        RouletteEffectData result = appliedSlots[selectedIndex];
        int value = result == null ? 0 : result.RollValue();

        if (applyToPlayer && result != null)
        {
            ApplyEffect(result.Effect, value);
        }

        ResultSelected?.Invoke(selectedIndex, result, value);
        return new RouletteSpinResult(selectedIndex, result, value);
    }

    private IEnumerator SpinRoutine(bool applyToPlayer, Action<RouletteSpinResult> onCompleted)
    {
        IsSpinning = true;
        int selectedIndex = UnityEngine.Random.Range(0, SlotCount);
        RouletteEffectData effect = appliedSlots[selectedIndex];
        int value = effect == null ? 0 : effect.RollValue();
        float duration = UnityEngine.Random.Range(
            Mathf.Min(spinDurationRange.x, spinDurationRange.y),
            Mathf.Max(spinDurationRange.x, spinDurationRange.y));

        if (wheelTransform != null)
        {
            float startAngle = wheelTransform.eulerAngles.z;
            float targetAngle = startAngle - 360f * 5f - selectedIndex * (360f / SlotCount);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float angle = Mathf.LerpUnclamped(startAngle, targetAngle, spinCurve.Evaluate(progress));
                wheelTransform.localEulerAngles = new Vector3(0f, 0f, angle);
                yield return null;
            }

            wheelTransform.localEulerAngles = new Vector3(0f, 0f, targetAngle);
        }
        else
        {
            yield return new WaitForSeconds(duration);
        }

        if (applyToPlayer && effect != null)
        {
            ApplyEffect(effect.Effect, value);
        }

        RouletteSpinResult result = new RouletteSpinResult(selectedIndex, effect, value);
        ResultSelected?.Invoke(selectedIndex, effect, value);
        IsSpinning = false;
        onCompleted?.Invoke(result);
    }

    private RouletteEffectData FindEffectByColor(Color requestedColor)
    {
        return availableEffects.Find(data => data != null && Approximately(data.Color, requestedColor));
    }

    private void ApplyEffect(RouletteEffectType effect, int value)
    {
        if (playerStats == null)
        {
            return;
        }

        switch (effect)
        {
            case RouletteEffectType.SelfBuff:
                playerStats.AddPermanentStat(StatType.Attack, value);
                playerStats.AddPermanentStat(StatType.Defense, value);
                break;
            case RouletteEffectType.Heal:
                playerStats.Heal(value);
                break;
            case RouletteEffectType.SelfDebuff:
                playerStats.AddTimedModifier(StatType.Attack, -Mathf.Abs(value), 1);
                playerStats.AddTimedModifier(StatType.Defense, -Mathf.Abs(value), 1);
                break;
        }
    }

    private static bool Approximately(Color left, Color right)
    {
        const float tolerance = 0.01f;
        return Mathf.Abs(left.r - right.r) < tolerance
            && Mathf.Abs(left.g - right.g) < tolerance
            && Mathf.Abs(left.b - right.b) < tolerance
            && Mathf.Abs(left.a - right.a) < tolerance;
    }

    private void OnValidate()
    {
        if (slotImages != null && slotImages.Length != SlotCount)
        {
            Array.Resize(ref slotImages, SlotCount);
        }
    }
}
