using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SkillInfo
{
    [SerializeField, HideInInspector] private string id;
    [SerializeField] private string skillName;

    public string Id => id;
    public string SkillName => skillName;

    internal void EnsureId()
    {
        if (string.IsNullOrEmpty(id))
        {
            id = Guid.NewGuid().ToString();
        }
    }
}

[CreateAssetMenu(fileName = "Skill Data", menuName = "Game Data/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("All Skills")]
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
