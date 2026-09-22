using System.Collections.Generic;

namespace Sigilwoven;

public static class SkillCatalog
{
    public static readonly List<SkillData> All = new()
    {
        new SkillData("fireball", "Fireball", SkillType.Projectile, DamageElement.Fire, 25f, new[] { "fire", "projectile" }, 0.4f, 1.0f, 10f),
        new SkillData("meteor", "Meteor", SkillType.AreaAttack, DamageElement.Fire, 60f, new[] { "fire", "area" }, 1.0f, 5.0f, 30f),
        new SkillData("chain_lightning", "Chain Lightning", SkillType.Projectile, DamageElement.Lightning, 22f, new[] { "lightning", "chain" }, 0.5f, 3.0f, 20f),
        new SkillData("frostbolt", "Frostbolt", SkillType.Projectile, DamageElement.Cold, 18f, new[] { "cold", "projectile" }, 0.4f, 0.8f, 8f),
        new SkillData("lightning_elemental", "Lightning Elemental", SkillType.Summon, DamageElement.Lightning, 12f, new[] { "summon", "lightning" }, 1.0f, 12.0f, 35f),
        new SkillData("lightning_wisp", "Lightning Wisp", SkillType.Summon, DamageElement.Lightning, 8f, new[] { "summon", "lightning", "fast" }, 0.8f, 8.0f, 25f),
        new SkillData("dread_hound", "Dread Hound", SkillType.Summon, DamageElement.Physical, 14f, new[] { "summon", "fear", "beast" }, 1.0f, 10.0f, 30f),
        new SkillData("bone_golem", "Bone Golem", SkillType.Summon, DamageElement.Physical, 18f, new[] { "summon", "tanky", "undead" }, 1.2f, 15.0f, 40f),
        new SkillData("phoenix", "Phoenix", SkillType.Summon, DamageElement.Fire, 16f, new[] { "summon", "fire", "flying" }, 1.2f, 18.0f, 45f),
        new SkillData("ember_imp", "Ember Imp", SkillType.Summon, DamageElement.Fire, 7f, new[] { "summon", "fire", "fast" }, 0.6f, 6.0f, 15f),
        new SkillData("frost_spider", "Frost Spider", SkillType.Summon, DamageElement.Cold, 9f, new[] { "summon", "cold", "fast" }, 0.7f, 7.0f, 18f),
        new SkillData("spark_bolt", "Spark Bolt", SkillType.Projectile, DamageElement.Lightning, 14f, new[] { "lightning", "projectile" }, 0.3f, 0.6f, 6f),
        new SkillData("venom_fang", "Venom Fang", SkillType.Projectile, DamageElement.Poison, 16f, new[] { "poison", "projectile" }, 0.35f, 0.9f, 8f),
        new SkillData("stone_shot", "Stone Shot", SkillType.Projectile, DamageElement.Physical, 20f, new[] { "physical", "projectile" }, 0.4f, 1.0f, 8f),
        new SkillData("ice_nova", "Ice Nova", SkillType.AreaAttack, DamageElement.Cold, 35f, new[] { "cold", "area" }, 0.8f, 4.0f, 22f),
        new SkillData("firestorm", "Firestorm", SkillType.AreaAttack, DamageElement.Fire, 45f, new[] { "fire", "area" }, 0.9f, 6.0f, 28f),
        new SkillData("thunderstorm", "Thunderstorm", SkillType.AreaAttack, DamageElement.Lightning, 40f, new[] { "lightning", "area" }, 0.9f, 6.0f, 26f),
        new SkillData("earthquake", "Earthquake", SkillType.AreaAttack, DamageElement.Physical, 50f, new[] { "physical", "area" }, 1.0f, 7.0f, 30f),
        new SkillData("plague_nova", "Plague Nova", SkillType.AreaAttack, DamageElement.Poison, 30f, new[] { "poison", "area" }, 0.8f, 5.0f, 24f),
        new SkillData("stone_skin", "Stone Skin", SkillType.Buff, DamageElement.Physical, 0f, new[] { "buff", "defense" }, 0.5f, 20.0f, 20f),
        new SkillData("haste_aura", "Haste Aura", SkillType.Buff, DamageElement.Lightning, 0f, new[] { "buff", "speed" }, 0.5f, 25.0f, 25f),
        new SkillData("frost_armor", "Frost Armor", SkillType.Buff, DamageElement.Cold, 0f, new[] { "buff", "defense", "cold" }, 0.5f, 20.0f, 20f),
        new SkillData("flame_dash", "Flame Dash", SkillType.Movement, DamageElement.Fire, 20f, new[] { "fire", "movement", "dash" }, 0.3f, 2.5f, 12f),
        new SkillData("toxic_cloud", "Toxic Cloud", SkillType.AreaAttack, DamageElement.Poison, 25f, new[] { "poison", "area", "dot" }, 0.7f, 5.0f, 20f),
        new SkillData("arc_surge", "Arc Surge", SkillType.Projectile, DamageElement.Lightning, 28f, new[] { "lightning", "projectile", "burst" }, 0.6f, 3.5f, 18f),
        new SkillData("aether_pulse", "Aether Pulse", SkillType.Buff, DamageElement.Lightning, 0f, new[] { "utility", "spam", "zero-damage", "link" }, 0.05f, 0.12f, 0f),
    };

    public static SkillData? ById(string id)
    {
        foreach (var s in All)
        {
            if (s.Id == id)
            {
                return s;
            }
        }
        return null;
    }
}
