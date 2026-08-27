using UnityEngine;

public enum BossActionType
{
    Attack,
    Defense,
    Skill1,
    Skill2,
    Skill3,
    Skill4,
    Skill5,
    None
}

[CreateAssetMenu(fileName = "New Boss", menuName = "Game Data/Boss")]
public class BossData : ScriptableObject
{
    [Header("보스 기본 정보")]
    [Tooltip("보스의 이름입니다.")]
    [SerializeField] private string bossName;
    [Tooltip("전투 화면에 표시할 보스 이미지입니다.")]
    [SerializeField] private Sprite bossImage;
    [Tooltip("보스의 최대 체력입니다.")]
    [Min(1)] [SerializeField] private int hp = 1;
    [Tooltip("보스 1페이즈의 기본 공격력입니다.")]
    [Min(0)] [SerializeField] private int atk;
    [Tooltip("보스 1페이즈의 기본 방어력입니다.")]
    [Min(0)] [SerializeField] private int def;
    [Tooltip("보스가 공격할 때 표시할 이미지입니다.")]
    [SerializeField] private Sprite atkImage;
    [Tooltip("보스가 방어할 때 표시할 이미지입니다.")]
    [SerializeField] private Sprite defImage;

    [Header("스킬 데이터")]
    [Tooltip("이 보스가 사용할 스킬이 들어 있는 스킬 데이터베이스입니다.")]
    [SerializeField] private SkillData skillData;

    [Header("1페이즈 스킬")]
    [SkillDropdown(nameof(skillData))] [InspectorName("Skill 1")]
    [Tooltip("1페이즈에서 사용할 첫 번째 스킬입니다.")]
    [SerializeField] private string skill1Id;
    [Tooltip("첫 번째 스킬 행동 이미지입니다.")]
    [SerializeField] private Sprite skill1Image;
    [SkillDropdown(nameof(skillData))] [InspectorName("Skill 2")]
    [Tooltip("1페이즈에서 사용할 두 번째 스킬입니다.")]
    [SerializeField] private string skill2Id;
    [Tooltip("두 번째 스킬 행동 이미지입니다.")]
    [SerializeField] private Sprite skill2Image;
    [SkillDropdown(nameof(skillData))] [InspectorName("Skill 3")]
    [Tooltip("1페이즈에서 사용할 세 번째 스킬입니다.")]
    [SerializeField] private string skill3Id;
    [Tooltip("세 번째 스킬 행동 이미지입니다.")]
    [SerializeField] private Sprite skill3Image;

    [Header("1페이즈 행동 패턴")]
    [Tooltip("1페이즈 첫 번째 행동입니다.")] [SerializeField] private BossActionType phase1Action1;
    [Tooltip("1페이즈 두 번째 행동입니다.")] [SerializeField] private BossActionType phase1Action2;
    [Tooltip("1페이즈 세 번째 행동입니다.")] [SerializeField] private BossActionType phase1Action3;
    [Tooltip("1페이즈 네 번째 행동입니다. None이면 앞 행동만 반복합니다.")] [SerializeField] private BossActionType phase1Action4 = BossActionType.None;
    [Tooltip("1페이즈 다섯 번째 행동입니다. None이면 앞 행동만 반복합니다.")] [SerializeField] private BossActionType phase1Action5 = BossActionType.None;
    [Tooltip("1페이즈 여섯 번째 행동입니다. None이면 앞 행동만 반복합니다.")] [SerializeField] private BossActionType phase1Action6 = BossActionType.None;
    [Tooltip("1페이즈 일곱 번째 행동입니다. None이면 앞 행동만 반복합니다.")] [SerializeField] private BossActionType phase1Action7 = BossActionType.None;

    [Header("2페이즈 정보")]
    [Tooltip("2페이즈가 시작될 때 교체하여 표시할 보스 이미지입니다.")]
    [SerializeField] private Sprite phase2BossImage;
    [Tooltip("현재 HP가 이 수치 이하가 되면 2페이즈를 시작합니다.")]
    [Min(0)] [SerializeField] private int phase2StartHp;
    [Tooltip("2페이즈에서 사용할 기본 공격력입니다.")]
    [Min(0)] [SerializeField] private int phase2Atk;
    [Tooltip("2페이즈에서 사용할 기본 방어력입니다.")]
    [Min(0)] [SerializeField] private int phase2Def;

