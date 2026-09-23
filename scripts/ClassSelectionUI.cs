using System;
using Godot;

namespace Sigilwoven;

/// <summary>Illustrated seven-class selection shown after the title screen.</summary>
public partial class ClassSelectionUI : CanvasLayer
{
    private readonly PlayerController _player;
    private HeroClass _selected;
    private readonly Button[] _cards = new Button[Enum.GetValues<HeroClass>().Length];
    private readonly Color[] _colors =
    {
        new(0.15f, 0.72f, 1f), new(0.68f, 0.3f, 1f), new(0.25f, 0.9f, 0.55f), new(1f, 0.22f, 0.12f),
        new(0.18f, 0.9f, 0.82f), new(0.46f, 0.3f, 0.85f), new(1f, 0.78f, 0.28f),
    };
    private Label? _detail;
    public event Action? Confirmed;

    public ClassSelectionUI(PlayerController player)
    {
        _player = player;
        _selected = player.Progression.HeroClass;
        Layer = 100;
        Visible = false;
    }

    public override void _Ready()
    {
        BuildScreen();
        SelectClass(_selected);
    }

    public void ShowSelection()
    {
        Visible = true;
        SelectClass(_player.Progression.HeroClass);
        if (DisplayServer.GetName() == "headless") ConfirmSelection();
    }

