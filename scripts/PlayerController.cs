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
    public PlayerInventory Inventory { get; } = new();
    private LinkMenuUI? _menu;
    private PassiveTreeUI? _passiveTree;
    private InventoryUI? _inventoryUi;
    private GameHud? _hud;
    private StandardMaterial3D? _bodyMaterial;
    private MeshInstance3D? _bodyMesh;
    private Node3D? _visualRoot;
    private float _visualTime;
    private readonly Color _bodyBaseColor = new(0.72f, 0.82f, 0.9f);
    private AnimationPlayer? _characterAnimator;
    private string _currentAnimation = "";
    private float _actionAnimationLeft;
    private bool _usesImportedModel;
    private bool _isDead;
    private bool _preGameLocked = true;
    private Vector3 _visualBaseScale = Vector3.One;
    private Vector3? _moveTarget;
    private float _stunLeft;
    private float _damageDebuffLeft;
    private float _ritualLockLeft;
    private float _invulnerableLeft;
    private float _hasteLeft;
    private float _stoneSkinLeft;
    private float _frostArmorLeft;
    private float _flaskCooldownLeft;
    private float _flaskHealLeft;
    private float _flaskHealPerSecond;

    public const float BaseMaxHealth = 150f;
    public const float BaseMaxMana = 100f;
    public float MaxHealth => BaseMaxHealth + Progression.HealthBonus + Inventory.HealthBonus;
    public float MaxMana => BaseMaxMana + Progression.ManaBonus + Inventory.ManaBonus;
    public float Health { get; private set; } = BaseMaxHealth;
    public float Mana { get; private set; } = BaseMaxMana;
    public const int MaxFlaskCharges = 30;
    public const int FlaskChargeCost = 10;
    public int FlaskCharges { get; private set; } = MaxFlaskCharges;
    public bool CanUseFlask => FlaskCharges >= FlaskChargeCost && _flaskCooldownLeft <= 0f && Health > 0f && Health < MaxHealth;

    public float DamageMultiplier => (_damageDebuffLeft > 0f ? 0.7f : 1f) * Progression.DamageMultiplier * (1f + Inventory.DamagePercent / 100f);
    public float CooldownMultiplier => Progression.CooldownMultiplier * (1f - Inventory.CooldownReduction / 100f);
    public bool IsBirthLocked => _stunLeft > 0f;
    public bool IsControlLocked => _ritualLockLeft > 0f;
    public bool IsGameplayInputLocked => _preGameLocked || _isDead || IsControlLocked || (_menu?.IsOpen() ?? false) || (_passiveTree?.IsOpen() ?? false) || (_inventoryUi?.IsOpen() ?? false);

    public override void _Ready()
    {
        AddToGroup("player");
        CollisionLayer = PhysicsLayers.Player;
        CollisionMask = PhysicsLayers.World | PhysicsLayers.Enemy;
        AddChild(Progression);
        AddChild(Inventory);
        Inventory.Changed += OnEquipmentChanged;
        Health = MaxHealth;
        Mana = MaxMana;
        Progression.Changed += UpdateProgressionHud;

        BuildHeroVisual();

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

        _inventoryUi = new InventoryUI(Inventory);
        AddChild(_inventoryUi);

        _hud = new GameHud(this, Caster);
        AddChild(_hud);
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
        if (@event is InputEventKey inventoryKey && inventoryKey.Pressed && !inventoryKey.Echo && inventoryKey.Keycode == Key.I)
        {
            if ((_inventoryUi?.IsOpen() ?? false) || (!(_menu?.IsOpen() ?? false) && !(_passiveTree?.IsOpen() ?? false) && !IsControlLocked))
            {
                _inventoryUi?.Toggle();
                GetViewport().SetInputAsHandled();
            }
            return;
        }
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
            if (key.Keycode == Key.F)
            {
                TryPickupNearbyLoot();
                GetViewport().SetInputAsHandled();
                return;
            }
            if (key.Keycode == Key.Q)
            {
                TryUseLifeFlask();
                GetViewport().SetInputAsHandled();
                return;
            }
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
        float step = (float)delta;
        _stunLeft = Mathf.Max(0f, _stunLeft - step);
        _damageDebuffLeft = Mathf.Max(0f, _damageDebuffLeft - (float)delta);
        _ritualLockLeft = Mathf.Max(0f, _ritualLockLeft - (float)delta);
        _invulnerableLeft = Mathf.Max(0f, _invulnerableLeft - step);
        _hasteLeft = Mathf.Max(0f, _hasteLeft - step);
        _stoneSkinLeft = Mathf.Max(0f, _stoneSkinLeft - step);
        _frostArmorLeft = Mathf.Max(0f, _frostArmorLeft - step);
        _flaskCooldownLeft = Mathf.Max(0f, _flaskCooldownLeft - step);
        _actionAnimationLeft = Mathf.Max(0f, _actionAnimationLeft - step);
        if (_flaskHealLeft > 0f)
        {
            float healingStep = Mathf.Min(step, _flaskHealLeft);
            Health = Mathf.Min(MaxHealth, Health + _flaskHealPerSecond * healingStep);
            _flaskHealLeft -= healingStep;
        }
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
        AnimateHero((float)delta, worldDir.LengthSquared() > 0.01f);

    }

    private void BuildHeroVisual()
    {
        _visualRoot = new Node3D();
        AddChild(_visualRoot);

        if (TryBuildImportedHero())
        {
            return;
        }

        Color classAccent = Progression.HeroClass switch
        {
            HeroClass.Aetherist => new Color(0.55f, 0.24f, 1f),
            HeroClass.Warden => new Color(0.2f, 0.9f, 0.58f),
            _ => new Color(0.1f, 0.72f, 1f),
        };
        _bodyMaterial = new StandardMaterial3D
        {
            AlbedoColor = _bodyBaseColor,
            AlbedoTexture = GD.Load<Texture2D>("res://assets/textures/hero_runebound_armor-v1.png"),
            Metallic = 0.65f,
            Roughness = 0.42f,
            EmissionEnabled = true,
            Emission = classAccent * 0.35f,
            EmissionEnergyMultiplier = 0.8f,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic,
        };
        var darkMetal = new StandardMaterial3D { AlbedoColor = new Color(0.055f, 0.07f, 0.1f), Metallic = 0.85f, Roughness = 0.3f };
        var runeMaterial = new StandardMaterial3D { AlbedoColor = classAccent, EmissionEnabled = true, Emission = classAccent, EmissionEnergyMultiplier = 3.2f, Metallic = 0.35f };

        _bodyMesh = AddVisualPart(_visualRoot, new BoxMesh { Size = new Vector3(0.72f, 0.82f, 0.42f) }, _bodyMaterial, new Vector3(0f, 1.15f, 0f));
        AddVisualPart(_visualRoot, new BoxMesh { Size = new Vector3(0.58f, 0.3f, 0.38f) }, darkMetal, new Vector3(0f, 0.66f, 0f));
        AddVisualPart(_visualRoot, new SphereMesh { Radius = 0.27f, Height = 0.54f }, _bodyMaterial, new Vector3(0f, 1.82f, 0f));
        AddVisualPart(_visualRoot, new CylinderMesh { TopRadius = 0.22f, BottomRadius = 0.29f, Height = 0.22f }, darkMetal, new Vector3(0f, 2.02f, 0f));

        foreach (float side in new[] { -1f, 1f })
        {
            AddVisualPart(_visualRoot, new SphereMesh { Radius = 0.24f, Height = 0.34f }, _bodyMaterial, new Vector3(side * 0.47f, 1.45f, 0f), new Vector3(1.18f, 0.72f, 1f));
            AddVisualPart(_visualRoot, new CylinderMesh { TopRadius = 0.1f, BottomRadius = 0.12f, Height = 0.62f }, darkMetal, new Vector3(side * 0.48f, 1.05f, 0f), new Vector3(0f, 0f, side * -0.12f));
            AddVisualPart(_visualRoot, new CylinderMesh { TopRadius = 0.13f, BottomRadius = 0.16f, Height = 0.68f }, darkMetal, new Vector3(side * 0.2f, 0.3f, 0f));
        }
        AddVisualPart(_visualRoot, new BoxMesh { Size = new Vector3(0.08f, 0.52f, 0.05f) }, runeMaterial, new Vector3(0f, 1.2f, -0.225f));
        AddVisualPart(_visualRoot, new BoxMesh { Size = new Vector3(0.15f, 1.35f, 0.09f) }, darkMetal, new Vector3(0.62f, 1.05f, 0.12f), new Vector3(0f, 0f, -0.35f));
        AddVisualPart(_visualRoot, new BoxMesh { Size = new Vector3(0.075f, 1.08f, 0.035f) }, runeMaterial, new Vector3(0.615f, 1.18f, 0.065f), new Vector3(0f, 0f, -0.35f));
    }

    private bool TryBuildImportedHero()
    {
        if (_visualRoot == null)
        {
            return false;
        }
        PackedScene? characterScene = GD.Load<PackedScene>("res://assets/models/quaternius_knight/KnightCharacter.fbx");
        if (characterScene?.Instantiate() is not Node3D character)
        {
            return false;
        }
        // Align the centimetre-scaled FBX with the player's gameplay capsule.
        _visualBaseScale = Vector3.One * 0.27f;
        _visualRoot.Scale = _visualBaseScale;
        _visualRoot.AddChild(character);
        _characterAnimator = character.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        Skeleton3D? skeleton = character.GetNodeOrNull<Skeleton3D>("HumanArmature/Skeleton3D");
        PackedScene? swordScene = GD.Load<PackedScene>("res://assets/models/quaternius_knight/Sword.fbx");
        if (skeleton != null && swordScene?.Instantiate() is Node3D sword)
        {
            var attachment = new BoneAttachment3D { BoneName = "Palm.R" };
            skeleton.AddChild(attachment);
            attachment.AddChild(sword);
            // Both FBX files carry a centimetre-to-metre x100 transform. The
            // sword is already under the character armature, so cancel its
            // second conversion to avoid a 100x weapon on the hand bone.
            sword.Scale = Vector3.One * 0.01f;
            sword.RotationDegrees = new Vector3(0f, 0f, 90f);
        }
        if (_characterAnimator == null)
        {
            character.QueueFree();
            _visualRoot.Scale = Vector3.One;
            return false;
        }
        SetAnimationLoop("HumanArmature|Idle_swordRight");
        SetAnimationLoop("HumanArmature|Run_swordRight");
        SetAnimationLoop("HumanArmature|Walking");
        _usesImportedModel = true;
        PlayCharacterAnimation("HumanArmature|Idle_swordRight", true);
        GD.Print("[Player] CC0 animated knight loaded with skeletal animation.");
        return true;
    }

    private void SetAnimationLoop(string animationName)
    {
        if (_characterAnimator?.GetAnimation(animationName) is Animation animation)
        {
            animation.LoopMode = Animation.LoopModeEnum.Linear;
        }
    }

    private void PlayCharacterAnimation(string animationName, bool force = false)
    {
        if (_characterAnimator == null || (!force && _currentAnimation == animationName))
        {
            return;
        }
        _currentAnimation = animationName;
        _characterAnimator.Play(animationName, 0.12f);
    }

    public void PlayCastAnimation()
    {
        if (!_usesImportedModel || _isDead) return;
        _actionAnimationLeft = 0.72f;
        PlayCharacterAnimation("HumanArmature|Run_swordAttack", true);
    }

    public void ConfirmClassSelection(HeroClass heroClass)
    {
        Progression.ChooseClassForRun(heroClass);
        Health = MaxHealth;
        Mana = MaxMana;
        _preGameLocked = false;
        _hud?.RefreshProgression();
        ShowCombatMessage($"{heroClass.ToString().ToUpperInvariant()} PATH BOUND");
    }

    private static MeshInstance3D AddVisualPart(Node3D parent, PrimitiveMesh mesh, Material material, Vector3 position, Vector3? rotation = null, Vector3? scale = null)
    {
        var part = new MeshInstance3D { Mesh = mesh, Position = position, Rotation = rotation ?? Vector3.Zero, Scale = scale ?? Vector3.One };
        part.SetSurfaceOverrideMaterial(0, material);
        parent.AddChild(part);
        return part;
    }

    private void AnimateHero(float delta, bool moving)
    {
        if (_visualRoot == null) return;
        if (_usesImportedModel)
        {
            if (!_isDead && _actionAnimationLeft <= 0f)
            {
                PlayCharacterAnimation(moving ? "HumanArmature|Run_swordRight" : "HumanArmature|Idle_swordRight");
            }
            return;
        }
        _visualTime += delta;
        float stride = moving ? Mathf.Sin(_visualTime * 10f) : Mathf.Sin(_visualTime * 2f) * 0.15f;
        _visualRoot.Position = new Vector3(0f, Mathf.Abs(stride) * (moving ? 0.055f : 0.018f), 0f);
        _visualRoot.Rotation = new Vector3(0f, 0f, stride * (moving ? 0.035f : 0.012f));
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
        if (_visualRoot == null)
        {
            return;
        }
        _visualRoot.Scale = _visualBaseScale * 0.05f;
        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(_visualRoot, "scale", _visualBaseScale, 5.0f);
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

    public void RechargeFlask(int charges)
    {
        if (charges <= 0)
        {
            return;
        }
        FlaskCharges = Mathf.Min(MaxFlaskCharges, FlaskCharges + charges);
    }

    private void TryPickupNearbyLoot()
    {
        LootDrop? nearest = null;
        float nearestDistance = 3.6f;
        foreach (Node node in GetTree().GetNodesInGroup("loot"))
        {
            if (node is not LootDrop drop || !IsInstanceValid(drop))
            {
                continue;
            }
            if (!Inventory.ShouldShow(drop.Item))
            {
                continue;
            }
            float distance = GlobalPosition.DistanceTo(drop.GlobalPosition);
            if (distance < nearestDistance)
            {
                nearest = drop;
                nearestDistance = distance;
            }
        }
        if (nearest == null)
        {
            ShowCombatMessage("NO LOOT IN REACH");
            return;
        }
        nearest.Collect(this);
    }

    private void OnEquipmentChanged()
    {
        Health = Mathf.Min(Health, MaxHealth);
        Mana = Mathf.Min(Mana, MaxMana);
        ShowCombatMessage(Inventory.BuildEquipmentSummary());
    }

    private void TryUseLifeFlask()
    {
        if (!CanUseFlask)
        {
            ShowCombatMessage(Health >= MaxHealth ? "LIFE ALREADY FULL" : FlaskCharges < FlaskChargeCost ? "LIFE FLASK HAS NO CHARGES" : "LIFE FLASK RECHARGING");
            return;
        }
        FlaskCharges -= FlaskChargeCost;
        _flaskCooldownLeft = 0.65f;
        Health = Mathf.Min(MaxHealth, Health + MaxHealth * 0.28f);
        _flaskHealLeft = 2.5f;
        _flaskHealPerSecond = MaxHealth * 0.18f / _flaskHealLeft;
        ShowCombatMessage("LIFE FLASK — RESTORING 46% HEALTH");
        FlashBody(new Color(1f, 0.16f, 0.22f));
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
            BeginDeath();
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
        if (_usesImportedModel)
        {
            _actionAnimationLeft = 0.8f;
            PlayCharacterAnimation("HumanArmature|Roll_sword", true);
        }
        GlobalPosition = destination;
        _moveTarget = null;
        FlashBody(new Color(1f, 0.35f, 0.08f));
    }

    public void ShowCombatMessage(string text)
    {
        _hud?.ShowCombatMessage(text);
    }

    public void SetObjectiveStatus(string text)
    {
        _hud?.SetObjective(text);
    }

    private void UpdateProgressionHud()
    {
        _hud?.RefreshProgression();
    }

    private void BeginDeath()
    {
        if (_isDead) return;
        _isDead = true;
        _moveTarget = null;
        PlayCharacterAnimation("HumanArmature|Death", true);
        ShowCombatMessage("FALLEN — THE TOTEM CALLS YOU BACK");
        GetTree().CreateTimer(1.45f).Timeout += RespawnAtTotem;
    }

    private void RespawnAtTotem()
    {
        Health = MaxHealth;
        Mana = MaxMana;
        FlaskCharges = MaxFlaskCharges;
        _flaskHealLeft = 0f;
        GlobalPosition = new Vector3(0f, 0.2f, 0f);
        _moveTarget = null;
        _invulnerableLeft = 2.5f;
        _isDead = false;
        _actionAnimationLeft = 0f;
        PlayCharacterAnimation("HumanArmature|Idle_swordRight", true);
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
        tween.TweenProperty(_bodyMaterial, "albedo_color", _bodyBaseColor, 0.18f);
    }

    private void UpdateHud()
    {
        // GameHud reads live values every frame; retained as a stable update hook.
    }

    public string GetCombatStatus()
    {
        string ritualState = IsControlLocked ? $"  •  RITUAL {Mathf.Ceil(_ritualLockLeft)}s" : "";
        string birthState = IsBirthLocked ? $"  •  STUN {Mathf.Ceil(_stunLeft)}s" : "";
        string debuffState = _damageDebuffLeft > 0f ? "  •  DAMAGE -30%" : "";
        return (Caster?.GetSelectedName() ?? "-") + ritualState + birthState + debuffState;
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
