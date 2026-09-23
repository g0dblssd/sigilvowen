using Godot;

namespace Sigilwoven;

public partial class InventoryUI : CanvasLayer
{
    private readonly PlayerInventory _inventory;
    private VBoxContainer? _itemList;
    private VBoxContainer? _equipmentList;
    private Label? _summary;
    private Label? _bagTitle;
    private Button? _filterButton;

    public InventoryUI(PlayerInventory inventory)
    {
        _inventory = inventory;
        Layer = 30;
    }

    public override void _Ready()
    {
        BuildWindow();
        _inventory.Changed += Refresh;
        Visible = false;
    }

    public bool IsOpen() => Visible;

    public void Toggle()
    {
        Visible = !Visible;
        if (Visible) Refresh();
    }

    private void BuildWindow()
    {
        var window = new PanelContainer
        {
            AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f,
            OffsetLeft = -475f, OffsetRight = 475f, OffsetTop = -315f, OffsetBottom = 315f,
        };
        window.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.012f, 0.02f, 0.045f, 0.98f), BorderColor = new Color(0.27f, 0.65f, 0.9f),
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12, CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12,
            ContentMarginLeft = 18f, ContentMarginRight = 18f, ContentMarginTop = 14f, ContentMarginBottom = 14f,
        });
        AddChild(window);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 10);
        window.AddChild(root);
        var header = new HBoxContainer();
        root.AddChild(header);
        var title = new Label { Text = "SIGIL CACHE  //  INVENTORY", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, Modulate = new Color(0.5f, 0.88f, 1f) };
        title.AddThemeFontSizeOverride("font_size", 22);
        header.AddChild(title);
        var close = new Button { Text = "CLOSE  [I]" };
        close.Pressed += Toggle;
        header.AddChild(close);

        _filterButton = new Button();
        _filterButton.Pressed += _inventory.CycleLootFilter;
        header.AddChild(_filterButton);

        _summary = new Label { Modulate = new Color(0.82f, 0.72f, 1f) };
        root.AddChild(_summary);
        var columns = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        columns.AddThemeConstantOverride("separation", 16);
        root.AddChild(columns);

        var bagPanel = new PanelContainer { CustomMinimumSize = new Vector2(560f, 500f), SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        bagPanel.AddThemeStyleboxOverride("panel", MakeInnerStyle());
        columns.AddChild(bagPanel);
        var bagRoot = new VBoxContainer();
        bagPanel.AddChild(bagRoot);
        _bagTitle = new Label();
        _bagTitle.AddThemeFontSizeOverride("font_size", 14);
        bagRoot.AddChild(_bagTitle);
        var bagScroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        bagRoot.AddChild(bagScroll);
        _itemList = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        bagScroll.AddChild(_itemList);

        var gearPanel = new PanelContainer { CustomMinimumSize = new Vector2(330f, 500f), SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        gearPanel.AddThemeStyleboxOverride("panel", MakeInnerStyle());
        columns.AddChild(gearPanel);
        var gearRoot = new VBoxContainer();
        gearPanel.AddChild(gearRoot);
        var gearTitle = new Label { Text = "EQUIPPED", Modulate = new Color(1f, 0.78f, 0.3f) };
        gearTitle.AddThemeFontSizeOverride("font_size", 14);
        gearRoot.AddChild(gearTitle);
        _equipmentList = new VBoxContainer();
        gearRoot.AddChild(_equipmentList);

        var footer = new Label { Text = "F picks up nearest loot  •  Click an item to equip it  •  Equipped affixes immediately affect combat", Modulate = new Color(0.5f, 0.58f, 0.7f) };
        footer.AddThemeFontSizeOverride("font_size", 11);
        root.AddChild(footer);
        Refresh();
    }

    private void Refresh()
    {
        if (_itemList == null || _equipmentList == null || _summary == null || _bagTitle == null || _filterButton == null) return;
        ClearChildren(_itemList);
        ClearChildren(_equipmentList);
        _summary.Text = _inventory.BuildEquipmentSummary();
        _bagTitle.Text = $"BACKPACK  •  CLICK TO EQUIP  •  {_inventory.Items.Count}/{PlayerInventory.Capacity}";
        _filterButton.Text = $"LOOT FILTER: {_inventory.MinimumVisibleRarity.ToString().ToUpperInvariant()}+";

        foreach (ItemData item in _inventory.Items)
        {
            var row = new HBoxContainer();
            _itemList.AddChild(row);
            var button = new Button
            {
                Text = $"{item.Name}   [{item.Slot}]   iLvl {item.ItemLevel}",
                TooltipText = item.BuildTooltip() + "\n\nCOMPARISON\n" + _inventory.BuildComparison(item),
                Alignment = HorizontalAlignment.Left, Modulate = item.RarityColor,
                CustomMinimumSize = new Vector2(420f, 34f), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            button.Pressed += () => _inventory.Equip(item);
            row.AddChild(button);
            var salvage = new Button { Text = "SALVAGE", TooltipText = "Destroy this item and recover crafting materials." };
            salvage.Pressed += () => _inventory.Salvage(item);
            row.AddChild(salvage);
        }
        if (_inventory.Items.Count == 0)
        {
            _itemList.AddChild(new Label { Text = "The cache is empty. Hunt enemies and collect drops with F.", Modulate = new Color(0.55f, 0.6f, 0.68f) });
        }

        foreach (EquipmentSlot slot in System.Enum.GetValues<EquipmentSlot>())
        {
            if (_inventory.Equipped.TryGetValue(slot, out ItemData? equipped))
            {
                _equipmentList.AddChild(new Label
                {
                    Text = $"{slot.ToString().ToUpperInvariant()}\n{equipped.Name}\n{BuildAffixSummary(equipped)}",
                    TooltipText = equipped.BuildTooltip(), Modulate = equipped.RarityColor,
                    CustomMinimumSize = new Vector2(290f, 58f), AutowrapMode = TextServer.AutowrapMode.WordSmart,
                });
            }
            else
            {
                _equipmentList.AddChild(new Label { Text = $"{slot.ToString().ToUpperInvariant()}  —  EMPTY", Modulate = new Color(0.38f, 0.42f, 0.5f), CustomMinimumSize = new Vector2(290f, 28f) });
            }
        }
    }

    private static string BuildAffixSummary(ItemData item)
    {
        if (item.Affixes.Count == 0) return "No affixes";
        string[] parts = new string[item.Affixes.Count];
        for (int i = 0; i < item.Affixes.Count; i++) parts[i] = item.Affixes[i].Format();
        return string.Join("  •  ", parts);
    }

    private static void ClearChildren(Node parent)
    {
        foreach (Node child in parent.GetChildren()) child.QueueFree();
    }

    private static StyleBoxFlat MakeInnerStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.025f, 0.038f, 0.07f, 0.92f), BorderColor = new Color(0.16f, 0.3f, 0.45f),
            BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
            ContentMarginLeft = 10f, ContentMarginRight = 10f, ContentMarginTop = 8f, ContentMarginBottom = 8f,
        };
    }
}