    [Header("2페이즈 추가 스킬")]
    [SkillDropdown(nameof(skillData))] [InspectorName("Skill 4")]
    [Tooltip("2페이즈에서 사용할 네 번째 스킬입니다.")]
    [SerializeField] private string skill4Id;
    [Tooltip("네 번째 스킬 행동 이미지입니다.")]
    [SerializeField] private Sprite skill4Image;
    [SkillDropdown(nameof(skillData))] [InspectorName("Skill 5")]
    [Tooltip("2페이즈에서 사용할 다섯 번째 스킬입니다.")]
    [SerializeField] private string skill5Id;
    [Tooltip("다섯 번째 스킬 행동 이미지입니다.")]
    [SerializeField] private Sprite skill5Image;

    [Header("2페이즈 행동 패턴")]
    [Tooltip("2페이즈 첫 번째 행동입니다.")] [SerializeField] private BossActionType phase2Action1;
    [Tooltip("2페이즈 두 번째 행동입니다.")] [SerializeField] private BossActionType phase2Action2;
    [Tooltip("2페이즈 세 번째 행동입니다.")] [SerializeField] private BossActionType phase2Action3;
    [Tooltip("2페이즈 네 번째 행동입니다. None이면 앞 행동만 반복합니다.")] [SerializeField] private BossActionType phase2Action4 = BossActionType.None;
    [Tooltip("2페이즈 다섯 번째 행동입니다. None이면 앞 행동만 반복합니다.")] [SerializeField] private BossActionType phase2Action5 = BossActionType.None;

    public string BossName => bossName;
    public Sprite BossImage => bossImage;
    public int Hp => hp;
    public int Atk => atk;
    public int Def => def;
    public Sprite AtkImage => atkImage;
    public Sprite DefImage => defImage;
    public SkillData SkillData => skillData;
    public SkillInfo Skill1 => GetSkill(skill1Id);
    public Sprite Skill1Image => skill1Image;
    public SkillInfo Skill2 => GetSkill(skill2Id);
    public Sprite Skill2Image => skill2Image;
    public SkillInfo Skill3 => GetSkill(skill3Id);
    public Sprite Skill3Image => skill3Image;
    public int Phase2StartHp => phase2StartHp;
    public Sprite Phase2BossImage => phase2BossImage;
    public int Phase2Atk => phase2Atk;
    public int Phase2Def => phase2Def;
    public SkillInfo Skill4 => GetSkill(skill4Id);
    public Sprite Skill4Image => skill4Image;
    public SkillInfo Skill5 => GetSkill(skill5Id);
    public Sprite Skill5Image => skill5Image;
    public int Phase1PatternCount => GetPhase1PatternCount();
    public int Phase2PatternCount => GetPatternCount(phase2Action4, phase2Action5);

    public BossActionType GetPhase1Action(int index)
    {
        return GetPatternAction(index, Phase1PatternCount, phase1Action1, phase1Action2,
            phase1Action3, phase1Action4, phase1Action5, phase1Action6, phase1Action7);
    }

    public BossActionType GetPhase2Action(int index)
    {
        return GetPatternAction(index, Phase2PatternCount, phase2Action1, phase2Action2,
            phase2Action3, phase2Action4, phase2Action5, BossActionType.None, BossActionType.None);
    }

    private SkillInfo GetSkill(string id) => skillData == null ? null : skillData.GetSkill(id);

    private static int GetPatternCount(BossActionType fourth, BossActionType fifth)
    {
        return fourth == BossActionType.None ? 3 : fifth == BossActionType.None ? 4 : 5;
    }

    private int GetPhase1PatternCount()
    {
        if (phase1Action4 == BossActionType.None) return 3;
        if (phase1Action5 == BossActionType.None) return 4;
        if (phase1Action6 == BossActionType.None) return 5;
        return phase1Action7 == BossActionType.None ? 6 : 7;
    }

    private static BossActionType GetPatternAction(int index, int count, BossActionType first,
        BossActionType second, BossActionType third, BossActionType fourth, BossActionType fifth,
        BossActionType sixth, BossActionType seventh)
    {
        switch (Mathf.Abs(index) % count)
        {
            case 0: return first;
            case 1: return second;
            case 2: return third;
            case 3: return fourth;
            case 4: return fifth;
            case 5: return sixth;
            default: return seventh;
        }
    }
}
