using Godot;

namespace Sigilwoven;

/// <summary>Combat-first ARPG HUD: resource orbs, flask, hotbar and live radar.</summary>
public partial class GameHud : CanvasLayer
{
    private readonly PlayerController _player;
    private readonly SkillCaster _caster;
    private readonly Label[] _skillNames = new Label[10];
    private readonly Label[] _skillCooldowns = new Label[10];
    private readonly PanelContainer[] _skillPanels = new PanelContainer[10];
    private readonly TextureRect[] _skillIcons = new TextureRect[10];
    private VitalOrb? _healthOrb;
    private VitalOrb? _manaOrb;
    private Label? _healthText;
    private Label? _manaText;
    private Label? _flaskText;
    private Label? _status;
    private Label? _combatMessage;
    private Label? _objective;
    private Label? _progression;

    public GameHud(PlayerController player, SkillCaster caster)
    {
        _player = player;
        _caster = caster;
        Layer = 8;
    }

    public override void _Ready()
    {
        BuildObjectiveStrip();
        BuildBottomHud();
        BuildMinimap();
        RefreshProgression();
        Refresh();
    }

    public override void _Process(double delta)
    {
        Refresh();
    }

    public void ShowCombatMessage(string text)
    {
        if (_combatMessage != null)
        {
            _combatMessage.Text = text;
        }
    }

    public void SetObjective(string text)
    {
        if (_objective != null)
        {
            _objective.Text = $"◆  {text}  ◆";
        }
    }

    public void RefreshProgression()
    {
        if (_progression == null)
        {
            return;
        }
        PlayerProgression p = _player.Progression;
        _progression.Text = p.Level < PlayerProgression.MaxLevel
            ? $"{p.HeroClass.ToString().ToUpperInvariant()}  •  LV {p.Level}  •  XP {p.Experience}/{p.ExperienceToNextLevel}  •  PASSIVE {p.PassivePoints}"
            : $"LV 300  •  PARAGON {p.ParagonLevel}  •  XP {p.ParagonExperience}/{p.ExperienceToNextParagon}  •  POINTS {p.ParagonPoints}";
    }

    private void BuildObjectiveStrip()
    {
        _objective = new Label
        {
            Text = "◆  AWAKEN  ◆",
            HorizontalAlignment = HorizontalAlignment.Center,
            AnchorLeft = 0.5f,
            AnchorRight = 0.5f,
            OffsetLeft = -260f,
            OffsetRight = 260f,
            OffsetTop = 12f,
            OffsetBottom = 42f,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Modulate = new Color(0.62f, 0.9f, 1f),
        };
        _objective.AddThemeFontSizeOverride("font_size", 16);
        AddChild(_objective);
    }

