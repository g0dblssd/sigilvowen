using Godot;

namespace Sigilwoven;

public enum EnemyArchetype
{
    Raider,
    Brute,
    Hexer,
    Guardian,
    Berserker,
    Shieldbearer,
    Leaper,
    Necromancer,
    Ghoul,
    SkeletonArcher,
    Vampire,
}

public enum EliteModifier
{
    None,
    Frenzied,
    Bulwark,
    Stormbound,
    Molten,
    Warden,
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
    private float _meleeWindup;
    private float _rangedWindup;
    private float _queuedMeleeDamage;
    private PlayerController? _queuedMeleeTarget;
    private PlayerController? _queuedRangedTarget;
    private float _hitFlash;
    private float _animationTime;
    private float _attackAnimationLeft;
    private float _poiseMax = 55f;
    private float _poise = 55f;
    private float _poiseRegenDelay;
    private float _shieldBlockCooldown;
    private NavigationAgent3D? _navigationAgent;
    private float _awarenessTimer;
    private float _allyAlertCooldown;
    private float _leapCooldown;
    private float _leapWindup;
    private float _leapTravel;
    private Vector3 _leapOrigin;
    private Vector3 _leapDestination;
    private float _ritualCooldown;
    private float _ritualWindup;
    private FallenRemnant? _ritualRemnant;
    private EnemyArchetype _archetype;
    private EliteModifier _eliteModifier;
    private Color _normalColor = Colors.White;
    private float _difficultyDamageMultiplier = 1f;
    private float _difficultyRarityBonus;
    private float _difficultyExperienceMultiplier = 1f;
    private float _bodyBaseY = 1f;
    private const float Gravity = 20f;
    private const float AttackRange = 1.9f;
    private const float MaxAttackHeightDifference = 2f;
    private const float AttackAnimationDuration = 0.28f;
    private const float StatusTickInterval = 0.25f;

    public bool IsTrainingDummy => _isTrainingDummy;
    public bool IsElite => _eliteModifier != EliteModifier.None;
    public float Health => Mathf.Max(0f, _hp);
    public EnemyArchetype Archetype => _archetype;
    public string DisplayTitle => IsElite ? $"ELITE {_eliteModifier.ToString().ToUpperInvariant()} {_archetype.ToString().ToUpperInvariant()}" : _archetype.ToString().ToUpperInvariant();
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
            EnemyArchetype.Berserker => 1.25f,
            EnemyArchetype.Shieldbearer => 1.85f,
            EnemyArchetype.Leaper => 1.05f,
            EnemyArchetype.Necromancer => 1.18f,
            EnemyArchetype.Ghoul => 0.9f,
            EnemyArchetype.SkeletonArcher => 0.72f,
            EnemyArchetype.Vampire => 1.45f,
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

    public void ApplyDifficulty(float healthMultiplier, float damageMultiplier, float rarityBonus, float experienceMultiplier = 1f)
    {
        MaxHp *= Mathf.Max(0.1f, healthMultiplier);
        _difficultyDamageMultiplier = Mathf.Max(0.1f, damageMultiplier);
        _difficultyRarityBonus = Mathf.Max(0f, rarityBonus);
        _difficultyExperienceMultiplier = Mathf.Max(0.1f, experienceMultiplier);
    }

    public static EliteModifier RollEliteModifier()
    {
        return (EliteModifier)GD.RandRange(1, 5);
    }

    public void JoinAmbush(Node3D target)
    {
        AcquireTarget(target, "AMBUSH");
    }

