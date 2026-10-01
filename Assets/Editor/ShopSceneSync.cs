using System.IO;
using GameCore;
using ScriptableObjects.GameEvents;
using ScriptableObjects.Services;
using Shop;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ShopSceneSync
{
    private const string MenuPath = "Assets/Scenes/Scene_Menu.unity";
    private const string GamePath = "Assets/Scenes/Scene_Game.unity";
    private const string CatalogPath = "Assets/Resources/ShopCatalog.json";
    private const string WalletPath = "Assets/ScriptableObjects/ScriptableObjects_Services/Resources/CoinWallet.asset";
    private const string CardPrefabPath = "Assets/Prefab/Prefab_UI/Prefab_MenuScene/Prefab_ShopItemCard.prefab";
    private const string PowerupCardPrefabPath = "Assets/Resources/Prefab_GamePowerupCard.prefab";
    private const string TabEventPath = "Assets/ScriptableObjects/Scriptableobjects_GameEvents/Scene_Menu_Events/PanelShowEvent.asset";
    private static readonly Color Ink = Hex("#273340");
    private static readonly Color Paper = Hex("#F6F2EA");
    private static readonly Color Card = Hex("#FFFFFF");
    private static readonly Color Teal = Hex("#397F9F");

    public static bool PrepareSceneSync()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Stop Play mode before syncing shop scenes.");
            return false;
        }
        return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
    }

    public static void SyncSceneUiAfterPreparation()
    {
        ShopCatalogData catalog = ShopCatalog.Parse(File.ReadAllText(CatalogPath), out string error);
        if (catalog == null)
            throw new System.InvalidOperationException("Shop catalog: " + error);

        SyncMenu(catalog);
        SyncGame();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Single);
        Debug.Log("Shop UI synced into Scene_Menu and Scene_Game. Save both scene changes to source control.");
    }

    private static void SyncMenu(ShopCatalogData catalog)
    {
        Scene scene = EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Single);
        Transform shop = Find(scene, "ShopPanel");
        if (!shop)
            throw new System.InvalidOperationException("Scene_Menu needs the existing ShopPanel under Canvas.");
        ShopItemCard cardPrefab = AssetDatabase.LoadAssetAtPath<ShopItemCard>(CardPrefabPath);
        if (!cardPrefab)
            throw new System.InvalidOperationException("Missing shop card prefab at " + CardPrefabPath);

        GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(shop.gameObject);
        if (prefabRoot == shop.gameObject)
            PrefabUtility.UnpackPrefabInstance(prefabRoot, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        foreach (string name in new[] { "ComingSoonText", "ShopHeader", "ShopScroll", "ShopFeedback" })
        {
            Transform old = shop.Find(name);
            if (old) Object.DestroyImmediate(old.gameObject);
        }

        Image background = shop.GetComponent<Image>();
        if (background) background.color = Paper;
        ShopPanelController panel = shop.GetComponent<ShopPanelController>();
        if (!panel) panel = shop.gameObject.AddComponent<ShopPanelController>();

        RectTransform header = Ui(shop, "ShopHeader", new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 125));
        TextMeshProUGUI title = Label(header, "ShopTitle", "SHOP", 62, Ink, TextAlignmentOptions.Left);
        Place(title.rectTransform, new Vector2(0, 0), new Vector2(0.6f, 1), new Vector2(0, 0),
            new Vector2(32, 0), Vector2.zero);
        TextMeshProUGUI balance = Label(header, "ShopBalance", "0 COINS", 40, Teal, TextAlignmentOptions.Right);
        Place(balance.rectTransform, new Vector2(0.6f, 0), Vector2.one, new Vector2(1, 0),
            new Vector2(-32, 0), Vector2.zero);

        RectTransform scroll = Ui(shop, "ShopScroll", new Vector2(0, 0), Vector2.one,
            new Vector2(0.5f, 0.5f), new Vector2(0, -13), new Vector2(-36, -215));
        scroll.gameObject.AddComponent<Image>().color = Card;
        ScrollRect scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        RectTransform viewport = Ui(scroll, "Viewport", Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        viewport.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        Mask mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        RectTransform content = Ui(viewport, "Content", new Vector2(0, 1), Vector2.one,
            new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
        VerticalLayoutGroup sectionsLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        sectionsLayout.padding = new RectOffset(18, 18, 18, 18);
        sectionsLayout.spacing = 20;
        sectionsLayout.childAlignment = TextAnchor.UpperCenter;
        sectionsLayout.childControlWidth = true;
        sectionsLayout.childControlHeight = false;
        sectionsLayout.childForceExpandWidth = true;
        sectionsLayout.childForceExpandHeight = false;

        int contentHeight = 36 + (catalog.sections.Length - 1) * 20;
        foreach (ShopSectionData section in catalog.sections)
        {
            int sectionHeight = 56 + section.items.Length * 130;
            contentHeight += sectionHeight;
            RectTransform sectionRect = Ui(content, "Section_" + section.id, new Vector2(0, 1), Vector2.one,
                new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, sectionHeight));
            sectionRect.gameObject.AddComponent<LayoutElement>().preferredHeight = sectionHeight;
            ShopSectionView view = sectionRect.gameObject.AddComponent<ShopSectionView>();
            view.SetSectionId(section.id);
            TextMeshProUGUI sectionTitle = Label(sectionRect, "SectionTitle", section.title, 36, Ink,
                TextAlignmentOptions.Left);
            Place(sectionTitle.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(0.5f, 1),
                new Vector2(12, -2), new Vector2(-24, 48));
            RectTransform cards = Ui(sectionRect, "Cards", new Vector2(0, 1), Vector2.one,
                new Vector2(0.5f, 1), new Vector2(0, -56), new Vector2(0, section.items.Length * 130));
            VerticalLayoutGroup cardLayout = cards.gameObject.AddComponent<VerticalLayoutGroup>();
            cardLayout.spacing = 12;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = false;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;
            SetObject(view, "titleText", sectionTitle);
            SetObject(view, "cardContainer", cards);
        }
        content.sizeDelta = new Vector2(0, contentHeight);
        scrollRect.viewport = viewport;
        scrollRect.content = content;

        TextMeshProUGUI feedback = Label(shop, "ShopFeedback", "BUY TO UNLOCK  •  EQUIP AFTER PURCHASE",
            30, Ink, TextAlignmentOptions.Center);
        Place(feedback.rectTransform, new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(0.5f, 0), new Vector2(0, 25), new Vector2(-50, 50));

        SetObject(panel, "coinWallet", AssetDatabase.LoadAssetAtPath<CoinWallet>(WalletPath));
        SetObject(panel, "balanceText", balance);
        SetObject(panel, "feedbackText", feedback);
        SetObject(panel, "sectionContainer", content);
        SetObject(panel, "itemCardPrefab", cardPrefab);
        SetObject(panel, "insufficientCoinsPopup", SyncInsufficientPopup(scene));
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static InsufficientCoinsPopup SyncInsufficientPopup(Scene scene)
    {
        Transform canvas = Find(scene, "Canvas");
        if (!canvas)
            throw new System.InvalidOperationException("Scene_Menu needs a Canvas for InsufficientCoinsPopup.");
        Transform existing = canvas.Find("InsufficientCoinsPopup");
        if (existing)
        {
            InsufficientCoinsPopup current = existing.GetComponent<InsufficientCoinsPopup>();
            if (!current)
                throw new System.InvalidOperationException("InsufficientCoinsPopup needs its controller component.");
            existing.SetAsLastSibling();
            return current;
        }

        RectTransform overlay = Ui(canvas, "InsufficientCoinsPopup", Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        overlay.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.10f, 0.14f, 0.72f);
        InsufficientCoinsPopup popup = overlay.gameObject.AddComponent<InsufficientCoinsPopup>();
        RectTransform card = Ui(overlay, "Dialog", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 575));
        card.gameObject.AddComponent<Image>().color = Paper;

        TextMeshProUGUI heading = Label(card, "Heading", "NEED MORE COINS", 48, Ink, TextAlignmentOptions.Center);
        Place(heading.rectTransform, new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0.5f, 1), new Vector2(0, -32), new Vector2(-48, 72));
        TextMeshProUGUI item = Label(card, "ItemName", "", 37, Teal, TextAlignmentOptions.Center);
        Place(item.rectTransform, new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0.5f, 1), new Vector2(0, -116), new Vector2(-48, 55));
        TextMeshProUGUI price = Label(card, "Price", "", 30, Ink, TextAlignmentOptions.Center);
        Place(price.rectTransform, new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0.5f, 1), new Vector2(0, -196), new Vector2(-48, 48));
        TextMeshProUGUI balance = Label(card, "Balance", "", 30, Ink, TextAlignmentOptions.Center);
        Place(balance.rectTransform, new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0.5f, 1), new Vector2(0, -251), new Vector2(-48, 48));
        TextMeshProUGUI needed = Label(card, "Needed", "", 31, Teal, TextAlignmentOptions.Center);
        Place(needed.rectTransform, new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0.5f, 1), new Vector2(0, -319), new Vector2(-48, 60));

        Button go = PopupButton(card, "GoPlayButton", "GO PLAY", new Vector2(-168, 56), Teal);
        Button close = PopupButton(card, "CloseButton", "CLOSE", new Vector2(168, 56), Ink);
        SetObject(popup, "itemText", item);
        SetObject(popup, "priceText", price);
        SetObject(popup, "balanceText", balance);
        SetObject(popup, "neededText", needed);
        SetObject(popup, "goPlayButton", go);
        SetObject(popup, "closeButton", close);
        IntEvent tabEvent = AssetDatabase.LoadAssetAtPath<IntEvent>(TabEventPath);
        if (!tabEvent)
            throw new System.InvalidOperationException("Scene_Menu PanelShowEvent asset is missing.");
        SetObject(popup, "onTabSelected", tabEvent);
        overlay.SetAsLastSibling();
        overlay.gameObject.SetActive(false);
        return popup;
    }

    private static Button PopupButton(Transform parent, string name, string text, Vector2 position, Color color)
    {
        RectTransform rect = Ui(parent, name, new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0.5f, 0), position, new Vector2(300, 90));
        rect.gameObject.AddComponent<Image>().color = color;
        Button button = rect.gameObject.AddComponent<Button>();
        TextMeshProUGUI label = Label(rect, "Label", text, 30, Color.white, TextAlignmentOptions.Center);
        Place(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        return button;
    }

    private static void SyncGame()
    {
        Scene scene = SceneManager.GetSceneByPath(GamePath);
        bool openedAdditive = !scene.IsValid() || !scene.isLoaded;
        if (openedAdditive)
            scene = EditorSceneManager.OpenScene(GamePath, OpenSceneMode.Additive);
        Transform canvas = Find(scene, "Canvas");
        Transform ball = Find(scene, "ball");
        Transform ring = Find(scene, "Ring");
        Transform visuals = Find(scene, "GameLogicVisuals");
        if (!canvas || !ball || !ring || !visuals)
            throw new System.InvalidOperationException("Scene_Game needs Canvas, GameLogicVisuals, ball, and Ring.");

        GameThemeApplier theme = visuals.GetComponent<GameThemeApplier>();
        if (!theme) theme = visuals.gameObject.AddComponent<GameThemeApplier>();
        SetObject(theme, "ballRenderer", ball.GetComponent<SpriteRenderer>());
        SetObject(theme, "ringRenderer", ring.GetComponent<SpriteRenderer>());
        Transform trail = ball.Find("Trail");
        if (!trail || !trail.TryGetComponent(out ParticleSystem trailParticles))
            throw new System.InvalidOperationException("Scene_Game needs a Trail ParticleSystem child on ball.");
        SetObject(theme, "trailParticles", trailParticles);

        GamePowerupCard cardPrefab = AssetDatabase.LoadAssetAtPath<GamePowerupCard>(PowerupCardPrefabPath);
        if (!cardPrefab)
            throw new System.InvalidOperationException("Missing game powerup card prefab at " + PowerupCardPrefabPath);

        RectTransform hud = canvas.Find("PowerupHud") as RectTransform;
        if (!hud)
            hud = Ui(canvas, "PowerupHud", Vector2.right, Vector2.right,
                new Vector2(1, 0), new Vector2(-38, 34), new Vector2(520, 90));
        foreach (string name in new[] { "ShieldButton", "DoubleScoreButton" })
        {
            Transform legacy = hud.Find(name);
            if (legacy) Object.DestroyImmediate(legacy.gameObject);
        }

        CanvasGroup group = hud.GetComponent<CanvasGroup>();
        if (!group) group = hud.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
        HorizontalLayoutGroup layout = hud.GetComponent<HorizontalLayoutGroup>();
        if (!layout) layout = hud.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        GamePowerupHud controller = hud.GetComponent<GamePowerupHud>();
        if (!controller) controller = hud.gameObject.AddComponent<GamePowerupHud>();
        SetObject(controller, "ball", ball.GetComponent<BallController>());
        SetObject(controller, "cardContainer", hud);
        SetObject(controller, "cardPrefab", cardPrefab);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (openedAdditive)
            EditorSceneManager.CloseScene(scene, true);
    }

    private static RectTransform Ui(Transform parent, string name, Vector2 anchorMin,
        Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static TextMeshProUGUI Label(Transform parent, string name, string text,
        float size, Color color, TextAlignmentOptions alignment)
    {
        RectTransform rect = Ui(parent, name, Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        return label;
    }

    private static void Place(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot,
        Vector2 position, Vector2 size)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetObject(Component component, string field, Object value)
    {
        SerializedObject so = new SerializedObject(component);
        SerializedProperty property = so.FindProperty(field);
        if (property == null)
            throw new System.InvalidOperationException(component.GetType().Name + " has no field " + field);
        property.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Transform Find(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindRecursive(root.transform, name);
            if (found) return found;
        }
        return null;
    }

    private static Transform FindRecursive(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            Transform found = FindRecursive(child, name);
            if (found) return found;
        }
        return null;
    }

    private static Color Hex(string code)
    {
        ColorUtility.TryParseHtmlString(code, out Color color);
        return color;
    }
}
