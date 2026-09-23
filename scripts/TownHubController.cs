using System;
using Godot;

namespace Sigilwoven;

/// <summary>Small functional sanctuary hub: guide, class-biased merchant and dust casino.</summary>
public partial class TownHubController : Node3D
{
    private enum Service { None, Guide, Merchant, Casino }
    private PlayerController? _player;
    private Node3D? _world;
    private PanelContainer? _panel;
    private VBoxContainer? _content;
    private Label? _prompt;
    private Service _nearest;
    private readonly ItemData[] _stock = new ItemData[3];

    public void Setup(PlayerController player, Node3D world)
    {
        _player = player;
        _world = world;
        RollStock();
    }

    public override void _Ready()
    {
        BuildNpc(Service.Guide, new Vector3(-5f, 0f, 4f), "WOVEN GUIDE", "OldClassy_Female.fbx", new Color(0.28f, 0.75f, 1f));
        BuildNpc(Service.Merchant, new Vector3(5f, 0f, 4f), "VARRA • RELIC BROKER", "Pirate_Female.fbx", new Color(1f, 0.63f, 0.2f));
        BuildNpc(Service.Casino, new Vector3(8f, 0f, -2f), "THE CROOKED WHEEL", "Suit_Male.fbx", new Color(0.78f, 0.25f, 1f));
        BuildUi();
    }