    public void AlertFromAlly(Node3D target)
    {
        if (_isTrainingDummy || _encounterTarget != null || GlobalPosition.DistanceTo(target.GlobalPosition) > 13f)
        {
            return;
        }
        AcquireTarget(target, "ALLY");
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
                AcquireTarget(player, "DAMAGE");
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
        _poiseMax = _archetype switch
        {
            EnemyArchetype.Brute => 115f,
            EnemyArchetype.Guardian => 260f,
            EnemyArchetype.Hexer => 42f,
            _ => 62f,
        } * (IsElite ? 1.65f : 1f);
        _poise = _poiseMax;

        if (!_isTrainingDummy)
        {
            _navigationAgent = new NavigationAgent3D
            {
                PathDesiredDistance = 0.45f,
                TargetDesiredDistance = AttackRange * 0.8f,
                Radius = _archetype == EnemyArchetype.Guardian ? 0.9f : 0.48f,
                Height = _archetype == EnemyArchetype.Guardian ? 3.1f : 2f,
                AvoidanceEnabled = false,
                NeighborDistance = 4.5f,
                MaxNeighbors = 10,
            };
            AddChild(_navigationAgent);
        }

        _normalColor = _isTrainingDummy
            ? new Color(0.62f, 0.48f, 0.25f)
            : IsElite ? _eliteModifier switch
            {
                EliteModifier.Frenzied => new Color(1f, 0.22f, 0.12f),
                EliteModifier.Bulwark => new Color(0.82f, 0.68f, 0.16f),
                EliteModifier.Molten => new Color(1f, 0.19f, 0.025f),
                EliteModifier.Warden => new Color(0.12f, 0.95f, 0.58f),
                _ => new Color(0.3f, 0.55f, 1f),
            }
            : _archetype switch
            {
                EnemyArchetype.Brute => new Color(0.68f, 0.42f, 0.36f),
                EnemyArchetype.Hexer => new Color(0.48f, 0.58f, 1f),
                EnemyArchetype.Guardian => new Color(0.9f, 0.62f, 0.18f),
                EnemyArchetype.Berserker => new Color(0.92f, 0.16f, 0.08f),
                EnemyArchetype.Shieldbearer => new Color(0.3f, 0.48f, 0.72f),
                EnemyArchetype.Leaper => new Color(1f, 0.42f, 0.08f),
                EnemyArchetype.Necromancer => new Color(0.42f, 0.95f, 0.48f),
                EnemyArchetype.Ghoul => new Color(0.48f, 0.72f, 0.34f),
                EnemyArchetype.SkeletonArcher => new Color(0.78f, 0.78f, 0.66f),
                EnemyArchetype.Vampire => new Color(0.78f, 0.08f, 0.16f),
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
            EnemyArchetype.Berserker => new Vector3(1.18f, 2.2f, 1.08f),
            EnemyArchetype.Shieldbearer => new Vector3(1.18f, 2.25f, 1.08f),
            EnemyArchetype.Leaper => new Vector3(0.92f, 1.78f, 0.88f),
            EnemyArchetype.Necromancer => new Vector3(0.9f, 1.92f, 0.9f),
            EnemyArchetype.Ghoul => new Vector3(0.95f, 1.75f, 0.9f),
            EnemyArchetype.SkeletonArcher => new Vector3(0.82f, 1.85f, 0.82f),
            EnemyArchetype.Vampire => new Vector3(1.02f, 2.05f, 0.95f),
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
        if (!_isTrainingDummy) AddArchetypeReadability();
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
        UpdateAwareness(step);
        UpdateSpecialAbilities(step);
        Vector3 velocity = Velocity;
        velocity.X = 0f;
        velocity.Z = 0f;
        UpdateAttackWindups(step);

        if (_encounterTarget != null && IsInstanceValid(_encounterTarget))
        {
            Vector3 direction = _encounterTarget.GlobalPosition - GlobalPosition;
            direction.Y = 0f;
            float distance = direction.Length();
            if (!IsStunned() && _leapWindup <= 0f && _leapTravel <= 0f && _ritualWindup <= 0f)
            {
                float moveMultiplier = _chillTimer > 0f ? 1f - _chillSlow : 1f;
                Vector3 moveDirection = Vector3.Zero;
                if (_archetype == EnemyArchetype.Hexer && distance > AttackRange && distance < 4f)
                {
                    moveDirection = -GetNavigationDirection(direction);
                }
                else if (distance > (_archetype == EnemyArchetype.Hexer ? 7f : AttackRange))
                {
                    moveDirection = GetNavigationDirection(direction);
                }
                Vector3 movement = moveDirection * GetMoveSpeed() * moveMultiplier;
                if (_meleeWindup > 0f || _rangedWindup > 0f)
                {
                    movement = Vector3.Zero;
                }
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
        _poiseRegenDelay = Mathf.Max(0f, _poiseRegenDelay - step);
        _shieldBlockCooldown = Mathf.Max(0f, _shieldBlockCooldown - step);
        _allyAlertCooldown = Mathf.Max(0f, _allyAlertCooldown - step);
        _leapCooldown = Mathf.Max(0f, _leapCooldown - step);
        _ritualCooldown = Mathf.Max(0f, _ritualCooldown - step);
        if (_poiseRegenDelay <= 0f && _poise < _poiseMax)
        {
            _poise = Mathf.Min(_poiseMax, _poise + _poiseMax * 0.22f * step);
        }
    }

    private float GetMoveSpeed()
    {
        float speed = _archetype switch
        {
            EnemyArchetype.Brute => 1.18f,
            EnemyArchetype.Hexer => 1.52f,
            EnemyArchetype.Guardian => 1.05f,
            EnemyArchetype.Berserker => 1.78f,
            EnemyArchetype.Shieldbearer => 1.22f,
            EnemyArchetype.Leaper => 2.05f,
            EnemyArchetype.Necromancer => 1.2f,
            EnemyArchetype.Ghoul => 2.2f,
            EnemyArchetype.SkeletonArcher => 1.5f,
            EnemyArchetype.Vampire => 1.95f,
            _ => 1.7f,
        };
        float woundedFrenzy = _archetype == EnemyArchetype.Berserker && _hp < MaxHp * 0.5f ? 1.48f : 1f;
        return speed * woundedFrenzy * (_eliteModifier == EliteModifier.Frenzied ? 1.42f : 1f);
    }

    private void AcquireTarget(Node3D target, string source)
    {
        if (_isTrainingDummy || !IsInstanceValid(target)) return;
        bool wasDormant = _encounterTarget == null;
        _encounterTarget = target;
        if (!wasDormant) return;
        Aggroed?.Invoke(this);
        if (_allyAlertCooldown > 0f) return;
        _allyAlertCooldown = 1.5f;
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is Enemy ally && ally != this && IsInstanceValid(ally) && GlobalPosition.DistanceTo(ally.GlobalPosition) <= 9f)
            {
                ally.AlertFromAlly(target);
            }
        }
        GD.Print($"[Aggro] {DisplayTitle} acquired target via {source}.");
    }

    private void UpdateAwareness(float step)
    {
        if (_isTrainingDummy || _encounterTarget != null) return;
        _awarenessTimer -= step;
        if (_awarenessTimer > 0f) return;
        _awarenessTimer = 0.18f + GD.Randf() * 0.12f;
        foreach (Node node in GetTree().GetNodesInGroup("player"))
        {
            if (node is not PlayerController player || !IsInstanceValid(player) || player.Health <= 0f) continue;
            float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
            bool heard = distance <= 6.2f && new Vector2(player.Velocity.X, player.Velocity.Z).Length() > 0.45f;
            bool seen = distance <= 12.5f && HasLineOfSight(player);
            if (heard || seen)
            {
                AcquireTarget(player, heard && !seen ? "SOUND" : "SIGHT");
                return;
            }
        }
    }

    private bool HasLineOfSight(PlayerController player)
    {
        var query = PhysicsRayQueryParameters3D.Create(GlobalPosition + Vector3.Up, player.GlobalPosition + Vector3.Up);
        query.CollisionMask = PhysicsLayers.World | PhysicsLayers.Player;
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
        Godot.Collections.Dictionary result = GetWorld3D().DirectSpaceState.IntersectRay(query);
        return result.Count == 0 || (result.TryGetValue("collider", out Variant collider) && collider.AsGodotObject() == player);
    }

    private Vector3 GetNavigationDirection(Vector3 fallbackDirection)
    {
        if (_navigationAgent == null || _encounterTarget == null || !IsInstanceValid(_encounterTarget)) return fallbackDirection.Normalized();
        _navigationAgent.TargetPosition = _encounterTarget.GlobalPosition;
        Vector3 direction = _navigationAgent.GetNextPathPosition() - GlobalPosition;
        direction.Y = 0f;
        return direction.LengthSquared() > 0.01f ? direction.Normalized() : fallbackDirection.Normalized();
    }

    private void UpdateSpecialAbilities(float step)
    {
        if (_encounterTarget is not PlayerController player || !IsInstanceValid(player) || player.Health <= 0f || IsStunned()) return;

        if (_leapWindup > 0f)
        {
            _leapWindup -= step;
            if (_leapWindup <= 0f)
            {
                _leapOrigin = GlobalPosition;
                _leapTravel = 0.42f;
                PlayModelAnimation("CharacterArmature|Punch", true);
            }
            return;
        }
        if (_leapTravel > 0f)
        {
            _leapTravel -= step;
            float progress = Mathf.Clamp(1f - _leapTravel / 0.42f, 0f, 1f);
            GlobalPosition = _leapOrigin.Lerp(_leapDestination, progress) + Vector3.Up * Mathf.Sin(progress * Mathf.Pi) * 2.6f;
            if (_leapTravel <= 0f)
            {
                GlobalPosition = _leapDestination;
                if (GlobalPosition.DistanceTo(player.GlobalPosition) <= 2.5f)
                {
                    player.TakeDamage(24f * (IsElite ? 1.3f : 1f) * _difficultyDamageMultiplier, "LEAPER IMPACT", DamageElement.Physical, 46f);
                }
                Node? scene = GetTree().CurrentScene;
                if (scene != null) SkillVfx.SpawnImpact(scene, GlobalPosition, DamageElement.Fire, true);
            }
            return;
        }
        if (_archetype == EnemyArchetype.Leaper && _leapCooldown <= 0f)
        {
            float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
            if (distance is >= 4f and <= 10.5f)
            {
                _leapDestination = player.GlobalPosition;
                _leapDestination.Y = 0f;
                _leapWindup = 0.72f;
                _leapCooldown = 4.8f;
                Node? scene = GetTree().CurrentScene;
                if (scene != null) DangerTelegraph.SpawnCircle(scene, _leapDestination, 2.35f, _leapWindup + 0.42f);
                return;
            }
        }

        if (_ritualWindup > 0f)
        {
            _ritualWindup -= step;
            if (_ritualWindup <= 0f && _ritualRemnant != null && IsInstanceValid(_ritualRemnant))
            {
                _ritualRemnant.Resurrect(player);
                _ritualRemnant = null;
            }
            return;
        }
        if (_archetype == EnemyArchetype.Necromancer && _ritualCooldown <= 0f)
        {
            FallenRemnant? closest = FindClosestRemnant(11f);
            if (closest != null)
            {
                _ritualRemnant = closest;
                _ritualWindup = 1.65f;
                _ritualCooldown = 7.5f;
                Node? scene = GetTree().CurrentScene;
                if (scene != null) DangerTelegraph.SpawnCircle(scene, closest.GlobalPosition, 1.4f, _ritualWindup);
                PlayModelAnimation("CharacterArmature|Shoot_OneHanded", true);
            }
        }
    }

    private FallenRemnant? FindClosestRemnant(float range)
    {
        FallenRemnant? closest = null;
        float best = range;
        foreach (Node node in GetTree().GetNodesInGroup("fallen_remnants"))
        {
            if (node is not FallenRemnant remnant || !IsInstanceValid(remnant)) continue;
            float distance = GlobalPosition.DistanceTo(remnant.GlobalPosition);
            if (distance < best)
            {
                best = distance;
                closest = remnant;
            }
        }
        return closest;
    }

    private bool TryBuildImportedEnemy()
    {
        bool kenneyModel = _archetype is EnemyArchetype.Ghoul or EnemyArchetype.SkeletonArcher or EnemyArchetype.Vampire;
        string file = _archetype switch
        {
            EnemyArchetype.Brute => "Viking_Male.fbx",
            EnemyArchetype.Hexer => "Wizard.fbx",
            EnemyArchetype.Guardian => "Knight_Golden_Male.fbx",
            EnemyArchetype.Berserker => "Viking_Male.fbx",
            EnemyArchetype.Shieldbearer => "Knight_Golden_Male.fbx",
            EnemyArchetype.Leaper => "Ninja_Male.fbx",
            EnemyArchetype.Necromancer => "Wizard.fbx",
            EnemyArchetype.Ghoul => "character-zombie.glb",
            EnemyArchetype.SkeletonArcher => "character-skeleton.glb",
            EnemyArchetype.Vampire => "character-vampire.glb",
            _ => "Ninja_Male.fbx",
        };
        string directory = kenneyModel ? "kenney_graveyard" : "quaternius_enemies";
        PackedScene? scene = GD.Load<PackedScene>($"res://assets/models/{directory}/{file}");
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
                EnemyArchetype.Berserker => 0.75f,
                EnemyArchetype.Shieldbearer => 0.78f,
                EnemyArchetype.Leaper => 0.61f,
                EnemyArchetype.Necromancer => 0.64f,
                EnemyArchetype.Ghoul => 1.05f,
                EnemyArchetype.SkeletonArcher => 1.05f,
                EnemyArchetype.Vampire => 1.08f,
                _ => 0.66f,
            }),
        };
        AddChild(_modelRoot);
        _modelRoot.AddChild(model);
        if (kenneyModel)
        {
            _modelAnimator = null;
            if (_mesh != null) _mesh.Visible = false;
            return true;
        }
        _modelAnimator = model.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        if (_modelAnimator == null)
        {
            if (_mesh != null) _mesh.Visible = false;
            return true;
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
        if (_rangedWindup > 0f || _meleeWindup > 0f || _leapWindup > 0f || _ritualWindup > 0f || ((_archetype is EnemyArchetype.Brute or EnemyArchetype.Leaper or EnemyArchetype.Ghoul) && _eliteModifier != EliteModifier.Stormbound) || distance < 3f || distance > 11f)
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
        _queuedRangedTarget = target;
        _rangedWindup = _eliteModifier == EliteModifier.Stormbound ? 0.28f : 0.42f;
        Node? scene = GetTree().CurrentScene;
        if (scene != null)
        {
            DangerTelegraph.SpawnLine(scene, GlobalPosition, target.GlobalPosition, _eliteModifier == EliteModifier.Stormbound ? 0.32f : 0.22f, _rangedWindup);
        }
    }

    private void UpdateMeleeAttack(PlayerController target, float distance, float step)
    {
        if (_meleeWindup > 0f || _rangedWindup > 0f || distance > AttackRange || Mathf.Abs(target.GlobalPosition.Y - GlobalPosition.Y) > MaxAttackHeightDifference)
        {
            return;
        }
        _attackCooldown -= step;
        if (_attackCooldown > 0f)
        {
            return;
        }
        _attackCooldown = (_archetype == EnemyArchetype.Brute ? 1.35f : _archetype == EnemyArchetype.Shieldbearer ? 1.55f : 1.05f) * (_eliteModifier == EliteModifier.Frenzied ? 0.62f : 1f);
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
            EnemyArchetype.Berserker => _hp < MaxHp * 0.5f ? 25f : 16f,
            EnemyArchetype.Shieldbearer => 18f,
            EnemyArchetype.Leaper => 13f,
            EnemyArchetype.Necromancer => 9f,
            _ => 12f,
        };
        if (IsElite) damage *= _eliteModifier == EliteModifier.Frenzied ? 1.5f : 1.3f;
        damage *= 1.25f * _difficultyDamageMultiplier;
        _queuedMeleeDamage = damage;
        _queuedMeleeTarget = target;
        _meleeWindup = _archetype switch
        {
            EnemyArchetype.Brute => 0.62f,
            EnemyArchetype.Guardian => 0.78f,
            _ => 0.36f,
        };
        Node? scene = GetTree().CurrentScene;
        if (scene != null)
        {
            DangerTelegraph.SpawnCircle(scene, target.GlobalPosition, _archetype == EnemyArchetype.Guardian ? 1.8f : 1.15f, _meleeWindup);
        }
    }

    private void UpdateAttackWindups(float step)
    {
        if (_meleeWindup > 0f)
        {
            _meleeWindup -= step;
            if (_meleeWindup <= 0f)
            {
                ResolveMeleeAttack();
            }
        }
        if (_rangedWindup > 0f)
        {
            _rangedWindup -= step;
            if (_rangedWindup <= 0f)
            {
                PlayerController? target = _queuedRangedTarget;
                _queuedRangedTarget = null;
                if (target != null && IsInstanceValid(target) && target.Health > 0f)
                {
                    FireShadowBolt(target);
                }
            }
        }
    }

    private void ResolveMeleeAttack()
    {
        PlayerController? target = _queuedMeleeTarget;
        _queuedMeleeTarget = null;
        if (target == null || !IsInstanceValid(target) || target.Health <= 0f)
        {
            return;
        }
        float hitRange = _archetype == EnemyArchetype.Guardian ? 2.8f : AttackRange + 0.35f;
        if (GlobalPosition.DistanceTo(target.GlobalPosition) > hitRange)
        {
            return;
        }
        target.TakeDamage(_queuedMeleeDamage, _archetype.ToString().ToUpperInvariant(), DamageElement.Physical, _archetype == EnemyArchetype.Brute ? 44f : 25f);
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

    private void AddArchetypeReadability()
    {
        if (_archetype == EnemyArchetype.Shieldbearer)
        {
            var shieldMaterial = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.12f, 0.2f, 0.32f),
                Metallic = 0.82f,
                Roughness = 0.28f,
                EmissionEnabled = true,
                Emission = new Color(0.05f, 0.22f, 0.55f),
                EmissionEnergyMultiplier = 1.2f,
            };
            var shield = new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(1.1f, 1.45f, 0.18f) },
                Position = new Vector3(0f, 1.05f, 0.68f),
            };
            shield.SetSurfaceOverrideMaterial(0, shieldMaterial);
            AddChild(shield);
        }
        else if (_archetype == EnemyArchetype.Berserker)
        {
            var rageMaterial = new StandardMaterial3D
            {
                AlbedoColor = new Color(1f, 0.08f, 0.02f, 0.7f),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                EmissionEnabled = true,
                Emission = new Color(1f, 0.03f, 0.01f),
                EmissionEnergyMultiplier = 3f,
            };
            var rage = new MeshInstance3D { Mesh = new TorusMesh { InnerRadius = 0.72f, OuterRadius = 0.84f }, Position = new Vector3(0f, 0.12f, 0f) };
            rage.SetSurfaceOverrideMaterial(0, rageMaterial);
            AddChild(rage);
        }
        else if (_archetype is EnemyArchetype.Leaper or EnemyArchetype.Necromancer)
        {
            Color tellColor = _archetype == EnemyArchetype.Leaper ? new Color(1f, 0.28f, 0.02f) : new Color(0.08f, 1f, 0.25f);
            var tellMaterial = new StandardMaterial3D
            {
                AlbedoColor = new Color(tellColor.R, tellColor.G, tellColor.B, 0.72f),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                EmissionEnabled = true,
                Emission = tellColor,
                EmissionEnergyMultiplier = 3.4f,
            };
            var tell = new MeshInstance3D
            {
                Mesh = new TorusMesh { InnerRadius = 0.5f, OuterRadius = 0.62f },
                Position = new Vector3(0f, _archetype == EnemyArchetype.Leaper ? 1.25f : 1.85f, 0f),
                Rotation = new Vector3(Mathf.Pi * 0.5f, 0f, 0f),
            };
            tell.SetSurfaceOverrideMaterial(0, tellMaterial);
            AddChild(tell);
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
        if (_eliteModifier == EliteModifier.Warden)
        {
            for (int i = 0; i < 3; i++)
            {
                float angle = Mathf.Tau * i / 3f;
                var pylon = new MeshInstance3D
                {
                    Mesh = new PrismMesh { Size = new Vector3(0.22f, 0.7f, 0.22f) },
                    Position = new Vector3(Mathf.Cos(angle) * 1.05f, 1.05f, Mathf.Sin(angle) * 1.05f),
                };
                pylon.SetSurfaceOverrideMaterial(0, material);
                AddChild(pylon);
            }
        }
        else if (_eliteModifier == EliteModifier.Molten)
        {
            var moltenRing = new MeshInstance3D { Mesh = new TorusMesh { InnerRadius = 1.05f, OuterRadius = 1.22f }, Position = new Vector3(0f, 0.08f, 0f) };
            moltenRing.SetSurfaceOverrideMaterial(0, material);
            AddChild(moltenRing);
        }
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
        if (_modelRoot != null && !_mesh.Visible)
        {
            _modelRoot.Position = new Vector3(0f, bob, attackPunch * 0.18f);
            _modelRoot.Rotation = new Vector3(attackPunch * -0.12f, 0f, sway * 0.7f);
        }
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
        _meleeWindup = 0f;
        _rangedWindup = 0f;
        _queuedMeleeTarget = null;
        _queuedRangedTarget = null;
        _leapWindup = 0f;
        _ritualWindup = 0f;
        _ritualRemnant = null;
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
        if (_archetype == EnemyArchetype.Shieldbearer && _shieldBlockCooldown <= 0f && IsPlayerInFront())
        {
            amount *= 0.22f;
            _shieldBlockCooldown = 2.8f;
            Node? blockScene = GetTree().CurrentScene;
            if (blockScene != null) SkillVfx.SpawnImpact(blockScene, GlobalPosition + Vector3.Up, DamageElement.Cold, true);
        }
        if (element != DamageElement.Physical)
        {
            float penetration = resolved?.ElementalPenetration ?? 0f;
            float effectiveResistance = Mathf.Clamp(GetElementResistance(element) - penetration, -25f, 75f);
            amount *= 1f - effectiveResistance / 100f;
        }
        if (_eliteModifier == EliteModifier.Bulwark) amount *= 0.72f;
        if (IsProtectedByWarden()) amount *= 0.64f;
        if (resolved != null && resolved.ExecuteThreshold > 0f && _hp <= MaxHp * resolved.ExecuteThreshold)
        {
            amount *= resolved.ExecuteMultiplier;
        }
        Color? hitColor = resolved?.IsCritical == true ? new Color(1f, 0.82f, 0.18f) : null;
        TakeRawDamage(amount, false, hitColor, resolved?.IsCritical == true);
        if (resolved?.IsCritical == true && _encounterTarget is PlayerController criticalOwner && IsInstanceValid(criticalOwner))
        {
            criticalOwner.TriggerHitFeedback(0.2f, true);
        }
        if (_hp > 0f && resolved != null && resolved.StaggerDamage > 0f)
        {
            _poise = Mathf.Max(0f, _poise - resolved.StaggerDamage);
            _poiseRegenDelay = 1.8f;
            if (_poise <= 0f)
            {
                _poise = _poiseMax;
                ApplyStun(_archetype == EnemyArchetype.Guardian ? 0.28f : 0.58f);
                Node? scene = GetTree().CurrentScene;
                if (scene != null) SkillVfx.SpawnImpact(scene, GlobalPosition, DamageElement.Physical, true);
            }
        }
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

    private float GetElementResistance(DamageElement element)
    {
        float resistance = (_archetype, element) switch
        {
            (EnemyArchetype.Brute, DamageElement.Fire) => 20f,
            (EnemyArchetype.Berserker, DamageElement.Fire) => 28f,
            (EnemyArchetype.Hexer, DamageElement.Lightning) => 24f,
            (EnemyArchetype.Shieldbearer, DamageElement.Cold) => 22f,
            (EnemyArchetype.Guardian, _) => 18f,
            _ => 0f,
        };
        return resistance + (_eliteModifier == EliteModifier.Bulwark ? 12f : 0f);
    }

    private void TakeRawDamage(float amount, bool isBurn, Color? numberColor = null, bool isCritical = false)
    {
        if (_hp <= 0f)
        {
            return;
        }
        if (!_isTrainingDummy)
        {
            _hp -= amount;
        }
        SpawnDamageNumber(amount, isBurn, numberColor, isCritical);
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
            SpawnFallenRemnant();
            if (_eliteModifier == EliteModifier.Molten) SpawnMoltenDeathHazard();
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
        int baseExperience = _archetype switch
        {
            EnemyArchetype.Brute => 34,
            EnemyArchetype.Hexer => 28,
            EnemyArchetype.Guardian => 180,
            EnemyArchetype.Berserker => 40,
            EnemyArchetype.Shieldbearer => 44,
            EnemyArchetype.Leaper => 38,
            EnemyArchetype.Necromancer => 48,
            EnemyArchetype.Vampire => 46,
            EnemyArchetype.SkeletonArcher => 30,
            EnemyArchetype.Ghoul => 24,
            _ => 20,
        };
        float levelScale = 1f + (player.Progression.Level - 1) * 0.018f;
        int experience = Mathf.Max(1, Mathf.RoundToInt(baseExperience * levelScale * _difficultyExperienceMultiplier));
        if (IsElite) experience = Mathf.RoundToInt(experience * 2.4f);
        player.Progression.GainExperience(experience);
        int flaskCharges = _archetype switch
        {
            EnemyArchetype.Brute => 5,
            EnemyArchetype.Hexer => 4,
            EnemyArchetype.Guardian => PlayerController.MaxFlaskCharges,
            EnemyArchetype.Berserker => 6,
            EnemyArchetype.Shieldbearer => 6,
            EnemyArchetype.Leaper => 5,
            EnemyArchetype.Necromancer => 7,
            _ => 3,
        };
        player.RechargeFlask(IsElite ? Mathf.Max(flaskCharges, 8) : flaskCharges);
    }

    private bool IsPlayerInFront()
    {
        if (_encounterTarget is not PlayerController player || !IsInstanceValid(player)) return false;
        Vector3 toPlayer = player.GlobalPosition - GlobalPosition;
        toPlayer.Y = 0f;
        if (toPlayer.LengthSquared() < 0.01f) return true;
        Vector3 forward = GlobalTransform.Basis.Z;
        forward.Y = 0f;
        return forward.Normalized().Dot(toPlayer.Normalized()) > 0.15f;
    }

    private void SpawnDamageNumber(float amount, bool isBurn, Color? color = null, bool isCritical = false)
    {
        var label = new Label3D();
        label.Text = isCritical ? $"✦ {(int)amount}!" : ((int)amount).ToString();
        label.FontSize = isCritical ? 62 : 48;
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

    private void SpawnFallenRemnant()
    {
        if (_archetype == EnemyArchetype.Guardian || _isTrainingDummy) return;
        Node? scene = GetTree().CurrentScene;
        if (scene == null) return;
        var remnant = new FallenRemnant();
        scene.AddChild(remnant);
        remnant.GlobalPosition = GlobalPosition;
    }

    private bool IsProtectedByWarden()
    {
        if (_eliteModifier == EliteModifier.Warden) return false;
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is Enemy enemy && enemy != this && IsInstanceValid(enemy) && enemy.Health > 0f && enemy._eliteModifier == EliteModifier.Warden && GlobalPosition.DistanceTo(enemy.GlobalPosition) <= 8f) return true;
        }
        return false;
    }

    private void SpawnMoltenDeathHazard()
    {
        Node? scene = GetTree().CurrentScene;
        if (scene == null) return;
        var hazard = new EliteDeathHazard();
        hazard.Configure(3.25f, 1.15f, 58f * _difficultyDamageMultiplier);
        scene.AddChild(hazard);
        hazard.GlobalPosition = GlobalPosition;
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
        float rarityBonus = _difficultyRarityBonus + (IsElite ? 0.24f : 0f) + _archetype switch
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
            HeroClass? preferredClass = _encounterTarget is PlayerController hero ? hero.Progression.HeroClass : null;
            LootDrop.Spawn(scene, GlobalPosition + scatter, ItemGenerator.Generate(level, rarityBonus, preferredClass));
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
        damage *= 1.25f * _difficultyDamageMultiplier;
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
