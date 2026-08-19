using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ItemDataInstaller
{
    private const string DataFolder = "Assets/Data/Items";
    private const string DatabasePath = "Assets/Data/Items/ItemDatabase.asset";
    private const string IgsScenePath = "Assets/Scenes/IGS.unity";

    private readonly struct ItemDefinition
    {
        public readonly string Id;
        public readonly string Name;
        public readonly ItemEffectType Effect;
        public readonly int Value;
        public readonly int Price;

        public ItemDefinition(string id, string name, ItemEffectType effect, int value, int price)
        {
            Id = id;
            Name = name;
            Effect = effect;
            Value = value;
            Price = price;
        }
    }

    private static readonly ItemDefinition[] Definitions =
    {
        new ItemDefinition("healing_potion", "회복 물약", ItemEffectType.Heal, 10, 10),
        new ItemDefinition("scarecrow", "허수아비", ItemEffectType.DodgeNextAttack, 1, 15),
        new ItemDefinition("attack_coin", "1회용 공격 코인", ItemEffectType.AttackRouletteCoin, 1, 20),
        new ItemDefinition("defense_coin", "1회용 방어 코인", ItemEffectType.DefenseRouletteCoin, 1, 20),
        new ItemDefinition("portable_bomb", "휴대용 폭탄", ItemEffectType.DamageEnemy, 5, 10),
        new ItemDefinition("darkness_generator", "어둠 생성기", ItemEffectType.DelayEnemyTurn, 1, 25),
        new ItemDefinition("crude_shield", "조잡한 방패", ItemEffectType.Barrier, 5, 10),
        new ItemDefinition("mysterious_mushroom", "신비한 버섯", ItemEffectType.Mushroom, 3, 15),
        new ItemDefinition("whetstone", "숫돌", ItemEffectType.AttackBuff, 5, 15),
        new ItemDefinition("fate_coin", "운명의 코인", ItemEffectType.FateCoin, 1, 33),
        new ItemDefinition("double_pouch", "2배 주머니", ItemEffectType.DoubleBattleGold, 2, 10)
    };

    static ItemDataInstaller()
    {
        EditorSceneManager.sceneOpened += HandleSceneOpened;
        EditorApplication.playModeStateChanged += HandlePlayModeChanged;
        EditorApplication.delayCall += BuildAndAssign;
    }

    [MenuItem("Tools/Wheel of Fortune/기본 아이템 11종 생성 및 등록")]
    public static void BuildAndAssign()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        EnsureFolder();
        List<ItemData> items = new List<ItemData>();
        foreach (ItemDefinition definition in Definitions) items.Add(CreateOrUpdate(definition));
        ItemDatabase database = CreateOrUpdateDatabase(items);
        AssignToActiveIgs(database);
        AssetDatabase.SaveAssets();
    }

    private static void HandleSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (scene.path == IgsScenePath) EditorApplication.delayCall += BuildAndAssign;
    }

    private static void HandlePlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += BuildAndAssign;
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(DataFolder)) AssetDatabase.CreateFolder("Assets/Data", "Items");
    }

    private static ItemData CreateOrUpdate(ItemDefinition definition)
    {
        string path = $"{DataFolder}/{definition.Name}.asset";
        ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<ItemData>();
            AssetDatabase.CreateAsset(item, path);
        }

        SerializedObject serialized = new SerializedObject(item);
        serialized.FindProperty("itemId").stringValue = definition.Id;
        serialized.FindProperty("itemName").stringValue = definition.Name;
        serialized.FindProperty("effect").enumValueIndex = (int)definition.Effect;
        serialized.FindProperty("value").intValue = definition.Value;
        serialized.FindProperty("price").intValue = definition.Price;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(item);
        return item;
    }

    private static ItemDatabase CreateOrUpdateDatabase(List<ItemData> items)
    {
        ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(DatabasePath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<ItemDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
        }

        SerializedObject serialized = new SerializedObject(database);
        SerializedProperty entries = serialized.FindProperty("items");
        entries.arraySize = items.Count;
        for (int i = 0; i < items.Count; i++) entries.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
        return database;
    }

    private static void AssignToActiveIgs(ItemDatabase database)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != IgsScenePath) return;
        ShopManager manager = null;
        RandomEventManager randomEventManager = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            manager ??= root.GetComponentInChildren<ShopManager>(true);
            randomEventManager ??= root.GetComponentInChildren<RandomEventManager>(true);
        }
        if (manager != null)
        {
            SerializedObject serialized = new SerializedObject(manager);
            serialized.FindProperty("itemDatabase").objectReferenceValue = database;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        if (randomEventManager != null)
        {
            SerializedObject serialized = new SerializedObject(randomEventManager);
            serialized.FindProperty("itemDatabase").objectReferenceValue = database;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
