using Godot;

namespace Sigilwoven;

public enum EnemyArchetype
{
    Raider,
    Brute,
    Hexer,
    Guardian,
}

public enum EliteModifier
{
    None,
    Frenzied,
    Bulwark,
    Stormbound,
}

public partial class Enemy : CharacterBody3D
{
    public float MaxHp = 60f;
    private float _hp;
    private float _stunTimer;
    private float _burnTimer;
    private float _burnDps;
    private float _burnTickTimer;
    private float _poisonTimer;
    private float _poisonDps;
    private float _poisonTickTimer;
    private float _chillTimer;
    private float _chillSlow;
    private MeshInstance3D? _burnFx;
    private MeshInstance3D? _poisonFx;
    private MeshInstance3D? _chillFx;
    private MeshInstance3D? _mesh;
    private StandardMaterial3D? _mat;
    private Node3D? _modelRoot;
    private AnimationPlayer? _modelAnimator;
    private string _modelAnimation = "";
    private Node3D? _encounterTarget;
    private bool _isTrainingDummy;
    private Label3D? _healthLabel;
    private float _attackCooldown;
    private float _rangedCooldown;
    private float _hitFlash;
    private float _animationTime;
    private float _attackAnimationLeft;
    private EnemyArchetype _archetype;
    private EliteModifier _eliteModifier;
    private Color _normalColor = Colors.White;
    private float _bodyBaseY = 1f;
    private const float Gravity = 20f;
    private const float AttackRange = 1.9f;
    private const float MaxAttackHeightDifference = 2f;
    private const float AttackAnimationDuration = 0.28f;
    private const float StatusTickInterval = 0.25f;

    public bool IsTrainingDummy => _isTrainingDummy;
    public bool IsElite => _eliteModifier != EliteModifier.None;
    public event System.Action<Enemy>? Died;
    public event System.Action<Enemy>? Aggroed;

    public void Configure(float maxHp, bool isTrainingDummy = false, EnemyArchetype archetype = EnemyArchetype.Raider)
    {
        _isTrainingDummy = isTrainingDummy;
        _archetype = archetype;
        float healthMultiplier = archetype switch
        {
            EnemyArchetype.Brute => 1.6f,
            EnemyArchetype.Hexer => 0.82f,
            EnemyArchetype.Guardian => 1f,
            _ => 1f,
        };
        MaxHp = isTrainingDummy ? maxHp : maxHp * healthMultiplier;
    }

    public void ConfigureElite(EliteModifier modifier)
    {
        _eliteModifier = modifier;
        if (modifier != EliteModifier.None)
        {
            MaxHp *= modifier == EliteModifier.Bulwark ? 3.2f : 2.25f;
        }
    }

    public static EliteModifier RollEliteModifier()
    {
        return (EliteModifier)GD.RandRange(1, 3);
    }

    public void JoinAmbush(Node3D target)
    {
        _encounterTarget = target;
    }

    private void AggroOnHit()
    {
        if (_isTrainingDummy || _encounterTarget != null)
        {
            return;
        }
        foreach (Node node in GetTree().GetNodesInGroup("player"))
        {
            if (node is PlayerController player && IsInstanceValid(player) && player.Health > 0f)
            {
                _encounterTarget = player;
                Aggroed?.Invoke(this);
                return;
            }
        }
    }

