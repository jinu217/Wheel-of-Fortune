using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class GameSceneInstaller
{
    private const string GameFontPath = "Assets/Fonts/RIDIBatang SDF.asset";
    private const string MarkerPath = "Assets/Scenes/GameScenesConfigured_v1.marker.txt";
    private const string GameStartPath = "Assets/Scenes/GameStartScene.unity";
    private const string BattlePath = "Assets/Scenes/MonsterBattleScene.unity";
    private const string RouletteDataPath = "Assets/Data/Roulette";

    static GameSceneInstaller()
    {
        EditorApplication.delayCall += BuildOnce;
    }

    [MenuItem("Tools/Wheel of Fortune/게임 씬 구성")]
    public static void RebuildFromMenu()
    {
        if (File.Exists(MarkerPath))
        {
            File.Delete(MarkerPath);
            AssetDatabase.Refresh();
        }

        BuildOnce();
    }

    private static void BuildOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(MarkerPath))
        {
            return;
        }

        ConfigureGameStartScene();
        ConfigureBattleScene();
        File.WriteAllText(MarkerPath, "GameStartScene, IGS and MonsterBattleScene configured.\n");
        AssetDatabase.ImportAsset(MarkerPath);
        AssetDatabase.SaveAssets();
        Debug.Log("GameStartScene → IGS → MonsterBattleScene 구성을 완료했습니다.");
    }

    private static void ConfigureGameStartScene()
    {
        WithScene(GameStartPath, scene =>
        {
            StartButton startButton = FindInScene<StartButton>(scene);
            if (startButton != null)
            {
                SerializedObject serialized = new SerializedObject(startButton);
                serialized.FindProperty("inGameSceneName").stringValue = "IGS";
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            Canvas canvas = FindInScene<Canvas>(scene);
            if (canvas != null && GameObject.Find("Game Start Title") == null)
            {
                GameObject title = CreateTMP("Game Start Title", canvas.transform, "운명의 수레바퀴", 56);
                SetRect(title.GetComponent<RectTransform>(), new Vector2(0f, 250f), new Vector2(850f, 120f));
            }
        });
    }

    private static void ConfigureBattleScene()
    {
        WithScene(BattlePath, scene =>
        {
            BattleManager battle = FindInScene<BattleManager>(scene);
            MonsterController monster = FindInScene<MonsterController>(scene);
            RouletteController roulette = FindInScene<RouletteController>(scene);
            BattleSceneController sceneController = FindInScene<BattleSceneController>(scene);
            Canvas canvas = FindInScene<Canvas>(scene);

            if (battle == null || monster == null || roulette == null || sceneController == null || canvas == null)
            {
                Debug.LogError("MonsterBattleScene의 기본 전투 컴포넌트가 부족합니다.");
                return;
            }

            Transform previous = canvas.transform.Find("Battle UI");
            if (previous != null)
            {
                UnityEngine.Object.DestroyImmediate(previous.gameObject);
            }

            List<RouletteEffectData> effects = CreateRouletteEffects();
            GameObject root = CreateUI("Battle UI", canvas.transform);
            Stretch(root.GetComponent<RectTransform>());

            GameObject background = CreateUI("Background", root.transform, typeof(Image));
            Stretch(background.GetComponent<RectTransform>());
            background.GetComponent<Image>().color = new Color(0.055f, 0.07f, 0.11f, 1f);

            GameObject playerCard = CreateCard("Player Character", root.transform, new Vector2(-300f, 270f), new Color(0.18f, 0.38f, 0.62f, 1f));
            CreateTMP("Player Label", playerCard.transform, "PLAYER", 32);
            GameObject monsterCard = CreateCard("Monster Character", root.transform, new Vector2(300f, 270f), new Color(0.55f, 0.18f, 0.18f, 1f));
            Image monsterImage = monsterCard.GetComponent<Image>();
            CreateTMP("Monster Label", monsterCard.transform, "MONSTER", 32);

            TMP_Text playerHp = CreateTMP("Player HP", root.transform, "HP 100 / 100", 26).GetComponent<TMP_Text>();
            SetRect(playerHp.rectTransform, new Vector2(-300f, 105f), new Vector2(400f, 45f));
            Slider playerHpSlider = CreateSlider("Player HP Slider", root.transform, new Vector2(-300f, 70f), new Vector2(400f, 28f));
            TMP_Text monsterHp = CreateTMP("Monster HP", root.transform, "HP", 30).GetComponent<TMP_Text>();
            SetRect(monsterHp.rectTransform, new Vector2(300f, 105f), new Vector2(400f, 45f));
            TMP_Text monsterBarrier = CreateTMP("Monster Barrier", root.transform, "+ 0", 24).GetComponent<TMP_Text>();
            SetRect(monsterBarrier.rectTransform, new Vector2(535f, 105f), new Vector2(100f, 45f));
            monsterBarrier.color = new Color(0.35f, 0.75f, 1f, 1f);
            Slider monsterHpSlider = CreateSlider("Monster HP Slider", root.transform, new Vector2(300f, 70f), new Vector2(400f, 28f));
            TMP_Text turnText = CreateTMP("Turn", root.transform, "PLAYER TURN", 38).GetComponent<TMP_Text>();
            SetRect(turnText.rectTransform, new Vector2(0f, 650f), new Vector2(600f, 80f));
            TMP_Text goldText = CreateTMP("Gold", root.transform, "GOLD : 0", 24).GetComponent<TMP_Text>();
            SetRect(goldText.rectTransform, new Vector2(-300f, 35f), new Vector2(400f, 40f));
            TMP_Text barrierText = CreateTMP("Barrier", root.transform, "+ 0", 24).GetComponent<TMP_Text>();
            SetRect(barrierText.rectTransform, new Vector2(-65f, 105f), new Vector2(100f, 45f));
            barrierText.color = new Color(0.35f, 0.75f, 1f, 1f);
            Image[] inventorySlots = new Image[3];
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                GameObject slot = CreateUI($"Inventory Slot {i + 1}", root.transform, typeof(Image));
                SetRect(slot.GetComponent<RectTransform>(), new Vector2(-410f + i * 110f, -20f), new Vector2(100f, 70f));
                slot.GetComponent<Image>().color = new Color(0.13f, 0.15f, 0.21f, 0.95f);
                GameObject icon = CreateUI("Item Icon", slot.transform, typeof(Image));
                Stretch(icon.GetComponent<RectTransform>());
                inventorySlots[i] = icon.GetComponent<Image>();
                inventorySlots[i].preserveAspect = true;
                inventorySlots[i].enabled = false;
            }

            Image[] actionImages = new Image[1];
            for (int i = 0; i < actionImages.Length; i++)
            {
                GameObject actionObject = CreateUI($"Monster Action {i + 1}", root.transform, typeof(Image));
                SetRect(actionObject.GetComponent<RectTransform>(), new Vector2(180f + i * 125f, 500f), new Vector2(100f, 100f));
                actionImages[i] = actionObject.GetComponent<Image>();
                actionImages[i].color = Color.white;
                actionImages[i].preserveAspect = true;
            }
            Image actionImage = actionImages[0];

            GameObject roulettePanel = CreateUI("Roulette Panel", root.transform, typeof(Image));
            SetRect(roulettePanel.GetComponent<RectTransform>(), new Vector2(0f, -360f), new Vector2(900f, 760f));
            roulettePanel.GetComponent<Image>().color = new Color(0.09f, 0.11f, 0.18f, 0.96f);
            GameObject wheel = CreateUI("Wheel", roulettePanel.transform);
            SetRect(wheel.GetComponent<RectTransform>(), new Vector2(0f, 80f), new Vector2(500f, 500f));

            Image[] slots = new Image[10];
            Color[] slotColors = {
                Color.green, Color.green, Color.blue, new Color(1f, 0.72f, 0.05f), Color.white,
                Color.yellow, Color.red, Color.red, Color.black, new Color(0.55f, 0.2f, 0.75f)
            };
            for (int i = 0; i < slots.Length; i++)
            {
                GameObject slot = CreateUI($"Slot {i + 1}", wheel.transform, typeof(Image));
                float angle = i * Mathf.PI * 2f / slots.Length + Mathf.PI * 0.5f;
                SetRect(slot.GetComponent<RectTransform>(), new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 205f, new Vector2(105f, 105f));
                slots[i] = slot.GetComponent<Image>();
                slots[i].color = slotColors[i];
            }

            GameObject rouletteCoinContainer = CreateUI("Roulette Coin Images", roulettePanel.transform);
            SetRect(rouletteCoinContainer.GetComponent<RectTransform>(), new Vector2(0f, -265f), new Vector2(640f, 44f));

            GameObject rouletteButton = CreateUI("Roulette Spin", roulettePanel.transform, typeof(Image), typeof(Button));
            rouletteButton.GetComponent<Image>().color = new Color(0.7f, 0.2f, 0.18f, 1f);
            SetRect(rouletteButton.GetComponent<RectTransform>(), new Vector2(0f, -325f), new Vector2(420f, 80f));
            UnityEventTools.AddPersistentListener(rouletteButton.GetComponent<Button>().onClick, battle.OnRouletteButton);

            GameObject victoryPanel = CreateResultPanel(root.transform, "Victory Panel", "승리!", new Color(0.15f, 0.5f, 0.25f, 0.98f));
            GameObject defeatPanel = CreateResultPanel(root.transform, "Defeat Panel", "패배", new Color(0.55f, 0.12f, 0.12f, 0.98f));
            victoryPanel.SetActive(false);
            defeatPanel.SetActive(false);

            BattleUIController battleUI = root.AddComponent<BattleUIController>();
            SetObjectReference(battleUI, "battleManager", battle);
            SetObjectReference(battleUI, "monster", monster);
            SetObjectReference(battleUI, "monsterImage", monsterImage);
            SetObjectReference(battleUI, "monsterActionImage", actionImage);
            SetObjectReference(battleUI, "playerHpText", playerHp);
            SetObjectReference(battleUI, "monsterHpText", monsterHp);
            SetObjectReference(battleUI, "turnText", turnText);
            SetObjectReference(battleUI, "rouletteButton", rouletteButton.GetComponent<Button>());
            SetObjectReference(battleUI, "playerHpSlider", playerHpSlider);
            SetObjectReference(battleUI, "monsterHpSlider", monsterHpSlider);
            SetObjectReference(battleUI, "monsterBarrierText", monsterBarrier);
            SetObjectReference(battleUI, "goldText", goldText);
            SetObjectReference(battleUI, "rouletteCoinContainer", rouletteCoinContainer.GetComponent<RectTransform>());
            SetObjectReference(battleUI, "barrierText", barrierText);
            SetObjectReferenceArray(battleUI, "inventorySlotImages", inventorySlots);

            ConfigureRoulette(roulette, effects, slots, wheel.GetComponent<RectTransform>());
            SetObjectReference(sceneController, "roulettePanel", roulettePanel);
            SetObjectReference(sceneController, "victoryPanel", victoryPanel);
            SetObjectReference(sceneController, "defeatPanel", defeatPanel);
            SetString(sceneController, "inGameSceneName", "IGS");
        });
    }

    private static List<RouletteEffectData> CreateRouletteEffects()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(RouletteDataPath)) AssetDatabase.CreateFolder("Assets/Data", "Roulette");

        string[] names = { "Green Success", "Blue Great Success", "Gold Extra Spin", "White Buff", "Yellow Heal", "Red Failure", "Black Debuff", "Purple Special" };
        Color[] colors = { Color.green, Color.blue, new Color(1f, 0.72f, 0.05f), Color.white, Color.yellow, Color.red, Color.black, new Color(0.55f, 0.2f, 0.75f) };
        RouletteEffectType[] types = { RouletteEffectType.Success, RouletteEffectType.GreatSuccess, RouletteEffectType.ExtraSpin, RouletteEffectType.SelfBuff, RouletteEffectType.Heal, RouletteEffectType.Failure, RouletteEffectType.SelfDebuff, RouletteEffectType.Special };
        List<RouletteEffectData> results = new List<RouletteEffectData>();

        for (int i = 0; i < names.Length; i++)
        {
            string path = $"{RouletteDataPath}/{names[i]}.asset";
            RouletteEffectData data = AssetDatabase.LoadAssetAtPath<RouletteEffectData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<RouletteEffectData>();
                AssetDatabase.CreateAsset(data, path);
            }

            SerializedObject serialized = new SerializedObject(data);
            serialized.FindProperty("color").colorValue = colors[i];
            serialized.FindProperty("effect").enumValueIndex = (int)types[i];
            serialized.FindProperty("statRange").vector2IntValue = new Vector2Int(types[i] == RouletteEffectType.Failure ? 0 : 2, types[i] == RouletteEffectType.Failure ? 0 : 5);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            results.Add(data);
        }

        return results;
    }

    private static void ConfigureRoulette(RouletteController roulette, List<RouletteEffectData> effects, Image[] slots, RectTransform wheel)
    {
        SerializedObject serialized = new SerializedObject(roulette);
        SerializedProperty slotProperty = serialized.FindProperty("slotImages");
        slotProperty.arraySize = slots.Length;
        for (int i = 0; i < slots.Length; i++) slotProperty.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];

        SerializedProperty effectsProperty = serialized.FindProperty("availableEffects");
        effectsProperty.arraySize = effects.Count;
        for (int i = 0; i < effects.Count; i++) effectsProperty.GetArrayElementAtIndex(i).objectReferenceValue = effects[i];

        Color[] colors = { Color.green, Color.red };
        int[] counts = { 9, 1 };
        SerializedProperty config = serialized.FindProperty("initialConfiguration");
        config.arraySize = colors.Length;
        for (int i = 0; i < colors.Length; i++)
        {
            SerializedProperty element = config.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("color").colorValue = colors[i];
            element.FindPropertyRelative("count").intValue = counts[i];
        }

        serialized.FindProperty("wheelTransform").objectReferenceValue = wheel;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WithScene(string path, Action<Scene> action)
    {
        Scene scene = SceneManager.GetSceneByPath(path);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        action(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (openedHere) EditorSceneManager.CloseScene(scene, true);
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }

        return null;
    }

    private static GameObject CreateCard(string name, Transform parent, Vector2 position, Color color)
    {
        GameObject card = CreateUI(name, parent, typeof(Image));
        SetRect(card.GetComponent<RectTransform>(), position, new Vector2(320f, 320f));
        card.GetComponent<Image>().color = color;
        return card;
    }

    private static GameObject CreateResultPanel(Transform parent, string name, string label, Color color)
    {
        GameObject panel = CreateUI(name, parent, typeof(Image));
        SetRect(panel.GetComponent<RectTransform>(), Vector2.zero, new Vector2(720f, 360f));
        panel.GetComponent<Image>().color = color;
        GameObject text = CreateTMP("Result", panel.transform, label, 64);
        Stretch(text.GetComponent<RectTransform>());
        return panel;
    }

    private static GameObject CreateButton(string name, Transform parent, string label, Color color)
    {
        GameObject button = CreateUI(name, parent, typeof(Image), typeof(Button));
        button.GetComponent<Image>().color = color;
        GameObject text = CreateTMP("Text", button.transform, label, 28);
        Stretch(text.GetComponent<RectTransform>());
        return button;
    }

    private static Slider CreateSlider(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject sliderObject = CreateUI(name, parent, typeof(Slider));
        SetRect(sliderObject.GetComponent<RectTransform>(), position, size);
        Slider slider = sliderObject.GetComponent<Slider>();
        GameObject background = CreateUI("Background", sliderObject.transform, typeof(Image));
        Stretch(background.GetComponent<RectTransform>());
        background.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f, 1f);
        GameObject fill = CreateUI("Fill", sliderObject.transform, typeof(Image));
        Stretch(fill.GetComponent<RectTransform>());
        fill.GetComponent<Image>().color = new Color(0.25f, 0.82f, 0.35f, 1f);
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.targetGraphic = background.GetComponent<Image>();
        slider.maxValue = 100f;
        slider.value = 100f;
        return slider;
    }

    private static GameObject CreateTMP(string name, Transform parent, string value, float fontSize)
    {
        GameObject textObject = CreateUI(name, parent, typeof(TextMeshProUGUI));
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(GameFontPath);
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        return textObject;
    }

    private static GameObject CreateUI(string name, Transform parent, params Type[] components)
    {
        List<Type> types = new List<Type> { typeof(RectTransform) };
        types.AddRange(components);
        GameObject result = new GameObject(name, types.ToArray());
        result.transform.SetParent(parent, false);
        return result;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetObjectReference(UnityEngine.Object target, string property, UnityEngine.Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObjectReferenceArray<T>(UnityEngine.Object target, string property, T[] values) where T : UnityEngine.Object
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty array = serialized.FindProperty(property);
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetString(UnityEngine.Object target, string property, string value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(property).stringValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
