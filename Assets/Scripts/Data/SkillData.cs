using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SkillInfo
{
    [SerializeField, HideInInspector] private string id;
    [Tooltip("인스펙터와 전투 UI에 표시할 스킬 이름입니다.")]
    [SerializeField] private string skillName;
    [Tooltip("스킬 효과를 설명하는 문장입니다.")]
    [TextArea] [SerializeField] private string description;
    [Tooltip("스킬이 실행할 효과 종류입니다.")]
    [SerializeField] private SkillEffectType effectType;
    [Tooltip("피해, 회복 또는 버프에 적용할 기본 수치입니다.")]
    [SerializeField] private int effectValue;
    [Tooltip("버프 효과가 유지되는 턴 수입니다.")]
    [Min(0)] [SerializeField] private int durationTurns;

    public string Id => id;
    public string SkillName => skillName;
    public string Description => description;
    public SkillEffectType EffectType => effectType;
    public int EffectValue => effectValue;
    public int DurationTurns => durationTurns;

    internal void EnsureId()
    {
        if (string.IsNullOrEmpty(id))
        {
            id = Guid.NewGuid().ToString();
        }
    }
}

public enum SkillEffectType
{
    Damage,
    Heal,
    AttackBuff,
    DefenseBuff
}

[CreateAssetMenu(fileName = "Skill Data", menuName = "Game Data/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("All Skills")]
    [Tooltip("게임에서 사용하는 모든 몬스터 스킬 목록입니다.")]
    [SerializeField] private List<SkillInfo> skills = new List<SkillInfo>();

    public IReadOnlyList<SkillInfo> Skills => skills;

    public SkillInfo GetSkill(string skillId)
    {
        if (string.IsNullOrEmpty(skillId))
        {
            return null;
        }

        return skills.Find(skill => skill != null && skill.Id == skillId);
    }

    private void OnValidate()
    {
        foreach (SkillInfo skill in skills)
        {
            skill?.EnsureId();
        }
    }
}
