using Godot;

namespace Sigilwoven;

/// <summary>Small recoverable mana drop created when a hostile is defeated.</summary>
public partial class AetherShard : Area3D
{
    private float _life = 16f;
    private float _age;
    private bool _collected;

    public override void _Ready()
    {
        Monitoring = true;
        Monitorable = false;
        CollisionLayer = PhysicsLayers.Pickup;
        CollisionMask = PhysicsLayers.Player;

        var material = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.25f, 0.9f, 1f),
            EmissionEnabled = true,
            Emission = new Color(0.1f, 0.7f, 1f),
            EmissionEnergyMultiplier = 2.5f,
        };
        var core = new MeshInstance3D { Mesh = new PrismMesh { Size = new Vector3(0.34f, 0.72f, 0.34f) }, Rotation = new Vector3(0.12f, 0.25f, -0.08f) };
        core.SetSurfaceOverrideMaterial(0, material);
        AddChild(core);
        for (int i = 0; i < 3; i++)
        {
            float angle = Mathf.Tau * i / 3f;
            var shard = new MeshInstance3D
            {
                Mesh = new PrismMesh { Size = new Vector3(0.12f, 0.36f, 0.12f) },
                Position = new Vector3(Mathf.Cos(angle) * 0.34f, -0.12f, Mathf.Sin(angle) * 0.34f),
                Rotation = new Vector3(0.35f, -angle, 0.28f),
            };
            shard.SetSurfaceOverrideMaterial(0, material);
            AddChild(shard);
        }
        var ring = new MeshInstance3D { Mesh = new TorusMesh { InnerRadius = 0.38f, OuterRadius = 0.44f }, Rotation = new Vector3(0.35f, 0f, 0.18f) };
        ring.SetSurfaceOverrideMaterial(0, material);
        AddChild(ring);
        var collision = new CollisionShape3D { Shape = new SphereShape3D { Radius = 0.65f } };
        AddChild(collision);
        var label = new Label3D { Text = "AETHER", FontSize = 26, OutlineSize = 5, Position = new Vector3(0f, 0.55f, 0f), Modulate = new Color(0.55f, 0.95f, 1f) };
        AddChild(label);
        BodyEntered += OnBodyEntered;
    }

    public override void _Process(double delta)
    {
        _age += (float)delta;
        _life -= (float)delta;
        Position = new Vector3(Position.X, 0.35f + Mathf.Sin(_age * 4f) * 0.12f, Position.Z);
        RotateY(2f * (float)delta);
        if (_life <= 0f)
        {
            QueueFree();
        }
    }

    private void OnBodyEntered(Node3D body)
    {
        if (!_collected && body is PlayerController player && IsInstanceValid(player))
        {
            _collected = true;
            player.RestoreMana(25f);
            QueueFree();
        }
    }
}
