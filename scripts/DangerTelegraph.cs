using Godot;

namespace Sigilwoven;

/// <summary>Shared visual language for avoidable enemy attacks.</summary>
public static class DangerTelegraph
{
    private static StandardMaterial3D MakeMaterial(Color color)
    {
        return new StandardMaterial3D
        {
            AlbedoColor = new Color(color.R, color.G, color.B, 0.24f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            EmissionEnabled = true,
            Emission = color,
            EmissionEnergyMultiplier = 2.8f,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };
    }

    public static void SpawnCircle(Node scene, Vector3 position, float radius, float duration, Color? tint = null)
    {
        Color color = tint ?? new Color(1f, 0.08f, 0.025f);
        var root = new Node3D();
        scene.AddChild(root);
        root.GlobalPosition = position + new Vector3(0f, 0.08f, 0f);
        var ring = new MeshInstance3D
        {
            Mesh = new TorusMesh { InnerRadius = radius * 0.82f, OuterRadius = radius },
            Scale = Vector3.One * 0.12f,
        };
        ring.SetSurfaceOverrideMaterial(0, MakeMaterial(color));
        root.AddChild(ring);
        Tween tween = root.CreateTween();
        tween.TweenProperty(ring, "scale", Vector3.One, duration).SetTrans(Tween.TransitionType.Linear);
        tween.TweenCallback(Callable.From(root.QueueFree));
    }

    public static void SpawnLine(Node scene, Vector3 from, Vector3 to, float width, float duration, Color? tint = null)
    {
        Vector3 flatFrom = new(from.X, 0.09f, from.Z);
        Vector3 flatTo = new(to.X, 0.09f, to.Z);
        Vector3 delta = flatTo - flatFrom;
        if (delta.LengthSquared() < 0.01f) return;
        Color color = tint ?? new Color(0.78f, 0.16f, 1f);
        var line = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(width, 0.035f, delta.Length()) },
            Scale = new Vector3(0.15f, 1f, 1f),
        };
        line.SetSurfaceOverrideMaterial(0, MakeMaterial(color));
        scene.AddChild(line);
        line.GlobalPosition = flatFrom.Lerp(flatTo, 0.5f);
        line.LookAt(flatTo, Vector3.Up, true);
        Tween tween = line.CreateTween();
        tween.TweenProperty(line, "scale", Vector3.One, duration).SetTrans(Tween.TransitionType.Linear);
        tween.TweenCallback(Callable.From(line.QueueFree));
    }
}
