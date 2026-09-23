using Godot;

namespace Sigilwoven;

public partial class PlayerController : CharacterBody3D
{
    private const float Speed = 6f;
    private const float Gravity = 20f;
    private const float DodgeSpeed = 17f;
    private const float DodgeDuration = 0.32f;
    private const float DodgeInvulnerability = 0.24f;
    private const float DodgeCooldown = 1.05f;
    private const float MaxPoise = 100f;

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
    private bool _proceduralImportedModel;
    private string _idleAnimation = "HumanArmature|Idle_swordRight";
    private string _runAnimation = "HumanArmature|Run_swordRight";
    private string _attackAnimation = "HumanArmature|Run_swordAttack";
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
    private float _dodgeTimeLeft;
    private float _dodgeCooldownLeft;
    private Vector3 _dodgeDirection = Vector3.Forward;
    private float _poise = MaxPoise;
    private float _poiseRegenDelay;
    private float _cameraImpulseLeft;
    private float _cameraImpulseStrength;
    private int _hitStopSerial;

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
    public float CriticalChance => Inventory.CriticalChance;
    public float CriticalDamage => Inventory.CriticalDamage;
    public float Armor => Inventory.Armor;
    public float ElementalPenetration => Inventory.ElementalPenetration;
    public float Poise => _poise;
    public bool IsDodging => _dodgeTimeLeft > 0f;
    public bool IsBirthLocked => _stunLeft > 0f;
    public bool IsControlLocked => _ritualLockLeft > 0f;
    public bool IsGameplayInputLocked => _preGameLocked || _isDead || IsControlLocked || _stunLeft > 0f || (_menu?.IsOpen() ?? false) || (_passiveTree?.IsOpen() ?? false) || (_inventoryUi?.IsOpen() ?? false);

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
        if (@event is InputEventMouseButton primary && primary.ButtonIndex == MouseButton.Right && primary.Pressed && Caster != null)
        {
            Caster.TryPrimaryAttack(GetAimPoint());
            GetViewport().SetInputAsHandled();
            return;
        }
        if (@event is InputEventKey key && key.Pressed && !key.Echo && Caster != null)
        {
            if (key.Keycode == Key.Space)
            {
                TryDodge();
                GetViewport().SetInputAsHandled();
                return;
            }
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
        _cameraImpulseLeft = Mathf.Max(0f, _cameraImpulseLeft - step);
        _dodgeTimeLeft = Mathf.Max(0f, _dodgeTimeLeft - step);
        _dodgeCooldownLeft = Mathf.Max(0f, _dodgeCooldownLeft - step);
        _poiseRegenDelay = Mathf.Max(0f, _poiseRegenDelay - step);
        if (_poiseRegenDelay <= 0f && _poise < MaxPoise)
        {
            _poise = Mathf.Min(MaxPoise, _poise + 24f * step);
        }
        if (_flaskHealLeft > 0f)
        {
            float healingStep = Mathf.Min(step, _flaskHealLeft);
            Health = Mathf.Min(MaxHealth, Health + _flaskHealPerSecond * healingStep);
            _flaskHealLeft -= healingStep;
        }
        Mana = Mathf.Min(MaxMana, Mana + 5f * step);
        UpdateHud();
    }

