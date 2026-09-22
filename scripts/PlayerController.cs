using Godot;

namespace Sigilwoven;

public partial class PlayerController : CharacterBody3D
{
    private const float Speed = 6f;
    private const float Gravity = 20f;

    private Node3D? _camPivot;
    private Camera3D? _camera;
    public SkillCaster? Caster { get; private set; }
    public PlayerProgression Progression { get; } = new();
    private LinkMenuUI? _menu;
    private PassiveTreeUI? _passiveTree;
    private Label? _statusLabel;
    private Label? _healthLabel;
    private Label? _manaLabel;
    private Label? _combatMessage;
    private Label? _objectiveLabel;
    private Label? _progressionLabel;
    private ProgressBar? _healthBar;
    private ProgressBar? _manaBar;
    private StandardMaterial3D? _bodyMaterial;
    private MeshInstance3D? _bodyMesh;
    private Vector3? _moveTarget;
    private float _stunLeft;
    private float _damageDebuffLeft;
    private float _ritualLockLeft;
    private float _invulnerableLeft;
    private float _hasteLeft;
    private float _stoneSkinLeft;
    private float _frostArmorLeft;

    public const float BaseMaxHealth = 150f;
    public const float BaseMaxMana = 100f;
    public float MaxHealth => BaseMaxHealth + Progression.HealthBonus;
    public float MaxMana => BaseMaxMana + Progression.ManaBonus;
    public float Health { get; private set; } = BaseMaxHealth;
    public float Mana { get; private set; } = BaseMaxMana;

    public float DamageMultiplier => (_damageDebuffLeft > 0f ? 0.7f : 1f) * Progression.DamageMultiplier;
    public bool IsBirthLocked => _stunLeft > 0f;
    public bool IsControlLocked => _ritualLockLeft > 0f;
    public bool IsGameplayInputLocked => IsControlLocked || (_menu?.IsOpen() ?? false) || (_passiveTree?.IsOpen() ?? false);

