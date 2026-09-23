using Godot;

namespace Sigilwoven;

/// <summary>Targetable ward focus. Destroying all three breaks the Warden aura.</summary>
public partial class WardenPylon : CharacterBody3D
{
    private Enemy? _warden;
    private float _maxHealth;
    private float _health;
    private int _index;
    private Node3D? _rotor;
    private MeshInstance3D? _core;
    private Label3D? _label;
    private float _age;
    private bool _destroyed;

    public void Configure(Enemy warden, float health, int index)
    {
        _warden = warden;
        _maxHealth = Mathf.Max(1f, health);
        _health = _maxHealth;
        _index = index;
        Name = $"WardPylon{index}";
    }

    public override void _Ready()
    {
        CollisionLayer = PhysicsLayers.Enemy;
        CollisionMask = 0;
        AddToGroup("warden_pylons");

        var metal = RealisticMaterialCatalog.ForgedIron();
        var ward = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.08f, 1f, 0.58f),
            EmissionEnabled = true,
            Emission = new Color(0.02f, 0.95f, 0.45f),
            EmissionEnergyMultiplier = 4.4f,
            Metallic = 0.35f,
            Roughness = 0.18f,
        };

        var baseMesh = new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.38f, BottomRadius = 0.52f, Height = 0.28f, RadialSegments = 8 },
            Position = new Vector3(0f, 0.14f, 0f),
        };
        baseMesh.SetSurfaceOverrideMaterial(0, metal);
        AddChild(baseMesh);

        _core = new MeshInstance3D
        {
            Mesh = new PrismMesh { Size = new Vector3(0.42f, 1.25f, 0.42f) },
            Position = new Vector3(0f, 0.88f, 0f),
            Rotation = new Vector3(0.08f, _index * 0.35f, -0.05f),
        };
        _core.SetSurfaceOverrideMaterial(0, ward);
        AddChild(_core);

        _rotor = new Node3D { Position = new Vector3(0f, 0.82f, 0f) };
        AddChild(_rotor);
        for (int i = 0; i < 4; i++)
        {
            float angle = Mathf.Tau * i / 4f;
            var fin = new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(0.08f, 0.48f, 0.5f) },
                Position = new Vector3(Mathf.Cos(angle) * 0.36f, 0f, Mathf.Sin(angle) * 0.36f),
                Rotation = new Vector3(0f, -angle, 0.28f),
            };
            fin.SetSurfaceOverrideMaterial(0, metal);
            _rotor.AddChild(fin);
        }
        for (int i = 0; i < 2; i++)
        {
            var ring = new MeshInstance3D
            {
                Mesh = new TorusMesh { InnerRadius = 0.5f + i * 0.12f, OuterRadius = 0.56f + i * 0.12f },
                Position = new Vector3(0f, 0.76f + i * 0.34f, 0f),
                Rotation = new Vector3(i * 0.65f, 0f, i == 0 ? 0.18f : -0.28f),
            };
            ring.SetSurfaceOverrideMaterial(0, ward);
            AddChild(ring);
        }

        AddChild(new OmniLight3D
        {
            Position = new Vector3(0f, 1f, 0f),
            LightColor = new Color(0.08f, 1f, 0.5f),
            LightEnergy = 1.7f,
            OmniRange = 4.2f,
        });
        AddChild(new CollisionShape3D
        {
            Shape = new CylinderShape3D { Radius = 0.55f, Height = 1.55f },
            Position = new Vector3(0f, 0.78f, 0f),
        });
        _label = new Label3D
        {
            Text = $"WARD PYLON {_index}  {_health:0}/{_maxHealth:0}",
            Position = new Vector3(0f, 1.72f, 0f),
            FontSize = 24,
            OutlineSize = 6,
            Modulate = new Color(0.25f, 1f, 0.62f),
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
        };
        AddChild(_label);
    }

    public override void _Process(double delta)
    {
        _age += (float)delta;
        _rotor?.RotateY((float)delta * (1.7f + _index * 0.18f));
        if (_core != null)
        {
            float pulse = 0.92f + Mathf.Sin(_age * 4.5f + _index) * 0.09f;
            _core.Scale = Vector3.One * pulse;
        }
    }

    public void TakeDamage(float amount, DamageElement element, ResolvedCast? resolved = null)
    {
        if (_destroyed || float.IsNaN(amount) || float.IsInfinity(amount)) return;
        _health -= Mathf.Max(0f, amount);
        if (_label != null) _label.Text = $"WARD PYLON {_index}  {Mathf.Max(0f, _health):0}/{_maxHealth:0}";
        Node? scene = GetTree().CurrentScene;
        if (scene != null) SkillVfx.SpawnImpact(scene, GlobalPosition + Vector3.Up * 0.8f, element, resolved?.IsCritical == true);
        if (_health > 0f) return;
        _destroyed = true;
        _warden?.NotifyWardenPylonDestroyed();
        if (scene != null) SkillVfx.SpawnImpact(scene, GlobalPosition + Vector3.Up * 0.8f, DamageElement.Lightning, true);
        QueueFree();
    }
}