    public override void _PhysicsProcess(double delta)
    {
        // ARPG movement: a ground click gives the hero a destination.
        Vector3 worldDir = Vector3.Zero;
        // Modal windows and interactive UI fully suspend click-to-move.
        bool inputBlocked = (IsGameplayInputLocked || IsPointerOverBlockingUi()) && !IsDodging;
        if (inputBlocked)
        {
            _moveTarget = null;
        }
        if (!inputBlocked && Input.IsMouseButtonPressed(MouseButton.Left))
        {
            _moveTarget = GetAimPoint();
        }
        if (IsDodging)
        {
            worldDir = _dodgeDirection;
            _moveTarget = null;
        }
        else if (!inputBlocked && _moveTarget.HasValue)
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
        float speed = IsDodging ? DodgeSpeed : Speed * (_hasteLeft > 0f ? 1.35f : 1f);
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
        bool knightRig = Progression.HeroClass == HeroClass.Runeblade;
        bool kenneyRig = Progression.HeroClass is HeroClass.Necromancer or HeroClass.Shadowstalker;
        string modelPath = Progression.HeroClass switch
        {
            HeroClass.Aetherist => "res://assets/models/quaternius_enemies/Wizard.fbx",
            HeroClass.Necromancer => "res://assets/models/kenney_graveyard/character-ghost.glb",
            HeroClass.Warden => "res://assets/models/quaternius_enemies/Ninja_Male.fbx",
            HeroClass.Shadowstalker => "res://assets/models/kenney_graveyard/character-vampire.glb",
            HeroClass.Berserker => "res://assets/models/quaternius_enemies/Viking_Male.fbx",
            HeroClass.Templar => "res://assets/models/quaternius_enemies/Knight_Golden_Male.fbx",
            _ => "res://assets/models/quaternius_knight/KnightCharacter.fbx",
        };
        PackedScene? characterScene = GD.Load<PackedScene>(modelPath);
        if (characterScene?.Instantiate() is not Node3D character)
        {
            return false;
        }
        // Align the centimetre-scaled FBX with the player's gameplay capsule.
        _visualBaseScale = Vector3.One * (knightRig ? 0.27f : kenneyRig ? 1f : 0.58f);
        _visualRoot.Scale = _visualBaseScale;
        _visualRoot.AddChild(character);
        _characterAnimator = character.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        if (kenneyRig) _characterAnimator = null;
        Skeleton3D? skeleton = knightRig ? character.GetNodeOrNull<Skeleton3D>("HumanArmature/Skeleton3D") : null;
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
        if (_characterAnimator == null && kenneyRig)
        {
            _proceduralImportedModel = true;
            _usesImportedModel = true;
            GD.Print($"[Player] CC0 {Progression.HeroClass} model loaded with procedural combat motion.");
            return true;
        }
        if (_characterAnimator == null)
        {
            character.QueueFree();
            _visualRoot.Scale = Vector3.One;
            return false;
        }
        _idleAnimation = knightRig ? "HumanArmature|Idle_swordRight" : "CharacterArmature|Idle";
        _runAnimation = knightRig ? "HumanArmature|Run_swordRight" : "CharacterArmature|Walk";
        _attackAnimation = knightRig ? "HumanArmature|Run_swordAttack" : "CharacterArmature|Punch";
        SetAnimationLoop(_idleAnimation);
        SetAnimationLoop(_runAnimation);
        _usesImportedModel = true;
        _proceduralImportedModel = false;
        PlayCharacterAnimation(_idleAnimation, true);
        GD.Print($"[Player] CC0 animated {Progression.HeroClass} model loaded with skeletal animation.");
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
        PlayCharacterAnimation(_attackAnimation, true);
    }

    public void PlaySkillAnimation(SkillData skill)
    {
        if (!_usesImportedModel || _isDead) return;
        _actionAnimationLeft = Mathf.Max(0.45f, skill.CastTime + 0.24f);
        if (Progression.HeroClass == HeroClass.Runeblade)
        {
            string clip = skill.Type switch
            {
                SkillType.Movement => "HumanArmature|Roll_sword",
                SkillType.Summon or SkillType.Projectile => "HumanArmature|swordAttackJump",
                _ => "HumanArmature|Run_swordAttack",
            };
            PlayCharacterAnimation(clip, true);
        }
        else
        {
            PlayCharacterAnimation(skill.Type is SkillType.Projectile or SkillType.Summon ? "CharacterArmature|Shoot_OneHanded" : "CharacterArmature|Punch", true);
        }
    }

    public void ConfirmClassSelection(HeroClass heroClass)
    {
        Progression.ChooseClassForRun(heroClass);
        Inventory.SetActiveClass(heroClass);
        Caster?.ConfigureClass(heroClass);
        RebuildHeroVisual();
        Health = MaxHealth;
        Mana = MaxMana;
        _preGameLocked = false;
        _hud?.RefreshProgression();
        ShowCombatMessage($"{heroClass.ToString().ToUpperInvariant()} PATH BOUND");
    }