    private void BuildBottomHud()
    {
        var frame = new Control
        {
            AnchorLeft = 0.5f,
            AnchorRight = 0.5f,
            AnchorTop = 1f,
            AnchorBottom = 1f,
            OffsetLeft = -500f,
            OffsetRight = 500f,
            OffsetTop = -176f,
            OffsetBottom = -6f,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        AddChild(frame);

        var backdrop = new Panel
        {
            Position = new Vector2(178f, 42f),
            Size = new Vector2(644f, 124f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        backdrop.AddThemeStyleboxOverride("panel", MakePanel(new Color(0.015f, 0.022f, 0.045f, 0.94f), new Color(0.2f, 0.52f, 0.72f, 0.8f), 10));
        frame.AddChild(backdrop);

        _combatMessage = new Label
        {
            Position = new Vector2(250f, 10f),
            Size = new Vector2(500f, 26f),
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = new Color(1f, 0.78f, 0.3f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _combatMessage.AddThemeFontSizeOverride("font_size", 15);
        frame.AddChild(_combatMessage);

        _healthOrb = new VitalOrb { Position = new Vector2(72f, 34f), Size = new Vector2(136f, 136f), LiquidColor = new Color(0.78f, 0.035f, 0.055f), RimColor = new Color(0.8f, 0.5f, 0.18f) };
        frame.AddChild(_healthOrb);
        _manaOrb = new VitalOrb { Position = new Vector2(792f, 34f), Size = new Vector2(136f, 136f), LiquidColor = new Color(0.04f, 0.35f, 0.95f), RimColor = new Color(0.25f, 0.65f, 1f) };
        frame.AddChild(_manaOrb);

        _healthText = MakeOrbLabel("HEALTH", new Vector2(79f, 85f));
        frame.AddChild(_healthText);
        _manaText = MakeOrbLabel("MANA", new Vector2(799f, 85f));
        frame.AddChild(_manaText);

        BuildFlask(frame);
        BuildSkillBar(frame);

        _status = new Label
        {
            Position = new Vector2(205f, 43f),
            Size = new Vector2(590f, 20f),
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = new Color(0.72f, 0.82f, 0.92f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _status.AddThemeFontSizeOverride("font_size", 11);
        frame.AddChild(_status);

        _progression = new Label
        {
            Position = new Vector2(205f, 138f),
            Size = new Vector2(590f, 18f),
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = new Color(0.75f, 0.64f, 1f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _progression.AddThemeFontSizeOverride("font_size", 11);
        frame.AddChild(_progression);

        var hint = new Label
        {
            Text = "LMB MOVE  •  1–6 / Z X C V CAST  •  Q FLASK  •  F LOOT  •  I INVENTORY  •  L LINKS  •  P PASSIVES",
            Position = new Vector2(205f, 155f),
            Size = new Vector2(590f, 15f),
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = new Color(0.42f, 0.5f, 0.62f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        hint.AddThemeFontSizeOverride("font_size", 9);
        frame.AddChild(hint);
    }

    private void BuildFlask(Control frame)
    {
        var flask = new PanelContainer
        {
            Position = new Vector2(8f, 78f),
            Size = new Vector2(66f, 86f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        flask.AddThemeStyleboxOverride("panel", MakePanel(new Color(0.16f, 0.025f, 0.035f, 0.96f), new Color(0.95f, 0.38f, 0.18f), 12));
        frame.AddChild(flask);
        _flaskText = new Label
        {
            Text = "Q\nLIFE\n30/30",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Modulate = new Color(1f, 0.72f, 0.55f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _flaskText.AddThemeFontSizeOverride("font_size", 12);
        flask.AddChild(_flaskText);
    }

    private void BuildSkillBar(Control frame)
    {
        var bar = new HBoxContainer
        {
            Position = new Vector2(205f, 66f),
            Size = new Vector2(590f, 68f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        bar.AddThemeConstantOverride("separation", 3);
        frame.AddChild(bar);

        string[] keys = { "1", "2", "3", "4", "5", "6", "Z", "X", "C", "V" };
        for (int i = 0; i < 10; i++)
        {
            var panel = new PanelContainer { CustomMinimumSize = new Vector2(56f, 72f), MouseFilter = Control.MouseFilterEnum.Ignore };
            panel.AddThemeStyleboxOverride("panel", MakePanel(new Color(0.035f, 0.055f, 0.09f, 0.98f), new Color(0.22f, 0.38f, 0.55f), 5));
            bar.AddChild(panel);
            _skillPanels[i] = panel;

            var stack = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            stack.AddThemeConstantOverride("separation", 0);
            panel.AddChild(stack);
            var key = new Label { Text = keys[i], HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(1f, 0.78f, 0.32f), MouseFilter = Control.MouseFilterEnum.Ignore };
            key.AddThemeFontSizeOverride("font_size", 12);
            stack.AddChild(key);
            _skillIcons[i] = new TextureRect
            {
                Texture = GD.Load<Texture2D>($"res://assets/ui/skill_icons/icon-{i:00}.png"),
                CustomMinimumSize = new Vector2(48f, 35f),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            stack.AddChild(_skillIcons[i]);
            _skillNames[i] = new Label { Text = "-", HorizontalAlignment = HorizontalAlignment.Center, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis, CustomMinimumSize = new Vector2(48f, 12f), MouseFilter = Control.MouseFilterEnum.Ignore };
            _skillNames[i].AddThemeFontSizeOverride("font_size", 7);
            stack.AddChild(_skillNames[i]);
            _skillCooldowns[i] = new Label { Text = "READY", HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(0.35f, 1f, 0.62f), MouseFilter = Control.MouseFilterEnum.Ignore };
            _skillCooldowns[i].AddThemeFontSizeOverride("font_size", 9);
            stack.AddChild(_skillCooldowns[i]);
        }
    }

    private void BuildMinimap()
    {
        var map = new MinimapControl(_player)
        {
            AnchorLeft = 1f,
            AnchorRight = 1f,
            OffsetLeft = -242f,
            OffsetRight = -18f,
            OffsetTop = 14f,
            OffsetBottom = 238f,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        AddChild(map);
        var title = new Label
        {
            Text = "AETHER REACH  •  SURFACE",
            Position = new Vector2(20f, 8f),
            Size = new Vector2(184f, 20f),
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = new Color(0.55f, 0.88f, 1f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        title.AddThemeFontSizeOverride("font_size", 10);
        map.AddChild(title);
    }

    private void Refresh()
    {
        if (_healthOrb != null) _healthOrb.Fill = _player.MaxHealth <= 0f ? 0f : _player.Health / _player.MaxHealth;
        if (_manaOrb != null) _manaOrb.Fill = _player.MaxMana <= 0f ? 0f : _player.Mana / _player.MaxMana;
        if (_healthText != null) _healthText.Text = $"HEALTH\n{(int)_player.Health} / {(int)_player.MaxHealth}";
        if (_manaText != null) _manaText.Text = $"MANA\n{(int)_player.Mana} / {(int)_player.MaxMana}";
        if (_flaskText != null)
        {
            _flaskText.Text = $"Q\nLIFE\n{_player.FlaskCharges}/{PlayerController.MaxFlaskCharges}";
            _flaskText.Modulate = _player.CanUseFlask ? new Color(1f, 0.72f, 0.55f) : new Color(0.42f, 0.35f, 0.38f);
        }
        if (_status != null) _status.Text = _player.GetCombatStatus();

        for (int i = 0; i < _skillNames.Length && i < _caster.Slots.Count; i++)
        {
            SkillData skill = _caster.Slots[i].Skill;
            bool unlocked = _player.Progression.IsSkillUnlocked(skill.Id);
            float cooldown = _caster.GetCooldownRemaining(i);
            int linkCount = _caster.Slots[i].Links.Count;
            string linkedNames = linkCount == 0 ? "No links" : string.Join(", ", _caster.Slots[i].Links.ConvertAll(link => link.DisplayName));
            _skillNames[i].Text = linkCount > 0 ? $"{skill.DisplayName.ToUpperInvariant()} ◆{linkCount}" : skill.DisplayName.ToUpperInvariant();
            _skillNames[i].Modulate = linkCount > 0 ? new Color(0.55f, 0.92f, 1f) : Colors.White;
            _skillNames[i].TooltipText = $"{skill.DisplayName}\n{skill.ManaCost:0} mana\nLinks: {linkedNames}";
            _skillCooldowns[i].Text = !unlocked ? $"LV {_player.Progression.GetSkillRequiredLevel(skill.Id)}" : cooldown > 0f ? $"{cooldown:0.0}s" : $"{skill.ManaCost:0} MP";
            _skillCooldowns[i].Modulate = !unlocked ? new Color(0.5f, 0.35f, 0.4f) : cooldown > 0f ? new Color(1f, 0.42f, 0.25f) : new Color(0.35f, 1f, 0.62f);
            _skillPanels[i].Modulate = i == _caster.SelectedSlot ? new Color(1.12f, 1.12f, 1.12f) : unlocked ? Colors.White : new Color(0.45f, 0.45f, 0.48f);
        }
    }

    private static Label MakeOrbLabel(string title, Vector2 position)
    {
        var label = new Label
        {
            Text = title,
            Position = position,
            Size = new Vector2(122f, 50f),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.AddThemeFontSizeOverride("font_size", 12);
        label.AddThemeColorOverride("font_shadow_color", Colors.Black);
        label.AddThemeConstantOverride("shadow_offset_x", 2);
        label.AddThemeConstantOverride("shadow_offset_y", 2);
        return label;
    }

    private static StyleBoxFlat MakePanel(Color background, Color border, int radius)
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            CornerRadiusBottomLeft = radius,
            CornerRadiusBottomRight = radius,
        };
    }
}

public partial class VitalOrb : Control
{
    private float _fill = 1f;
    public Color LiquidColor { get; set; } = Colors.Red;
    public Color RimColor { get; set; } = Colors.Gold;
    public float Fill
    {
        get => _fill;
        set { _fill = Mathf.Clamp(value, 0f, 1f); QueueRedraw(); }
    }

    public override void _Draw()
    {
        Vector2 center = Size * 0.5f;
        float radius = Mathf.Min(Size.X, Size.Y) * 0.46f;
        DrawCircle(center, radius + 5f, new Color(0.02f, 0.025f, 0.04f, 0.98f));
        DrawCircle(center, radius, new Color(0.055f, 0.06f, 0.08f));
        float liquidTop = center.Y + radius - radius * 2f * _fill;
        for (float y = liquidTop; y <= center.Y + radius; y += 2f)
        {
            float dy = y - center.Y;
            float halfWidth = Mathf.Sqrt(Mathf.Max(0f, radius * radius - dy * dy));
            DrawLine(new Vector2(center.X - halfWidth, y), new Vector2(center.X + halfWidth, y), LiquidColor, 2.2f);
        }
        DrawArc(center, radius + 2f, 0f, Mathf.Tau, 64, RimColor, 3f, true);
        DrawArc(center, radius - 5f, 3.7f, 5.3f, 24, new Color(1f, 1f, 1f, 0.28f), 3f, true);
    }
}

public partial class MinimapControl : Control
{
    private readonly PlayerController _player;
    private const float RadarRange = 48f;

    public MinimapControl(PlayerController player)
    {
        _player = player;
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        Vector2 center = Size * 0.5f + new Vector2(0f, 8f);
        float radius = Mathf.Min(Size.X, Size.Y) * 0.44f;
        DrawCircle(center, radius + 5f, new Color(0.01f, 0.018f, 0.035f, 0.94f));
        DrawCircle(center, radius, new Color(0.035f, 0.07f, 0.105f, 0.9f));
        DrawArc(center, radius * 0.5f, 0f, Mathf.Tau, 48, new Color(0.2f, 0.48f, 0.62f, 0.25f), 1f);
        DrawLine(center + new Vector2(-radius, 0f), center + new Vector2(radius, 0f), new Color(0.2f, 0.48f, 0.62f, 0.2f));
        DrawLine(center + new Vector2(0f, -radius), center + new Vector2(0f, radius), new Color(0.2f, 0.48f, 0.62f, 0.2f));

        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is not Enemy enemy || !IsInstanceValid(enemy) || enemy.IsTrainingDummy)
            {
                continue;
            }
            Vector3 relative = enemy.GlobalPosition - _player.GlobalPosition;
            Vector2 point = new(relative.X, relative.Z);
            point *= radius / RadarRange;
            if (point.Length() <= radius - 5f)
            {
                DrawCircle(center + point, enemy.IsElite ? 4.5f : 3.2f, enemy.IsElite ? new Color(1f, 0.75f, 0.12f) : new Color(1f, 0.24f, 0.16f));
            }
        }

        foreach (Node node in GetTree().GetNodesInGroup("loot"))
        {
            if (node is not LootDrop drop || !IsInstanceValid(drop) || drop.Item.Rarity < ItemRarity.Rare)
            {
                continue;
            }
            Vector3 relative = drop.GlobalPosition - _player.GlobalPosition;
            Vector2 point = new(relative.X, relative.Z);
            point *= radius / RadarRange;
            if (point.Length() <= radius - 5f)
            {
                DrawCircle(center + point, 2.8f, drop.Item.RarityColor);
            }
        }

        Vector2 totem = new Vector2(-_player.GlobalPosition.X, -_player.GlobalPosition.Z) * (radius / RadarRange);
        if (totem.Length() <= radius - 5f)
        {
            DrawCircle(center + totem, 4f, new Color(0.55f, 0.2f, 1f));
        }

        float facing = _player.Rotation.Y;
        Vector2 forward = new(-Mathf.Sin(facing), -Mathf.Cos(facing));
        Vector2 side = new(-forward.Y, forward.X);
        Vector2[] arrow = { center + forward * 10f, center - forward * 6f + side * 5f, center - forward * 6f - side * 5f };
        DrawColoredPolygon(arrow, new Color(0.3f, 0.95f, 1f));
        DrawArc(center, radius + 2f, 0f, Mathf.Tau, 64, new Color(0.25f, 0.7f, 0.95f), 2.5f, true);
    }
}