    public override void _Ready()
    {
        CollisionLayer = PhysicsLayers.Player;
        CollisionMask = PhysicsLayers.World | PhysicsLayers.Enemy;
        AddChild(Progression);
        Health = MaxHealth;
        Mana = MaxMana;
        Progression.Changed += UpdateProgressionHud;

        _bodyMaterial = new StandardMaterial3D { AlbedoColor = new Color(0.3f, 0.6f, 1f), EmissionEnabled = true, Emission = new Color(0.05f, 0.15f, 0.35f), EmissionEnergyMultiplier = 0.5f };
        _bodyMesh = new MeshInstance3D { Mesh = new CapsuleMesh { Radius = 0.4f, Height = 1.6f } };
        _bodyMesh.SetSurfaceOverrideMaterial(0, _bodyMaterial);
        _bodyMesh.Position = new Vector3(0, 1f, 0);
        AddChild(_bodyMesh);

        var col = new CollisionShape3D { Shape = new CapsuleShape3D { Radius = 0.4f, Height = 1.6f } };
        col.Position = new Vector3(0, 1f, 0);
        AddChild(col);

        _camPivot = new Node3D();
        _camPivot.Position = new Vector3(0, 1.6f, 0);
        AddChild(_camPivot);

        _camera = new Camera3D();
        _camera.Current = true;
        _camera.Fov = 52f;
        // Top-level prevents the camera from inheriting the hero's facing rotation.
        _camera.TopLevel = true;
        _camPivot.AddChild(_camera);
        UpdateFixedCamera();

        Caster = new SkillCaster();
        AddChild(Caster);
        Caster.Setup(this);

        _menu = new LinkMenuUI();
        AddChild(_menu);
        _menu.Setup(Caster, Progression);

        _passiveTree = new PassiveTreeUI();
        _passiveTree.Setup(Progression);
        AddChild(_passiveTree);

        var layer = new CanvasLayer();
        layer.Layer = 5;
        AddChild(layer);
        var hudBack = new ColorRect
        {
            Position = new Vector2(8, 7),
            Size = new Vector2(430, 236),
            Color = new Color(0.015f, 0.025f, 0.07f, 0.78f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        layer.AddChild(hudBack);
        var title = new Label
        {
            Text = "SIGILWOVEN  //  AETHER ARENA",
            Position = new Vector2(20, 12),
            Modulate = new Color(0.35f, 0.85f, 1f),
        };
        title.AddThemeFontSizeOverride("font_size", 16);
        layer.AddChild(title);
        var hint = new Label();
        hint.Text = "Hold LMB move | 1-6, Z/X/C/V cast | L links | P passives | Esc close";
        hint.Position = new Vector2(20, 36);
        hint.Modulate = new Color(0.72f, 0.78f, 0.9f);
        hint.AddThemeFontSizeOverride("font_size", 12);
        layer.AddChild(hint);

        _statusLabel = new Label();
        _statusLabel.Position = new Vector2(20, 61);
        _statusLabel.Modulate = new Color(0.95f, 0.9f, 0.55f);
        _statusLabel.AddThemeFontSizeOverride("font_size", 13);
        _statusLabel.Text = "slot 1";
        layer.AddChild(_statusLabel);

        _healthLabel = new Label { Position = new Vector2(20, 88), Modulate = new Color(1f, 0.45f, 0.45f) };
        layer.AddChild(_healthLabel);
        _healthBar = new ProgressBar { Position = new Vector2(115, 88), Size = new Vector2(295, 16), MaxValue = MaxHealth, ShowPercentage = false };
        layer.AddChild(_healthBar);
        _manaLabel = new Label { Position = new Vector2(20, 114), Modulate = new Color(0.45f, 0.75f, 1f) };
        layer.AddChild(_manaLabel);
        _manaBar = new ProgressBar { Position = new Vector2(115, 114), Size = new Vector2(295, 16), MaxValue = MaxMana, ShowPercentage = false };
        layer.AddChild(_manaBar);
        _combatMessage = new Label { Position = new Vector2(20, 143), Modulate = new Color(1f, 0.8f, 0.35f) };
        layer.AddChild(_combatMessage);
        _objectiveLabel = new Label { Position = new Vector2(20, 171), Modulate = new Color(0.48f, 0.9f, 1f) };
        _objectiveLabel.AddThemeFontSizeOverride("font_size", 13);
        _objectiveLabel.Text = "OBJECTIVE  //  AWAKEN";
        layer.AddChild(_objectiveLabel);
        _progressionLabel = new Label { Position = new Vector2(20, 199), Modulate = new Color(0.82f, 0.72f, 1f) };
        _progressionLabel.AddThemeFontSizeOverride("font_size", 12);
        layer.AddChild(_progressionLabel);
        UpdateProgressionHud();
        UpdateHud();

        try
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        catch
        {
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (IsGameplayInputLocked)
        {
            return;
        }
        if (@event is InputEventMouseButton mouse && mouse.ButtonIndex == MouseButton.Left && mouse.Pressed)
        {
            _moveTarget = GetAimPoint();
            GetViewport().SetInputAsHandled();
            return;
        }
        if (@event is InputEventKey key && key.Pressed && !key.Echo && Caster != null)
        {
            int slot = key.Keycode switch
            {
                Key.Key1 => 0,
                Key.Key2 => 1,
                Key.Key3 => 2,
                Key.Key4 => 3,
                Key.Key5 => 4,
                Key.Key6 => 5,
                Key.Z => 6,
                Key.X => 7,
                Key.C => 8,
                Key.V => 9,
                _ => -1,
            };
            if (slot >= 0 && !IsGameplayInputLocked)
            {
                Caster.SelectAndCast(slot, GetAimPoint());
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public override void _Process(double delta)
    {
        if (_statusLabel != null && Caster != null)
        {
            string ritualState = IsControlLocked ? $" | RITUAL {Mathf.Ceil(_ritualLockLeft)}s" : "";
            string birthState = IsBirthLocked ? $" | STUN {Mathf.Ceil(_stunLeft)}s" : "";
            string debuffState = _damageDebuffLeft > 0f ? " | damage -30%" : "";
            _statusLabel.Text = Caster.GetSelectedName() + ritualState + birthState + debuffState;
        }
        float step = (float)delta;
        _stunLeft = Mathf.Max(0f, _stunLeft - step);
        _damageDebuffLeft = Mathf.Max(0f, _damageDebuffLeft - (float)delta);
        _ritualLockLeft = Mathf.Max(0f, _ritualLockLeft - (float)delta);
        _invulnerableLeft = Mathf.Max(0f, _invulnerableLeft - step);
        _hasteLeft = Mathf.Max(0f, _hasteLeft - step);
        _stoneSkinLeft = Mathf.Max(0f, _stoneSkinLeft - step);
        _frostArmorLeft = Mathf.Max(0f, _frostArmorLeft - step);
        Mana = Mathf.Min(MaxMana, Mana + 8f * step);
        UpdateHud();
    }

    public override void _PhysicsProcess(double delta)
    {
        // ARPG movement: a ground click gives the hero a destination.
        Vector3 worldDir = Vector3.Zero;
        // Modal windows and interactive UI fully suspend click-to-move.
        bool inputBlocked = IsGameplayInputLocked || IsPointerOverBlockingUi();
        if (inputBlocked)
        {
            _moveTarget = null;
        }
        if (!inputBlocked && Input.IsMouseButtonPressed(MouseButton.Left))
        {
            _moveTarget = GetAimPoint();
        }
        if (!inputBlocked && _moveTarget.HasValue)
        {
            worldDir = _moveTarget.Value - GlobalPosition;
            worldDir.Y = 0f;
            if (worldDir.Length() < 0.2f)
            {
                _moveTarget = null;
                worldDir = Vector3.Zero;
            }
            else
            {
                worldDir = worldDir.Normalized();
            }
        }

        Vector3 vel = Velocity;
        float speed = Speed * (_hasteLeft > 0f ? 1.35f : 1f);
        vel.X = worldDir.X * speed;
        vel.Z = worldDir.Z * speed;
        vel.Y -= Gravity * (float)delta;
        Velocity = vel;
        MoveAndSlide();
        if (GlobalPosition.Y < 0f)
        {
            GlobalPosition = new Vector3(GlobalPosition.X, 0f, GlobalPosition.Z);
        }

        // Face movement direction.
        if (worldDir.Length() > 0.1f)
        {
            float targetYaw = Mathf.Atan2(-worldDir.X, -worldDir.Z) + Mathf.Pi;
            Rotation = new Vector3(0, targetYaw, 0);
        }

        UpdateFixedCamera();

    }

    private bool IsPointerOverBlockingUi()
    {
        Control? hovered = GetViewport().GuiGetHoveredControl();
        return hovered != null && hovered.MouseFilter != Control.MouseFilterEnum.Ignore;
    }

    public void BeginBirth(float stunSeconds, float damageDebuffSeconds)
    {
        _stunLeft = stunSeconds;
        _damageDebuffLeft = damageDebuffSeconds;
        _ritualLockLeft = damageDebuffSeconds;
        _moveTarget = null;
        if (_bodyMesh == null)
        {
            return;
        }
        _bodyMesh.Scale = Vector3.One * 0.05f;
        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(_bodyMesh, "scale", Vector3.One, 5.0f);
    }

    private void UpdateFixedCamera()
    {
        if (_camera == null)
        {
            return;
        }
        // A constant world-space angle, independent of the player's Rotation.
        _camera.GlobalPosition = GlobalPosition + new Vector3(0f, 17.6f, 14f);
        _camera.LookAt(GlobalPosition + new Vector3(0f, 0.6f, 0f), Vector3.Up);
    }

    public bool TrySpendMana(float amount)
    {
        if (Mana + 0.001f < amount)
        {
            ShowCombatMessage("NOT ENOUGH MANA");
            return false;
        }
        Mana -= amount;
        return true;
    }

    public void RestoreMana(float amount)
    {
        Mana = Mathf.Min(MaxMana, Mana + amount);
        ShowCombatMessage($"AETHER +{(int)amount}");
    }

    public void TakeDamage(float amount, string source = "Enemy")
    {
        if (_invulnerableLeft > 0f || Health <= 0f || float.IsNaN(amount) || float.IsInfinity(amount))
        {
            return;
        }
        amount = Mathf.Max(0f, amount);
        float mitigation = _stoneSkinLeft > 0f ? 0.55f : (_frostArmorLeft > 0f ? 0.75f : 1f);
        float finalDamage = amount * mitigation;
        Health = Mathf.Max(0f, Health - finalDamage);
        ShowCombatMessage($"{source}: -{(int)finalDamage} HP");
        FlashBody(new Color(1f, 0.08f, 0.08f));
        if (Health <= 0f)
        {
            RespawnAtTotem();
        }
    }

    public void ActivateBuff(string skillId, float duration)
    {
        switch (skillId)
        {
            case "haste_aura": _hasteLeft = Mathf.Max(_hasteLeft, duration); break;
            case "stone_skin": _stoneSkinLeft = Mathf.Max(_stoneSkinLeft, duration); break;
            case "frost_armor": _frostArmorLeft = Mathf.Max(_frostArmorLeft, duration); break;
        }
        ShowCombatMessage($"{skillId.Replace('_', ' ').ToUpper()} {duration:0}s");
    }

    public void DashTo(Vector3 destination)
    {
        GlobalPosition = destination;
        _moveTarget = null;
        FlashBody(new Color(1f, 0.35f, 0.08f));
    }

    public void ShowCombatMessage(string text)
    {
        if (_combatMessage != null)
        {
            _combatMessage.Text = text;
        }
    }

    public void SetObjectiveStatus(string text)
    {
        if (_objectiveLabel != null)
        {
            _objectiveLabel.Text = $"OBJECTIVE  //  {text}";
        }
    }

    private void UpdateProgressionHud()
    {
        if (_progressionLabel == null)
        {
            return;
        }
        if (Progression.Level < PlayerProgression.MaxLevel)
        {
            _progressionLabel.Text = $"{Progression.HeroClass.ToString().ToUpperInvariant()}  •  LV {Progression.Level}/{PlayerProgression.MaxLevel}  •  XP {Progression.Experience}/{Progression.ExperienceToNextLevel}  •  PASSIVE {Progression.PassivePoints}";
        }
        else
        {
            _progressionLabel.Text = $"LV 300  •  PARAGON {Progression.ParagonLevel}  •  XP {Progression.ParagonExperience}/{Progression.ExperienceToNextParagon}  •  POINTS {Progression.ParagonPoints}";
        }
    }

    private void RespawnAtTotem()
    {
        Health = MaxHealth;
        Mana = MaxMana;
        GlobalPosition = new Vector3(0f, 0.2f, 0f);
        _moveTarget = null;
        _invulnerableLeft = 2.5f;
        ShowCombatMessage("REBORN AT THE TOTEM — 2s WARD");
        FlashBody(new Color(0.2f, 0.85f, 1f));
    }

    private void FlashBody(Color flash)
    {
        if (_bodyMaterial == null)
        {
            return;
        }
        _bodyMaterial.AlbedoColor = flash;
        var tween = CreateTween();
        tween.TweenProperty(_bodyMaterial, "albedo_color", new Color(0.3f, 0.6f, 1f), 0.18f);
    }

    private void UpdateHud()
    {
        if (_healthBar != null)
        {
            _healthBar.MaxValue = MaxHealth;
            _healthBar.Value = Health;
        }
        if (_manaBar != null)
        {
            _manaBar.MaxValue = MaxMana;
            _manaBar.Value = Mana;
        }
        if (_healthLabel != null) _healthLabel.Text = $"HEALTH  {(int)Health}/{(int)MaxHealth}";
        if (_manaLabel != null) _manaLabel.Text = $"MANA    {(int)Mana}/{(int)MaxMana}";
    }

    private Vector3 GetAimPoint()
    {
        if (_camera == null)
        {
            return GlobalPosition + new Vector3(0, 0, -5f);
        }
        Vector2 mouse = GetViewport().GetMousePosition();
        Vector3 origin = _camera.ProjectRayOrigin(mouse);
        Vector3 dir = _camera.ProjectRayNormal(mouse);
        if (Mathf.Abs(dir.Y) < 0.0001f)
        {
            return GlobalPosition + -GlobalTransform.Basis.Z * 5f;
        }
        float t = -origin.Y / dir.Y;
        if (t < 0f || t > 200f)
        {
            return origin + dir * 10f;
        }
        return origin + dir * t;
    }
}
