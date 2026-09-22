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
        CollisionLayer = 16;
        CollisionMask = 2;

        var material = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.25f, 0.9f, 1f),
            EmissionEnabled = true,
            Emission = new Color(0.1f, 0.7f, 1f),
            EmissionEnergyMultiplier = 2.5f,
        };
        var mesh = new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.28f, Height = 0.56f } };
        mesh.SetSurfaceOverrideMaterial(0, material);
        AddChild(mesh);
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
