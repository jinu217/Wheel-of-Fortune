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
    [SerializeField] private Sprite monsterImage;

    [Min(1)]
    [SerializeField] private int hp = 1;

    [Min(0)]
    [SerializeField] private int atk;

    [Min(0)]
    [SerializeField] private int def;

    [Min(0)]
    [SerializeField] private int coin;

    [Header("Stat Images")]
    [SerializeField] private Sprite atkImage;
    [SerializeField] private Sprite defImage;

    [Header("Skills")]
    [SerializeField] private SkillData skillData;

    [Header("Skill 1")]
    [SkillDropdown(nameof(skillData))]
    [InspectorName("Skill 1")]
    [SerializeField] private string skill1Id;

    [Min(0)]
    [SerializeField] private int skill1Turn;

    [SerializeField] private Sprite skill1Image;

    [Header("Skill 2")]
    [SkillDropdown(nameof(skillData))]
    [InspectorName("Skill 2")]
    [SerializeField] private string skill2Id;

    [Min(0)]
    [SerializeField] private int skill2Turn;

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
