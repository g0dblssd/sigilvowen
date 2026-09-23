using Godot;

namespace Sigilwoven;

/// <summary>Short-lived corpse marker that a Necromancer can raise.</summary>
public partial class FallenRemnant : Node3D
{
    private float _lifetime = 9f;
    private MeshInstance3D? _sigil;
    private bool _consumed;

    public override void _Ready()
    {
        AddToGroup("fallen_remnants");
        var material = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.12f, 0.55f, 0.18f, 0.48f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            EmissionEnabled = true,
            Emission = new Color(0.04f, 0.8f, 0.16f),
            EmissionEnergyMultiplier = 2.2f,
        };
        _sigil = new MeshInstance3D
        {
            Mesh = new TorusMesh { InnerRadius = 0.42f, OuterRadius = 0.56f },
            Position = new Vector3(0f, 0.06f, 0f),
        };
        _sigil.SetSurfaceOverrideMaterial(0, material);
        AddChild(_sigil);
    }

    public override void _Process(double delta)
    {
        if (_consumed) return;
        float step = (float)delta;
        _lifetime -= step;
        _sigil?.RotateY(step * 2.2f);
        if (_lifetime <= 0f) QueueFree();
    }

    public void Resurrect(PlayerController target)
    {
        if (_consumed || !IsInstanceValid(target)) return;
        _consumed = true;
        Node? scene = GetTree().CurrentScene;
        if (scene != null)
        {
            var risen = new Enemy();
            risen.Configure(42f, false, EnemyArchetype.Raider);
            scene.AddChild(risen);
            risen.GlobalPosition = GlobalPosition;
            risen.JoinAmbush(target);
            SkillVfx.SpawnImpact(scene, GlobalPosition + Vector3.Up * 0.5f, DamageElement.Poison, true);
        }
        QueueFree();
    }
}
