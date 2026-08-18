using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct RouletteEffectCount
{
    [Tooltip("룰렛 칸에 적용할 효과 종류입니다.")]
    public RouletteEffectType effect;
    [Tooltip("이 효과를 배치할 룰렛 칸 개수입니다. 전체 합계는 10이어야 합니다.")]
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
    [Tooltip("효과 종류별 룰렛 이미지와 수치가 저장된 데이터 목록입니다.")]
    [SerializeField] private List<RouletteEffectData> availableEffects = new List<RouletteEffectData>();
    [Tooltip("룰렛 결과를 적용할 플레이어입니다. 전투 씬에서 자동 연결할 수 있습니다.")]
    [SerializeField] private PlayerStatManager playerStats;
    [Tooltip("전투 시작 시 룰렛 10칸에 적용할 색상별 개수입니다. 합계는 반드시 10이어야 합니다.")]
    [SerializeField] private List<RouletteEffectCount> initialConfiguration = new List<RouletteEffectCount>
    {
        new RouletteEffectCount { effect = RouletteEffectType.Success, count = 9 },
        new RouletteEffectCount { effect = RouletteEffectType.Failure, count = 1 }
    };
    [Tooltip("운명의 코인 성공 시 사용할 행운 룰렛 구성입니다.")]
    [SerializeField] private List<RouletteEffectCount> fortuneConfiguration = new List<RouletteEffectCount>
    {
        new RouletteEffectCount { effect = RouletteEffectType.Success, count = 4 },
        new RouletteEffectCount { effect = RouletteEffectType.GreatSuccess, count = 2 },
        new RouletteEffectCount { effect = RouletteEffectType.ExtraSpin, count = 2 },
        new RouletteEffectCount { effect = RouletteEffectType.Heal, count = 1 },
        new RouletteEffectCount { effect = RouletteEffectType.SelfBuff, count = 1 }
    };
    [Tooltip("운명의 코인 실패 시 사용할 불행 룰렛 구성입니다.")]
    [SerializeField] private List<RouletteEffectCount> misfortuneConfiguration = new List<RouletteEffectCount>
    {
        new RouletteEffectCount { effect = RouletteEffectType.Failure, count = 5 },
        new RouletteEffectCount { effect = RouletteEffectType.Success, count = 3 },
        new RouletteEffectCount { effect = RouletteEffectType.SelfDebuff, count = 2 }
    };
    [Header("Spin Animation")]
    [Tooltip("회전 애니메이션을 적용할 룰렛 UI Transform입니다.")]
    [SerializeField] private RectTransform wheelTransform;
    [Tooltip("룰렛 회전 시간의 최소값(X)과 최대값(Y)입니다.")]
    [SerializeField] private Vector2 spinDurationRange = new Vector2(3f, 5f);
    [Tooltip("룰렛의 가속과 감속 형태를 결정하는 애니메이션 곡선입니다.")]
    [SerializeField] private AnimationCurve spinCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private readonly RouletteEffectData[] appliedSlots = new RouletteEffectData[SlotCount];
    private bool restoreDefaultAfterSpin;
    private PlayerAbilityManager abilities;

    public event Action<int, RouletteEffectData, int> ResultSelected;
    public bool IsSpinning { get; private set; }

    public void SetPlayerStats(PlayerStatManager stats)
    {
        playerStats = stats;
    }

    public void SetAbilityManager(PlayerAbilityManager manager)
    {
        abilities = manager;
    }

    public bool ConfigureInitialSlots()
    {
        return Configure(initialConfiguration);
    }

    public bool ConfigureFateCoinSpin()
    {
        if (restoreDefaultAfterSpin) return false;
        bool fortune = UnityEngine.Random.value < 0.75f;
        bool configured = Configure(fortune ? fortuneConfiguration : misfortuneConfiguration);
        restoreDefaultAfterSpin = configured;
        return configured;
    }

    public bool Configure(IReadOnlyList<RouletteEffectCount> effectCounts)
    {
        if (effectCounts == null || slotImages == null || slotImages.Length != SlotCount)
        {
            return false;
        }

        int requestedCount = 0;
        foreach (RouletteEffectCount setting in effectCounts)
        {
            requestedCount += setting.count;
        }

        if (requestedCount != SlotCount)
        {
            Debug.LogError("Roulette slot counts must add up to exactly 10.", this);
            return false;
        }

        int slotIndex = 0;
        foreach (RouletteEffectCount setting in effectCounts)
        {
            RouletteEffectData effect = availableEffects.Find(data => data != null && data.Effect == setting.effect);
            if (effect == null)
            {
                Debug.LogError($"{setting.effect} 효과의 RouletteEffectData가 없습니다.", this);
                return false;
            }

            for (int i = 0; i < setting.count; i++)
            {
                appliedSlots[slotIndex] = effect;
                ApplySlotVisual(slotIndex, effect);
                slotIndex++;
            }
        }

        ApplyChanceMutations();
        ApplyAbilitySlotConversions();

        return true;
    }

    private void ApplyChanceMutations()
    {
        ChanceSystemManager chanceSystem = GameSessionManager.Instance == null
            ? null : GameSessionManager.Instance.ChanceSystem;
        if (chanceSystem == null) return;

        foreach (ChanceRouletteMutation mutation in chanceSystem.RouletteMutations)
        {
            System.Random random = new System.Random(mutation.seed);
            switch (mutation.type)
            {
                case ChanceRouletteMutationType.GreenTwoToRed:
                    ReplaceRandom(RouletteEffectType.Success, RouletteEffectType.Failure, 2, random);
                    break;
                case ChanceRouletteMutationType.RedToBlackGreenToWhite:
                    ReplaceRandom(RouletteEffectType.Failure, RouletteEffectType.SelfDebuff, 1, random);
                    ReplaceRandom(RouletteEffectType.Success, RouletteEffectType.SelfBuff, 1, random);
                    break;
                case ChanceRouletteMutationType.GreenTwoToGold:
                    ReplaceRandom(RouletteEffectType.Success, RouletteEffectType.ExtraSpin, 2, random);
                    break;
                case ChanceRouletteMutationType.RedGreenToYellow:
                    ReplaceRandom(RouletteEffectType.Failure, RouletteEffectType.Heal, 1, random);
                    ReplaceRandom(RouletteEffectType.Success, RouletteEffectType.Heal, 1, random);
                    break;
                case ChanceRouletteMutationType.RandomOneToBlack:
                    ReplaceRandomExcluding(new[] { RouletteEffectType.SelfDebuff },
                        RouletteEffectType.SelfDebuff, 1, random);
                    break;
                case ChanceRouletteMutationType.GreenTwoToBlue:
                    ReplaceRandom(RouletteEffectType.Success, RouletteEffectType.GreatSuccess, 2, random);
                    break;
                case ChanceRouletteMutationType.SafeTwoToRedBlack:
                    ReplaceRandomExcluding(new[] { RouletteEffectType.Failure, RouletteEffectType.SelfDebuff },
                        RouletteEffectType.Failure, 1, random);
                    ReplaceRandomExcluding(new[] { RouletteEffectType.Failure, RouletteEffectType.SelfDebuff },
                        RouletteEffectType.SelfDebuff, 1, random);
                    break;
                case ChanceRouletteMutationType.GreenToLuckyAndUnlucky:
                    RouletteEffectType lucky = new[] { RouletteEffectType.GreatSuccess, RouletteEffectType.ExtraSpin,
                        RouletteEffectType.SelfBuff, RouletteEffectType.Heal }[random.Next(0, 4)];
                    RouletteEffectType unlucky = random.Next(0, 2) == 0
                        ? RouletteEffectType.Failure : RouletteEffectType.SelfDebuff;
                    ReplaceRandom(RouletteEffectType.Success, lucky, 1, random);
                    ReplaceRandom(RouletteEffectType.Success, unlucky, 1, random);
                    break;
            }
        }
    }

    private void ReplaceRandom(RouletteEffectType source, RouletteEffectType target, int count,
        System.Random random)
    {
        List<int> candidates = new List<int>();
        for (int i = 0; i < appliedSlots.Length; i++)
            if (appliedSlots[i] != null && appliedSlots[i].Effect == source) candidates.Add(i);
        ReplaceCandidateSlots(candidates, target, count, random);
    }

    private void ReplaceRandomExcluding(RouletteEffectType[] excluded, RouletteEffectType target,
        int count, System.Random random)
    {
        List<int> candidates = new List<int>();
        for (int i = 0; i < appliedSlots.Length; i++)
        {
            if (appliedSlots[i] == null) continue;
            bool blocked = Array.Exists(excluded, effect => appliedSlots[i].Effect == effect);
            if (!blocked) candidates.Add(i);
        }
        ReplaceCandidateSlots(candidates, target, count, random);
    }

    private void ReplaceCandidateSlots(List<int> candidates, RouletteEffectType target, int count,
        System.Random random)
    {
        RouletteEffectData replacement = availableEffects.Find(data => data != null && data.Effect == target);
        if (replacement == null) return;
        for (int i = 0; i < count && candidates.Count > 0; i++)
        {
            int candidateIndex = random.Next(0, candidates.Count);
            int slotIndex = candidates[candidateIndex];
            candidates.RemoveAt(candidateIndex);
            appliedSlots[slotIndex] = replacement;
            ApplySlotVisual(slotIndex, replacement);
        }
    }

    private void ApplyAbilitySlotConversions()
    {
        if (abilities == null) return;

        if (abilities.Has(PassiveAbilityType.GreenToGold))
        {
            ReplaceFirstSuccessSlot(RouletteEffectType.ExtraSpin);
        }

        if (abilities.Has(PassiveAbilityType.GreenToBlue))
        {
            ReplaceFirstSuccessSlot(RouletteEffectType.GreatSuccess);
        }
    }

    private void ReplaceFirstSuccessSlot(RouletteEffectType replacementType)
    {
        RouletteEffectData replacement = availableEffects.Find(
            data => data != null && data.Effect == replacementType);
        if (replacement == null) return;

        for (int i = 0; i < appliedSlots.Length; i++)
        {
            if (appliedSlots[i] != null && appliedSlots[i].Effect == RouletteEffectType.Success)
            {
                appliedSlots[i] = replacement;
                ApplySlotVisual(i, replacement);
                return;
            }
        }
    }

    private void ApplySlotVisual(int slotIndex, RouletteEffectData effect)
    {
        if (effect == null || slotImages == null || slotIndex < 0 || slotIndex >= slotImages.Length
            || slotImages[slotIndex] == null) return;
        Image image = slotImages[slotIndex];
        image.sprite = effect.RouletteImage;
        image.color = effect.RouletteImage == null ? Color.clear : Color.white;
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

        RestoreDefaultConfigurationIfNeeded();

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
        RestoreDefaultConfigurationIfNeeded();
        ResultSelected?.Invoke(selectedIndex, effect, value);
        IsSpinning = false;
        onCompleted?.Invoke(result);
    }

    private void RestoreDefaultConfigurationIfNeeded()
    {
        if (!restoreDefaultAfterSpin) return;
        restoreDefaultAfterSpin = false;
        Configure(initialConfiguration);
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
                playerStats.AddBattleStatModifier(1, 1);
                break;
            case RouletteEffectType.Heal:
                playerStats.Heal(10);
                break;
            case RouletteEffectType.SelfDebuff:
                playerStats.AddBattleStatModifier(-1, -1);
                break;
        }
    }

    private void OnValidate()
    {
        if (slotImages != null && slotImages.Length != SlotCount)
        {
            Array.Resize(ref slotImages, SlotCount);
        }
    }
}
