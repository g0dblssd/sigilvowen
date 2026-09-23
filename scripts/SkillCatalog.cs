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
        new SkillData("rune_cleave", "Rune Cleave", SkillType.AreaAttack, DamageElement.Physical, 42f, new[] { "attack", "melee", "arc" }, 0.35f, 2.2f, 14f, "Carves a wide arc and deals heavy stagger to enemies in front.", HeroClass.Runeblade),
        new SkillData("blade_vortex", "Blade Vortex", SkillType.AreaAttack, DamageElement.Physical, 36f, new[] { "attack", "melee", "area" }, 0.4f, 4.5f, 20f, "A ring of runic steel punishes enemies surrounding the warrior.", HeroClass.Runeblade),
        new SkillData("blood_lunge", "Blood Lunge", SkillType.Movement, DamageElement.Physical, 34f, new[] { "attack", "movement", "melee" }, 0.2f, 3.4f, 12f, "Lunges through a target line and strikes everything at the destination.", HeroClass.Runeblade),
        new SkillData("void_lance", "Void Lance", SkillType.Projectile, DamageElement.Lightning, 38f, new[] { "spell", "projectile", "void" }, 0.45f, 2.4f, 16f, "Launches a dense lance of aether with high critical scaling.", HeroClass.Aetherist),
        new SkillData("gravity_well", "Gravity Well", SkillType.AreaAttack, DamageElement.Cold, 48f, new[] { "spell", "area", "control" }, 0.8f, 6.5f, 28f, "Collapses the target area in cold aether, damaging and chilling the pack.", HeroClass.Aetherist),
        new SkillData("arcane_echo", "Arcane Echo", SkillType.Buff, DamageElement.Lightning, 0f, new[] { "spell", "buff", "haste" }, 0.35f, 18f, 22f, "Accelerates casting for six seconds; best used before a burst window.", HeroClass.Aetherist),
        new SkillData("split_arrow", "Split Arrow", SkillType.Projectile, DamageElement.Physical, 29f, new[] { "attack", "bow", "projectile" }, 0.25f, 1.4f, 8f, "Fires a fast physical arrow designed for sustained ranged pressure.", HeroClass.Warden),
        new SkillData("venom_knives", "Venom Knives", SkillType.AreaAttack, DamageElement.Poison, 33f, new[] { "attack", "dagger", "poison", "area" }, 0.3f, 3.2f, 15f, "Throws poisoned knives around the Warden and applies close-range pressure.", HeroClass.Warden),
        new SkillData("shadow_step", "Shadow Step", SkillType.Movement, DamageElement.Poison, 27f, new[] { "attack", "dagger", "movement" }, 0.15f, 2.8f, 10f, "Dashes through danger and cuts enemies at the destination.", HeroClass.Warden),
        new SkillData("war_cry", "War Cry", SkillType.Buff, DamageElement.Physical, 0f, new[] { "buff", "rage", "melee" }, 0.35f, 12f, 14f, "Roars to enter a six-second frenzy and accelerate heavy attacks.", HeroClass.Berserker),
        new SkillData("seismic_rage", "Seismic Rage", SkillType.AreaAttack, DamageElement.Physical, 58f, new[] { "attack", "area", "stagger" }, 0.75f, 5.5f, 26f, "Smashes the earth in a brutal shockwave with enormous stagger.", HeroClass.Berserker),
        new SkillData("executioner_leap", "Executioner's Leap", SkillType.Movement, DamageElement.Physical, 72f, new[] { "attack", "movement", "area" }, 0.4f, 7f, 30f, "Leaps into a pack and executes wounded prey in a bloody impact.", HeroClass.Berserker),
        new SkillData("bone_spear", "Bone Spear", SkillType.Projectile, DamageElement.Physical, 34f, new[] { "spell", "projectile", "bone" }, 0.4f, 1.8f, 12f, "Launches a piercing shard of grave-forged bone.", HeroClass.Necromancer),
        new SkillData("raise_legion", "Raise Legion", SkillType.Summon, DamageElement.Poison, 15f, new[] { "summon", "undead", "poison" }, 1.0f, 10f, 32f, "Calls a grave servant whose strikes spread decay.", HeroClass.Necromancer),
        new SkillData("corpse_bloom", "Corpse Bloom", SkillType.AreaAttack, DamageElement.Poison, 55f, new[] { "spell", "area", "poison" }, 0.8f, 6f, 28f, "Detonates a field of spectral remains beneath the target pack.", HeroClass.Necromancer),
        new SkillData("fan_of_knives", "Fan of Knives", SkillType.AreaAttack, DamageElement.Poison, 38f, new[] { "attack", "dagger", "area" }, 0.25f, 2.2f, 13f, "Throws a circular fan of venom-coated blades.", HeroClass.Shadowstalker),
        new SkillData("smoke_bomb", "Smoke Bomb", SkillType.Buff, DamageElement.Poison, 0f, new[] { "buff", "shadow", "haste" }, 0.25f, 9f, 16f, "Vanishes into smoke, gaining speed for an assassination window.", HeroClass.Shadowstalker),
        new SkillData("death_mark", "Death Mark", SkillType.Projectile, DamageElement.Poison, 64f, new[] { "attack", "projectile", "critical" }, 0.35f, 5f, 24f, "Hurls a shadow blade engineered for devastating critical strikes.", HeroClass.Shadowstalker),
        new SkillData("holy_smite", "Holy Smite", SkillType.AreaAttack, DamageElement.Lightning, 40f, new[] { "attack", "area", "holy" }, 0.5f, 2.8f, 16f, "Calls down a radiant hammer strike on the chosen ground.", HeroClass.Templar),
        new SkillData("consecrated_ground", "Consecrated Ground", SkillType.Buff, DamageElement.Fire, 0f, new[] { "buff", "defense", "holy" }, 0.5f, 14f, 20f, "Consecrates the battlefield and hardens the Templar against harm.", HeroClass.Templar),
        new SkillData("judgment_bell", "Judgment Bell", SkillType.AreaAttack, DamageElement.Lightning, 68f, new[] { "attack", "area", "stagger" }, 0.9f, 7.5f, 34f, "Rings a sacred verdict that crushes and staggers surrounding enemies.", HeroClass.Templar),
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
