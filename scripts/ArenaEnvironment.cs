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
        BuildDarkFantasySetDressing();
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

        CreateWall(new Vector3(0f, 0f, -64f), new Vector3(128f, 1.4f, 1f), wallMaterial);
        CreateWall(new Vector3(0f, 0f, 64f), new Vector3(128f, 1.4f, 1f), wallMaterial);
        CreateWall(new Vector3(-64f, 0f, 0f), new Vector3(1f, 1.4f, 128f), wallMaterial);
        CreateWall(new Vector3(64f, 0f, 0f), new Vector3(1f, 1.4f, 128f), wallMaterial);
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

        const int count = 12;
        for (int i = 0; i < count; i++)
        {
            float angle = Mathf.Tau * i / count + Mathf.Pi / 8f;
            var anchor = new Node3D
            {
                Position = new Vector3(Mathf.Cos(angle) * 52f, 0f, Mathf.Sin(angle) * 52f),
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

    private void BuildDarkFantasySetDressing()
    {
        var rng = new RandomNumberGenerator { Seed = 45045 };
        var bark = new StandardMaterial3D { AlbedoColor = new Color(0.055f, 0.042f, 0.048f), Roughness = 1f };
        var stone = new StandardMaterial3D { AlbedoColor = new Color(0.16f, 0.17f, 0.2f), Roughness = 0.96f };
        var ember = new StandardMaterial3D { AlbedoColor = new Color(0.45f, 0.08f, 0.025f), EmissionEnabled = true, Emission = new Color(1f, 0.12f, 0.015f), EmissionEnergyMultiplier = 4f };

        AddImportedDecor("crypt-large.glb", new Vector3(-18f, 0f, 35f), 2.3f, 18f);
        AddImportedDecor("crypt-large.glb", new Vector3(39f, 0f, 8f), 1.9f, -74f);
        AddImportedDecor("altar-stone.glb", new Vector3(-8f, 0f, 11f), 1.35f, 15f);
        AddImportedDecor("coffin-old.glb", new Vector3(-12f, 0f, 18f), 1.2f, 38f);
        AddImportedDecor("pillar-obelisk.glb", new Vector3(12f, 0f, 14f), 1.8f, 0f);
        AddImportedDecor("iron-fence-damaged.glb", new Vector3(23f, 0f, 17f), 1.6f, 66f);
        AddImportedDecor("pine-crooked.glb", new Vector3(-38f, 0f, -16f), 2.1f, -28f);
        AddImportedDecor("pine-crooked.glb", new Vector3(43f, 0f, -34f), 1.8f, 72f);
        AddImportedDecor("rocks-tall.glb", new Vector3(-29f, 0f, -36f), 2f, 20f);

        for (int i = 0; i < 28; i++)
        {
            float angle = rng.RandfRange(0f, Mathf.Tau);
            float radius = rng.RandfRange(30f, 59f);
            var tree = new Node3D { Position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius), Rotation = new Vector3(0f, rng.RandfRange(0f, Mathf.Tau), rng.RandfRange(-0.11f, 0.11f)) };
            AddChild(tree);
            var trunk = new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.12f, BottomRadius = 0.38f, Height = rng.RandfRange(3.4f, 6.2f), RadialSegments = 7 }, Position = new Vector3(0f, 2.2f, 0f) }; trunk.SetSurfaceOverrideMaterial(0, bark); tree.AddChild(trunk);
            for (int branch = 0; branch < 3; branch++)
            {
                float side = branch % 2 == 0 ? 1f : -1f;
                var limb = new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.035f, BottomRadius = 0.11f, Height = rng.RandfRange(1.7f, 2.6f), RadialSegments = 6 }, Position = new Vector3(side * 0.55f, 3.4f + branch * 0.48f, 0f), RotationDegrees = new Vector3(0f, branch * 48f, side * 58f) }; limb.SetSurfaceOverrideMaterial(0, bark); tree.AddChild(limb);
            }
        }

        for (int i = 0; i < 42; i++)
        {
            float angle = rng.RandfRange(0f, Mathf.Tau);
            float radius = rng.RandfRange(16f, 57f);
            var grave = new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(rng.RandfRange(0.34f, 0.6f), rng.RandfRange(0.8f, 1.35f), 0.18f) }, Position = new Vector3(Mathf.Cos(angle) * radius, 0.52f, Mathf.Sin(angle) * radius), RotationDegrees = new Vector3(rng.RandfRange(-8f, 8f), Mathf.RadToDeg(angle) + rng.RandfRange(-25f, 25f), rng.RandfRange(-9f, 9f)) };
            grave.SetSurfaceOverrideMaterial(0, stone); AddChild(grave);
        }

        Vector3[] ruins = { new(-23f, 0f, -18f), new(24f, 0f, -26f), new(-32f, 0f, 22f), new(31f, 0f, 26f) };
        foreach (Vector3 origin in ruins)
        {
            var arch = new Node3D { Position = origin, RotationDegrees = new Vector3(0f, rng.RandfRange(0f, 360f), 0f) }; AddChild(arch);
            for (int side = -1; side <= 1; side += 2)
            {
                var column = new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.75f, 4.6f, 0.85f) }, Position = new Vector3(side * 1.65f, 2.3f, 0f) }; column.SetSurfaceOverrideMaterial(0, stone); arch.AddChild(column);
            }
            var lintel = new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(4.1f, 0.72f, 0.9f) }, Position = new Vector3(0f, 4.45f, 0f) }; lintel.SetSurfaceOverrideMaterial(0, stone); arch.AddChild(lintel);
        }

        for (int i = 0; i < 8; i++)
        {
            float angle = Mathf.Tau * i / 8f;
            var brazier = new Node3D { Position = new Vector3(Mathf.Cos(angle) * 13.5f, 0f, Mathf.Sin(angle) * 13.5f) }; AddChild(brazier);
            var bowl = new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.48f, BottomRadius = 0.28f, Height = 0.32f, RadialSegments = 8 }, Position = new Vector3(0f, 1f, 0f) }; bowl.SetSurfaceOverrideMaterial(0, stone); brazier.AddChild(bowl);
            var flame = new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.24f, Height = 0.65f }, Position = new Vector3(0f, 1.35f, 0f), Scale = new Vector3(0.7f, 1.3f, 0.7f) }; flame.SetSurfaceOverrideMaterial(0, ember); brazier.AddChild(flame);
            brazier.AddChild(new OmniLight3D { Position = new Vector3(0f, 1.5f, 0f), LightColor = new Color(1f, 0.2f, 0.04f), LightEnergy = 1.8f, OmniRange = 5f, ShadowEnabled = i % 2 == 0 });
        }
    }

    private void AddImportedDecor(string file, Vector3 position, float scale, float yaw)
    {
        PackedScene? scene = GD.Load<PackedScene>($"res://assets/models/kenney_graveyard/{file}");
        if (scene?.Instantiate() is not Node3D model) return;
        model.Position = position;
        model.Scale = Vector3.One * scale;
        model.RotationDegrees = new Vector3(0f, yaw, 0f);
        AddChild(model);
    }
}
