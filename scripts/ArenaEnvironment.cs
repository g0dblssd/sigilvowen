using System.Collections.Generic;
using Godot;

namespace Sigilwoven;

/// <summary>Physical arena boundary and lightweight animated set dressing.</summary>
public partial class ArenaEnvironment : Node3D
{
    private readonly List<Node3D> _crystals = new();
    private float _time;

    public override void _Ready()
    {
        BuildBoundary();
        BuildAetherCrystals();
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        for (int i = 0; i < _crystals.Count; i++)
        {
            Node3D crystal = _crystals[i];
            float phase = _time * 1.25f + i * 0.8f;
            crystal.Position = new Vector3(crystal.Position.X, 1.45f + Mathf.Sin(phase) * 0.18f, crystal.Position.Z);
            crystal.Rotation = new Vector3(0.12f, phase * 0.35f, Mathf.Sin(phase * 0.7f) * 0.08f);
        }
    }

    private void BuildBoundary()
    {
        var wallMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.48f, 0.54f, 0.64f),
            AlbedoTexture = GD.Load<Texture2D>("res://assets/textures/arena_rune_stone.png"),
            Roughness = 0.94f,
            Metallic = 0.06f,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic,
            Uv1Scale = new Vector3(5f, 1f, 1f),
        };

        CreateWall(new Vector3(0f, 0f, -24f), new Vector3(48f, 1.4f, 1f), wallMaterial);
        CreateWall(new Vector3(0f, 0f, 24f), new Vector3(48f, 1.4f, 1f), wallMaterial);
        CreateWall(new Vector3(-24f, 0f, 0f), new Vector3(1f, 1.4f, 48f), wallMaterial);
        CreateWall(new Vector3(24f, 0f, 0f), new Vector3(1f, 1.4f, 48f), wallMaterial);
    }

    private void CreateWall(Vector3 position, Vector3 size, StandardMaterial3D material)
    {
        var body = new StaticBody3D
        {
            Position = position,
            CollisionLayer = PhysicsLayers.World,
            CollisionMask = 0,
        };
        AddChild(body);

        var mesh = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = size },
            Position = new Vector3(0f, size.Y * 0.5f, 0f),
        };
        mesh.SetSurfaceOverrideMaterial(0, material);
        body.AddChild(mesh);

        var collision = new CollisionShape3D
        {
            Shape = new BoxShape3D { Size = size },
            Position = mesh.Position,
        };
        body.AddChild(collision);
    }

    private void BuildAetherCrystals()
    {
        var crystalMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.12f, 0.52f, 0.88f),
            Metallic = 0.3f,
            Roughness = 0.22f,
            EmissionEnabled = true,
            Emission = new Color(0.03f, 0.35f, 1f),
            EmissionEnergyMultiplier = 3.2f,
        };

        const int count = 8;
        for (int i = 0; i < count; i++)
        {
            float angle = Mathf.Tau * i / count + Mathf.Pi / 8f;
            var anchor = new Node3D
            {
                Position = new Vector3(Mathf.Cos(angle) * 19f, 0f, Mathf.Sin(angle) * 19f),
            };
            AddChild(anchor);

            var crystal = new MeshInstance3D
            {
                Mesh = new CylinderMesh { TopRadius = 0.06f, BottomRadius = 0.38f, Height = 2.1f, RadialSegments = 6 },
                Position = new Vector3(0f, 1.45f, 0f),
                Rotation = new Vector3(0.12f, angle, 0.08f),
            };
            crystal.SetSurfaceOverrideMaterial(0, crystalMaterial);
            anchor.AddChild(crystal);
            _crystals.Add(crystal);

            var baseMesh = new MeshInstance3D
            {
                Mesh = new CylinderMesh { TopRadius = 0.5f, BottomRadius = 0.72f, Height = 0.32f, RadialSegments = 8 },
                Position = new Vector3(0f, 0.16f, 0f),
            };
            baseMesh.SetSurfaceOverrideMaterial(0, crystalMaterial);
            anchor.AddChild(baseMesh);

            if (i % 2 == 0)
            {
                anchor.AddChild(new OmniLight3D
                {
                    Position = new Vector3(0f, 1.8f, 0f),
                    LightColor = new Color(0.08f, 0.42f, 1f),
                    LightEnergy = 1.7f,
                    OmniRange = 5.5f,
                    ShadowEnabled = false,
                });
            }
        }
    }
}
