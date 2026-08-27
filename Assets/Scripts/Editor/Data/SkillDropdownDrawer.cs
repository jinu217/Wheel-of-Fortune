using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SkillDropdownAttribute))]
public class SkillDropdownDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SkillDropdownAttribute dropdown = (SkillDropdownAttribute)attribute;
        SerializedProperty skillDataProperty = property.serializedObject.FindProperty(
            dropdown.SkillDataFieldName);
        SkillData skillData = skillDataProperty?.objectReferenceValue as SkillData;

        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label.text, "SkillDropdown requires a string field.");
            return;
        }

        if (skillData == null)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.Popup(position, label.text, 0, new[] { "Assign Skill Data first" });
            }

            return;
        }

        List<string> names = new List<string> { "None" };
        List<string> ids = new List<string> { string.Empty };

        foreach (SkillInfo skill in skillData.Skills)
        {
            if (skill == null)
            {
                continue;
            }

            names.Add(string.IsNullOrWhiteSpace(skill.SkillName) ? "Unnamed Skill" : skill.SkillName);
            ids.Add(skill.Id);
        }

        int currentIndex = ids.IndexOf(property.stringValue);
        if (currentIndex < 0 && !string.IsNullOrEmpty(property.stringValue))
        {
            // SkillData 목록에서 찾지 못한 기존 ID도 보존합니다. 컴파일 후 인스펙터가
            // 다시 그려지는 것만으로 값이 None으로 덮어써지는 것을 방지합니다.
            names.Add($"Missing Skill ({property.stringValue})");
            ids.Add(property.stringValue);
            currentIndex = ids.Count - 1;
        }
        else
        {
            currentIndex = Mathf.Max(0, currentIndex);
        }

        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.BeginChangeCheck();
        int selectedIndex = EditorGUI.Popup(position, label.text, currentIndex, names.ToArray());
        if (EditorGUI.EndChangeCheck())
        {
            property.stringValue = ids[selectedIndex];
        }
        EditorGUI.EndProperty();
    }
}
