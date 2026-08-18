using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[InitializeOnLoad]
public static class InGameSceneInstaller
{
    private const string ScenePath = "Assets/Scenes/IGS.unity";

    static InGameSceneInstaller()
    {
        EditorApplication.delayCall += Install;
    }

    [MenuItem("Tools/Wheel of Fortune/IGS 씬 UI 설치")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedForInstall = !scene.IsValid() || !scene.isLoaded;
        Scene previousActive = SceneManager.GetActiveScene();
        if (openedForInstall) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);

        InGameSceneController controller = FindInScene<InGameSceneController>(scene);
        if (controller != null)
        {
            SerializedObject serialized = new SerializedObject(controller);
            SerializedProperty canvasProperty = serialized.FindProperty("inGameCanvas");
            SerializedProperty mapPanelProperty = serialized.FindProperty("mapPanel");
            if (canvasProperty.objectReferenceValue == null || mapPanelProperty.objectReferenceValue == null)
            {
                if (canvasProperty.objectReferenceValue != null)
                    Object.DestroyImmediate(canvasProperty.objectReferenceValue);
                MethodInfo buildCanvas = typeof(InGameSceneController).GetMethod(
                    "BuildCanvas", BindingFlags.Instance | BindingFlags.NonPublic);
                buildCanvas?.Invoke(controller, null);
                serialized.Update();
            }

            MethodInfo updateLayout = typeof(InGameSceneController).GetMethod(
                "UpdateMapLayout", BindingFlags.Instance | BindingFlags.NonPublic);
            updateLayout?.Invoke(controller, null);

            serialized.Update();
            EnsureRandomEventPanels(controller, serialized);
            EnsureShopItemObjects(scene);

            if (FindInScene<EventSystem>(scene) == null)
            {
                GameObject eventSystem = new GameObject(
                    "EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                SceneManager.MoveGameObjectToScene(eventSystem, scene);
            }

            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (previousActive.IsValid() && previousActive.isLoaded)
            SceneManager.SetActiveScene(previousActive);
        if (openedForInstall) EditorSceneManager.CloseScene(scene, true);
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null) return component;
        }
        return null;
    }

    private static void EnsureRandomEventPanels(InGameSceneController sceneController, SerializedObject controllerSerialized)
    {
        SerializedProperty controllerProperty = controllerSerialized.FindProperty("randomEventPanelController");
        if (controllerProperty.objectReferenceValue != null) return;

        GameObject canvas = controllerSerialized.FindProperty("inGameCanvas").objectReferenceValue as GameObject;
        if (canvas == null) return;
        TMP_FontAsset font = controllerSerialized.FindProperty("inGameFont").objectReferenceValue as TMP_FontAsset;

        GameObject root = new GameObject("Random Event Panels", typeof(RectTransform), typeof(RandomEventPanelController));
        root.transform.SetParent(canvas.transform, false);
        Stretch(root.GetComponent<RectTransform>());
        RandomEventPanelController panelController = root.GetComponent<RandomEventPanelController>();
        SerializedObject panelSerialized = new SerializedObject(panelController);
        Set(panelSerialized, "manager", FindInScene<RandomEventManager>(sceneController.gameObject.scene));

        GameObject treasure = CreatePanel(root.transform, "Treasure Chest Panel", new Color(0.12f, 0.085f, 0.035f, 0.98f));
        CreateText(treasure.transform, "Title", "보물상자", font, new Vector2(0, 430), new Vector2(800, 90), 48);
        TMP_Text treasureText = CreateText(treasure.transform, "Description", "75%: 50 골드\n25%: 미믹 전투", font, new Vector2(0, 280), new Vector2(760, 130), 30);
        Button chestButton = CreateButton(treasure.transform, "Treasure Chest Image", "보물상자 열기", font, Vector2.zero, new Vector2(360, 360));

        GameObject shaman = CreatePanel(root.transform, "Shaman Panel", new Color(0.12f, 0.06f, 0.16f, 0.98f));
        CreateText(shaman.transform, "Title", "주술사", font, new Vector2(0, 430), new Vector2(800, 90), 48);
        TMP_Text shamanText = CreateText(shaman.transform, "Description", "20골드를 지불하고 무작위 능력을 얻습니다.", font, new Vector2(0, 270), new Vector2(800, 120), 30);
        Button shamanButton = CreateButton(shaman.transform, "Purchase Image", "20골드 - 능력 획득", font, Vector2.zero, new Vector2(500, 180));

        GameObject causality = CreatePanel(root.transform, "Causality Shrine Panel", new Color(0.05f, 0.08f, 0.16f, 0.98f));
        CreateText(causality.transform, "Title", "인과율의 신전", font, new Vector2(0, 480), new Vector2(800, 90), 48);
        Button deleteButton = CreateButton(causality.transform, "Delete Choice Image", "우연 1개 선택 후 삭제\n+ 무작위 능력 1개 삭제", font, new Vector2(-260, 100), new Vector2(440, 430));
        Button gainButton = CreateButton(causality.transform, "Gain Choice Image", "우연 1개 무작위 획득\n+ 능력 1개 선택", font, new Vector2(260, 100), new Vector2(440, 430));
        GameObject choiceContainerObject = new GameObject("Detail Choice Container", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        choiceContainerObject.transform.SetParent(causality.transform, false);
        RectTransform choiceContainer = choiceContainerObject.GetComponent<RectTransform>();
        SetRect(choiceContainer, new Vector2(0, -340), new Vector2(1000, 220));
        HorizontalLayoutGroup layout = choiceContainerObject.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter; layout.spacing = 20f; layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;
        Button choiceTemplate = CreateButton(choiceContainer, "Choice Button Template", "선택지", font, Vector2.zero, new Vector2(300, 190));
        choiceTemplate.gameObject.SetActive(false);

        GameObject spring = CreatePanel(root.transform, "Life Spring Panel", new Color(0.035f, 0.13f, 0.12f, 0.98f));
        CreateText(spring.transform, "Title", "생명의 샘", font, new Vector2(0, 430), new Vector2(800, 90), 48);
        TMP_Text springText = CreateText(spring.transform, "Description", "HP가 전부 회복됩니다.", font, new Vector2(0, 100), new Vector2(800, 260), 30);
        Button springConfirm = CreateButton(spring.transform, "Confirm Image", "확인", font, new Vector2(0, -360), new Vector2(300, 90));

        GameObject thorn = CreatePanel(root.transform, "Thorn Bush Panel", new Color(0.13f, 0.055f, 0.07f, 0.98f));
        CreateText(thorn.transform, "Title", "가시 덤불", font, new Vector2(0, 430), new Vector2(800, 90), 48);
        TMP_Text thornText = CreateText(thorn.transform, "Description", "HP가 5 감소하고 무작위 아이템을 획득합니다.", font, new Vector2(0, 180), new Vector2(850, 180), 30);
        Image thornItem = CreateImage(thorn.transform, "Acquired Item Image", new Vector2(0, -40), new Vector2(180, 180));
        Button thornConfirm = CreateButton(thorn.transform, "Confirm Image", "확인", font, new Vector2(0, -360), new Vector2(300, 90));

        Set(panelSerialized, "treasurePanel", treasure); Set(panelSerialized, "treasureChestButton", chestButton); Set(panelSerialized, "treasureDescriptionText", treasureText);
        Set(panelSerialized, "shamanPanel", shaman); Set(panelSerialized, "shamanPurchaseButton", shamanButton); Set(panelSerialized, "shamanDescriptionText", shamanText);
        Set(panelSerialized, "causalityPanel", causality); Set(panelSerialized, "causalityDeleteButton", deleteButton); Set(panelSerialized, "causalityGainButton", gainButton);
        Set(panelSerialized, "causalityChoiceContainer", choiceContainer); Set(panelSerialized, "causalityChoiceButtonTemplate", choiceTemplate);
        Set(panelSerialized, "lifeSpringPanel", spring); Set(panelSerialized, "lifeSpringConfirmButton", springConfirm); Set(panelSerialized, "lifeSpringDescriptionText", springText);
        Set(panelSerialized, "thornBushPanel", thorn); Set(panelSerialized, "thornBushConfirmButton", thornConfirm); Set(panelSerialized, "thornBushDescriptionText", thornText); Set(panelSerialized, "thornBushItemImage", thornItem);
        panelSerialized.ApplyModifiedPropertiesWithoutUndo();

        treasure.SetActive(false); shaman.SetActive(false); causality.SetActive(false); spring.SetActive(false); thorn.SetActive(false);
        controllerProperty.objectReferenceValue = panelController;
        controllerSerialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(panelController);
        EditorUtility.SetDirty(sceneController);
    }

    private static void EnsureShopItemObjects(Scene scene)
    {
        ShopCatalogUI catalog = FindInScene<ShopCatalogUI>(scene);
        if (catalog == null) return;
        SerializedObject catalogSerialized = new SerializedObject(catalog);
        SerializedProperty viewsProperty = catalogSerialized.FindProperty("itemViews");
        bool complete = viewsProperty != null && viewsProperty.arraySize == 3;
        if (complete)
        {
            for (int i = 0; i < 3; i++)
                complete &= viewsProperty.GetArrayElementAtIndex(i).objectReferenceValue != null;
        }
        if (complete) return;

        Transform container = catalog.transform.Find("Shop Item Container");
        if (container == null)
        {
            GameObject containerObject = new GameObject("Shop Item Container", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            containerObject.transform.SetParent(catalog.transform, false);
            container = containerObject.transform;
            SetRect(containerObject.GetComponent<RectTransform>(), new Vector2(0f, 20f), new Vector2(960f, 520f));
            HorizontalLayoutGroup layout = containerObject.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter; layout.spacing = 30f;
            layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;
        }

        TMP_Text sceneText = FindInScene<TMP_Text>(scene);
        TMP_FontAsset font = sceneText == null ? null : sceneText.font;
        viewsProperty.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            GameObject card = new GameObject($"Shop Item {i + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ShopItemView));
            card.transform.SetParent(container, false);
            card.GetComponent<RectTransform>().sizeDelta = new Vector2(280f, 420f);
            Image background = card.GetComponent<Image>();
            background.color = new Color(0.16f, 0.18f, 0.24f, 1f);

            Image itemImage = CreateImage(card.transform, "Item Image Button", new Vector2(0f, 90f), new Vector2(150f, 150f));
            itemImage.gameObject.AddComponent<Button>().targetGraphic = itemImage;
            TMP_Text description = CreateText(card.transform, "Item Description", string.Empty, font,
                new Vector2(0f, -65f), new Vector2(250f, 125f), 24f);
            TMP_Text price = CreateText(card.transform, "Price Text", string.Empty, font,
                new Vector2(0f, -165f), new Vector2(250f, 50f), 24f);

            ShopItemView view = card.GetComponent<ShopItemView>();
            SerializedObject viewSerialized = new SerializedObject(view);
            Set(viewSerialized, "itemImage", itemImage);
            Set(viewSerialized, "itemDescriptionText", description);
            Set(viewSerialized, "priceText", price);
            viewSerialized.ApplyModifiedPropertiesWithoutUndo();
            viewsProperty.GetArrayElementAtIndex(i).objectReferenceValue = view;
            EditorUtility.SetDirty(view);
        }
        catalogSerialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
    }

    private static void Set(SerializedObject target, string propertyName, Object value)
    {
        target.FindProperty(propertyName).objectReferenceValue = value;
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false); Stretch(panel.GetComponent<RectTransform>());
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private static Button CreateButton(Transform parent, string name, string label, TMP_FontAsset font, Vector2 position, Vector2 size)
    {
        GameObject value = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        value.transform.SetParent(parent, false); SetRect(value.GetComponent<RectTransform>(), position, size);
        value.GetComponent<Image>().color = new Color(0.24f, 0.28f, 0.38f, 1f);
        CreateText(value.transform, "Text", label, font, Vector2.zero, size - new Vector2(24, 24), 25);
        return value.GetComponent<Button>();
    }

    private static TMP_Text CreateText(Transform parent, string name, string textValue, TMP_FontAsset font, Vector2 position, Vector2 size, float fontSize)
    {
        GameObject value = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        value.transform.SetParent(parent, false); SetRect(value.GetComponent<RectTransform>(), position, size);
        TMP_Text text = value.GetComponent<TMP_Text>(); text.font = font; text.text = textValue; text.fontSize = fontSize; text.color = Color.white; text.alignment = TextAlignmentOptions.Center;
        return text;
    }

    private static Image CreateImage(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject value = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        value.transform.SetParent(parent, false); SetRect(value.GetComponent<RectTransform>(), position, size);
        value.GetComponent<Image>().color = Color.white;
        return value.GetComponent<Image>();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.anchoredPosition = position; rect.sizeDelta = size;
    }
}
