using Godot;

namespace Sigilwoven;

public partial class Main : Node3D
{
    public override void _Ready()
    {
        GD.Print("[Sigilwoven] Building world...");

        var sun = new DirectionalLight3D();
        sun.RotationDegrees = new Vector3(-50f, -30f, 0f);
        sun.LightEnergy = 1.1f;
        sun.ShadowEnabled = true;
        sun.DirectionalShadowMode = DirectionalLight3D.ShadowMode.Parallel4Splits;
        AddChild(sun);

        var env = new WorldEnvironment();
        var environment = new Environment();
        environment.BackgroundMode = Environment.BGMode.Sky;
        var skyMat = new ProceduralSkyMaterial();
        var sky = new Sky();
        sky.SkyMaterial = skyMat;
        environment.Sky = sky;
        environment.AmbientLightSource = Environment.AmbientSource.Sky;
        environment.AmbientLightEnergy = 0.6f;
        env.Environment = environment;
        AddChild(env);

        var groundBody = new StaticBody3D();
        groundBody.CollisionLayer = 1;
        groundBody.CollisionMask = 0;
        AddChild(groundBody);

        var groundMat = new StandardMaterial3D { AlbedoColor = new Color(0.16f, 0.18f, 0.23f), Metallic = 0.15f, Roughness = 0.85f };
        var groundMesh = new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(60f, 1f, 60f) } };
        groundMesh.SetSurfaceOverrideMaterial(0, groundMat);
        groundMesh.Position = new Vector3(0, -0.5f, 0);
        groundBody.AddChild(groundMesh);

        var groundCol = new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(60f, 1f, 60f) } };
        groundCol.Position = new Vector3(0, -0.5f, 0);
        groundBody.AddChild(groundCol);

        BuildArenaMarkers();

        // A soft focal light gives the spawn point a readable silhouette even
        // before the first spell is cast.
        var arenaLight = new OmniLight3D
        {
            LightColor = new Color(0.18f, 0.55f, 1f),
            LightEnergy = 2.2f,
            OmniRange = 10f,
            ShadowEnabled = true,
        };
        arenaLight.Position = new Vector3(0f, 3.5f, 0f);
        AddChild(arenaLight);

        var player = new PlayerController();
        AddChild(player);
        player.Position = new Vector3(0, 0.2f, 0);

        var totem = new CrystalTotem();
        AddChild(totem);
        totem.Position = player.Position;

        // Permanent target for verifying casts and damage numbers before combat starts.
        var dummy = new Enemy();
        dummy.Configure(999999f, true);
        AddChild(dummy);
        dummy.Position = new Vector3(0f, 0f, -7f);

        var birth = new SpawnSequence();
        AddChild(birth);
        birth.Setup(player, this);

        GD.Print("[Sigilwoven] World ready.");
    }

    private void BuildArenaMarkers()
    {
        var runeMat = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.12f, 0.55f, 0.8f),
            EmissionEnabled = true,
            Emission = new Color(0.05f, 0.35f, 0.75f),
            EmissionEnergyMultiplier = 1.4f,
        };
        for (int i = 0; i < 8; i++)
        {
            float angle = Mathf.Tau * i / 8f;
            var rune = new MeshInstance3D { Mesh = new TorusMesh { InnerRadius = 0.75f, OuterRadius = 0.88f } };
            rune.SetSurfaceOverrideMaterial(0, runeMat);
            rune.Position = new Vector3(Mathf.Cos(angle) * 11f, 0.03f, Mathf.Sin(angle) * 11f);
            AddChild(rune);
        }

        var centerMat = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.07f, 0.18f, 0.3f, 0.8f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            EmissionEnabled = true,
            Emission = new Color(0.03f, 0.25f, 0.65f),
            EmissionEnergyMultiplier = 1.8f,
        };
        var center = new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 3.8f, BottomRadius = 3.8f, Height = 0.08f },
            Position = new Vector3(0f, 0.04f, 0f),
        };
        center.SetSurfaceOverrideMaterial(0, centerMat);
        AddChild(center);
        var innerRing = new MeshInstance3D
        {
            Mesh = new TorusMesh { InnerRadius = 2.7f, OuterRadius = 2.82f },
            Position = new Vector3(0f, 0.1f, 0f),
        };
        innerRing.SetSurfaceOverrideMaterial(0, runeMat);
        AddChild(innerRing);

        var pillarMat = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.22f, 0.24f, 0.31f),
            EmissionEnabled = true,
            Emission = new Color(0.12f, 0.3f, 0.55f),
            EmissionEnergyMultiplier = 0.7f,
        };
        Vector3[] pillars = { new(-14f, 0f, -14f), new(14f, 0f, -14f), new(-14f, 0f, 14f), new(14f, 0f, 14f) };
        foreach (var position in pillars)
        {
            var pillar = new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.6f, BottomRadius = 0.9f, Height = 5f } };
            pillar.SetSurfaceOverrideMaterial(0, pillarMat);
            pillar.Position = position + new Vector3(0f, 2.5f, 0f);
            AddChild(pillar);
            var beacon = new OmniLight3D { LightColor = new Color(0.12f, 0.4f, 0.9f), LightEnergy = 1.1f, OmniRange = 5f };
            beacon.Position = position + new Vector3(0f, 4.2f, 0f);
            AddChild(beacon);
        }
    }
}
