using System;
using System.Collections.Generic;
using System.IO;
using Shop;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class ShopCatalogEditorWindow : EditorWindow
{
    private const string CatalogPath = "Assets/Resources/ShopCatalog.json";
    private ShopCatalogData draft;
    private readonly HashSet<ShopSectionData> savedSections = new HashSet<ShopSectionData>();
    private readonly HashSet<ShopItemData> savedItems = new HashSet<ShopItemData>();
    private ShopSectionData selectedSection;
    private ShopItemData selectedItem;
    private ScrollView navigationScroll;
    private ScrollView detailsScroll;
    private VisualElement navigationRoot;
    private VisualElement detailsRoot;
    private Button saveButton;
    private Label status;
    private bool dirty;

    [MenuItem("Tools/Shop/Catalog Editor")]
    public static void Open()
    {
        ShopCatalogEditorWindow window = GetWindow<ShopCatalogEditorWindow>("Shop Catalog");
        window.minSize = new Vector2(720, 500);
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;
        root.Clear();
        root.style.paddingLeft = 12;
        root.style.paddingRight = 12;
        root.style.paddingTop = 12;
        root.style.paddingBottom = 12;

        VisualElement header = Row();
        header.style.justifyContent = Justify.SpaceBetween;
        Label title = new Label("Shop Catalog");
        title.style.fontSize = 20;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.Add(title);
        VisualElement actions = Row();
        actions.Add(new Button(() => ReloadFromDisk(true)) { text = "Discard / Reload" });
        saveButton = new Button(SaveAndSyncScenes) { text = "Save and Sync Scenes" };
        actions.Add(saveButton);
        header.Add(actions);
        root.Add(header);
        root.Add(new Label("Choose a section or item, edit it, then save. New themes appear in the shop after scene sync."));

        status = new Label();
        status.style.marginTop = 8;
        status.style.marginBottom = 8;
        root.Add(status);

        VisualElement workspace = Row();
        workspace.style.flexGrow = 1;
        workspace.style.minHeight = 300;
        root.Add(workspace);

        navigationScroll = new ScrollView();
        navigationScroll.style.width = 250;
        navigationScroll.style.flexShrink = 0;
        navigationScroll.style.backgroundColor = new Color(0.16f, 0.18f, 0.22f, 0.08f);
        navigationScroll.style.marginRight = 12;
        navigationScroll.style.paddingLeft = 6;
        navigationScroll.style.paddingRight = 6;
        navigationRoot = new VisualElement();
        navigationScroll.Add(navigationRoot);
        workspace.Add(navigationScroll);

        detailsScroll = new ScrollView();
        detailsScroll.style.flexGrow = 1;
        detailsRoot = new VisualElement();
        detailsRoot.style.paddingRight = 8;
        detailsScroll.Add(detailsRoot);
        workspace.Add(detailsScroll);
        ReloadFromDisk(false);
    }

    private void ReloadFromDisk(bool confirmDiscard)
    {
        if (confirmDiscard && dirty &&
            !EditorUtility.DisplayDialog("Discard catalog edits?", "Unsaved edits in this window will be lost.",
                "Discard", "Cancel"))
            return;

        if (!File.Exists(CatalogPath))
        {
            draft = null;
            selectedSection = null;
            selectedItem = null;
            RefreshViews();
            SetStatus("Missing " + CatalogPath, true);
            return;
        }
        string selectedSectionId = selectedSection?.id;
        string selectedItemId = selectedItem?.id;
        ShopCatalogData loaded = ShopCatalog.Parse(File.ReadAllText(CatalogPath), out string error);
        if (loaded == null)
        {
            draft = null;
            selectedSection = null;
            selectedItem = null;
            RefreshViews();
            SetStatus("Catalog is invalid: " + error, true);
            return;
        }
        draft = loaded;
        dirty = false;
        savedSections.Clear();
        savedItems.Clear();
        foreach (ShopSectionData section in draft.sections)
        {
            savedSections.Add(section);
            foreach (ShopItemData item in section.items)
                savedItems.Add(item);
        }
        selectedSection = Array.Find(draft.sections, section => section.id == selectedSectionId) ??
            draft.sections[0];
        selectedItem = Array.Find(selectedSection.items, item => item.id == selectedItemId);
        RefreshViews();
    }

    private void SaveAndSyncScenes()
    {
        if (draft == null) return;
        string json = JsonUtility.ToJson(draft, true);
        if (ShopCatalog.Parse(json, out string error) == null)
        {
            SetStatus("Fix catalog before saving: " + error, true);
            return;
        }
        if (!ShopSceneSync.PrepareSceneSync())
        {
            SetStatus("Save and sync cancelled; catalog JSON was not changed.", true);
            return;
        }

        try
        {
            File.WriteAllText(CatalogPath, json + Environment.NewLine);
            AssetDatabase.ImportAsset(CatalogPath, ImportAssetOptions.ForceUpdate);
            ShopCatalog.Reload();
            ShopSceneSync.SyncSceneUiAfterPreparation();
            ReloadFromDisk(false);
            SetStatus("Catalog saved and Scene_Menu/Scene_Game synced.", false);
        }
        catch (Exception ex)
        {
            SetStatus("Sync failed: " + ex.Message + " Check the saved JSON and run Tools > Shop > Sync Scene UI.", true);
            Debug.LogException(ex);
        }
    }

    private void RefreshViews()
    {
        BuildNavigation();
        BuildDetails();
        detailsScroll.scrollOffset = Vector2.zero;
        UpdateValidation();
    }

    private void BuildNavigation()
    {
        Vector2 scrollOffset = navigationScroll.scrollOffset;
        navigationRoot.Clear();
        if (draft == null) return;

        VisualElement heading = Row();
        Label label = new Label("SECTIONS");
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.flexGrow = 1;
        heading.Add(label);
        navigationRoot.Add(heading);

        foreach (ShopSectionData section in draft.sections)
        {
            Button sectionButton = NavigationButton(section.title + "  (" + section.items.Length + ")",
                () => Select(section, null), section == selectedSection && selectedItem == null);
            sectionButton.style.marginTop = 6;
            navigationRoot.Add(sectionButton);

            foreach (ShopItemData item in section.items)
            {
                string price = item.price == 0 ? "Free" : item.price + " coins";
                Button itemButton = NavigationButton(item.title + "  ·  " + price,
                    () => Select(section, item), item == selectedItem);
                itemButton.style.marginLeft = 14;
                navigationRoot.Add(itemButton);
            }
        }
        navigationScroll.scrollOffset = scrollOffset;
    }

    private void Select(ShopSectionData section, ShopItemData item)
    {
        selectedSection = section;
        selectedItem = item;
        BuildNavigation();
        BuildDetails();
        detailsScroll.scrollOffset = Vector2.zero;
    }

    private void BuildDetails()
    {
        detailsRoot.Clear();
        if (selectedSection == null)
        {
            detailsRoot.Add(new Label("Choose a section to start editing."));
            return;
        }
        if (selectedItem == null)
            BuildSectionDetails();
        else
            BuildItemDetails();
    }

    private void BuildSectionDetails()
    {
        ShopSectionData section = selectedSection;
        int index = Array.IndexOf(draft.sections, section);
        detailsRoot.Add(Heading(section.title, "SECTION"));
        detailsRoot.Add(new Label("Sections appear in this order in the shop."));

        TextField title = new TextField("Section name") { value = section.title };
        title.RegisterValueChangedCallback(e => { section.title = e.newValue; MarkDirty(); });
        detailsRoot.Add(title);
        TextField id = new TextField("Section ID") { value = section.id };
        id.SetEnabled(!savedSections.Contains(section));
        id.RegisterValueChangedCallback(e => { section.id = e.newValue; MarkDirty(); });
        detailsRoot.Add(id);
        if (savedSections.Contains(section))
            detailsRoot.Add(Hint("Saved IDs are fixed because other data may refer to them."));

        VisualElement order = Row();
        order.style.marginTop = 14;
        order.Add(ActionButton("Move up", () => MoveSection(index, -1), index > 0));
        order.Add(ActionButton("Move down", () => MoveSection(index, 1), index < draft.sections.Length - 1));
        order.Add(ActionButton("Remove section", () => RemoveSection(index), !ContainsRequired(section)));
        detailsRoot.Add(order);

        ShopItemKind addKind = ThemeKindForSection(section);
        if (ShopCatalog.IsTheme(addKind))
        {
            detailsRoot.Add(Heading("Add a theme", "ITEMS"));
            detailsRoot.Add(new Button(() => AddItem(index)) { text = AddItemLabel(addKind) });
        }
    }

    private void BuildItemDetails()
    {
        ShopSectionData section = selectedSection;
        ShopItemData item = selectedItem;
        int sectionIndex = Array.IndexOf(draft.sections, section);
        int itemIndex = Array.IndexOf(section.items, item);
        detailsRoot.Add(new Button(() => Select(section, null)) { text = "← " + section.title });
        detailsRoot.Add(Heading(item.title, KindLabel(item.kind)));

        TextField title = new TextField("Name shown in shop") { value = item.title };
        title.RegisterValueChangedCallback(e => { item.title = e.newValue; MarkDirty(); });
        detailsRoot.Add(title);
        TextField description = new TextField("Description") { value = item.description, multiline = true };
        description.RegisterValueChangedCallback(e => { item.description = e.newValue; MarkDirty(); });
        detailsRoot.Add(description);
        IntegerField price = new IntegerField("Price in coins") { value = item.price };
        bool freeDefault = item.id == ShopItemIds.BallDefault || item.id == ShopItemIds.WorldDefault;
        price.SetEnabled(!freeDefault);
        price.RegisterValueChangedCallback(e => { item.price = e.newValue; MarkDirty(); });
        detailsRoot.Add(price);

        if (ShopCatalog.IsTheme(item.kind))
        {
            ColorUtility.TryParseHtmlString(item.color, out Color parsedColor);
            ColorField color = new ColorField("Theme color") { value = parsedColor };
            color.RegisterValueChangedCallback(e =>
            {
                item.color = "#" + ColorUtility.ToHtmlStringRGBA(e.newValue);
                MarkDirty();
            });
            detailsRoot.Add(color);
        }
        else if (item.kind == ShopItemKind.DoubleScore)
        {
            FloatField duration = new FloatField("Duration (seconds)") { value = item.duration };
            duration.RegisterValueChangedCallback(e => { item.duration = e.newValue; MarkDirty(); });
            detailsRoot.Add(duration);
            IntegerField multiplier = new IntegerField("Score multiplier") { value = item.scoreMultiplier };
            multiplier.RegisterValueChangedCallback(e => { item.scoreMultiplier = e.newValue; MarkDirty(); });
            detailsRoot.Add(multiplier);
        }
        else if (item.kind == ShopItemKind.Shield)
        {
            IntegerField hits = new IntegerField("Hits blocked") { value = item.shieldHits };
            hits.RegisterValueChangedCallback(e => { item.shieldHits = e.newValue; MarkDirty(); });
            detailsRoot.Add(hits);
        }

        detailsRoot.Add(Heading("Catalog ID", "ADVANCED"));
        TextField id = new TextField("Item ID") { value = item.id };
        id.SetEnabled(!savedItems.Contains(item));
        id.RegisterValueChangedCallback(e => { item.id = e.newValue; MarkDirty(); });
        detailsRoot.Add(id);
        detailsRoot.Add(Hint(savedItems.Contains(item) ?
            "Saved IDs are fixed because player inventory uses them." :
            "Set the new ID before saving. Use lowercase letters, numbers, and underscores."));

        VisualElement order = Row();
        order.style.marginTop = 14;
        order.Add(ActionButton("Move up", () => MoveItem(sectionIndex, itemIndex, -1), itemIndex > 0));
        order.Add(ActionButton("Move down", () => MoveItem(sectionIndex, itemIndex, 1),
            itemIndex < section.items.Length - 1));
        order.Add(ActionButton("Remove item", () => RemoveItem(sectionIndex, itemIndex), !IsRequired(item.id)));
        detailsRoot.Add(order);

        ShopItemKind addKind = ThemeKindForSection(section);
        if (ShopCatalog.IsTheme(addKind))
            detailsRoot.Add(new Button(() => AddItem(sectionIndex)) { text = AddItemLabel(addKind) });
    }

    private static ShopItemKind ThemeKindForSection(ShopSectionData section)
    {
        ShopItemKind kind = ShopItemKind.Unknown;
        foreach (ShopItemData item in section.items)
        {
            if (!ShopCatalog.IsTheme(item.kind)) return ShopItemKind.Unknown;
            if (kind != ShopItemKind.Unknown && kind != item.kind) return ShopItemKind.Unknown;
            kind = item.kind;
        }
        return kind;
    }

    private static string AddItemLabel(ShopItemKind kind)
    {
        return kind == ShopItemKind.BallTheme ? "+ Add ball theme" : "+ Add ring theme";
    }

    private void AddItem(int sectionIndex)
    {
        ShopSectionData section = draft.sections[sectionIndex];
        ShopItemKind kind = ThemeKindForSection(section);
        if (!ShopCatalog.IsTheme(kind)) return;
        var items = new List<ShopItemData>(section.items)
        {
            new ShopItemData
            {
                id = UniqueItemId(kind == ShopItemKind.BallTheme ? "ball_new" : "world_new"),
                title = "New Theme", description = "Describe this theme", kind = kind,
                price = 15, color = "#FFFFFF"
            }
        };
        section.items = items.ToArray();
        selectedSection = section;
        selectedItem = section.items[section.items.Length - 1];
        dirty = true;
        RefreshViews();
    }

    private void RemoveSection(int index)
    {
        ShopSectionData section = draft.sections[index];
        if (ContainsRequired(section)) return;
        if (!EditorUtility.DisplayDialog("Remove section?", "Remove " + section.title + " and all its items?", "Remove", "Cancel")) return;
        var sections = new List<ShopSectionData>(draft.sections);
        sections.RemoveAt(index);
        draft.sections = sections.ToArray();
        selectedSection = draft.sections[Mathf.Min(index, draft.sections.Length - 1)];
        selectedItem = null;
        dirty = true;
        RefreshViews();
    }

    private void RemoveItem(int sectionIndex, int itemIndex)
    {
        ShopSectionData section = draft.sections[sectionIndex];
        ShopItemData item = section.items[itemIndex];
        if (IsRequired(item.id)) return;
        if (!EditorUtility.DisplayDialog("Remove item?", "Remove " + item.title + "? Existing player ownership for this ID will no longer appear in the shop.", "Remove", "Cancel")) return;
        var items = new List<ShopItemData>(section.items);
        items.RemoveAt(itemIndex);
        section.items = items.ToArray();
        selectedItem = null;
        dirty = true;
        RefreshViews();
    }

    private void MoveSection(int index, int direction)
    {
        Swap(draft.sections, index, index + direction);
        dirty = true;
        RefreshViews();
    }

    private void MoveItem(int sectionIndex, int index, int direction)
    {
        Swap(draft.sections[sectionIndex].items, index, index + direction);
        dirty = true;
        RefreshViews();
    }

    private static void Swap<T>(T[] values, int a, int b)
    {
        T temp = values[a];
        values[a] = values[b];
        values[b] = temp;
    }

    private string UniqueItemId(string seed)
    {
        string candidate = seed;
        int number = 2;
        while (Array.Exists(draft.sections, section => Array.Exists(section.items, item => item.id == candidate)))
            candidate = seed + "_" + number++;
        return candidate;
    }

    private static bool IsRequired(string id)
    {
        return id == ShopItemIds.Shield || id == ShopItemIds.DoubleScore ||
            id == ShopItemIds.BallDefault || id == ShopItemIds.WorldDefault;
    }

    private static bool ContainsRequired(ShopSectionData section)
    {
        return Array.Exists(section.items, item => IsRequired(item.id));
    }

    private static VisualElement Row()
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.marginBottom = 5;
        return row;
    }

    private static Button NavigationButton(string caption, Action action, bool selected)
    {
        var button = new Button(action) { text = caption };
        button.style.height = 30;
        button.style.unityTextAlign = TextAnchor.MiddleLeft;
        if (selected)
            button.style.backgroundColor = new Color(0.35f, 0.63f, 0.78f, 0.45f);
        return button;
    }

    private static VisualElement Heading(string title, string category)
    {
        var heading = new VisualElement();
        heading.style.marginTop = 12;
        heading.style.marginBottom = 12;
        Label eyebrow = new Label(category);
        eyebrow.style.fontSize = 10;
        eyebrow.style.unityFontStyleAndWeight = FontStyle.Bold;
        heading.Add(eyebrow);
        Label name = new Label(title);
        name.style.fontSize = 19;
        name.style.unityFontStyleAndWeight = FontStyle.Bold;
        heading.Add(name);
        return heading;
    }

    private static Label Hint(string message)
    {
        var hint = new Label(message);
        hint.style.color = new Color(0.42f, 0.45f, 0.49f);
        hint.style.marginBottom = 8;
        return hint;
    }

    private static string KindLabel(ShopItemKind kind)
    {
        switch (kind)
        {
            case ShopItemKind.Shield: return "SHIELD POWERUP";
            case ShopItemKind.DoubleScore: return "SCORE POWERUP";
            case ShopItemKind.BallTheme: return "BALL THEME";
            case ShopItemKind.WorldTheme: return "RING THEME";
            default: return "ITEM";
        }
    }

    private static Button ActionButton(string caption, Action action, bool enabled)
    {
        var button = new Button(action) { text = caption };
        button.SetEnabled(enabled);
        return button;
    }

    private void MarkDirty()
    {
        dirty = true;
        BuildNavigation();
        UpdateValidation();
    }

    private void UpdateValidation()
    {
        if (draft == null)
        {
            saveButton.SetEnabled(false);
            return;
        }
        bool valid = ShopCatalog.Parse(JsonUtility.ToJson(draft), out string error) != null;
        saveButton.SetEnabled(valid);
        SetStatus(valid ? (dirty ? "Unsaved changes. Save and Sync Scenes when ready." :
            "Catalog loaded. Select a section or item to edit.") : "Fix before saving: " + error, !valid);
    }

    private void SetStatus(string message, bool error)
    {
        if (status == null) return;
        status.text = message;
        status.style.color = error ? new Color(0.8f, 0.2f, 0.2f) : new Color(0.16f, 0.55f, 0.32f);
    }
}