    private void RebuildHeroVisual()
    {
        if (_visualRoot != null && IsInstanceValid(_visualRoot)) _visualRoot.QueueFree();
        _visualRoot = null;
        _characterAnimator = null;
        _usesImportedModel = false;
        _proceduralImportedModel = false;
        _currentAnimation = "";
        _visualBaseScale = Vector3.One;
        BuildHeroVisual();
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
            if (_proceduralImportedModel)
            {
                _visualTime += delta;
                float proceduralStride = moving ? Mathf.Sin(_visualTime * 10f) : Mathf.Sin(_visualTime * 2f) * 0.15f;
                float castLean = _actionAnimationLeft > 0f ? Mathf.Sin((0.72f - Mathf.Min(0.72f, _actionAnimationLeft)) / 0.72f * Mathf.Pi) : 0f;
                _visualRoot.Position = new Vector3(0f, Mathf.Abs(proceduralStride) * (moving ? 0.06f : 0.018f), castLean * 0.2f);
                _visualRoot.Rotation = new Vector3(-castLean * 0.16f, 0f, proceduralStride * 0.035f);
                return;
            }
            if (!_isDead && _actionAnimationLeft <= 0f)
            {
                PlayCharacterAnimation(moving ? _runAnimation : _idleAnimation);
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
        Vector3 impulse = _cameraImpulseLeft > 0f
            ? new Vector3((float)GD.RandRange(-_cameraImpulseStrength, _cameraImpulseStrength), (float)GD.RandRange(-_cameraImpulseStrength * 0.35f, _cameraImpulseStrength * 0.35f), (float)GD.RandRange(-_cameraImpulseStrength, _cameraImpulseStrength))
            : Vector3.Zero;
        _camera.GlobalPosition = GlobalPosition + new Vector3(0f, 17.6f, 14f) + impulse;
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
        Health = Mathf.Min(MaxHealth, Health + MaxHealth * 0.22f);
        _flaskHealLeft = 2.5f;
        _flaskHealPerSecond = MaxHealth * 0.13f / _flaskHealLeft;
        ShowCombatMessage("LIFE FLASK — RESTORING 35% HEALTH");
        FlashBody(new Color(1f, 0.16f, 0.22f));
    }

    private void TryDodge()
    {
        if (_dodgeCooldownLeft > 0f || IsDodging || IsGameplayInputLocked)
        {
            if (_dodgeCooldownLeft > 0f) ShowCombatMessage($"DODGE RECHARGING {_dodgeCooldownLeft:0.0}s");
            return;
        }
        Vector3 direction = GetAimPoint() - GlobalPosition;
        direction.Y = 0f;
        if (direction.LengthSquared() < 0.1f)
        {
            direction = -GlobalTransform.Basis.Z;
        }
        _dodgeDirection = direction.Normalized();
        _dodgeTimeLeft = DodgeDuration;
        _dodgeCooldownLeft = DodgeCooldown;
        _invulnerableLeft = Mathf.Max(_invulnerableLeft, DodgeInvulnerability);
        _moveTarget = null;
        if (_usesImportedModel)
        {
            _actionAnimationLeft = DodgeDuration + 0.2f;
            PlayCharacterAnimation("HumanArmature|Roll_sword", true);
        }
        Node? scene = GetTree().CurrentScene;
        if (scene != null)
        {
            SkillVfx.SpawnDashTrail(scene, GlobalPosition, GlobalPosition + _dodgeDirection * 3.2f, DamageElement.Physical, false);
        }
        ShowCombatMessage("EVADE — INVULNERABLE");
    }

    public void TakeDamage(float amount, string source = "Enemy", DamageElement element = DamageElement.Physical, float poiseDamage = 20f)
    {
        if (_invulnerableLeft > 0f || Health <= 0f || float.IsNaN(amount) || float.IsInfinity(amount))
        {
            if (IsDodging) ShowCombatMessage("EVADED");
            return;
        }
        amount = Mathf.Max(0f, amount);
        float defenseMultiplier = element == DamageElement.Physical
            ? 1f - Mathf.Min(0.7f, Armor / (Armor + 250f))
            : 1f - GetResistance(element) / 100f;
        float buffMultiplier = _stoneSkinLeft > 0f ? 0.55f : (_frostArmorLeft > 0f ? 0.75f : 1f);
        float finalDamage = amount * defenseMultiplier * buffMultiplier;
        Health = Mathf.Max(0f, Health - finalDamage);
        _poise = Mathf.Max(0f, _poise - Mathf.Max(0f, poiseDamage));
        _poiseRegenDelay = 2f;
        string defense = element == DamageElement.Physical ? $"ARMOR {Armor:0}" : $"{element.ToString().ToUpperInvariant()} RES {GetResistance(element):0}%";
        ShowCombatMessage($"{source}: -{(int)finalDamage} HP  •  {defense}");
        TriggerHitFeedback(0.16f, false);
        FlashBody(new Color(1f, 0.08f, 0.08f));
        if (_poise <= 0f && Health > 0f)
        {
            _poise = MaxPoise;
            _stunLeft = Mathf.Max(_stunLeft, 0.62f);
            _moveTarget = null;
            ShowCombatMessage("POISE BROKEN — STAGGERED");
        }
        if (Health <= 0f)
        {
            BeginDeath();
        }
    }

    private float GetResistance(DamageElement element)
    {
        return element switch
        {
            DamageElement.Fire => Inventory.FireResistance,
            DamageElement.Cold => Inventory.ColdResistance,
            DamageElement.Lightning => Inventory.LightningResistance,
            DamageElement.Poison => Inventory.PoisonResistance,
            _ => 0f,
        };
    }

    public void TriggerHitFeedback(float strength, bool hitStop)
    {
        _cameraImpulseStrength = Mathf.Max(_cameraImpulseStrength, strength);
        _cameraImpulseLeft = Mathf.Max(_cameraImpulseLeft, hitStop ? 0.12f : 0.08f);
        if (!hitStop) return;
        int serial = ++_hitStopSerial;
        Engine.TimeScale = 0.16;
        SceneTreeTimer timer = GetTree().CreateTimer(0.045, true, false, true);
        timer.Timeout += () =>
        {
            if (serial == _hitStopSerial) Engine.TimeScale = 1.0;
        };
    }

    public void ActivateBuff(string skillId, float duration)
    {
        switch (skillId)
        {
            case "haste_aura":
            case "arcane_echo":
            case "war_cry":
            case "smoke_bomb": _hasteLeft = Mathf.Max(_hasteLeft, duration); break;
            case "stone_skin":
            case "consecrated_ground": _stoneSkinLeft = Mathf.Max(_stoneSkinLeft, duration); break;
            case "frost_armor": _frostArmorLeft = Mathf.Max(_frostArmorLeft, duration); break;
        }
        ShowCombatMessage($"{skillId.Replace('_', ' ').ToUpper()} {duration:0}s");
    }

    public void DashTo(Vector3 destination)
    {
        if (_usesImportedModel)
        {
            _actionAnimationLeft = 0.8f;
            PlayCharacterAnimation(Progression.HeroClass == HeroClass.Runeblade ? "HumanArmature|Roll_sword" : "CharacterArmature|Punch", true);
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
        int experienceLost = Progression.ApplyDeathPenalty();
        ShowCombatMessage(experienceLost > 0 ? $"FALLEN — LOST {experienceLost} XP" : "FALLEN — THE TOTEM CALLS YOU BACK");
        GetTree().CreateTimer(1.45f).Timeout += RespawnAtTotem;
    }

    private void RespawnAtTotem()
    {
        Health = MaxHealth;
        Mana = MaxMana;
        FlaskCharges = MaxFlaskCharges;
        _flaskHealLeft = 0f;
        _poise = MaxPoise;
        _dodgeTimeLeft = 0f;
        _dodgeCooldownLeft = 0f;
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
        string defenseState = $"  •  ARM {Armor:0}  •  POISE {_poise:0}/{MaxPoise:0}  •  CRIT {CriticalChance:0.#}%";
        string dodgeState = IsDodging ? "  •  DODGING" : _dodgeCooldownLeft > 0f ? $"  •  DODGE {_dodgeCooldownLeft:0.0}s" : "  •  DODGE READY";
        return (Caster?.GetSelectedName() ?? "-") + ritualState + birthState + debuffState + defenseState + dodgeState;
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
