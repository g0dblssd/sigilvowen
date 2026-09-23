using Godot;

namespace Sigilwoven;

public partial class Minion : CharacterBody3D
{
    private SkillData? _skill;
    private ResolvedCast? _resolved;
    private Node3D? _followTarget;
    private float _attackCooldown;
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

        if (!TryBuildImportedMinion()) BuildProceduralMinion(mat);

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

    private bool TryBuildImportedMinion()
    {
        if (_skill == null) return false;
        string? path = _skill.Id switch
        {
            "bone_golem" or "raise_legion" => "res://assets/models/kenney_graveyard/character-skeleton.glb",
            "dread_hound" => "res://assets/models/quaternius_characters/Pug.fbx",
            "ember_imp" => "res://assets/models/kenney_graveyard/character-ghost.glb",
            _ => null,
        };
        if (path == null || GD.Load<PackedScene>(path)?.Instantiate() is not Node3D model) return false;
        model.Scale = Vector3.One * (_skill.Id == "dread_hound" ? 0.46f : _skill.Id == "bone_golem" ? 1.25f : 0.82f);
        model.Position = _skill.Id == "dread_hound" ? new Vector3(0f, 0f, 0f) : new Vector3(0f, 0.05f, 0f);
        AddChild(model);
        AnimationPlayer? animator = model.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        if (animator?.GetAnimation("CharacterArmature|Walk") is Animation walk)
        {
            walk.LoopMode = Animation.LoopModeEnum.Linear;
            animator.Play("CharacterArmature|Walk");
        }
        return true;
    }

    private void BuildProceduralMinion(StandardMaterial3D material)
    {
        var root = new Node3D();
        AddChild(root);
        string id = _skill?.Id ?? "summon";
        if (id == "frost_spider")
        {
            AddPart(root, new SphereMesh { Radius = 0.38f, Height = 0.56f }, material, new Vector3(0f, 0.52f, 0f), new Vector3(1.25f, 0.7f, 1.5f));
            AddPart(root, new SphereMesh { Radius = 0.26f, Height = 0.42f }, material, new Vector3(0f, 0.55f, -0.5f));
            for (int i = 0; i < 8; i++)
            {
                float side = i < 4 ? -1f : 1f;
                int row = i % 4;
                AddPart(root, new CylinderMesh { TopRadius = 0.035f, BottomRadius = 0.065f, Height = 0.92f, RadialSegments = 6 }, material,
                    new Vector3(side * 0.48f, 0.38f, -0.48f + row * 0.31f), new Vector3(0.62f, 0f, side * 0.92f));
            }
            return;
        }
        if (id == "phoenix")
        {
            AddPart(root, new SphereMesh { Radius = 0.32f, Height = 0.9f }, material, new Vector3(0f, 0.9f, 0f), new Vector3(0.9f, 1.25f, 0.9f));
            AddPart(root, new SphereMesh { Radius = 0.2f, Height = 0.38f }, material, new Vector3(0f, 1.42f, -0.08f));
            for (int i = 0; i < 3; i++)
            {
                float spread = 0.45f + i * 0.22f;
                AddPart(root, new PrismMesh { Size = new Vector3(0.18f, 0.68f, 1.25f) }, material, new Vector3(-spread, 1f, 0.08f), new Vector3(0.18f, -0.22f, 0.95f - i * 0.12f));
                AddPart(root, new PrismMesh { Size = new Vector3(0.18f, 0.68f, 1.25f) }, material, new Vector3(spread, 1f, 0.08f), new Vector3(0.18f, 0.22f, -0.95f + i * 0.12f));
            }
            AddPart(root, new PrismMesh { Size = new Vector3(0.18f, 1.1f, 0.22f) }, material, new Vector3(0f, 0.62f, 0.48f), new Vector3(0.7f, 0f, 0f));
            return;
        }

        AddPart(root, new PrismMesh { Size = new Vector3(0.72f, 1.35f, 0.72f) }, material, new Vector3(0f, 0.95f, 0f));
        for (int i = 0; i < 4; i++)
        {
            float angle = Mathf.Tau * i / 4f;
            AddPart(root, new PrismMesh { Size = new Vector3(0.18f, 0.72f, 0.18f) }, material,
                new Vector3(Mathf.Cos(angle) * 0.62f, 0.92f, Mathf.Sin(angle) * 0.62f), new Vector3(0.35f, -angle, 0.2f));
        }
        AddPart(root, new TorusMesh { InnerRadius = 0.52f, OuterRadius = 0.64f }, material, new Vector3(0f, 1.72f, 0f), new Vector3(0.28f, 0f, 0.16f));
    }

    private static MeshInstance3D AddPart(Node3D parent, PrimitiveMesh mesh, Material material, Vector3 position, Vector3? rotation = null, Vector3? scale = null)
    {
        var part = new MeshInstance3D { Mesh = mesh, Position = position, Rotation = rotation ?? Vector3.Zero, Scale = scale ?? Vector3.One };
        part.SetSurfaceOverrideMaterial(0, material);
        parent.AddChild(part);
        return part;
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
