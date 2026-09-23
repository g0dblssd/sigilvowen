using Godot;

namespace Sigilwoven;

/// <summary>Runtime-only ritual focal point: crystal, light and orbiting sigils.</summary>
public partial class CrystalTotem : Node3D
{
    private Node3D? _sigilRing;
    private OmniLight3D? _light;
    private float _age;

    public override void _Ready()
    {
        var crystalMat = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.18f, 0.75f, 1f),
            EmissionEnabled = true,
            Emission = new Color(0.05f, 0.65f, 1f),
            EmissionEnergyMultiplier = 3.2f,
        };
        var crystal = new MeshInstance3D { Mesh = new PrismMesh { Size = new Vector3(1.1f, 3.4f, 1.1f) }, Rotation = new Vector3(0.03f, 0.18f, -0.04f) };
        crystal.SetSurfaceOverrideMaterial(0, crystalMat);
        crystal.Position = new Vector3(0, 1.7f, 0);
        AddChild(crystal);

        var forgedMat = RealisticMaterialCatalog.ForgedIron();
        for (int tier = 0; tier < 3; tier++)
        {
            var baseRing = new MeshInstance3D
            {
                Mesh = new CylinderMesh { TopRadius = 1.05f - tier * 0.18f, BottomRadius = 1.2f - tier * 0.18f, Height = 0.22f, RadialSegments = 10 },
                Position = new Vector3(0f, 0.11f + tier * 0.2f, 0f),
            };
            baseRing.SetSurfaceOverrideMaterial(0, forgedMat);
            AddChild(baseRing);
        }
        for (int i = 0; i < 4; i++)
        {
            float angle = Mathf.Tau * i / 4f;
            var claw = new MeshInstance3D
            {
                Mesh = new PrismMesh { Size = new Vector3(0.22f, 1.35f, 0.28f) },
                Position = new Vector3(Mathf.Cos(angle) * 0.78f, 0.92f, Mathf.Sin(angle) * 0.78f),
                Rotation = new Vector3(0.22f, -angle, 0.18f),
            };
            claw.SetSurfaceOverrideMaterial(0, forgedMat);
            AddChild(claw);
        }

        _light = new OmniLight3D { LightColor = new Color(0.2f, 0.75f, 1f), LightEnergy = 5f, OmniRange = 11f };
        _light.Position = new Vector3(0, 2f, 0);
        AddChild(_light);

        _sigilRing = new Node3D { Position = new Vector3(0, 0.2f, 0) };
        AddChild(_sigilRing);
        var sigilMat = new StandardMaterial3D { AlbedoColor = new Color(0.6f, 0.95f, 1f), EmissionEnabled = true, Emission = new Color(0.2f, 0.8f, 1f), EmissionEnergyMultiplier = 4f };
        for (int i = 0; i < 6; i++)
        {
            float angle = Mathf.Tau * i / 6f;
            var shard = new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.12f, Height = 0.24f } };
            shard.SetSurfaceOverrideMaterial(0, sigilMat);
            shard.Position = new Vector3(Mathf.Cos(angle) * 1.5f, 0.25f + (i % 2) * 0.32f, Mathf.Sin(angle) * 1.5f);
            _sigilRing.AddChild(shard);
        }
    }

    public override void _Process(double delta)
    {
        _age += (float)delta;
        if (_sigilRing != null)
        {
            _sigilRing.RotateY(1.5f * (float)delta);
            _sigilRing.Position = new Vector3(0, 0.25f + Mathf.Sin(_age * 2f) * 0.12f, 0);
        }
        if (_light != null)
        {
            _light.LightEnergy = 4.2f + Mathf.Sin(_age * 3.5f) * 1.4f;
        }
    }
}
