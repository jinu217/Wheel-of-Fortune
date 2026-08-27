using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ShopInnSceneInstaller
{
    private const string ScenePath = "Assets/Scenes/IGS.unity";
    private const string MarkerPath = "Assets/Scenes/IGS_ShopInnConfigured_v1.marker.txt";
    private const string FontPath = "Assets/Fonts/RIDIBatang SDF.asset";

    static ShopInnSceneInstaller()
    {
        EditorSceneManager.sceneOpened += HandleSceneOpened;
        EditorApplication.playModeStateChanged += HandlePlayModeChanged;
        EditorApplication.delayCall += TryBuildActiveScene;
    }

    [MenuItem("Tools/Wheel of Fortune/IGS 상점 여관 UI 다시 구성")]
    public static void RebuildFromMenu()
    {
        if (File.Exists(MarkerPath)) File.Delete(MarkerPath);
        TryBuildActiveScene();
    }

    private static void HandleSceneOpened(Scene scene, OpenSceneMode mode)
    {
        TryBuild(scene);
    }

    private static void HandlePlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += TryBuildActiveScene;
    }

    private static void TryBuildActiveScene()
    {
        TryBuild(SceneManager.GetActiveScene());
    }

    private static void TryBuild(Scene scene)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath || File.Exists(MarkerPath)) return;

        InGameEventCoordinator coordinator = FindInScene<InGameEventCoordinator>(scene);
        ShopManager shopManager = FindInScene<ShopManager>(scene);
        InnManager innManager = FindInScene<InnManager>(scene);
        if (coordinator == null || shopManager == null || innManager == null)
        {
            Debug.LogError("IGS 상점·여관 UI 구성에 필요한 Manager가 없습니다.");
            return;
        }

        GameObject oldCanvas = FindRoot(scene, "Shop Inn Canvas");
        if (oldCanvas != null) Object.DestroyImmediate(oldCanvas);

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        GameObject canvasObject = new GameObject("Shop Inn Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(canvasObject, scene);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);

        GameObject shopPanel = CreatePanel("Shop Panel", canvasObject.transform, new Color(0.055f, 0.07f, 0.11f, 0.98f));
        CreateText("Shop Title", shopPanel.transform, "상점", font, 48f, new Vector2(0f, 500f), new Vector2(800f, 90f));
        GameObject container = new GameObject("Shop Item Container", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        container.transform.SetParent(shopPanel.transform, false);
        RectTransform containerRect = container.GetComponent<RectTransform>();
        SetRect(containerRect, new Vector2(0f, 30f), new Vector2(960f, 520f));
        HorizontalLayoutGroup layout = container.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 30f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        ShopCatalogUI catalog = shopPanel.AddComponent<ShopCatalogUI>();
        SetObjectReference(catalog, "shopManager", shopManager);
        SetObjectReference(catalog, "itemContainer", container.transform);

        GameObject innPanel = CreatePanel("Inn Panel", canvasObject.transform, new Color(0.09f, 0.065f, 0.04f, 0.98f));
        CreateText("Inn Title", innPanel.transform, "여관", font, 48f, new Vector2(0f, 500f), new Vector2(800f, 90f));
        InnPanelUI innPanelUI = innPanel.AddComponent<InnPanelUI>();
        innPanelUI.SetManager(innManager);

        GameObject systems = shopManager.gameObject;
        ShopInnFlowController flow = systems.GetComponent<ShopInnFlowController>();
        if (flow == null) flow = systems.AddComponent<ShopInnFlowController>();
        SetObjectReference(flow, "eventCoordinator", coordinator);
        SetObjectReference(flow, "shopManager", shopManager);
        SetObjectReference(flow, "shopCatalog", catalog);
        SetObjectReference(flow, "shopPanel", shopPanel);
        SetObjectReference(flow, "innPanel", innPanel);
        SetObjectReference(flow, "innPanelUI", innPanelUI);
        SetObjectReference(flow, "innManager", innManager);

        Button closeShop = CreateButton("Close Shop", shopPanel.transform, "상점 닫기", font, new Vector2(0f, -560f));
        SetObjectReference(catalog, "exitButtonObject", closeShop.gameObject);
        UnityEventTools.AddPersistentListener(closeShop.onClick, flow.CloseShopAndOpenInn);
        Button closeInn = CreateButton("Close Inn", innPanel.transform, "여관 닫기", font, new Vector2(0f, -560f));
        SetObjectReference(innPanelUI, "exitButtonObject", closeInn.gameObject);
        UnityEventTools.AddPersistentListener(closeInn.onClick, flow.CloseInn);

        shopPanel.SetActive(false);
        innPanel.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        File.WriteAllText(MarkerPath, "IGS shop and inn panels configured.\n");
        AssetDatabase.ImportAsset(MarkerPath);
        AssetDatabase.SaveAssets();
        Debug.Log("IGS 상점·여관 패널과 인스펙터 참조 연결을 완료했습니다.");
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private static Button CreateButton(string name, Transform parent, string label, TMP_FontAsset font, Vector2 position)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        SetRect(buttonObject.GetComponent<RectTransform>(), position, new Vector2(320f, 90f));
        buttonObject.GetComponent<Image>().color = new Color(0.25f, 0.42f, 0.30f, 1f);
        CreateText("Text", buttonObject.transform, label, font, 28f, Vector2.zero, new Vector2(300f, 75f));
        return buttonObject.GetComponent<Button>();
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, TMP_FontAsset font, float size, Vector2 position, Vector2 dimensions)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        SetRect(textObject.GetComponent<RectTransform>(), position, dimensions);
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        return text;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetObjectReference(Object target, string propertyName, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
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

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == name) return root;
        return null;
    }
}
