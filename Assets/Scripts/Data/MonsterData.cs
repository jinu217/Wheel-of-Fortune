using UnityEngine;

public class SkillDropdownAttribute : PropertyAttribute
{
    public string SkillDataFieldName { get; }

    public SkillDropdownAttribute(string skillDataFieldName)
    {
        SkillDataFieldName = skillDataFieldName;
    }
}

[CreateAssetMenu(fileName = "New Monster", menuName = "Game Data/Monster")]
public class MonsterData : ScriptableObject
{
    [Header("Monster Info")]
    [Tooltip("전투 화면에 표시할 몬스터 이미지입니다.")]
    [SerializeField] private Sprite monsterImage;

    [Min(1)]
    [Tooltip("몬스터의 최대 체력입니다.")]
    [SerializeField] private int hp = 1;

    [Min(0)]
    [Tooltip("몬스터의 기본 공격력입니다.")]
    [SerializeField] private int atk;

    [Min(0)]
    [Tooltip("몬스터의 기본 방어력입니다.")]
    [SerializeField] private int def;

    [Min(0)]
    [Tooltip("몬스터 처치 시 플레이어가 얻는 골드입니다.")]
    [SerializeField] private int coin;

    [Header("Stat Images")]
    [Tooltip("몬스터가 공격할 때 표시할 행동 이미지입니다.")]
    [SerializeField] private Sprite atkImage;
    [Tooltip("몬스터가 방어할 때 표시할 행동 이미지입니다.")]
    [SerializeField] private Sprite defImage;

    [Header("Skills")]
    [Tooltip("이 몬스터가 선택할 수 있는 전체 스킬 목록입니다.")]
    [SerializeField] private SkillData skillData;

    [Header("Skill 1")]
    [SkillDropdown(nameof(skillData))]
    [InspectorName("Skill 1")]
    [Tooltip("몬스터가 사용할 첫 번째 스킬입니다.")]
    [SerializeField] private string skill1Id;

    [Min(0)]
    [Tooltip("첫 번째 스킬을 다시 사용할 수 있을 때까지 필요한 턴입니다.")]
    [SerializeField] private int skill1Turn;

    [Tooltip("첫 번째 스킬 사용 시 표시할 행동 이미지입니다.")]
    [SerializeField] private Sprite skill1Image;

    [Header("Skill 2")]
    [SkillDropdown(nameof(skillData))]
    [InspectorName("Skill 2")]
    [Tooltip("몬스터가 사용할 두 번째 스킬입니다.")]
    [SerializeField] private string skill2Id;

    [Min(0)]
    [Tooltip("두 번째 스킬을 다시 사용할 수 있을 때까지 필요한 턴입니다.")]
    [SerializeField] private int skill2Turn;

    [Tooltip("두 번째 스킬 사용 시 표시할 행동 이미지입니다.")]
    [SerializeField] private Sprite skill2Image;

    public Sprite MonsterImage => monsterImage;
    public int Hp => hp;
    public int Atk => atk;
    public int Def => def;
    public int Coin => coin;
    public Sprite AtkImage => atkImage;
    public Sprite DefImage => defImage;
    public SkillData SkillData => skillData;
    public SkillInfo Skill1 => skillData == null ? null : skillData.GetSkill(skill1Id);
    public int Skill1Turn => skill1Turn;
    public Sprite Skill1Image => skill1Image;
    public SkillInfo Skill2 => skillData == null ? null : skillData.GetSkill(skill2Id);
    public int Skill2Turn => skill2Turn;
    public Sprite Skill2Image => skill2Image;
}
