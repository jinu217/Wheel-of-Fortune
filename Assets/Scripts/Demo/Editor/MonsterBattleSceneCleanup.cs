using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MonsterBattleSceneCleanup
{
    private const string ScenePath = "Assets/Scenes/MonsterBattleScene.unity";
    private const string MarkerPath = "Assets/Scenes/MonsterBattleUI_Cleanup_v3.marker.txt";

    static MonsterBattleSceneCleanup()
    {
        EditorApplication.delayCall += RunOnce;
    }

    [MenuItem("Tools/Wheel of Fortune/전투 씬 불필요 UI 정리")]
    public static void RunFromMenu()
    {
        if (File.Exists(MarkerPath)) File.Delete(MarkerPath);
        RunOnce();
    }

    private static void RunOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || EditorApplication.isUpdating || File.Exists(MarkerPath))
        {
            return;
        }

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        BattleUIController battleUI = FindInScene<BattleUIController>(scene);
        if (battleUI != null)
        {
            DestroyNamed(scene, "Roulette Coins");
            DestroyNamed(scene, "Attack Coins");
            DestroyNamed(scene, "Defense Coins");
            DestroyNamed(scene, "Monster Action (2)");
            DestroyNamed(scene, "Monster Action (3)");
            DestroyNamed(scene, "Monster Action (4)");

            GameObject previewObject = FindNamed(scene, "Monster Action (1)");
            if (previewObject == null) previewObject = FindNamed(scene, "Monster Action");
            Image preview = previewObject == null ? null : previewObject.GetComponent<Image>();
            if (previewObject != null) previewObject.name = "Monster Action Preview";

            SerializedObject serialized = new SerializedObject(battleUI);
            SetReference(serialized, "rouletteCoinText", null);
            SetReference(serialized, "rouletteButtonText", null);
            SetReference(serialized, "monsterActionImage", preview);
            SerializedProperty previews = serialized.FindProperty("monsterActionPreviewImages");
            if (previews != null)
            {
                previews.arraySize = 1;
                previews.GetArrayElementAtIndex(0).objectReferenceValue = preview;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        File.WriteAllText(MarkerPath, "Obsolete battle demo UI removed.\n");
        AssetDatabase.ImportAsset(MarkerPath);
        if (openedHere) EditorSceneManager.CloseScene(scene, true);
        Debug.Log("MonsterBattleScene의 불필요한 구형 UI 오브젝트를 삭제했습니다.");
    }

    private static void SetReference(SerializedObject serialized, string name, Object value)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property != null) property.objectReferenceValue = value;
    }

    private static void DestroyNamed(Scene scene, string name)
    {
        GameObject target = FindNamed(scene, name);
        if (target != null) Object.DestroyImmediate(target);
    }

    private static GameObject FindNamed(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name) return child.gameObject;
            }
        }
        return null;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T result = root.GetComponentInChildren<T>(true);
            if (result != null) return result;
        }
        return null;
    }
}
