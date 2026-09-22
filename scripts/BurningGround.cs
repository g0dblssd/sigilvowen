using Godot;

namespace Sigilwoven;

public partial class BurningGround : Area3D
{
    public float Dps = 12f;
    public float Duration = 1.5f;
    public float Radius = 3f;
    public ResolvedCast? Resolved;

    private float _tick;

    public void Configure(float dps, float duration, float radius, ResolvedCast? resolved)
    {
        Dps = dps;
        Duration = duration;
        Radius = radius;
        Resolved = resolved;
    }

    public override void _Ready()
    {
        Monitoring = true;
        Monitorable = false;
        CollisionLayer = 0;
        CollisionMask = PhysicsLayers.Enemy;

        var col = new CollisionShape3D
        {
            Shape = new CylinderShape3D { Height = 1f, Radius = Radius }
        };
        col.Position = new Vector3(0, 0.5f, 0);
        AddChild(col);

        var mat = new StandardMaterial3D
        {
            AlbedoColor = new Color(1f, 0.45f, 0.1f, 0.55f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            EmissionEnabled = true,
            Emission = new Color(1f, 0.3f, 0.05f),
            EmissionEnergyMultiplier = 1.5f,
        };
        var disc = new CylinderMesh
        {
            TopRadius = Radius,
            BottomRadius = Radius,
            Height = 0.15f,
        };
        var mesh = new MeshInstance3D { Mesh = disc };
        mesh.SetSurfaceOverrideMaterial(0, mat);
        mesh.Position = new Vector3(0, 0.1f, 0);
        AddChild(mesh);
    }

    public override void _Process(double delta)
    {
        Duration -= (float)delta;
        if (Duration <= 0f)
        {
            QueueFree();
            return;
        }
        _tick -= (float)delta;
        if (_tick <= 0f)
        {
            _tick = 0.25f;
            foreach (var body in GetOverlappingBodies())
            {
                if (body is Enemy e && IsInstanceValid(e) && !e.IsQueuedForDeletion())
                {
                    e.TakeDamage(Dps * 0.25f, DamageElement.Fire, Resolved);
                }
            }
        }
    }
}
