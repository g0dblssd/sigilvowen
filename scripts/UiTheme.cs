using Godot;

namespace Sigilwoven;

/// <summary>Restrained, high-contrast visual language shared by every game screen.</summary>
public static class UiTheme
{
    public static readonly Color Ink = new(0.018f, 0.019f, 0.02f, 0.97f);
    public static readonly Color Raised = new(0.045f, 0.043f, 0.04f, 0.98f);
    public static readonly Color Steel = new(0.31f, 0.32f, 0.31f);
    public static readonly Color Bronze = new(0.68f, 0.51f, 0.3f);
    public static readonly Color Text = new(0.94f, 0.91f, 0.84f);
    public static readonly Color Muted = new(0.62f, 0.6f, 0.56f);
    public static readonly Color Warning = new(0.63f, 0.29f, 0.2f);

    private static Theme? _theme;

    public static void Apply(Control root)
    {
        root.Theme = Get();
    }

    public static Theme Get()
    {
        if (_theme != null) return _theme;

        _theme = new Theme
        {
            DefaultFont = GD.Load<Font>("res://assets/fonts/SourceSans3-Variable.ttf"),
            DefaultFontSize = 15,
        };
        Font heading = GD.Load<Font>("res://assets/fonts/Cinzel-Variable.ttf");
        _theme.SetFont("font", "Button", heading);
        _theme.SetFont("font", "OptionButton", heading);
        _theme.SetFont("font", "CheckBox", heading);

        _theme.SetColor("font_color", "Label", Text);
        _theme.SetColor("font_color", "Button", Text);
        _theme.SetColor("font_hover_color", "Button", new Color(0.94f, 0.9f, 0.8f));
        _theme.SetColor("font_pressed_color", "Button", new Color(0.73f, 0.59f, 0.38f));
        _theme.SetColor("font_disabled_color", "Button", new Color(0.31f, 0.3f, 0.28f));
        _theme.SetStylebox("normal", "Button", Box(new Color(0.055f, 0.052f, 0.048f, 0.98f), Steel, 1, 1, 9));
        _theme.SetStylebox("hover", "Button", Box(new Color(0.09f, 0.078f, 0.06f, 0.99f), Bronze, 1, 1, 9));
        _theme.SetStylebox("pressed", "Button", Box(new Color(0.025f, 0.024f, 0.022f, 1f), Bronze, 1, 1, 9));
        _theme.SetStylebox("disabled", "Button", Box(new Color(0.028f, 0.028f, 0.027f, 0.92f), new Color(0.14f, 0.14f, 0.13f), 1, 1, 9));

        foreach (string type in new[] { "OptionButton", "CheckBox", "LineEdit" })
        {
            _theme.SetStylebox("normal", type, Box(new Color(0.042f, 0.041f, 0.039f, 0.98f), Steel, 1, 1, 7));
            _theme.SetStylebox("hover", type, Box(new Color(0.07f, 0.064f, 0.054f, 0.99f), Bronze, 1, 1, 7));
            _theme.SetStylebox("pressed", type, Box(new Color(0.025f, 0.024f, 0.022f, 1f), Bronze, 1, 1, 7));
        }
        _theme.SetStylebox("panel", "Panel", Panel());
        _theme.SetStylebox("panel", "PanelContainer", Panel());
        _theme.SetStylebox("background", "ProgressBar", Box(new Color(0.025f, 0.024f, 0.022f), Steel, 1, 1, 0));
        _theme.SetStylebox("fill", "ProgressBar", Box(new Color(0.42f, 0.12f, 0.09f), Bronze, 1, 1, 0));
        _theme.SetStylebox("separator", "HSeparator", Box(new Color(0.18f, 0.17f, 0.15f), new Color(0.18f, 0.17f, 0.15f), 0, 0, 0));
        return _theme;
    }

    public static StyleBoxFlat Panel(int margin = 14, Color? border = null) =>
        Box(Ink, border ?? Steel, 1, 1, margin);

    public static StyleBoxFlat Inset(int margin = 9, Color? border = null) =>
        Box(new Color(0.03f, 0.029f, 0.027f, 0.96f), border ?? new Color(0.18f, 0.18f, 0.17f), 1, 1, margin);

    public static StyleBoxFlat Box(Color background, Color border, int width, int radius, int margin)
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = width,
            BorderWidthTop = width,
            BorderWidthRight = width,
            BorderWidthBottom = width,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            CornerRadiusBottomLeft = radius,
            CornerRadiusBottomRight = radius,
            ContentMarginLeft = margin,
            ContentMarginRight = margin,
            ContentMarginTop = margin,
            ContentMarginBottom = margin,
        };
    }
}
