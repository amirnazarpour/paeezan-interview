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
    private VisualElement sectionsRoot;
    private Label status;
    private bool dirty;

    [MenuItem("Tools/Shop/Catalog Editor")]
    public static void Open()
    {
        ShopCatalogEditorWindow window = GetWindow<ShopCatalogEditorWindow>("Shop Catalog");
        window.minSize = new Vector2(600, 500);
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;
        root.Clear();
        root.style.paddingLeft = 12;
        root.style.paddingRight = 12;
        root.style.paddingTop = 12;
        root.style.paddingBottom = 12;

        Label title = new Label("Shop Catalog");
        title.style.fontSize = 20;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        root.Add(title);
        root.Add(new Label("Edit catalog data; cards are instantiated from Prefab_ShopItemCard at runtime."));

        VisualElement toolbar = Row();
        toolbar.style.marginTop = 10;
        toolbar.Add(new Button(() => ReloadFromDisk(true)) { text = "Reload JSON" });
        toolbar.Add(new Button(SaveAndSyncScenes) { text = "Save and Sync Scenes" });
        root.Add(toolbar);
        status = new Label();
        status.style.marginTop = 8;
        status.style.marginBottom = 8;
        root.Add(status);

        ScrollView scroll = new ScrollView();
        scroll.style.flexGrow = 1;
        sectionsRoot = new VisualElement();
        scroll.Add(sectionsRoot);
        root.Add(scroll);
        root.Add(new Button(AddSection) { text = "Add Section" });
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
            SetStatus("Missing " + CatalogPath, true);
            return;
        }
        ShopCatalogData loaded = ShopCatalog.Parse(File.ReadAllText(CatalogPath), out string error);
        if (loaded == null)
        {
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
        BuildSections();
        SetStatus("Loaded " + CatalogPath, false);
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

    private void BuildSections()
    {
        sectionsRoot.Clear();
        if (draft == null) return;
        for (int s = 0; s < draft.sections.Length; s++)
        {
            int sectionIndex = s;
            ShopSectionData section = draft.sections[s];
            VisualElement box = Box();
            VisualElement header = Row();
            Label name = new Label("Section " + (s + 1));
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(name);
            header.Add(ActionButton("↑", () => MoveSection(sectionIndex, -1), s > 0));
            header.Add(ActionButton("↓", () => MoveSection(sectionIndex, 1), s < draft.sections.Length - 1));
            header.Add(ActionButton("Remove", () => RemoveSection(sectionIndex), !ContainsRequired(section)));
            box.Add(header);

            TextField id = new TextField("Section ID") { value = section.id };
            id.SetEnabled(!savedSections.Contains(section));
            id.RegisterValueChangedCallback(e => { section.id = e.newValue; MarkDirty(); });
            box.Add(id);
            TextField title = new TextField("Section title") { value = section.title };
            title.RegisterValueChangedCallback(e => { section.title = e.newValue; MarkDirty(); });
            box.Add(title);

            for (int i = 0; i < section.items.Length; i++)
                box.Add(BuildItem(sectionIndex, i));

            VisualElement add = Row();
            add.Add(new Button(() => AddItem(sectionIndex, ShopItemKind.BallTheme)) { text = "Add Ball Theme" });
            add.Add(new Button(() => AddItem(sectionIndex, ShopItemKind.WorldTheme)) { text = "Add Ring Theme" });
            box.Add(add);
            sectionsRoot.Add(box);
        }
    }

    private VisualElement BuildItem(int sectionIndex, int itemIndex)
    {
        ShopSectionData section = draft.sections[sectionIndex];
        ShopItemData item = section.items[itemIndex];
        bool required = IsRequired(item.id);
        VisualElement box = Box();
        box.style.marginLeft = 16;
        VisualElement header = Row();
        header.Add(new Label(item.kind + "  •  " + item.id));
        header.Add(ActionButton("↑", () => MoveItem(sectionIndex, itemIndex, -1), itemIndex > 0));
        header.Add(ActionButton("↓", () => MoveItem(sectionIndex, itemIndex, 1), itemIndex < section.items.Length - 1));
        header.Add(ActionButton("Remove", () => RemoveItem(sectionIndex, itemIndex), !required));
        box.Add(header);

        TextField id = new TextField("Item ID") { value = item.id };
        id.SetEnabled(!savedItems.Contains(item));
        id.RegisterValueChangedCallback(e => { item.id = e.newValue; MarkDirty(); });
        box.Add(id);
        TextField title = new TextField("Name") { value = item.title };
        title.RegisterValueChangedCallback(e => { item.title = e.newValue; MarkDirty(); });
        box.Add(title);
        TextField description = new TextField("Description") { value = item.description };
        description.RegisterValueChangedCallback(e => { item.description = e.newValue; MarkDirty(); });
        box.Add(description);
        IntegerField price = new IntegerField("Price") { value = item.price };
        price.SetEnabled(item.id != ShopItemIds.BallDefault && item.id != ShopItemIds.WorldDefault);
        price.RegisterValueChangedCallback(e => { item.price = e.newValue; MarkDirty(); });
        box.Add(price);

        if (ShopCatalog.IsTheme(item.kind))
        {
            ColorUtility.TryParseHtmlString(item.color, out Color parsedColor);
            ColorField color = new ColorField("Theme color") { value = parsedColor };
            color.RegisterValueChangedCallback(e =>
            {
                item.color = "#" + ColorUtility.ToHtmlStringRGBA(e.newValue);
                MarkDirty();
            });
            box.Add(color);
        }
        if (item.kind == ShopItemKind.DoubleScore)
        {
            FloatField duration = new FloatField("Duration (seconds)") { value = item.duration };
            duration.RegisterValueChangedCallback(e => { item.duration = e.newValue; MarkDirty(); });
            box.Add(duration);
            IntegerField multiplier = new IntegerField("Score multiplier") { value = item.scoreMultiplier };
            multiplier.RegisterValueChangedCallback(e => { item.scoreMultiplier = e.newValue; MarkDirty(); });
            box.Add(multiplier);
        }
        if (item.kind == ShopItemKind.Shield)
        {
            IntegerField hits = new IntegerField("Blocked hits") { value = item.shieldHits };
            hits.RegisterValueChangedCallback(e => { item.shieldHits = e.newValue; MarkDirty(); });
            box.Add(hits);
        }
        return box;
    }

    private void AddSection()
    {
        if (draft == null) return;
        var sections = new List<ShopSectionData>(draft.sections)
        {
            new ShopSectionData { id = UniqueSectionId("new_section"), title = "NEW SECTION", items = Array.Empty<ShopItemData>() }
        };
        draft.sections = sections.ToArray();
        MarkDirty();
        BuildSections();
    }

    private void AddItem(int sectionIndex, ShopItemKind kind)
    {
        ShopSectionData section = draft.sections[sectionIndex];
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
        MarkDirty();
        BuildSections();
    }

    private void RemoveSection(int index)
    {
        ShopSectionData section = draft.sections[index];
        if (ContainsRequired(section)) return;
        if (!EditorUtility.DisplayDialog("Remove section?", "Remove " + section.title + " and all its items?", "Remove", "Cancel")) return;
        var sections = new List<ShopSectionData>(draft.sections);
        sections.RemoveAt(index);
        draft.sections = sections.ToArray();
        MarkDirty();
        BuildSections();
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
        MarkDirty();
        BuildSections();
    }

    private void MoveSection(int index, int direction)
    {
        Swap(draft.sections, index, index + direction);
        MarkDirty();
        BuildSections();
    }

    private void MoveItem(int sectionIndex, int index, int direction)
    {
        Swap(draft.sections[sectionIndex].items, index, index + direction);
        MarkDirty();
        BuildSections();
    }

    private static void Swap<T>(T[] values, int a, int b)
    {
        T temp = values[a];
        values[a] = values[b];
        values[b] = temp;
    }

    private string UniqueSectionId(string seed)
    {
        string candidate = seed;
        int number = 2;
        while (Array.Exists(draft.sections, section => section.id == candidate))
            candidate = seed + "_" + number++;
        return candidate;
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

    private static VisualElement Box()
    {
        var box = new VisualElement();
        box.style.paddingLeft = 8;
        box.style.paddingRight = 8;
        box.style.paddingTop = 8;
        box.style.paddingBottom = 8;
        box.style.marginBottom = 10;
        box.style.backgroundColor = new Color(0.2f, 0.23f, 0.27f, 0.15f);
        return box;
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
        SetStatus("Unsaved catalog edits", false);
    }

    private void SetStatus(string message, bool error)
    {
        if (status == null) return;
        status.text = message;
        status.style.color = error ? new Color(0.8f, 0.2f, 0.2f) : new Color(0.16f, 0.55f, 0.32f);
    }
}
