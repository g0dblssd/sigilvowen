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
        SpawnElementBurst(scene, position, element, linked ? 16 : 10);
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

    public static void SpawnSkillImpact(Node scene, SkillData skill, Vector3 position, ResolvedCast resolved)
    {
        switch (skill.Id)
        {
            case "meteor":
                SpawnMeteor(scene, position, resolved.Element);
                break;
            case "ice_nova":
                SpawnNova(scene, position, DamageElement.Cold, 18);
                break;
            case "firestorm":
                SpawnStorm(scene, position, DamageElement.Fire);
                break;
            case "thunderstorm":
                SpawnStorm(scene, position, DamageElement.Lightning);
                break;
            case "earthquake":
                SpawnEarthquake(scene, position);
                break;
            case "plague_nova":
            case "toxic_cloud":
                SpawnCloud(scene, position, DamageElement.Poison, 10);
                break;
            default:
                SpawnImpact(scene, position, resolved.Element, resolved.AppliedLinks.Count > 0);
                break;
        }
        SpawnLinkLayers(scene, position, resolved);
    }

    public static void SpawnProjectileImpact(Node scene, string skillId, Vector3 position, ResolvedCast resolved)
    {
        if (skillId == "frostbolt")
        {
            SpawnNova(scene, position, DamageElement.Cold, 8);
        }
        else if (skillId is "chain_lightning" or "arc_surge" or "spark_bolt")
        {
            SpawnLightningArcs(scene, position, skillId == "arc_surge" ? 8 : 5);
        }
        else if (skillId == "venom_fang")
        {
            SpawnCloud(scene, position, DamageElement.Poison, 5);
        }
        else
        {
            SpawnImpact(scene, position, resolved.Element, false);
        }
        SpawnLinkLayers(scene, position, resolved);
    }

    public static void SpawnProjectileTrail(Node scene, Vector3 position, DamageElement element, bool linked)
    {
        var mote = new MeshInstance3D
        {
            Mesh = element == DamageElement.Cold
                ? new PrismMesh { Size = new Vector3(0.08f, 0.2f, 0.08f) }
                : new SphereMesh { Radius = 0.07f, Height = 0.14f },
        };
        mote.SetSurfaceOverrideMaterial(0, MakeMaterial(element));
        scene.AddChild(mote);
        mote.GlobalPosition = position;
        mote.Scale = Vector3.One * (linked ? 1.35f : 1f);
        Tween tween = mote.CreateTween();
        tween.TweenProperty(mote, "scale", Vector3.One * 0.02f, 0.22f);
        tween.TweenCallback(Callable.From(mote.QueueFree));
    }

    public static void SpawnBeam(Node scene, Vector3 from, Vector3 to, DamageElement element, float width = 0.08f)
    {
        Vector3 delta = to - from;
        if (delta.LengthSquared() < 0.01f) return;
        var beam = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(width, width, delta.Length()) },
        };
        beam.SetSurfaceOverrideMaterial(0, MakeMaterial(element));
        scene.AddChild(beam);
        beam.GlobalPosition = from.Lerp(to, 0.5f);
        beam.LookAt(to, Vector3.Up, true);
        Tween tween = beam.CreateTween();
        tween.TweenProperty(beam, "scale", new Vector3(0.04f, 0.04f, 1f), 0.16f);
        tween.TweenCallback(Callable.From(beam.QueueFree));
    }

    public static void SpawnLinkLayers(Node scene, Vector3 position, ResolvedCast resolved)
    {
        foreach (string linkId in resolved.AppliedLinks)
        {
            switch (linkId)
            {
                case "lightning_form":
                case "lightning_imbuement":
                    SpawnLightningArcs(scene, position, 4);
                    break;
                case "fire_link":
                    SpawnRuneRing(scene, position, DamageElement.Fire, 1.35f);
                    SpawnElementBurst(scene, position, DamageElement.Fire, 7);
                    break;
                case "cold_touch":
                    SpawnNova(scene, position, DamageElement.Cold, 6);
                    break;
                case "venom_seal":
                    SpawnCloud(scene, position, DamageElement.Poison, 4);
                    break;
                case "execution_mark":
                    SpawnRuneRing(scene, position + new Vector3(0f, 1f, 0f), DamageElement.Physical, 0.75f);
                    break;
                case "chain_extension":
                    SpawnRuneRing(scene, position, DamageElement.Lightning, 1.7f);
                    break;
                case "stun_impacts":
                    SpawnShockwave(scene, position, new Color(1f, 0.82f, 0.25f));
                    break;
                case "persist_aura":
                    SpawnRuneRing(scene, position, resolved.Element, 2.1f);
                    break;
                case "aether_pulse_link":
                    SpawnRuneRing(scene, position, DamageElement.Lightning, 0.9f);
                    SpawnRuneRing(scene, position, DamageElement.Cold, 1.25f);
                    break;
            }
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

    private static void SpawnMeteor(Node scene, Vector3 position, DamageElement element)
    {
        SpawnRuneRing(scene, position, element, 2.4f);
        var meteor = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.62f, Height = 1.25f },
            Scale = new Vector3(0.8f, 1.35f, 0.8f),
        };
        meteor.SetSurfaceOverrideMaterial(0, MakeMaterial(element));
        scene.AddChild(meteor);
        meteor.GlobalPosition = position + new Vector3(0.8f, 8f, 0.6f);
        Tween fall = meteor.CreateTween();
        fall.TweenProperty(meteor, "global_position", position + new Vector3(0f, 0.55f, 0f), 0.22f)
            .SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.In);
        fall.TweenCallback(Callable.From(() =>
        {
            SpawnImpact(scene, position, element, false);
            meteor.QueueFree();
        }));
    }

    private static void SpawnNova(Node scene, Vector3 position, DamageElement element, int count)
    {
        var root = new Node3D { GlobalPosition = position };
        scene.AddChild(root);
        StandardMaterial3D material = MakeMaterial(element);
        for (int i = 0; i < count; i++)
        {
            float angle = Mathf.Tau * i / count;
            var spike = new MeshInstance3D
            {
                Mesh = new PrismMesh { Size = new Vector3(0.16f, 0.85f + GD.Randf() * 0.7f, 0.2f) },
                Position = new Vector3(Mathf.Cos(angle) * 0.45f, 0.2f, Mathf.Sin(angle) * 0.45f),
                Rotation = new Vector3(0.45f, -angle, 0f),
                Scale = Vector3.One * 0.08f,
            };
            spike.SetSurfaceOverrideMaterial(0, material);
            root.AddChild(spike);
            Tween shard = spike.CreateTween();
            shard.SetParallel(true);
            shard.TweenProperty(spike, "position", new Vector3(Mathf.Cos(angle) * 3.4f, 0.2f, Mathf.Sin(angle) * 3.4f), 0.28f);
            shard.TweenProperty(spike, "scale", Vector3.One, 0.12f);
            shard.SetParallel(false);
            shard.TweenProperty(spike, "scale", Vector3.One * 0.02f, 0.22f);
        }
        Tween cleanup = root.CreateTween();
        cleanup.TweenInterval(0.55f);
        cleanup.TweenCallback(Callable.From(root.QueueFree));
    }

    private static void SpawnStorm(Node scene, Vector3 position, DamageElement element)
    {
        for (int i = 0; i < 7; i++)
        {
            float angle = GD.Randf() * Mathf.Tau;
            float radius = GD.Randf() * 3.2f;
            Vector3 strike = position + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Vector3 sky = strike + new Vector3((float)GD.RandRange(-0.8f, 0.8f), 7f + GD.Randf() * 3f, (float)GD.RandRange(-0.8f, 0.8f));
            SpawnBeam(scene, sky, strike + new Vector3(0f, 0.15f, 0f), element, element == DamageElement.Lightning ? 0.11f : 0.22f);
            SpawnElementBurst(scene, strike, element, 4);
        }
    }

    private static void SpawnEarthquake(Node scene, Vector3 position)
    {
        SpawnShockwave(scene, position, new Color(0.85f, 0.55f, 0.22f));
        var material = MakeMaterial(DamageElement.Physical);
        for (int i = 0; i < 14; i++)
        {
            float angle = Mathf.Tau * i / 14f + GD.Randf() * 0.14f;
            var debris = new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(0.25f, 0.35f + GD.Randf() * 0.5f, 1.4f + GD.Randf()) },
                Rotation = new Vector3(0f, -angle, (float)GD.RandRange(-0.25f, 0.25f)),
            };
            debris.SetSurfaceOverrideMaterial(0, material);
            scene.AddChild(debris);
            debris.GlobalPosition = position + new Vector3(Mathf.Cos(angle) * 1.7f, 0.12f, Mathf.Sin(angle) * 1.7f);
            Tween tween = debris.CreateTween();
            tween.TweenProperty(debris, "scale", new Vector3(1f, 0.05f, 1f), 0.48f);
            tween.TweenCallback(Callable.From(debris.QueueFree));
        }
    }

    private static void SpawnCloud(Node scene, Vector3 position, DamageElement element, int count)
    {
        StandardMaterial3D material = MakeMaterial(element, 0.5f);
        for (int i = 0; i < count; i++)
        {
            float angle = GD.Randf() * Mathf.Tau;
            var cloud = new MeshInstance3D
            {
                Mesh = new SphereMesh { Radius = 0.42f + GD.Randf() * 0.25f, Height = 0.85f + GD.Randf() * 0.5f },
                Scale = Vector3.One * 0.25f,
            };
            cloud.SetSurfaceOverrideMaterial(0, material);
            scene.AddChild(cloud);
            cloud.GlobalPosition = position + new Vector3(Mathf.Cos(angle) * GD.Randf() * 2f, 0.35f + GD.Randf(), Mathf.Sin(angle) * GD.Randf() * 2f);
            Tween tween = cloud.CreateTween();
            tween.SetParallel(true);
            tween.TweenProperty(cloud, "scale", Vector3.One * (1.2f + GD.Randf()), 0.48f);
            tween.TweenProperty(cloud, "position:y", cloud.Position.Y + 1f, 0.48f);
            tween.SetParallel(false);
            tween.TweenProperty(cloud, "scale", Vector3.One * 0.05f, 0.28f);
            tween.TweenCallback(Callable.From(cloud.QueueFree));
        }
    }

    private static void SpawnRuneRing(Node scene, Vector3 position, DamageElement element, float size)
    {
        var ring = new MeshInstance3D
        {
            Mesh = new TorusMesh { InnerRadius = 0.72f, OuterRadius = 0.82f },
            Scale = Vector3.One * 0.15f,
        };
        ring.SetSurfaceOverrideMaterial(0, MakeMaterial(element));
        scene.AddChild(ring);
        ring.GlobalPosition = position + new Vector3(0f, 0.08f, 0f);
        Tween tween = ring.CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(ring, "scale", Vector3.One * size, 0.32f);
        tween.TweenProperty(ring, "rotation:y", Mathf.Pi, 0.32f);
        tween.SetParallel(false);
        tween.TweenProperty(ring, "scale", Vector3.One * 0.02f, 0.16f);
        tween.TweenCallback(Callable.From(ring.QueueFree));
    }

    private static void SpawnShockwave(Node scene, Vector3 position, Color color)
    {
        var material = new StandardMaterial3D
        {
            AlbedoColor = new Color(color.R, color.G, color.B, 0.65f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            EmissionEnabled = true,
            Emission = color,
            EmissionEnergyMultiplier = 3f,
        };
        var wave = new MeshInstance3D
        {
            Mesh = new TorusMesh { InnerRadius = 0.72f, OuterRadius = 0.98f },
            Scale = Vector3.One * 0.2f,
        };
        wave.SetSurfaceOverrideMaterial(0, material);
        scene.AddChild(wave);
        wave.GlobalPosition = position + new Vector3(0f, 0.12f, 0f);
        Tween tween = wave.CreateTween();
        tween.TweenProperty(wave, "scale", Vector3.One * 4.2f, 0.28f);
        tween.TweenProperty(wave, "scale", Vector3.One * 0.02f, 0.16f);
        tween.TweenCallback(Callable.From(wave.QueueFree));
    }

    private static void SpawnElementBurst(Node scene, Vector3 position, DamageElement element, int count)
    {
        var burst = new Node3D();
        scene.AddChild(burst);
        burst.GlobalPosition = position + new Vector3(0f, 0.18f, 0f);
        StandardMaterial3D material = MakeMaterial(element);
        for (int i = 0; i < count; i++)
        {
            float angle = Mathf.Tau * i / count + GD.Randf() * 0.25f;
            float length = 0.35f + GD.Randf() * 0.65f;
            PrimitiveMesh mesh = element switch
            {
                DamageElement.Fire => new SphereMesh { Radius = 0.07f, Height = 0.16f },
                DamageElement.Cold => new PrismMesh { Size = new Vector3(0.1f, 0.42f, 0.12f) },
                DamageElement.Poison => new SphereMesh { Radius = 0.1f, Height = 0.2f },
                _ => new BoxMesh { Size = new Vector3(0.055f, length, 0.055f) },
            };
            var shard = new MeshInstance3D
            {
                Mesh = mesh,
                Position = new Vector3(Mathf.Cos(angle) * 0.42f, GD.Randf() * 0.5f, Mathf.Sin(angle) * 0.42f),
                Rotation = new Vector3(Mathf.Pi * 0.35f, -angle, GD.Randf() * 0.5f),
            };
            shard.SetSurfaceOverrideMaterial(0, material);
            burst.AddChild(shard);
            Vector3 target = shard.Position + new Vector3(Mathf.Cos(angle) * (1.2f + GD.Randf()), 0.5f + GD.Randf() * 1.4f, Mathf.Sin(angle) * (1.2f + GD.Randf()));
            Tween tween = shard.CreateTween();
            tween.SetParallel(true);
            tween.TweenProperty(shard, "position", target, 0.34f);
            tween.TweenProperty(shard, "scale", Vector3.One * 0.08f, 0.34f);
        }
        var light = new OmniLight3D { LightColor = material.Emission, LightEnergy = 4.2f, OmniRange = 6f, Position = new Vector3(0f, 0.8f, 0f) };
        burst.AddChild(light);
        Tween lightTween = light.CreateTween();
        lightTween.TweenProperty(light, "light_energy", 0f, 0.32f);
        Tween cleanup = burst.CreateTween();
        cleanup.TweenInterval(0.38f);
        cleanup.TweenCallback(Callable.From(burst.QueueFree));
    }

    public static void SpawnMeleeHit(Node scene, Vector3 position)
    {
        var node = new Node3D();
        scene.AddChild(node);
        node.GlobalPosition = position;
        var material = MakeMaterial(DamageElement.Physical);

        for (int i = 0; i < 5; i++)
        {
            float angle = Mathf.Tau * i / 5f;
            var shard = new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(0.09f, 0.09f, 0.65f) },
                Position = new Vector3(Mathf.Cos(angle) * 0.22f, 0f, Mathf.Sin(angle) * 0.22f),
                Rotation = new Vector3(0f, -angle, 0f),
            };
            shard.SetSurfaceOverrideMaterial(0, material);
            node.AddChild(shard);
        }

        node.Scale = Vector3.One * 0.2f;
        var tween = node.CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(node, "scale", Vector3.One * 1.25f, 0.16f);
        tween.TweenProperty(node, "rotation:y", Mathf.Pi * 0.35f, 0.16f);
        tween.SetParallel(false);
        tween.TweenProperty(node, "scale", Vector3.One * 0.05f, 0.12f);
        tween.TweenCallback(Callable.From(node.QueueFree));
    }

    public static Color ElementColor(DamageElement element)
    {
        return element switch
        {
            DamageElement.Fire => new Color(1f, 0.18f, 0.03f),
            DamageElement.Cold => new Color(0.25f, 0.82f, 1f),
            DamageElement.Lightning => new Color(0.95f, 0.92f, 0.12f),
            DamageElement.Poison => new Color(0.2f, 1f, 0.3f),
            _ => new Color(0.85f, 0.72f, 0.5f),
        };
    }

    private static StandardMaterial3D MakeMaterial(DamageElement element, float alpha = 1f)
    {
        Color baseColor = ElementColor(element);
        Color color = new(baseColor.R, baseColor.G, baseColor.B, alpha);
        return new StandardMaterial3D
        {
            AlbedoColor = color,
            Transparency = alpha < 1f ? BaseMaterial3D.TransparencyEnum.Alpha : BaseMaterial3D.TransparencyEnum.Disabled,
            EmissionEnabled = true,
            Emission = baseColor,
            EmissionEnergyMultiplier = 3.4f,
        };
    }
}
