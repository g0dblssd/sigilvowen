using Godot;

namespace Sigilwoven;

/// <summary>Short, code-built spell animations. Linked effects add a second visible element.</summary>
public static class SkillVfx
{
    public static void SpawnCastRing(Node scene, Vector3 position, DamageElement element, float size)
    {
        var node = new Node3D();
        scene.AddChild(node);
        node.GlobalPosition = position + new Vector3(0f, 0.06f, 0f);
        var ring = new MeshInstance3D { Mesh = new TorusMesh { InnerRadius = 0.35f, OuterRadius = 0.52f } };
        ring.SetSurfaceOverrideMaterial(0, MakeMaterial(element));
        node.AddChild(ring);
        node.Scale = Vector3.One * 0.12f * size;
        var tween = node.CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(node, "scale", Vector3.One * size * 2.2f, 0.28f);
        tween.TweenProperty(node, "rotation:y", Mathf.Tau * 1.3f, 0.28f);
        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(node.QueueFree));
    }

    public static void SpawnImpact(Node scene, Vector3 position, DamageElement element, bool linked)
    {
        var node = new Node3D();
        scene.AddChild(node);
        node.GlobalPosition = position;
        var flare = new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.12f, BottomRadius = 0.82f, Height = 3.4f } };
        flare.SetSurfaceOverrideMaterial(0, MakeMaterial(element));
        node.AddChild(flare);
        node.Scale = new Vector3(1f, 0.05f, 1f);
        var tween = node.CreateTween();
        tween.TweenProperty(node, "scale", new Vector3(1.2f, 1.1f, 1.2f), 0.13f);
        tween.TweenProperty(node, "scale", new Vector3(2.1f, 0.05f, 2.1f), 0.27f);
        tween.TweenCallback(Callable.From(node.QueueFree));
        if (linked)
        {
            // Blue-yellow sparks make a linked meteor / chained area visibly different.
            SpawnLightningArcs(scene, position, 5);
        }
    }

    public static void SpawnSummon(Node scene, Vector3 position, DamageElement element, bool linked)
    {
        SpawnCastRing(scene, position, element, 1.6f);
        if (linked)
        {
            SpawnLightningArcs(scene, position, 4);
        }
    }

    public static void SpawnDashTrail(Node scene, Vector3 from, Vector3 to, DamageElement element, bool lightningLinked)
    {
        Vector3 delta = to - from;
        float distance = delta.Length();
        int marks = Mathf.Clamp(Mathf.CeilToInt(distance / 1.1f), 2, 7);
        for (int i = 0; i <= marks; i++)
        {
            float progress = i / (float)marks;
            Vector3 point = from.Lerp(to, progress);
            SpawnCastRing(scene, point, element, 0.65f + progress * 0.55f);
        }
        SpawnImpact(scene, to, element, lightningLinked);
        if (lightningLinked)
        {
            SpawnLightningArcs(scene, to, 6);
        }
    }

    public static void SpawnLightningArcs(Node scene, Vector3 position, int count)
    {
        var material = MakeMaterial(DamageElement.Lightning);
        for (int i = 0; i < count; i++)
        {
            float angle = Mathf.Tau * i / count;
            var spark = new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.09f, 1.6f, 0.09f) } };
            spark.SetSurfaceOverrideMaterial(0, material);
            scene.AddChild(spark);
            spark.GlobalPosition = position + new Vector3(Mathf.Cos(angle) * 1.1f, 0.8f, Mathf.Sin(angle) * 1.1f);
            spark.Rotation = new Vector3(Mathf.Sin(angle) * 0.45f, angle, Mathf.Cos(angle) * 0.45f);
            var tween = spark.CreateTween();
            tween.TweenProperty(spark, "scale", Vector3.One * 0.1f, 0.22f);
            tween.TweenCallback(Callable.From(spark.QueueFree));
        }
    }

    private static StandardMaterial3D MakeMaterial(DamageElement element)
    {
        Color color = element switch
        {
            DamageElement.Fire => new Color(1f, 0.18f, 0.03f),
            DamageElement.Cold => new Color(0.25f, 0.82f, 1f),
            DamageElement.Lightning => new Color(0.95f, 0.92f, 0.12f),
            DamageElement.Poison => new Color(0.2f, 1f, 0.3f),
            _ => new Color(0.85f, 0.72f, 0.5f),
        };
        return new StandardMaterial3D { AlbedoColor = color, EmissionEnabled = true, Emission = color, EmissionEnergyMultiplier = 3.4f };
    }
}