    public override void _Ready()
    {
        CollisionLayer = PhysicsLayers.Enemy;
        CollisionMask = PhysicsLayers.World | PhysicsLayers.Player | PhysicsLayers.Ally;
        AddToGroup("enemies");
        _hp = MaxHp;

        _normalColor = _isTrainingDummy
            ? new Color(0.62f, 0.48f, 0.25f)
            : IsElite ? _eliteModifier switch
            {
                EliteModifier.Frenzied => new Color(1f, 0.22f, 0.12f),
                EliteModifier.Bulwark => new Color(0.82f, 0.68f, 0.16f),
                _ => new Color(0.3f, 0.55f, 1f),
            }
            : _archetype switch
            {
                EnemyArchetype.Brute => new Color(0.68f, 0.42f, 0.36f),
                EnemyArchetype.Hexer => new Color(0.48f, 0.58f, 1f),
                EnemyArchetype.Guardian => new Color(0.9f, 0.62f, 0.18f),
                _ => Colors.White,
            };
        _mat = new StandardMaterial3D
        {
            AlbedoColor = _normalColor,
            AlbedoTexture = _isTrainingDummy ? null : GD.Load<Texture2D>("res://assets/textures/raider_armor.png"),
            Roughness = 0.78f,
            Metallic = _isTrainingDummy ? 0f : 0.24f,
            EmissionEnabled = true,
            Emission = _isTrainingDummy ? new Color(0.2f, 0.12f, 0.03f) : new Color(0.32f, 0.018f, 0.008f),
            EmissionEnergyMultiplier = 0.7f,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic,
        };
        Vector3 bodySize = _archetype switch
        {
            EnemyArchetype.Brute => new Vector3(1.28f, 2.35f, 1.18f),
            EnemyArchetype.Hexer => new Vector3(0.86f, 1.82f, 0.86f),
            EnemyArchetype.Guardian => new Vector3(1.75f, 3.15f, 1.65f),
            _ => new Vector3(1f, 2f, 1f),
        };
        _bodyBaseY = bodySize.Y * 0.5f;
        var box = new BoxMesh { Size = bodySize };
        _mesh = new MeshInstance3D { Mesh = box };
        _mesh.SetSurfaceOverrideMaterial(0, _mat);
        _mesh.Position = new Vector3(0, _bodyBaseY, 0);
        AddChild(_mesh);

        if (!_isTrainingDummy && !TryBuildImportedEnemy())
        {
            AddRaiderDetails();
        }
        if (!_isTrainingDummy && IsElite) AddEliteEffect();

        var col = new CollisionShape3D { Shape = new BoxShape3D { Size = bodySize } };
        col.Position = new Vector3(0, _bodyBaseY, 0);
        AddChild(col);

        if (_isTrainingDummy)
        {
            var title = new Label3D { Text = "TRAINING DUMMY", FontSize = 42, OutlineSize = 8, Modulate = new Color(1f, 0.85f, 0.4f) };
            title.Position = new Vector3(0f, 2.55f, 0f);
            AddChild(title);
        }
        else
        {
            _healthLabel = new Label3D { FontSize = 34, OutlineSize = 6, Modulate = _archetype == EnemyArchetype.Hexer ? new Color(0.65f, 0.75f, 1f) : new Color(1f, 0.75f, 0.75f), Position = new Vector3(0f, bodySize.Y + 0.35f, 0f) };
            AddChild(_healthLabel);
            UpdateHealthLabel();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float step = (float)delta;
        _animationTime += step;
        Vector3 velocity = Velocity;
        velocity.X = 0f;
        velocity.Z = 0f;

        if (_encounterTarget != null && IsInstanceValid(_encounterTarget))
        {
            Vector3 direction = _encounterTarget.GlobalPosition - GlobalPosition;
            direction.Y = 0f;
            float distance = direction.Length();
            if (!IsStunned())
            {
                float moveMultiplier = _chillTimer > 0f ? 1f - _chillSlow : 1f;
                Vector3 moveDirection = Vector3.Zero;
                if (_archetype == EnemyArchetype.Hexer && distance > AttackRange && distance < 4f)
                {
                    moveDirection = -direction.Normalized();
                }
                else if (distance > (_archetype == EnemyArchetype.Hexer ? 7f : AttackRange))
                {
                    moveDirection = direction.Normalized();
                }
                Vector3 movement = moveDirection * GetMoveSpeed() * moveMultiplier;
                velocity.X = movement.X;
                velocity.Z = movement.Z;
                if (direction.LengthSquared() > 0.01f)
                {
                    LookAt(GlobalPosition + direction, Vector3.Up, true);
                }

                if (_encounterTarget is PlayerController player)
                {
                    UpdateRangedAttack(player, distance, step);
                    UpdateMeleeAttack(player, distance, step);
                }
            }
        }

        if (!IsOnFloor())
        {
            velocity.Y -= Gravity * step;
        }
        else if (velocity.Y < 0f)
        {
            velocity.Y = 0f;
        }
        Velocity = velocity;
        MoveAndSlide();
        AnimateBody(step, new Vector2(velocity.X, velocity.Z).Length() > 0.05f);

        if (_stunTimer > 0f)
        {
            _stunTimer -= step;
        }
        if (_burnTimer > 0f)
        {
            _burnTimer -= step;
            _burnTickTimer -= step;
            if (_burnTickTimer <= 0f)
            {
                _burnTickTimer += StatusTickInterval;
                TakeRawDamage(_burnDps * StatusTickInterval, true);
            }
            if (_burnTimer <= 0f)
            {
                _burnTimer = 0f;
                _burnDps = 0f;
                _burnTickTimer = 0f;
                ClearStatusFx(ref _burnFx);
            }
        }
        if (_poisonTimer > 0f)
        {
            _poisonTimer -= step;
            _poisonTickTimer -= step;
            if (_poisonTickTimer <= 0f)
            {
                _poisonTickTimer += StatusTickInterval;
                TakeRawDamage(_poisonDps * StatusTickInterval, false, new Color(0.25f, 1f, 0.3f));
            }
            if (_poisonTimer <= 0f)
            {
                _poisonTimer = 0f;
                _poisonDps = 0f;
                _poisonTickTimer = 0f;
                ClearStatusFx(ref _poisonFx);
            }
        }
        if (_chillTimer > 0f)
        {
            _chillTimer -= step;
            if (_chillTimer <= 0f)
            {
                _chillTimer = 0f;
                _chillSlow = 0f;
                ClearStatusFx(ref _chillFx);
            }
        }
        AnimateStatusFx(_burnFx, step, 2.8f);
        AnimateStatusFx(_poisonFx, step, -1.9f);
        AnimateStatusFx(_chillFx, step, 1.25f);
        _hitFlash = Mathf.Max(0f, _hitFlash - step);
    }

    private float GetMoveSpeed()
    {
        float speed = _archetype switch
        {
            EnemyArchetype.Brute => 1.18f,
            EnemyArchetype.Hexer => 1.52f,
            EnemyArchetype.Guardian => 1.05f,
            _ => 1.7f,
        };
        return speed * (_eliteModifier == EliteModifier.Frenzied ? 1.42f : 1f);
    }

    private bool TryBuildImportedEnemy()
    {
        string file = _archetype switch
        {
            EnemyArchetype.Brute => "Viking_Male.fbx",
            EnemyArchetype.Hexer => "Wizard.fbx",
            EnemyArchetype.Guardian => "Knight_Golden_Male.fbx",
            _ => "Ninja_Male.fbx",
        };
        PackedScene? scene = GD.Load<PackedScene>($"res://assets/models/quaternius_enemies/{file}");
        if (scene?.Instantiate() is not Node3D model)
        {
            return false;
        }
        _modelRoot = new Node3D
        {
            Scale = Vector3.One * (_archetype switch
            {
                EnemyArchetype.Brute => 0.8f,
                EnemyArchetype.Hexer => 0.62f,
                EnemyArchetype.Guardian => 1f,
                _ => 0.66f,
            }),
        };
        AddChild(_modelRoot);
        _modelRoot.AddChild(model);
        _modelAnimator = model.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        if (_modelAnimator == null)
        {
            _modelRoot.QueueFree();
            _modelRoot = null;
            return false;
        }
        if (_modelAnimator.GetAnimation("CharacterArmature|Idle") is Animation idle) idle.LoopMode = Animation.LoopModeEnum.Linear;
        if (_modelAnimator.GetAnimation("CharacterArmature|Walk") is Animation walk) walk.LoopMode = Animation.LoopModeEnum.Linear;
        if (_mesh != null) _mesh.Visible = false;
        PlayModelAnimation("CharacterArmature|Idle", true);
        return true;
    }

    private void PlayModelAnimation(string animationName, bool force = false)
    {
        if (_modelAnimator == null || (!force && _modelAnimation == animationName)) return;
        _modelAnimation = animationName;
        _modelAnimator.Play(animationName, 0.1f);
    }

    private void UpdateRangedAttack(PlayerController target, float distance, float step)
    {
        if ((_archetype == EnemyArchetype.Brute && _eliteModifier != EliteModifier.Stormbound) || distance < 3f || distance > 11f)
        {
            return;
        }
        _rangedCooldown -= step;
        if (_rangedCooldown > 0f)
        {
            return;
        }
        _rangedCooldown = _eliteModifier == EliteModifier.Stormbound ? 0.85f + GD.Randf() * 0.2f : _archetype switch
        {
            EnemyArchetype.Hexer => 1.45f + GD.Randf() * 0.35f,
            EnemyArchetype.Guardian => 1.8f + GD.Randf() * 0.3f,
            _ => 2.4f + GD.Randf() * 0.8f,
        };
        FireShadowBolt(target);
    }

    private void UpdateMeleeAttack(PlayerController target, float distance, float step)
    {
        if (distance > AttackRange || Mathf.Abs(target.GlobalPosition.Y - GlobalPosition.Y) > MaxAttackHeightDifference)
        {
            return;
        }
        _attackCooldown -= step;
        if (_attackCooldown > 0f)
        {
            return;
        }
        _attackCooldown = (_archetype == EnemyArchetype.Brute ? 1.35f : 1.05f) * (_eliteModifier == EliteModifier.Frenzied ? 0.62f : 1f);
        _attackAnimationLeft = AttackAnimationDuration;
        if (_modelAnimator != null)
        {
            _attackAnimationLeft = 0.62f;
            PlayModelAnimation("CharacterArmature|Punch", true);
        }
        float damage = _archetype switch
        {
            EnemyArchetype.Brute => 22f,
            EnemyArchetype.Hexer => 7f,
            EnemyArchetype.Guardian => 34f,
            _ => 12f,
        };
        if (IsElite) damage *= _eliteModifier == EliteModifier.Frenzied ? 1.5f : 1.3f;
        target.TakeDamage(damage, _archetype.ToString().ToUpperInvariant());
        SpawnMeleeImpact(target.GlobalPosition);
        FlashHit(new Color(1f, 0.85f, 0.25f));
    }

    private void AddRaiderDetails()
    {
        if (_mesh == null)
        {
            return;
        }

        Color glow = _archetype switch
        {
            EnemyArchetype.Hexer => new Color(0.2f, 0.55f, 1f),
            EnemyArchetype.Guardian => new Color(1f, 0.62f, 0.05f),
            _ => new Color(1f, 0.18f, 0.03f),
        };
        var emberMaterial = new StandardMaterial3D
        {
            AlbedoColor = glow,
            EmissionEnabled = true,
            Emission = glow,
            EmissionEnergyMultiplier = 4f,
        };
        var visor = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(0.52f, 0.09f, 0.05f) },
            Position = new Vector3(0f, 0.28f, 0.52f),
        };
        visor.SetSurfaceOverrideMaterial(0, emberMaterial);
        _mesh.AddChild(visor);

