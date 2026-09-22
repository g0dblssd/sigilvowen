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
    private MeshInstance3D? _mesh;
    private float _age;
    private bool _hit;

    public void Configure(float damage, Vector3 direction, ResolvedCast? resolved)
    {
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

        var mat = new StandardMaterial3D
        {
            AlbedoColor = new Color(1f, 0.8f, 0.3f),
            EmissionEnabled = true,
            Emission = new Color(1f, 0.6f, 0.1f),
            EmissionEnergyMultiplier = 2f,
        };
        _mesh = new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.25f, Height = 0.5f } };
        _mesh.SetSurfaceOverrideMaterial(0, mat);
        AddChild(_mesh);
        var light = new OmniLight3D { LightColor = mat.Emission, LightEnergy = 1.4f, OmniRange = 3.5f };
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
        if (_hit || body is not Enemy e || !IsInstanceValid(e) || e.IsQueuedForDeletion())
        {
            return;
        }
        _hit = true;
        var element = Resolved != null ? Resolved.Element : DamageElement.Fire;
        e.TakeDamage(Damage, element, Resolved);
        if (Resolved != null && Resolved.HasLightningInfusion)
        {
            var scene = GetTree().CurrentScene;
            if (scene != null)
            {
                SkillVfx.SpawnLightningArcs(scene, GlobalPosition, 4);
            }
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
