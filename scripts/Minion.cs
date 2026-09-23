using Godot;

namespace Sigilwoven;

public partial class Minion : CharacterBody3D
{
    private SkillData? _skill;
    private ResolvedCast? _resolved;
    private Node3D? _followTarget;
    private float _attackCooldown;
    private MeshInstance3D? _mesh;
    private float _lifeLeft;

    private const float Speed = 5f;
    private const float AttackRange = 2.6f;
    private const float AggroRange = 16f;

    public void Configure(SkillData skill, ResolvedCast resolved, Node3D followTarget)
    {
        _skill = skill;
        _resolved = resolved;
        _followTarget = followTarget;
        _lifeLeft = 22f * resolved.DurationMultiplier;
    }

    public override void _Ready()
    {
        CollisionLayer = PhysicsLayers.Ally;
        CollisionMask = PhysicsLayers.World | PhysicsLayers.Enemy;

        bool lightning = _resolved != null && _resolved.LightningForm;
        var mat = new StandardMaterial3D();
        if (lightning)
        {
            // Lightning form: glowing cyan/yellow body.
            mat.AlbedoColor = new Color(0.3f, 1f, 1f);
            mat.EmissionEnabled = true;
            mat.Emission = new Color(0.4f, 1f, 1f);
            mat.EmissionEnergyMultiplier = 2.0f;
        }
        else
        {
            mat.AlbedoColor = new Color(0.3f, 0.8f, 0.35f);
        }

        var capsule = new CapsuleMesh { Radius = 0.4f, Height = 1.4f };
        _mesh = new MeshInstance3D { Mesh = capsule };
        _mesh.SetSurfaceOverrideMaterial(0, mat);
        _mesh.Position = new Vector3(0, 0.9f, 0);
        AddChild(_mesh);

        // Small lightning halo for lightning form.
        if (lightning)
        {
            var haloMat = new StandardMaterial3D
            {
                AlbedoColor = new Color(1f, 0.95f, 0.4f),
                EmissionEnabled = true,
                Emission = new Color(1f, 0.9f, 0.2f),
                EmissionEnergyMultiplier = 2.5f,
            };
            var halo = new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.18f, Height = 0.36f } };
            halo.SetSurfaceOverrideMaterial(0, haloMat);
            halo.Position = new Vector3(0, 1.9f, 0);
            AddChild(halo);
        }

        var col = new CollisionShape3D { Shape = new CapsuleShape3D { Radius = 0.4f, Height = 1.4f } };
        col.Position = new Vector3(0, 0.9f, 0);
        AddChild(col);
    }

    public override void _PhysicsProcess(double delta)
    {
        _lifeLeft -= (float)delta;
        if (_lifeLeft <= 0f)
        {
            QueueFree();
            return;
        }
        if (_followTarget == null || _skill == null || _resolved == null)
        {
            return;
        }

        var target = FindNearestEnemy();
        Vector3 moveDir;
        if (target != null)
        {
            float dist = GlobalPosition.DistanceTo(target.GlobalPosition);
            if (dist > AttackRange)
            {
                moveDir = (target.GlobalPosition - GlobalPosition);
                moveDir.Y = 0f;
                moveDir = moveDir.Normalized();
            }
            else
            {
                moveDir = Vector3.Zero;
                _attackCooldown -= (float)delta;
                if (_attackCooldown <= 0f)
                {
                    _attackCooldown = 1.0f;
                    Strike(target);
                }
            }
        }
        else
        {
            float distOwner = GlobalPosition.DistanceTo(_followTarget.GlobalPosition);
            if (distOwner > 3f)
            {
                moveDir = (_followTarget.GlobalPosition - GlobalPosition);
                moveDir.Y = 0f;
                moveDir = moveDir.Normalized();
            }
            else
            {
                moveDir = Vector3.Zero;
            }
        }

        Vector3 vel = Velocity;
        vel.X = moveDir.X * Speed;
        vel.Z = moveDir.Z * Speed;
        vel.Y -= 20f * (float)delta;
        if (GlobalPosition.Y < 0f)
        {
            GlobalPosition = new Vector3(GlobalPosition.X, 0f, GlobalPosition.Z);
            vel.Y = 0f;
        }
        Velocity = vel;
        MoveAndSlide();
    }

    private Enemy? FindNearestEnemy()
    {
        var tree = GetTree();
        if (tree == null)
        {
            return null;
        }
        Enemy? best = null;
        Enemy? dummy = null;
        float bestDist = AggroRange;
        foreach (var n in tree.GetNodesInGroup("enemies"))
        {
            if (n is Enemy e && IsInstanceValid(e) && !e.IsQueuedForDeletion())
            {
                if (e.IsTrainingDummy)
                {
                    dummy = e;
                    continue;
                }
                float d = GlobalPosition.DistanceTo(e.GlobalPosition);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = e;
                }
            }
        }
        return best ?? dummy;
    }

    private void Strike(Enemy enemy)
    {
        if (_resolved == null)
        {
            return;
        }
        // Lightning-form strike: lightning damage + 50% stun handled in TakeDamage.
        enemy.TakeDamage(_resolved.Damage, _resolved.Element, _resolved);
        Node? scene = GetTree().CurrentScene;
        if (scene != null)
        {
            SkillVfx.SpawnProjectileImpact(scene, _skill?.Id ?? "summon", enemy.GlobalPosition + Vector3.Up, _resolved);
        }
    }
}
