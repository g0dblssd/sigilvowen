using Godot;

namespace Sigilwoven;

public partial class Enemy : CharacterBody3D
{
    public float MaxHp = 60f;
    private float _hp;
    private float _stunTimer;
    private float _burnTimer;
    private float _burnDps;
    private float _poisonTimer;
    private float _poisonDps;
    private float _chillTimer;
    private float _chillSlow;
    private MeshInstance3D? _mesh;
    private StandardMaterial3D? _mat;
    private Node3D? _encounterTarget;
    private bool _isTrainingDummy;
    private Label3D? _healthLabel;
    private float _attackCooldown;
    private float _rangedCooldown;
    private float _hitFlash;
    private const float EncounterSpeed = 1.7f;
    private const float Gravity = 20f;
    private const float AttackRange = 1.9f;
    private const float AttackDamage = 12f;

    public bool IsTrainingDummy => _isTrainingDummy;

    public void Configure(float maxHp, bool isTrainingDummy = false)
    {
        MaxHp = maxHp;
        _isTrainingDummy = isTrainingDummy;
    }

    public void JoinAmbush(Node3D target)
    {
        _encounterTarget = target;
    }

    public override void _Ready()
    {
        CollisionLayer = PhysicsLayers.Enemy;
        CollisionMask = PhysicsLayers.Player | PhysicsLayers.Ally;
        AddToGroup("enemies");
        _hp = MaxHp;

        _mat = new StandardMaterial3D
        {
            AlbedoColor = _isTrainingDummy ? new Color(0.62f, 0.48f, 0.25f) : new Color(0.8f, 0.25f, 0.25f),
            EmissionEnabled = _isTrainingDummy,
            Emission = new Color(0.2f, 0.12f, 0.03f),
            EmissionEnergyMultiplier = 0.8f,
        };
        var box = new BoxMesh { Size = new Vector3(1f, 2f, 1f) };
        _mesh = new MeshInstance3D { Mesh = box };
        _mesh.SetSurfaceOverrideMaterial(0, _mat);
        _mesh.Position = new Vector3(0, 1f, 0);
        AddChild(_mesh);

        var col = new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(1f, 2f, 1f) } };
        col.Position = new Vector3(0, 1f, 0);
        AddChild(col);

        if (_isTrainingDummy)
        {
            var title = new Label3D { Text = "TRAINING DUMMY", FontSize = 42, OutlineSize = 8, Modulate = new Color(1f, 0.85f, 0.4f) };
            title.Position = new Vector3(0f, 2.55f, 0f);
            AddChild(title);
        }
        else
        {
            _healthLabel = new Label3D { FontSize = 34, OutlineSize = 6, Modulate = new Color(1f, 0.75f, 0.75f), Position = new Vector3(0f, 2.35f, 0f) };
            AddChild(_healthLabel);
            UpdateHealthLabel();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float step = (float)delta;
        Vector3 velocity = Velocity;
        velocity.X = 0f;
        velocity.Z = 0f;

        if (_encounterTarget != null && IsInstanceValid(_encounterTarget))
        {
            Vector3 direction = _encounterTarget.GlobalPosition - GlobalPosition;
            direction.Y = 0f;
            float distance = direction.Length();
            if (!IsStunned() && distance > AttackRange)
            {
                float moveMultiplier = _chillTimer > 0f ? 1f - _chillSlow : 1f;
                Vector3 movement = direction.Normalized() * EncounterSpeed * moveMultiplier;
                velocity.X = movement.X;
                velocity.Z = movement.Z;
                LookAt(GlobalPosition + direction, Vector3.Up, true);
                if (distance > 4f && distance < 10f)
                {
                    _rangedCooldown -= step;
                    if (_rangedCooldown <= 0f && _encounterTarget is PlayerController rangedTarget)
                    {
                        _rangedCooldown = 2.4f + GD.Randf() * 0.8f;
                        FireShadowBolt(rangedTarget);
                    }
                }
            }
            else if (!IsStunned() && distance <= AttackRange && _encounterTarget is PlayerController player)
            {
                _attackCooldown -= (float)delta;
                if (_attackCooldown <= 0f)
                {
                    _attackCooldown = 1.05f;
                    player.TakeDamage(AttackDamage, "RAIDER");
                    FlashHit(new Color(1f, 0.85f, 0.25f));
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

        if (_stunTimer > 0f)
        {
            _stunTimer -= step;
        }
        if (_burnTimer > 0f)
        {
            _burnTimer -= step;
            TakeRawDamage(_burnDps * step, true);
        }
        if (_poisonTimer > 0f)
        {
            _poisonTimer -= step;
            TakeRawDamage(_poisonDps * step, false, new Color(0.25f, 1f, 0.3f));
        }
        if (_chillTimer > 0f)
        {
            _chillTimer -= step;
        }
        _hitFlash = Mathf.Max(0f, _hitFlash - step);
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
        _burnDps = Mathf.Max(_burnDps, dps);
        _burnTimer = Mathf.Max(_burnTimer, duration);
    }

    public void ApplyPoison(float dps, float duration)
    {
        _poisonDps = Mathf.Max(_poisonDps, dps);
        _poisonTimer = Mathf.Max(_poisonTimer, duration);
    }

    public void ApplyChill(float slow, float duration)
    {
        _chillSlow = Mathf.Max(_chillSlow, slow);
        _chillTimer = Mathf.Max(_chillTimer, duration);
        FlashHit(new Color(0.25f, 0.82f, 1f));
    }

    public void TakeDamage(float amount, DamageElement element, ResolvedCast? resolved = null)
    {
        if (_hp <= 0f || IsQueuedForDeletion() || float.IsNaN(amount) || float.IsInfinity(amount))
        {
            return;
        }
        amount = Mathf.Max(0f, amount);
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
        if (_isTrainingDummy)
        {
            return;
        }
        if (_hp <= 0f)
        {
            DropAether();
            QueueFree();
        }
        else
        {
            UpdateHealthLabel();
        }
    }

    private void SpawnDamageNumber(float amount, bool isBurn, Color? color = null)
    {
        var label = new Label3D();
        label.Text = ((int)amount).ToString();
        label.FontSize = 48;
        label.Modulate = color ?? (isBurn ? new Color(1f, 0.5f, 0.1f) : Colors.White);
        label.OutlineSize = 8;
        label.Position = new Vector3(GD.Randf() * 0.6f - 0.3f, 2.4f, 0);
        AddChild(label);
        var tween = CreateTween();
        tween.TweenProperty(label, "position:y", 3.2f, 0.7);
        tween.TweenCallback(Callable.From(label.QueueFree));
    }

    private void UpdateHealthLabel()
    {
        if (_healthLabel != null)
        {
            _healthLabel.Text = $"{Mathf.Max(0, Mathf.CeilToInt(_hp))} / {Mathf.CeilToInt(MaxHp)}";
        }
    }

    private void FlashHit(Color color)
    {
        if (_mat == null)
        {
            return;
        }
        _mat.AlbedoColor = color;
        var normal = _isTrainingDummy ? new Color(0.62f, 0.48f, 0.25f) : new Color(0.8f, 0.25f, 0.25f);
        var tween = CreateTween();
        tween.TweenProperty(_mat, "albedo_color", normal, 0.12f);
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

    private void FireShadowBolt(PlayerController target)
    {
        var scene = GetTree().CurrentScene;
        if (scene == null)
        {
            return;
        }
        var bolt = new RaiderBolt();
        bolt.Configure(target, 9f);
        scene.AddChild(bolt);
        bolt.GlobalPosition = GlobalPosition + new Vector3(0f, 1.1f, 0f);
        FlashHit(new Color(0.8f, 0.2f, 1f));
    }
}
