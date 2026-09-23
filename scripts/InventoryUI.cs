using Godot;

namespace Sigilwoven;

public partial class InventoryUI : CanvasLayer
{
    private readonly PlayerInventory _inventory;
    private GridContainer? _itemGrid;
    private GridContainer? _equipmentGrid;
    private Label? _summary;
    private Label? _bagTitle;
    private Label? _detail;
    private Label? _currencies;
    private Button? _filterButton;

    public InventoryUI(PlayerInventory inventory) { _inventory = inventory; Layer = 30; }
    public override void _Ready() { BuildWindow(); _inventory.Changed += Refresh; Visible = false; }
    public override void _ExitTree() => _inventory.Changed -= Refresh;
    public bool IsOpen() => Visible;
    public void Toggle() { Visible = !Visible; if (Visible) Refresh(); }

    private void BuildWindow()
    {
        var window = new PanelContainer { AnchorLeft = 0.07f, AnchorRight = 0.93f, AnchorTop = 0.05f, AnchorBottom = 0.95f };
        window.AddThemeStyleboxOverride("panel", MakeStyle(new Color(0.008f, 0.014f, 0.032f, 0.985f), new Color(0.24f, 0.66f, 0.94f), 15, 2, 18));
        AddChild(window);
        var root = new VBoxContainer(); root.AddThemeConstantOverride("separation", 10); window.AddChild(root);
        var header = new HBoxContainer(); root.AddChild(header);
        var title = new Label { Text = "◆  SIGIL ARMORY", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, Modulate = new Color(0.48f, 0.88f, 1f) };
        title.AddThemeFontSizeOverride("font_size", 25); header.AddChild(title);
        _filterButton = new Button(); _filterButton.Pressed += _inventory.CycleLootFilter; header.AddChild(_filterButton);
        var sort = new Button { Text = "SORT BY POWER" }; sort.Pressed += _inventory.SortInventory; header.AddChild(sort);
        var salvageLow = new Button { Text = "SALVAGE COMMON + MAGIC", TooltipText = "Destroys all Common and Magic items in the backpack." };
        salvageLow.Pressed += () => { int count = _inventory.SalvageBelowRare(); if (_detail != null) _detail.Text = $"SALVAGED {count} LOW-RARITY ITEMS"; }; header.AddChild(salvageLow);
        var close = new Button { Text = "CLOSE [I]" }; close.Pressed += Toggle; header.AddChild(close);
        _summary = new Label { Modulate = new Color(0.76f, 0.82f, 1f) }; root.AddChild(_summary);
        _currencies = new Label { Modulate = new Color(1f, 0.76f, 0.3f) }; root.AddChild(_currencies);

        var columns = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill }; columns.AddThemeConstantOverride("separation", 14); root.AddChild(columns);
        var bagPanel = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        bagPanel.AddThemeStyleboxOverride("panel", MakeStyle(new Color(0.018f, 0.03f, 0.06f, 0.96f), new Color(0.13f, 0.3f, 0.5f), 9, 1, 10)); columns.AddChild(bagPanel);
        var bagRoot = new VBoxContainer(); bagPanel.AddChild(bagRoot);
        _bagTitle = new Label { Modulate = new Color(0.62f, 0.82f, 1f) }; _bagTitle.AddThemeFontSizeOverride("font_size", 16); bagRoot.AddChild(_bagTitle);
        var bagScroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill }; bagRoot.AddChild(bagScroll);
        _itemGrid = new GridContainer { Columns = 5, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _itemGrid.AddThemeConstantOverride("h_separation", 7); _itemGrid.AddThemeConstantOverride("v_separation", 7); bagScroll.AddChild(_itemGrid);

        var gearPanel = new PanelContainer { CustomMinimumSize = new Vector2(430f, 0f), SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        gearPanel.AddThemeStyleboxOverride("panel", MakeStyle(new Color(0.022f, 0.028f, 0.052f, 0.97f), new Color(0.48f, 0.31f, 0.72f), 9, 1, 10)); columns.AddChild(gearPanel);
        var gearRoot = new VBoxContainer(); gearPanel.AddChild(gearRoot);
        var gearTitle = new Label { Text = "EQUIPPED RELICS  •  CLICK TO UNEQUIP", Modulate = new Color(1f, 0.73f, 0.3f) };
        gearTitle.AddThemeFontSizeOverride("font_size", 16); gearRoot.AddChild(gearTitle);
        _equipmentGrid = new GridContainer { Columns = 3, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; gearRoot.AddChild(_equipmentGrid);
        _detail = new Label { Text = "Hover an item to inspect it.", AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsVertical = Control.SizeFlags.ExpandFill, Modulate = new Color(0.72f, 0.78f, 0.9f) }; gearRoot.AddChild(_detail);
        var footer = new Label { Text = "F PICKUP  •  HOVER INSPECT  •  EQUIP/SALVAGE ON CARD  •  RARITY BORDER = LOOT QUALITY", HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(0.42f, 0.52f, 0.67f) };
        footer.AddThemeFontSizeOverride("font_size", 10); root.AddChild(footer);
        Refresh();
    }

    private void Refresh()
    {
        if (_itemGrid == null || _equipmentGrid == null || _summary == null || _bagTitle == null || _filterButton == null || _currencies == null) return;
        ClearChildren(_itemGrid); ClearChildren(_equipmentGrid);
        _summary.Text = _inventory.BuildEquipmentSummary();
        _currencies.Text = $"CRAFTING  ◆  {_inventory.SalvageDust} DUST   ◇  {_inventory.SigilShards} SHARDS   ✦  {_inventory.SigilCores} CORES";
        _bagTitle.Text = $"BACKPACK  •  {_inventory.Items.Count}/{PlayerInventory.Capacity} SLOTS";
        _filterButton.Text = $"GROUND LOOT: {_inventory.MinimumVisibleRarity.ToString().ToUpperInvariant()}+";
        foreach (ItemData item in _inventory.Items) _itemGrid.AddChild(BuildBagCard(item));
        for (int i = _inventory.Items.Count; i < PlayerInventory.Capacity; i++) _itemGrid.AddChild(BuildEmptyBagSlot());
        foreach (EquipmentSlot slot in System.Enum.GetValues<EquipmentSlot>()) _equipmentGrid.AddChild(BuildEquipmentCard(slot));
    }

    private Control BuildBagCard(ItemData item)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(132f, 174f), TooltipText = item.BuildTooltip() + "\n\nCOMPARISON\n" + _inventory.BuildComparison(item) };
        panel.AddThemeStyleboxOverride("panel", MakeStyle(new Color(0.025f, 0.035f, 0.06f, 0.98f), item.RarityColor, 7, item.Rarity >= ItemRarity.Rare ? 2 : 1, 5));
        panel.MouseEntered += () => ShowItemDetail(item, true);
        var stack = new VBoxContainer(); stack.AddThemeConstantOverride("separation", 2); panel.AddChild(stack);
        stack.AddChild(new TextureRect { Texture = ItemIconCatalog.Get(item.Slot), CustomMinimumSize = new Vector2(92f, 82f), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = Control.MouseFilterEnum.Ignore });
        var name = new Label { Text = item.Name.ToUpperInvariant(), HorizontalAlignment = HorizontalAlignment.Center, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis, Modulate = item.RarityColor, MouseFilter = Control.MouseFilterEnum.Ignore };
        name.AddThemeFontSizeOverride("font_size", 9); stack.AddChild(name);
        var meta = new Label { Text = $"{item.Slot}  •  PWR {item.GearScore}", HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(0.62f, 0.7f, 0.82f), MouseFilter = Control.MouseFilterEnum.Ignore };
        meta.AddThemeFontSizeOverride("font_size", 8); stack.AddChild(meta);
        var actions = new HBoxContainer(); stack.AddChild(actions);
        bool classAllowed = item.CanEquip(_inventory.ActiveClass);
        var equip = new Button { Text = classAllowed ? "EQUIP" : "LOCKED", Disabled = !classAllowed, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, TooltipText = classAllowed ? "Equip and compare this item." : $"{_inventory.ActiveClass} cannot equip {item.WeaponType}." }; equip.Pressed += () => _inventory.Equip(item); actions.AddChild(equip);
        var salvage = new Button { Text = "DUST", TooltipText = "Destroy for crafting materials." }; salvage.Pressed += () => _inventory.Salvage(item); actions.AddChild(salvage);
        return panel;
    }

    private Control BuildEquipmentCard(EquipmentSlot slot)
    {
        bool occupied = _inventory.Equipped.TryGetValue(slot, out ItemData? item);
        Color border = occupied ? item!.RarityColor : new Color(0.18f, 0.23f, 0.32f);
        var button = new Button { CustomMinimumSize = new Vector2(128f, 142f), TooltipText = occupied ? item!.BuildTooltip() : $"EMPTY {slot.ToString().ToUpperInvariant()} SLOT" };
        button.AddThemeStyleboxOverride("normal", MakeStyle(new Color(0.016f, 0.024f, 0.046f, 0.98f), border, 8, occupied ? 2 : 1, 5));
        var stack = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }; button.AddChild(stack); stack.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        stack.AddChild(new Label { Text = slot.ToString().ToUpperInvariant(), HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(0.58f, 0.68f, 0.82f), MouseFilter = Control.MouseFilterEnum.Ignore });
        stack.AddChild(new TextureRect { Texture = ItemIconCatalog.Get(slot), Modulate = occupied ? Colors.White : new Color(0.22f, 0.25f, 0.3f), CustomMinimumSize = new Vector2(96f, 78f), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = Control.MouseFilterEnum.Ignore });
        var label = new Label { Text = occupied ? item!.Name.ToUpperInvariant() : "EMPTY", HorizontalAlignment = HorizontalAlignment.Center, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis, Modulate = border, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", 8); stack.AddChild(label);
        if (occupied) { button.Pressed += () => _inventory.Unequip(slot); button.MouseEntered += () => ShowItemDetail(item!, false); }
        return button;
    }

    private Control BuildEmptyBagSlot()
    {
        var empty = new PanelContainer { CustomMinimumSize = new Vector2(132f, 174f), MouseFilter = Control.MouseFilterEnum.Ignore };
        empty.AddThemeStyleboxOverride("panel", MakeStyle(new Color(0.012f, 0.018f, 0.032f, 0.75f), new Color(0.1f, 0.15f, 0.23f), 7, 1, 5));
        empty.AddChild(new Label { Text = "◇\nEMPTY", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Modulate = new Color(0.2f, 0.27f, 0.38f), MouseFilter = Control.MouseFilterEnum.Ignore }); return empty;
    }

    private void ShowItemDetail(ItemData item, bool compare)
    {
        if (_detail == null) return;
        _detail.Text = item.BuildTooltip() + (compare ? "\n\nEQUIPPED COMPARISON\n" + _inventory.BuildComparison(item) : "\n\nCURRENTLY EQUIPPED"); _detail.Modulate = item.RarityColor;
    }

    private static void ClearChildren(Node parent) { foreach (Node child in parent.GetChildren()) child.QueueFree(); }
    private static StyleBoxFlat MakeStyle(Color background, Color border, int radius, int width, int margin) => new()
    {
        BgColor = background, BorderColor = border,
        BorderWidthLeft = width, BorderWidthTop = width, BorderWidthRight = width, BorderWidthBottom = width,
        CornerRadiusTopLeft = radius, CornerRadiusTopRight = radius, CornerRadiusBottomLeft = radius, CornerRadiusBottomRight = radius,
        ContentMarginLeft = margin, ContentMarginRight = margin, ContentMarginTop = margin, ContentMarginBottom = margin,
    };
}