        var bladeMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.16f, 0.17f, 0.2f),
            Metallic = 0.85f,
            Roughness = 0.28f,
        };
        var blade = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = _archetype switch { EnemyArchetype.Guardian => new Vector3(0.28f, 1.9f, 0.36f), EnemyArchetype.Brute => new Vector3(0.2f, 1.45f, 0.28f), _ => new Vector3(0.12f, 1.05f, 0.18f) } },
            Position = new Vector3(0.72f, -0.05f, -0.12f),
            Rotation = new Vector3(0f, 0f, -0.35f),
        };
        blade.SetSurfaceOverrideMaterial(0, bladeMaterial);
        _mesh.AddChild(blade);

        // Layered low-poly silhouette: helmet, pauldrons, limbs and weapon turn
        // the old collision box into a readable armored combatant.
        var clothMaterial = new StandardMaterial3D
        {
            AlbedoColor = _archetype == EnemyArchetype.Hexer ? new Color(0.08f, 0.1f, 0.24f) : new Color(0.16f, 0.035f, 0.03f),
            Roughness = 0.95f,
        };
        var head = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = _archetype == EnemyArchetype.Guardian ? 0.33f : 0.24f, Height = _archetype == EnemyArchetype.Guardian ? 0.66f : 0.48f },
            Position = new Vector3(0f, _bodyBaseY + 0.18f, 0f),
        };
        head.SetSurfaceOverrideMaterial(0, bladeMaterial);
        _mesh.AddChild(head);
        var helmet = new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.17f, BottomRadius = 0.29f, Height = 0.24f },
            Position = new Vector3(0f, _bodyBaseY + 0.42f, 0f),
        };
        helmet.SetSurfaceOverrideMaterial(0, _mat);
        _mesh.AddChild(helmet);
        float shoulderX = _archetype == EnemyArchetype.Guardian ? 1.05f : _archetype == EnemyArchetype.Brute ? 0.78f : 0.62f;
        foreach (float side in new[] { -1f, 1f })
        {
            var shoulder = new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.25f, Height = 0.35f }, Position = new Vector3(side * shoulderX, _bodyBaseY * 0.52f, 0f), Scale = new Vector3(1.35f, 0.72f, 1f) };
            shoulder.SetSurfaceOverrideMaterial(0, _mat);
            _mesh.AddChild(shoulder);
            var arm = new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.1f, BottomRadius = 0.14f, Height = _bodyBaseY * 0.85f }, Position = new Vector3(side * shoulderX, 0f, 0f), Rotation = new Vector3(0f, 0f, side * -0.14f) };
            arm.SetSurfaceOverrideMaterial(0, clothMaterial);
            _mesh.AddChild(arm);
            var leg = new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.15f, BottomRadius = 0.19f, Height = _bodyBaseY }, Position = new Vector3(side * 0.25f, -_bodyBaseY * 0.78f, 0f) };
            leg.SetSurfaceOverrideMaterial(0, clothMaterial);
            _mesh.AddChild(leg);
        }
        var loincloth = new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.5f, 0.65f, 0.08f) }, Position = new Vector3(0f, -_bodyBaseY * 0.46f, 0.54f) };
        loincloth.SetSurfaceOverrideMaterial(0, clothMaterial);
        _mesh.AddChild(loincloth);

        if (_archetype == EnemyArchetype.Hexer)
        {
            var halo = new MeshInstance3D
            {
                Mesh = new TorusMesh { InnerRadius = 0.5f, OuterRadius = 0.58f },
                Position = new Vector3(0f, 0.72f, 0f),
            };
            halo.SetSurfaceOverrideMaterial(0, emberMaterial);
            _mesh.AddChild(halo);
        }
        else if (_archetype == EnemyArchetype.Guardian)
        {
            var crown = new MeshInstance3D
            {
                Mesh = new TorusMesh { InnerRadius = 0.68f, OuterRadius = 0.79f },
                Position = new Vector3(0f, 1.05f, 0f),
                Rotation = new Vector3(Mathf.Pi * 0.5f, 0f, 0f),
            };
            crown.SetSurfaceOverrideMaterial(0, emberMaterial);
            _mesh.AddChild(crown);
        }
    }

    private void AddEliteEffect()
    {
        Color color = _normalColor;
        var material = new StandardMaterial3D
        {
            AlbedoColor = color,
            EmissionEnabled = true,
            Emission = color,
            EmissionEnergyMultiplier = 4.5f,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        };
        var halo = new MeshInstance3D
        {
            Mesh = new TorusMesh { InnerRadius = 0.85f, OuterRadius = 0.98f },
            Position = new Vector3(0f, 0.14f, 0f),
        };
        halo.SetSurfaceOverrideMaterial(0, material);
        AddChild(halo);
        var light = new OmniLight3D { LightColor = color, LightEnergy = 1.5f, OmniRange = 4.5f, Position = new Vector3(0f, 1.3f, 0f) };
        AddChild(light);
    }

    private void AnimateBody(float step, bool moving)
    {
        if (_mesh == null || _mat == null)
        {
            return;
        }

        if (_modelAnimator != null)
        {
            _attackAnimationLeft = Mathf.Max(0f, _attackAnimationLeft - step);
            if (_attackAnimationLeft <= 0f)
            {
                PlayModelAnimation(moving ? "CharacterArmature|Walk" : "CharacterArmature|Idle");
            }
            return;
        }

        float walkWeight = moving && !IsStunned() ? 1f : 0f;
        float stride = Mathf.Sin(_animationTime * 9f);
        float bob = Mathf.Abs(stride) * 0.075f * walkWeight;
        float sway = stride * 0.055f * walkWeight;
        float attackPunch = 0f;
        if (_attackAnimationLeft > 0f)
        {
            _attackAnimationLeft = Mathf.Max(0f, _attackAnimationLeft - step);
            float progress = 1f - _attackAnimationLeft / AttackAnimationDuration;
            attackPunch = Mathf.Sin(progress * Mathf.Pi);
        }

        _mesh.Position = new Vector3(0f, _bodyBaseY + bob, attackPunch * 0.22f);
        _mesh.Rotation = new Vector3(attackPunch * -0.18f, 0f, sway);
        _mesh.Scale = new Vector3(1f + attackPunch * 0.12f, 1f - attackPunch * 0.08f, 1f + attackPunch * 0.18f);
        if (_hitFlash <= 0f)
        {
            _mat.EmissionEnergyMultiplier = _isTrainingDummy
                ? 0.7f
                : 0.62f + Mathf.Sin(_animationTime * 3.5f) * 0.14f;
        }
    }

    private void SpawnMeleeImpact(Vector3 position)
    {
        var scene = GetTree().CurrentScene;
        if (scene != null)
        {
            SkillVfx.SpawnMeleeHit(scene, position + new Vector3(0f, 0.75f, 0f));
        }
    }

    public bool IsStunned()
    {
        return _stunTimer > 0f;
    }

    public void ApplyStun(float duration)
    {
        _stunTimer = Mathf.Max(_stunTimer, duration);
    }

    public void ApplyBurn(float dps, float duration)
    {
        if (_burnTimer <= 0f)
        {
            _burnTickTimer = StatusTickInterval;
        }
        _burnDps = Mathf.Max(_burnDps, dps);
        _burnTimer = Mathf.Max(_burnTimer, duration);
        _burnFx ??= CreateStatusFx(DamageElement.Fire, 0.18f, false);
    }

    public void ApplyPoison(float dps, float duration)
    {
        if (_poisonTimer <= 0f)
        {
            _poisonTickTimer = StatusTickInterval;
        }
        _poisonDps = Mathf.Max(_poisonDps, dps);
        _poisonTimer = Mathf.Max(_poisonTimer, duration);
        _poisonFx ??= CreateStatusFx(DamageElement.Poison, _bodyBaseY, true);
    }

    public void ApplyChill(float slow, float duration)
    {
        _chillSlow = Mathf.Max(_chillSlow, slow);
        _chillTimer = Mathf.Max(_chillTimer, duration);
        _chillFx ??= CreateStatusFx(DamageElement.Cold, 0.42f, false);
        FlashHit(new Color(0.25f, 0.82f, 1f));
    }

    private MeshInstance3D CreateStatusFx(DamageElement element, float height, bool cloud)
    {
        Color color = SkillVfx.ElementColor(element);
        var material = new StandardMaterial3D
        {
            AlbedoColor = new Color(color.R, color.G, color.B, cloud ? 0.18f : 0.72f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            EmissionEnabled = true,
            Emission = color,
            EmissionEnergyMultiplier = 2.6f,
        };
        PrimitiveMesh shape = cloud
            ? new SphereMesh { Radius = 0.78f, Height = 1.55f }
            : new TorusMesh { InnerRadius = 0.62f, OuterRadius = 0.78f };
        var effect = new MeshInstance3D { Mesh = shape, Position = new Vector3(0f, height, 0f) };
        effect.SetSurfaceOverrideMaterial(0, material);
        AddChild(effect);
        return effect;
    }

    private static void AnimateStatusFx(MeshInstance3D? effect, float step, float speed)
    {
        if (effect == null || !IsInstanceValid(effect)) return;
        effect.RotateY(step * speed);
        float pulse = 0.94f + Mathf.Sin(Time.GetTicksMsec() * 0.008f + speed) * 0.08f;
        effect.Scale = Vector3.One * pulse;
    }

    private static void ClearStatusFx(ref MeshInstance3D? effect)
    {
        if (effect != null && IsInstanceValid(effect)) effect.QueueFree();
        effect = null;
    }

    public void TakeDamage(float amount, DamageElement element, ResolvedCast? resolved = null)
    {
        if (_hp <= 0f || IsQueuedForDeletion() || float.IsNaN(amount) || float.IsInfinity(amount))
        {
            return;
        }
        AggroOnHit();
        amount = Mathf.Max(0f, amount);
        if (_eliteModifier == EliteModifier.Bulwark) amount *= 0.72f;
        if (resolved != null && resolved.ExecuteThreshold > 0f && _hp <= MaxHp * resolved.ExecuteThreshold)
        {
            amount *= resolved.ExecuteMultiplier;
        }
        TakeRawDamage(amount, false);
        if (resolved != null)
        {
            if (resolved.HasStun && GD.Randf() < resolved.StunChance)
            {
                ApplyStun(resolved.StunDuration);
            }
            if (resolved.HasBurn)
            {
                ApplyBurn(resolved.BurnDps, resolved.BurnDuration);
            }
            if (resolved.HasPoison)
            {
                ApplyPoison(resolved.PoisonDps, resolved.PoisonDuration);
            }
            if (resolved.HasChill)
            {
                ApplyChill(resolved.ChillSlow, resolved.ChillDuration);
            }
        }
    }

    private void TakeRawDamage(float amount, bool isBurn, Color? numberColor = null)
    {
        if (_hp <= 0f)
        {
            return;
        }
        if (!_isTrainingDummy)
        {
            _hp -= amount;
        }
        SpawnDamageNumber(amount, isBurn, numberColor);
        FlashHit(numberColor ?? (isBurn ? new Color(1f, 0.28f, 0.05f) : new Color(1f, 1f, 1f)));
        if (!isBurn && _modelAnimator != null && _hp > 0f)
        {
            _attackAnimationLeft = 0.38f;
            PlayModelAnimation("CharacterArmature|RecieveHit", true);
        }
        if (_isTrainingDummy)
        {
            return;
        }
        if (_hp <= 0f)
        {
            Died?.Invoke(this);
            AwardExperience();
            DropAether();
            DropLoot();
            if (_modelAnimator != null)
            {
                CollisionLayer = 0;
                CollisionMask = 0;
                SetPhysicsProcess(false);
                if (_healthLabel != null) _healthLabel.Visible = false;
                PlayModelAnimation("CharacterArmature|Defeat", true);
                GetTree().CreateTimer(1.6f).Timeout += QueueFree;
            }
            else
            {
                QueueFree();
            }
        }
        else
        {
            UpdateHealthLabel();
        }
    }

    private void AwardExperience()
    {
        if (_encounterTarget is not PlayerController player || !IsInstanceValid(player))
        {
            return;
        }
        int experience = _archetype switch
        {
            EnemyArchetype.Brute => 48,
            EnemyArchetype.Hexer => 38,
            EnemyArchetype.Guardian => 600,
            _ => 25,
        };
        if (IsElite) experience *= 3;
        player.Progression.GainExperience(experience);
        int flaskCharges = _archetype switch
        {
            EnemyArchetype.Brute => 5,
            EnemyArchetype.Hexer => 4,
            EnemyArchetype.Guardian => PlayerController.MaxFlaskCharges,
            _ => 3,
        };
        player.RechargeFlask(IsElite ? Mathf.Max(flaskCharges, 8) : flaskCharges);
    }

    private void SpawnDamageNumber(float amount, bool isBurn, Color? color = null)
    {
        var label = new Label3D();
        label.Text = ((int)amount).ToString();
        label.FontSize = 48;
        label.Modulate = color ?? (isBurn ? new Color(1f, 0.5f, 0.1f) : Colors.White);
        label.OutlineSize = 8;
        label.Position = new Vector3(GD.Randf() * 0.6f - 0.3f, _bodyBaseY * 2f + 0.4f, 0);
        AddChild(label);
        var tween = CreateTween();
        tween.TweenProperty(label, "position:y", 3.2f, 0.7);
        tween.TweenCallback(Callable.From(label.QueueFree));
    }

    private void UpdateHealthLabel()
    {
        if (_healthLabel != null)
        {
            string elite = IsElite ? $"ELITE {_eliteModifier.ToString().ToUpperInvariant()}  •  " : "";
            _healthLabel.Text = $"{elite}{_archetype.ToString().ToUpperInvariant()}  {Mathf.Max(0, Mathf.CeilToInt(_hp))} / {Mathf.CeilToInt(MaxHp)}";
            if (IsElite) _healthLabel.Modulate = _normalColor;
        }
    }

    private void FlashHit(Color color)
    {
        if (_mat == null)
        {
            return;
        }
        _mat.AlbedoColor = color;
        var tween = CreateTween();
        tween.TweenProperty(_mat, "albedo_color", _normalColor, 0.12f);
        _hitFlash = 0.12f;
    }

    private void DropAether()
    {
        var scene = GetTree().CurrentScene;
        if (scene == null)
        {
            return;
        }
        var shard = new AetherShard();
        scene.AddChild(shard);
        shard.GlobalPosition = GlobalPosition + new Vector3(0f, 0.35f, 0f);
    }

    private void DropLoot()
    {
        var scene = GetTree().CurrentScene;
        if (scene == null)
        {
            return;
        }
        float chance = IsElite ? 1f : _archetype switch
        {
            EnemyArchetype.Brute => 0.48f,
            EnemyArchetype.Hexer => 0.4f,
            EnemyArchetype.Guardian => 1f,
            _ => 0.28f,
        };
        if (GD.Randf() > chance)
        {
            return;
        }
        int level = _encounterTarget is PlayerController player ? player.Progression.Level : 1;
        float rarityBonus = (IsElite ? 0.24f : 0f) + _archetype switch
        {
            EnemyArchetype.Brute => 0.08f,
            EnemyArchetype.Hexer => 0.06f,
            EnemyArchetype.Guardian => 0.35f,
            _ => 0f,
        };
        int drops = _archetype == EnemyArchetype.Guardian ? 3 : IsElite ? 2 : 1;
        for (int i = 0; i < drops; i++)
        {
            Vector3 scatter = new((i - (drops - 1) * 0.5f) * 0.9f, 0.12f, 0f);
            LootDrop.Spawn(scene, GlobalPosition + scatter, ItemGenerator.Generate(level, rarityBonus));
        }
    }

    private void FireShadowBolt(PlayerController target)
    {
        var scene = GetTree().CurrentScene;
        if (scene == null)
        {
            return;
        }
        var bolt = new RaiderBolt();
        float damage = _archetype switch
        {
            EnemyArchetype.Hexer => 14f,
            EnemyArchetype.Guardian => 22f,
            _ => 9f,
        };
        if (IsElite) damage *= _eliteModifier == EliteModifier.Stormbound ? 1.45f : 1.25f;
        bolt.Configure(target, damage);
        if (_modelAnimator != null)
        {
            _attackAnimationLeft = 0.5f;
            PlayModelAnimation("CharacterArmature|Shoot_OneHanded", true);
        }
        scene.AddChild(bolt);
        bolt.GlobalPosition = GlobalPosition + new Vector3(0f, 1.1f, 0f);
        FlashHit(new Color(0.8f, 0.2f, 1f));
    }
}
