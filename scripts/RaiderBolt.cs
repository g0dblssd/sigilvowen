using Godot;

namespace Sigilwoven;

/// <summary>Simple enemy projectile so ranged pressure accompanies the melee pack.</summary>
public partial class RaiderBolt : Node3D
{
    private PlayerController? _target;
    private float _damage;
    private float _life = 3f;
    private Vector3 _direction;
    private bool _resolved;

    public void Configure(PlayerController target, float damage)
    {
        _target = target;
        _damage = damage;
    }

    public override void _Ready()
    {
        var mat = new StandardMaterial3D { AlbedoColor = new Color(0.7f, 0.12f, 1f), EmissionEnabled = true, Emission = new Color(0.5f, 0.02f, 0.9f), EmissionEnergyMultiplier = 3f };
        var mesh = new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.22f, Height = 0.44f } };
        mesh.SetSurfaceOverrideMaterial(0, mat);
        AddChild(mesh);
        var light = new OmniLight3D { LightColor = new Color(0.7f, 0.15f, 1f), LightEnergy = 1.2f, OmniRange = 3f };
        AddChild(light);
        if (_target != null)
        {
            _direction = (_target.GlobalPosition + new Vector3(0f, 0.8f, 0f) - GlobalPosition).Normalized();
        }
    }

    public override void _Process(double delta)
    {
        float step = (float)delta;
        GlobalPosition += _direction * 10f * step;
        RotateY(8f * step);
        _life -= step;
        if (!_resolved && _target != null && IsInstanceValid(_target) && !_target.IsQueuedForDeletion() && GlobalPosition.DistanceTo(_target.GlobalPosition + new Vector3(0f, 0.8f, 0f)) < 0.7f)
        {
            _resolved = true;
            _target.TakeDamage(_damage, "SHADOW BOLT");
            var scene = GetTree().CurrentScene;
            if (scene != null)
            {
                SkillVfx.SpawnLightningArcs(scene, GlobalPosition, 3);
            }
            QueueFree();
        }
        else if (_life <= 0f)
        {
            QueueFree();
        }
    }
}
