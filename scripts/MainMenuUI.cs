using System;
using Godot;

namespace Sigilwoven;

/// <summary>Full title screen shown before any gameplay input is enabled.</summary>
public partial class MainMenuUI : CanvasLayer
{
    public event Action? Started;

    public MainMenuUI() { Layer = 120; }

    public override void _Ready()
    {
        var background = new TextureRect
        {
            AnchorRight = 1f, AnchorBottom = 1f,
            Texture = GD.Load<Texture2D>("res://assets/ui/main-menu-background-v1.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        AddChild(background);
        var veil = new ColorRect { AnchorRight = 1f, AnchorBottom = 1f, Color = new Color(0.005f, 0.008f, 0.016f, 0.34f), MouseFilter = Control.MouseFilterEnum.Ignore };
        background.AddChild(veil);

        var panel = new PanelContainer { AnchorLeft = 0.08f, AnchorRight = 0.37f, AnchorTop = 0.16f, AnchorBottom = 0.86f };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.008f, 0.012f, 0.024f, 0.9f), BorderColor = new Color(0.34f, 0.48f, 0.62f, 0.75f),
            BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10,
            ContentMarginLeft = 34f, ContentMarginRight = 34f, ContentMarginTop = 32f, ContentMarginBottom = 32f,
        });
        background.AddChild(panel);
        var column = new VBoxContainer(); column.AddThemeConstantOverride("separation", 16); panel.AddChild(column);
        var title = new Label { Text = "SIGILWOVEN", Modulate = new Color(0.72f, 0.9f, 1f) }; title.AddThemeFontSizeOverride("font_size", 46); column.AddChild(title);
        var subtitle = new Label { Text = "ECHOES OF THE ABYSS", Modulate = new Color(1f, 0.64f, 0.25f) }; subtitle.AddThemeFontSizeOverride("font_size", 18); column.AddChild(subtitle);
        column.AddChild(new HSeparator());
        var flavor = new Label { Text = "A dark action-RPG prototype\nSeven classes • linked skills • endless vaults", AutowrapMode = TextServer.AutowrapMode.WordSmart, Modulate = new Color(0.7f, 0.74f, 0.82f) };
        flavor.AddThemeFontSizeOverride("font_size", 15); column.AddChild(flavor);
        var spacer = new Control { SizeFlagsVertical = Control.SizeFlags.ExpandFill }; column.AddChild(spacer);
        var start = MenuButton("ENTER THE SHATTERED REALM", new Color(0.2f, 0.66f, 0.94f)); start.Pressed += StartGame; column.AddChild(start);
        var codex = MenuButton("CODEX  //  47 SKILLS • 7 CLASSES", new Color(0.42f, 0.42f, 0.48f)); codex.Disabled = true; column.AddChild(codex);
        var quit = MenuButton("LEAVE GAME", new Color(0.52f, 0.22f, 0.2f)); quit.Pressed += () => GetTree().Quit(); column.AddChild(quit);
        column.AddChild(new Label { Text = "VERSION 0.0.46  •  SHATTERED REALM", Modulate = new Color(0.45f, 0.48f, 0.56f), HorizontalAlignment = HorizontalAlignment.Center });

        if (DisplayServer.GetName() == "headless") StartGame();
    }

    private static Button MenuButton(string text, Color color)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(0f, 56f) };
        button.AddThemeFontSizeOverride("font_size", 16);
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.Modulate = color;
        return button;
    }

    private void StartGame()
    {
        Started?.Invoke();
        QueueFree();
    }
}
