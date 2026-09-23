using Godot;

namespace Sigilwoven;

public partial class Projectile : Area3D
{
    public float Damage = 20f;
    public ResolvedCast? Resolved;
    public Vector3 Direction = Vector3.Forward;
    public float Speed = 18f;
    public float Life = 2f;
    public int ChainLeft;
    public string SkillId = "fireball";
    private MeshInstance3D? _mesh;
    private float _age;
    private bool _hit;
    private float _trailTimer;

    public void Configure(string skillId, float damage, Vector3 direction, ResolvedCast? resolved)
    {
        SkillId = skillId;
        Damage = damage;
        Direction = direction.Normalized();
        Resolved = resolved;
        ChainLeft = resolved != null ? resolved.ChainCount : 0;
    }

    public override void _Ready()
    {
        Monitoring = true;
        Monitorable = false;
        CollisionLayer = PhysicsLayers.PlayerProjectile;
        CollisionMask = PhysicsLayers.Enemy;

        DamageElement element = Resolved?.Element ?? DamageElement.Fire;
        Color color = SkillVfx.ElementColor(element);
        var mat = new StandardMaterial3D
        {
            AlbedoColor = color,
            EmissionEnabled = true,
            Emission = color,
            EmissionEnergyMultiplier = 4f,
        };
        PrimitiveMesh projectileMesh = SkillId switch
        {
            "frostbolt" => new PrismMesh { Size = new Vector3(0.32f, 0.32f, 0.8f) },
            "stone_shot" => new BoxMesh { Size = new Vector3(0.42f, 0.42f, 0.64f) },
            "venom_fang" => new SphereMesh { Radius = 0.22f, Height = 0.62f },
            _ => new SphereMesh { Radius = 0.25f, Height = 0.5f },
        };
        _mesh = new MeshInstance3D { Mesh = projectileMesh };
        _mesh.SetSurfaceOverrideMaterial(0, mat);
        AddChild(_mesh);
        var light = new OmniLight3D { LightColor = color, LightEnergy = 1.4f, OmniRange = 3.5f };
        AddChild(light);
        if (Resolved != null && Resolved.HasLightningInfusion)
        {
            var lightningMat = new StandardMaterial3D { AlbedoColor = new Color(1f, 0.95f, 0.15f), EmissionEnabled = true, Emission = new Color(1f, 0.85f, 0.05f), EmissionEnergyMultiplier = 4f };
            var halo = new MeshInstance3D { Mesh = new TorusMesh { InnerRadius = 0.3f, OuterRadius = 0.38f } };
            halo.SetSurfaceOverrideMaterial(0, lightningMat);
            halo.Rotation = new Vector3(Mathf.Pi * 0.5f, 0f, 0f);
            AddChild(halo);
        }

        var col = new CollisionShape3D { Shape = new SphereShape3D { Radius = 0.3f } };
        AddChild(col);

        BodyEntered += OnBodyEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        _age += (float)delta;
        _trailTimer -= (float)delta;
        if (_trailTimer <= 0f)
        {
            _trailTimer = 0.045f;
            Node? scene = GetTree().CurrentScene;
            if (scene != null)
            {
                SkillVfx.SpawnProjectileTrail(scene, GlobalPosition, Resolved?.Element ?? DamageElement.Fire, Resolved?.AppliedLinks.Count > 0);
            }
        }
        if (_mesh != null)
        {
            float pulse = 0.9f + Mathf.Sin(_age * 18f) * 0.22f;
            _mesh.Scale = Vector3.One * pulse;
            _mesh.RotateY(9f * (float)delta);
        }
        GlobalPosition += Direction * Speed * (float)delta;
        Life -= (float)delta;
        if (Life <= 0f)
        {
            QueueFree();
            return;
        }
        if (GlobalPosition.Y < 0f || GlobalPosition.Y > 30f)
        {
            QueueFree();
        }
    }

    private void OnBodyEntered(Node3D body)
    {
        if (_hit || !IsInstanceValid(body) || body.IsQueuedForDeletion())
        {
            return;
        }
        _hit = true;
        var element = Resolved != null ? Resolved.Element : DamageElement.Fire;
        if (body is WardenPylon pylon)
        {
            pylon.TakeDamage(Damage, element, Resolved);
            Node? pylonScene = GetTree().CurrentScene;
            if (Resolved != null && pylonScene != null) SkillVfx.SpawnProjectileImpact(pylonScene, SkillId, GlobalPosition, Resolved);
            QueueFree();
            return;
        }
        if (body is not Enemy e)
        {
            _hit = false;
            return;
        }
        e.TakeDamage(Damage, element, Resolved);
        Node? scene = GetTree().CurrentScene;
        if (Resolved != null && scene != null)
        {
            SkillVfx.SpawnProjectileImpact(scene, SkillId, GlobalPosition, Resolved);
        }
        if (ChainLeft > 0)
        {
            ChainLeft--;
            // Chain: splash nearby enemies.
            foreach (var n in GetTree().GetNodesInGroup("enemies"))
            {
                if (n is Enemy other && other != e && IsInstanceValid(other) && !other.IsQueuedForDeletion())
                {
                    if (other.GlobalPosition.DistanceTo(GlobalPosition) < 5f)
                    {
                        other.TakeDamage(Damage * 0.7f, element, Resolved);
                        if (scene != null)
                        {
                            SkillVfx.SpawnBeam(scene, GlobalPosition, other.GlobalPosition + Vector3.Up, element, 0.1f);
                            if (Resolved != null) SkillVfx.SpawnLinkLayers(scene, other.GlobalPosition, Resolved);
                        }
                        ChainLeft--;
                        if (ChainLeft <= 0)
                        {
                            break;
                        }
                    }
                }
            }
        }
        QueueFree();
    }
}
