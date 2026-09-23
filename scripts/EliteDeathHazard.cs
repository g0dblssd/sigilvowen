using Godot;

namespace Sigilwoven;

/// <summary>Readable delayed elite death explosion that rewards moving after a kill.</summary>
public partial class EliteDeathHazard : Node3D
{
    private float _radius;
    private float _delay;
    private float _damage;
    private float _elapsed;
    private bool _resolved;
    private MeshInstance3D? _disc;

    public void Configure(float radius, float delay, float damage)
    {
        _radius = radius;
        _delay = delay;
        _damage = damage;
    }

    public override void _Ready()
    {
        var material = new StandardMaterial3D
        {
            AlbedoColor = new Color(1f, 0.08f, 0.01f, 0.28f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            EmissionEnabled = true, Emission = new Color(1f, 0.04f, 0.005f), EmissionEnergyMultiplier = 3.8f,
        };
        _disc = new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = _radius, BottomRadius = _radius, Height = 0.08f }, Position = new Vector3(0f, 0.06f, 0f), Scale = Vector3.One * 0.12f };
        _disc.SetSurfaceOverrideMaterial(0, material);
        AddChild(_disc);
        AddChild(new OmniLight3D { LightColor = new Color(1f, 0.12f, 0.02f), LightEnergy = 2.5f, OmniRange = _radius * 1.8f, Position = Vector3.Up * 0.4f });
    }

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        float progress = Mathf.Clamp(_elapsed / Mathf.Max(0.05f, _delay), 0f, 1f);
        if (_disc != null)
        {
            float pulse = 0.9f + Mathf.Sin(_elapsed * 22f) * 0.08f;
            _disc.Scale = Vector3.One * Mathf.Lerp(0.12f, 1f, progress) * pulse;
        }
        if (!_resolved && _elapsed >= _delay)
        {
            _resolved = true;
            foreach (Node node in GetTree().GetNodesInGroup("player"))
            {
                if (node is PlayerController player && IsInstanceValid(player) && player.GlobalPosition.DistanceTo(GlobalPosition) <= _radius)
                    player.TakeDamage(_damage, "MOLTEN DEATH", DamageElement.Fire, 55f);
            }
            Node? scene = GetTree().CurrentScene;
            if (scene != null) SkillVfx.SpawnImpact(scene, GlobalPosition + Vector3.Up * 0.25f, DamageElement.Fire, true);
            GetTree().CreateTimer(0.22f).Timeout += QueueFree;
        }
    }
}
