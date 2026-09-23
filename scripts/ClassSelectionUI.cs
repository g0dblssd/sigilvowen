using Godot;

namespace Sigilwoven;

/// <summary>Mandatory pre-run class choice shown before the birth ritual.</summary>
public partial class ClassSelectionUI : CanvasLayer
{
    private readonly PlayerController _player;
    private HeroClass _selected;
    private readonly Button[] _cards = new Button[3];
    private Label? _detail;
    public event System.Action? Confirmed;

    public ClassSelectionUI(PlayerController player)
    {
        _player = player;
        _selected = player.Progression.HeroClass;
        Layer = 100;
    }

    public override void _Ready()
    {
        BuildScreen();
        SelectClass(_selected);
        if (DisplayServer.GetName() == "headless")
        {
            ConfirmSelection();
        }
    }

    private void BuildScreen()
    {
        var shade = new ColorRect
        {
            AnchorRight = 1f,
            AnchorBottom = 1f,
            Color = new Color(0.004f, 0.008f, 0.02f, 0.97f),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        AddChild(shade);

        var window = new PanelContainer
        {
            AnchorLeft = 0.5f,
            AnchorRight = 0.5f,
            AnchorTop = 0.5f,
            AnchorBottom = 0.5f,
            OffsetLeft = -500f,
            OffsetRight = 500f,
            OffsetTop = -295f,
            OffsetBottom = 295f,
        };
        window.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.018f, 0.03f, 0.065f, 0.99f),
            BorderColor = new Color(0.28f, 0.68f, 0.92f),
            BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
            CornerRadiusTopLeft = 14, CornerRadiusTopRight = 14, CornerRadiusBottomLeft = 14, CornerRadiusBottomRight = 14,
            ContentMarginLeft = 28f, ContentMarginRight = 28f, ContentMarginTop = 24f, ContentMarginBottom = 24f,
        });
        shade.AddChild(window);
        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 14);
        window.AddChild(root);

        var title = new Label { Text = "CHOOSE YOUR WOVEN PATH", HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(0.55f, 0.9f, 1f) };
        title.AddThemeFontSizeOverride("font_size", 28);
        root.AddChild(title);
        var subtitle = new Label { Text = "The class can be chosen before every run. Progression, items and unlocked Links are preserved.", HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(0.65f, 0.7f, 0.82f) };
        root.AddChild(subtitle);

        var cards = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        cards.AddThemeConstantOverride("separation", 14);
        root.AddChild(cards);
        AddCard(cards, HeroClass.Runeblade, "RUNEBLADE", "Balanced melee caster\n+8% base damage\n+10 bonus health", new Color(0.15f, 0.72f, 1f));
        AddCard(cards, HeroClass.Aetherist, "AETHERIST", "Aggressive spell weaver\n+12% base damage\n+30 mana • 5% cooldown", new Color(0.68f, 0.3f, 1f));
        AddCard(cards, HeroClass.Warden, "WARDEN", "Durable frontline anchor\n+3% base damage\n+30 bonus health", new Color(0.25f, 0.9f, 0.55f));

        _detail = new Label { HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(1f, 0.8f, 0.34f) };
        _detail.AddThemeFontSizeOverride("font_size", 14);
        root.AddChild(_detail);
        var confirm = new Button { Text = "BEGIN THE AWAKENING", CustomMinimumSize = new Vector2(0f, 52f) };
        confirm.AddThemeFontSizeOverride("font_size", 18);
        confirm.Pressed += ConfirmSelection;
        root.AddChild(confirm);
    }

    private void AddCard(HBoxContainer parent, HeroClass heroClass, string title, string description, Color color)
    {
        var button = new Button
        {
            Text = $"{title}\n\n{description}",
            CustomMinimumSize = new Vector2(300f, 330f),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            Modulate = color,
        };
        button.AddThemeFontSizeOverride("font_size", 16);
        button.Pressed += () => SelectClass(heroClass);
        parent.AddChild(button);
        _cards[(int)heroClass] = button;
    }

    private void SelectClass(HeroClass heroClass)
    {
        _selected = heroClass;
        for (int i = 0; i < _cards.Length; i++)
        {
            if (_cards[i] != null) _cards[i].SelfModulate = i == (int)heroClass ? Colors.White : new Color(0.48f, 0.48f, 0.52f);
        }
        if (_detail != null) _detail.Text = $"SELECTED  //  {heroClass.ToString().ToUpperInvariant()}";
    }

    private void ConfirmSelection()
    {
        _player.ConfirmClassSelection(_selected);
        Visible = false;
        Confirmed?.Invoke();
        QueueFree();
    }
}
