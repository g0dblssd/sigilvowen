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
            Texture = GD.Load<Texture2D>("res://assets/ui/main-menu-background-v2.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        UiTheme.Apply(background);
        AddChild(background);
        var veil = new ColorRect { AnchorRight = 1f, AnchorBottom = 1f, Color = new Color(0.008f, 0.008f, 0.008f, 0.48f), MouseFilter = Control.MouseFilterEnum.Ignore };
        background.AddChild(veil);

        var panel = new PanelContainer { AnchorLeft = 0.08f, AnchorRight = 0.37f, AnchorTop = 0.16f, AnchorBottom = 0.86f };
        panel.AddThemeStyleboxOverride("panel", UiTheme.Panel(34, UiTheme.Bronze));
        background.AddChild(panel);
        var column = new VBoxContainer(); column.AddThemeConstantOverride("separation", 16); panel.AddChild(column);
        var title = new Label { Text = "SIGILWOVEN", Modulate = new Color(0.84f, 0.81f, 0.72f) }; title.AddThemeFontOverride("font", GD.Load<Font>("res://assets/fonts/Cinzel-Variable.ttf")); title.AddThemeFontSizeOverride("font_size", 46); column.AddChild(title);
        var subtitle = new Label { Text = "ECHOES OF THE ABYSS", Modulate = UiTheme.Bronze }; subtitle.AddThemeFontOverride("font", GD.Load<Font>("res://assets/fonts/Cinzel-Variable.ttf")); subtitle.AddThemeFontSizeOverride("font_size", 17); column.AddChild(subtitle);
        column.AddChild(new HSeparator());
        var flavor = new Label { Text = "A dark action-RPG\nSeven classes  ·  linked skills  ·  endless vaults", AutowrapMode = TextServer.AutowrapMode.WordSmart, Modulate = UiTheme.Muted };
        flavor.AddThemeFontSizeOverride("font_size", 15); column.AddChild(flavor);
        var spacer = new Control { SizeFlagsVertical = Control.SizeFlags.ExpandFill }; column.AddChild(spacer);
        var start = MenuButton("ENTER THE SHATTERED REALM", new Color(0.2f, 0.66f, 0.94f)); start.Pressed += StartGame; column.AddChild(start);
        var codex = MenuButton("CODEX  //  47 SKILLS • 7 CLASSES", new Color(0.42f, 0.42f, 0.48f)); codex.Disabled = true; column.AddChild(codex);
        var quit = MenuButton("LEAVE GAME", new Color(0.52f, 0.22f, 0.2f)); quit.Pressed += () => GetTree().Quit(); column.AddChild(quit);
        column.AddChild(new Label { Text = "VERSION 0.0.47  ·  SHATTERED REALM", Modulate = UiTheme.Muted, HorizontalAlignment = HorizontalAlignment.Center });

        if (DisplayServer.GetName() == "headless") StartGame();
    }

    private static Button MenuButton(string text, Color color)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(0f, 56f) };
        button.AddThemeFontSizeOverride("font_size", 16);
        button.AddThemeColorOverride("font_hover_color", new Color(0.94f, 0.9f, 0.8f));
        return button;
    }

    private void StartGame()
    {
        Started?.Invoke();
        QueueFree();
    }
}