    public override void _Process(double delta)
    {
        if (_player == null || _prompt == null) return;
        _nearest = Service.None;
        float best = 3.2f;
        foreach (Node child in GetChildren())
        {
            if (child is not Node3D npc || !npc.HasMeta("service")) continue;
            float distance = _player.GlobalPosition.DistanceTo(npc.GlobalPosition);
            if (distance < best) { best = distance; _nearest = (Service)(int)npc.GetMeta("service"); }
        }
        _prompt.Visible = _nearest != Service.None && !(_panel?.Visible ?? false);
        if (_prompt.Visible) _prompt.Text = $"[E]  SPEAK WITH {ServiceName(_nearest)}";
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.E)
        {
            if (_panel?.Visible == true) ClosePanel();
            else if (_nearest != Service.None) OpenService(_nearest);
            GetViewport().SetInputAsHandled();
        }
        if (@event is InputEventKey esc && esc.Pressed && !esc.Echo && esc.Keycode == Key.Escape && _panel?.Visible == true)
        {
            ClosePanel();
            GetViewport().SetInputAsHandled();
        }
    }

    private void BuildNpc(Service service, Vector3 position, string labelText, string modelFile, Color color)
    {
        var anchor = new Node3D { Position = position }; anchor.SetMeta("service", (int)service); AddChild(anchor);
        PackedScene? scene = GD.Load<PackedScene>($"res://assets/models/quaternius_characters/{modelFile}");
        if (scene?.Instantiate() is Node3D model)
        {
            model.Scale = Vector3.One * 0.58f; anchor.AddChild(model);
            AnimationPlayer? animator = model.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
            if (animator?.GetAnimation("CharacterArmature|Idle") is Animation idle) { idle.LoopMode = Animation.LoopModeEnum.Linear; animator.Play("CharacterArmature|Idle"); }
        }
        else
        {
            var fallback = new MeshInstance3D { Mesh = new CapsuleMesh { Radius = 0.45f, Height = 1.8f }, Position = Vector3.Up };
            fallback.SetSurfaceOverrideMaterial(0, new StandardMaterial3D { AlbedoColor = color }); anchor.AddChild(fallback);
        }
        var label = new Label3D { Text = labelText + "\n[E] INTERACT", Position = new Vector3(0f, 2.65f, 0f), FontSize = 34, Modulate = color, Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, OutlineSize = 8 };
        anchor.AddChild(label);
        var light = new OmniLight3D { Position = new Vector3(0f, 2f, 0f), LightColor = color, LightEnergy = 0.9f, OmniRange = 4f }; anchor.AddChild(light);
    }

    private void BuildUi()
    {
        var layer = new CanvasLayer { Layer = 75 }; AddChild(layer);
        _prompt = new Label { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.73f, AnchorBottom = 0.73f, OffsetLeft = -230f, OffsetRight = 230f, OffsetTop = -22f, OffsetBottom = 22f, HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(1f, 0.78f, 0.3f), Visible = false };
        _prompt.AddThemeFontSizeOverride("font_size", 18); layer.AddChild(_prompt);
        _panel = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -390f, OffsetRight = 390f, OffsetTop = -280f, OffsetBottom = 280f, Visible = false };
        _panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.01f, 0.016f, 0.032f, 0.985f), BorderColor = new Color(0.72f, 0.48f, 0.2f), BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2, ContentMarginLeft = 24f, ContentMarginRight = 24f, ContentMarginTop = 20f, ContentMarginBottom = 20f, CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10 });
        layer.AddChild(_panel); _content = new VBoxContainer(); _content.AddThemeConstantOverride("separation", 12); _panel.AddChild(_content);
    }

    private void OpenService(Service service)
    {
        if (_panel == null || _content == null || _player == null) return;
        foreach (Node child in _content.GetChildren()) child.QueueFree();
        _panel.Visible = true;
        AddHeading(ServiceName(service));
        if (service == Service.Guide) BuildGuide();
        else if (service == Service.Merchant) BuildMerchant();
        else BuildCasino();
        var close = new Button { Text = "CLOSE  [E / ESC]", CustomMinimumSize = new Vector2(0f, 42f) }; close.Pressed += ClosePanel; _content.AddChild(close);
    }

    private void BuildGuide()
    {
        AddText("Welcome, Woven. Left-click moves; RMB performs your class attack. Keys 1–6 and Z/X/C/V cast the linked hotbar. Q drinks the life flask, F collects loot, I opens gear, and D opens the Vault map.");
        AddText("Every class begins with one signature skill. Further techniques awaken across the full journey at levels 15–240, then gain ranks through use. Hover every skill for its final linked values. Salvage weak equipment into dust; spend it at Varra or risk it at the Crooked Wheel.");
        AddText("The realm is intentionally cruel. Pull small groups, dodge telegraphs with Space, keep your flask charged by kills, and choose higher Vault torment only when your build can survive it.");
    }

    private void BuildMerchant()
    {
        AddText($"Class-biased relics for {_player!.Progression.HeroClass}. Stock refreshes after purchase. Dust: {_player.Inventory.SalvageDust}");
        int[] costs = { 12, 22, 36 };
        for (int i = 0; i < _stock.Length; i++)
        {
            int index = i;
            ItemData item = _stock[i];
            var buy = new Button { Text = $"{item.Name}  •  {item.Rarity.ToString().ToUpperInvariant()}  •  POWER {item.GearScore}  //  {costs[i]} DUST", TooltipText = item.BuildTooltip(), CustomMinimumSize = new Vector2(0f, 58f) };
            buy.Modulate = item.RarityColor; buy.Pressed += () => Buy(index, costs[index]); _content!.AddChild(buy);
        }
    }

    private void BuildCasino()
    {
        AddText($"THE CROOKED WHEEL  •  Dust: {_player!.Inventory.SalvageDust}\n10 dust per spin. 55% relic, 25% dust refund, 15% shard cache, 5% jackpot. The house offers no mercy.");
        var spin = new Button { Text = "SPIN THE CROOKED WHEEL  //  10 DUST", CustomMinimumSize = new Vector2(0f, 82f), Modulate = new Color(0.82f, 0.32f, 1f) };
        spin.AddThemeFontSizeOverride("font_size", 20); spin.Pressed += Gamble; _content!.AddChild(spin);
    }

    private void Buy(int index, int cost)
    {
        if (_player == null) return;
        if (_player.Inventory.Items.Count >= PlayerInventory.Capacity) { _player.ShowCombatMessage("INVENTORY FULL"); return; }
        if (!_player.Inventory.TrySpendDust(cost)) { _player.ShowCombatMessage("NOT ENOUGH SALVAGE DUST"); return; }
        _player.Inventory.TryAdd(_stock[index]); _player.ShowCombatMessage($"PURCHASED: {_stock[index].Name.ToUpperInvariant()}");
        RollStock(); OpenService(Service.Merchant);
    }

    private void Gamble()
    {
        if (_player == null || !_player.Inventory.TrySpendDust(10)) { _player?.ShowCombatMessage("THE WHEEL DEMANDS 10 DUST"); return; }
        float roll = GD.Randf();
        if (roll < 0.55f)
        {
            ItemData item = ItemGenerator.Generate(_player.Progression.Level + GD.RandRange(0, 4), 0.12f, _player.Progression.HeroClass);
            if (_player.Inventory.TryAdd(item)) _player.ShowCombatMessage($"WHEEL RELIC: {item.Name.ToUpperInvariant()}"); else _player.Inventory.GrantCurrency(10);
        }
        else if (roll < 0.8f) { int refund = GD.RandRange(4, 18); _player.Inventory.GrantCurrency(refund); _player.ShowCombatMessage($"WHEEL REFUND: {refund} DUST"); }
        else if (roll < 0.95f) { _player.Inventory.GrantCurrency(0, 2); _player.ShowCombatMessage("WHEEL CACHE: 2 SIGIL SHARDS"); }
        else { _player.Inventory.GrantCurrency(50, 5, 1); _player.ShowCombatMessage("JACKPOT — SIGIL CORE + 50 DUST"); }
        OpenService(Service.Casino);
    }

    private void RollStock()
    {
        if (_player == null) return;
        for (int i = 0; i < _stock.Length; i++) _stock[i] = ItemGenerator.Generate(_player.Progression.Level + i * 2, 0.08f + i * 0.1f, _player.Progression.HeroClass);
    }

    private void AddHeading(string text) { var label = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(1f, 0.72f, 0.28f) }; label.AddThemeFontSizeOverride("font_size", 25); _content!.AddChild(label); }
    private void AddText(string text) => _content!.AddChild(new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(700f, 64f), Modulate = new Color(0.78f, 0.82f, 0.9f) });
    private void ClosePanel() { if (_panel != null) _panel.Visible = false; }
    private static string ServiceName(Service service) => service switch { Service.Guide => "THE WOVEN GUIDE", Service.Merchant => "VARRA • RELIC BROKER", Service.Casino => "THE CROOKED WHEEL", _ => "SANCTUARY" };
}