    private void BuildScreen()
    {
        var shade = new ColorRect { AnchorRight = 1f, AnchorBottom = 1f, Color = new Color(0.004f, 0.008f, 0.018f, 0.985f), MouseFilter = Control.MouseFilterEnum.Stop };
        AddChild(shade);
        var window = new PanelContainer { AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f, OffsetLeft = -570f, OffsetRight = 570f, OffsetTop = -390f, OffsetBottom = 390f };
        window.AddThemeStyleboxOverride("panel", PanelStyle(new Color(0.24f, 0.56f, 0.78f)));
        shade.AddChild(window);
        var root = new VBoxContainer(); root.AddThemeConstantOverride("separation", 10); window.AddChild(root);
        var title = new Label { Text = "CHOOSE YOUR WOVEN PATH", HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(0.68f, 0.9f, 1f) };
        title.AddThemeFontSizeOverride("font_size", 29); root.AddChild(title);
        root.AddChild(new Label { Text = "Seven classes. Skills awaken across levels 1–240, grow through use, and Paragon begins at 300.", HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(0.68f, 0.7f, 0.78f) });

        var grid = new GridContainer { Columns = 4, SizeFlagsVertical = Control.SizeFlags.ExpandFill, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        grid.AddThemeConstantOverride("h_separation", 9); grid.AddThemeConstantOverride("v_separation", 9); root.AddChild(grid);
        AddCard(grid, HeroClass.Runeblade, "RUNEBLADE", "Sword • stagger • blood");
        AddCard(grid, HeroClass.Aetherist, "AETHERIST", "Spells • range • burst");
        AddCard(grid, HeroClass.Warden, "WARDEN", "Bow • dagger • poison");
        AddCard(grid, HeroClass.Berserker, "BERSERKER", "Axe • rage • executions");
        AddCard(grid, HeroClass.Necromancer, "NECROMANCER", "Bone • minions • decay");
        AddCard(grid, HeroClass.Shadowstalker, "SHADOWSTALKER", "Daggers • marks • crits");
        AddCard(grid, HeroClass.Templar, "TEMPLAR", "Hammer • wards • judgment");

        _detail = new Label { HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(1f, 0.8f, 0.34f) };
        _detail.AddThemeFontSizeOverride("font_size", 15); root.AddChild(_detail);
        var confirm = new Button { Text = "BIND THIS PATH  //  BEGIN THE AWAKENING", CustomMinimumSize = new Vector2(0f, 48f) };
        confirm.AddThemeFontSizeOverride("font_size", 18); confirm.Pressed += ConfirmSelection; root.AddChild(confirm);
    }

    private void AddCard(GridContainer parent, HeroClass heroClass, string title, string description)
    {
        Color color = _colors[(int)heroClass];
        var button = new Button
        {
            Text = $"{title}\n{description}", Icon = Portrait((int)heroClass), ExpandIcon = true,
            CustomMinimumSize = new Vector2(265f, 295f), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            TooltipText = BuildClassTooltip(heroClass),
        };
        button.AddThemeFontSizeOverride("font_size", 14);
        button.AddThemeStyleboxOverride("normal", CardStyle(color, 0.08f, 2));
        button.AddThemeStyleboxOverride("hover", CardStyle(color, 0.16f, 3));
        button.Pressed += () => SelectClass(heroClass);
        parent.AddChild(button);
        _cards[(int)heroClass] = button;
    }

    private static AtlasTexture Portrait(int index)
    {
        Texture2D atlas = GD.Load<Texture2D>("res://assets/ui/class-portrait-atlas-v1.png");
        float cellWidth = atlas.GetWidth() / 4f;
        float cellHeight = atlas.GetHeight() / 2f;
        return new AtlasTexture { Atlas = atlas, Region = new Rect2((index % 4) * cellWidth, (index / 4) * cellHeight, cellWidth, cellHeight) };
    }

    private static string BuildClassTooltip(HeroClass heroClass) => heroClass switch
    {
        HeroClass.Runeblade => "Runic melee duelist. Reliable cleaves, high stagger, balanced survival.",
        HeroClass.Aetherist => "Fragile ranged caster. Huge mana pool and elemental burst windows.",
        HeroClass.Warden => "Adaptive hunter. Bow at range, knives up close, venom attrition.",
        HeroClass.Berserker => "Slow heavy weapon master. Highest raw damage, rage and impact attacks.",
        HeroClass.Necromancer => "Grave caster. Bone projectiles, undead summons and spreading decay.",
        HeroClass.Shadowstalker => "Fast assassin. Critical marks, smoke and poisoned throwing blades.",
        _ => "Armored holy vanguard. Hammer stagger, defensive ground and radiant judgment.",
    };

    private void SelectClass(HeroClass heroClass)
    {
        _selected = heroClass;
        for (int i = 0; i < _cards.Length; i++) if (_cards[i] != null) _cards[i].SelfModulate = i == (int)heroClass ? Colors.White : new Color(0.43f, 0.43f, 0.46f);
        if (_detail != null) _detail.Text = $"SELECTED  //  {heroClass.ToString().ToUpperInvariant()}  —  {BuildClassTooltip(heroClass)}";
    }

    private void ConfirmSelection()
    {
        _player.ConfirmClassSelection(_selected);
        Visible = false;
        Confirmed?.Invoke();
        QueueFree();
    }

    private static StyleBoxFlat PanelStyle(Color border) => new()
    {
        BgColor = new Color(0.012f, 0.019f, 0.04f, 0.99f), BorderColor = border,
        BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
        CornerRadiusTopLeft = 14, CornerRadiusTopRight = 14, CornerRadiusBottomLeft = 14, CornerRadiusBottomRight = 14,
        ContentMarginLeft = 22f, ContentMarginRight = 22f, ContentMarginTop = 18f, ContentMarginBottom = 18f,
    };

    private static StyleBoxFlat CardStyle(Color color, float brightness, int border) => new()
    {
        BgColor = new Color(color.R * brightness, color.G * brightness, color.B * brightness, 0.99f), BorderColor = new Color(color.R, color.G, color.B, 0.72f),
        BorderWidthLeft = border, BorderWidthTop = border, BorderWidthRight = border, BorderWidthBottom = border,
        CornerRadiusTopLeft = 9, CornerRadiusTopRight = 9, CornerRadiusBottomLeft = 9, CornerRadiusBottomRight = 9,
    };
}
