using System.Collections.Generic;

namespace Sigilwoven;

// Result of LinkSystem.ResolveCast: final damage + bonus procs.
public class ResolvedCast
{
    public readonly List<string> AppliedLinks = new();
    public DamageElement Element;
    public float Damage;
    public float CooldownMultiplier = 1f;
    public int ChainCount;
    public bool LightningForm;
    public bool HasLightningInfusion;
    public bool HasBurn;
    public float BurnDps;
    public float BurnDuration;
    public bool HasPoison;
    public float PoisonDps;
    public float PoisonDuration;
    public bool HasChill;
    public float ChillSlow;
    public float ChillDuration;
    public float ExecuteThreshold;
    public float ExecuteMultiplier = 1f;
    public bool HasStun;
    public float StunChance;
    public float StunDuration;
    public bool HasGroundFire;
    public float GroundFireDps;
    public float GroundFireDuration;
    public float GroundFireRadius;
    public float DurationMultiplier = 1f;
    public bool IsCritical;
    public float CriticalMultiplier = 1f;
    public float StaggerDamage;
    public float ElementalPenetration;
}

public static class LinkSystem
{
    public static string? GetIncompatibilityReason(SkillData skill, SkillLinkData link)
    {
        bool damaging = skill.BaseDamage > 0f;
        bool compatible = link.Id switch
        {
            "lightning_form" => skill.Type == SkillType.Summon,
            "chain_extension" => skill.Type is SkillType.Projectile or SkillType.AreaAttack,
            "persist_aura" => skill.Type is SkillType.Buff or SkillType.Summon,
            "stun_impacts" or "execution_mark" or "fire_link" or "cold_touch" or "venom_seal" => damaging,
            _ => true,
        };
        return compatible ? null : $"{link.DisplayName} is incompatible with {skill.DisplayName} ({skill.Type}).";
    }

    // Core mechanic: combine a skill with its links into final cast params.
    public static ResolvedCast ResolveCast(SkillData skill, IEnumerable<SkillLinkData> links)
    {
        var r = new ResolvedCast
        {
            Element = skill.DamageElement,
            Damage = skill.BaseDamage,
            ChainCount = skill.Id == "chain_lightning" ? 2 : 0,
        };

        foreach (var link in links)
        {
            r.AppliedLinks.Add(link.Id);
            switch (link.Id)
            {
                case "lightning_form":
                    // Demo #1: summon becomes lightning form.
                    if (skill.Type == SkillType.Summon)
                    {
                        r.LightningForm = true;
                        r.Element = DamageElement.Lightning;
                        r.HasStun = true;
                        r.StunChance = System.Math.Max(r.StunChance, 0.5f);
                        r.StunDuration = System.Math.Max(r.StunDuration, 0.5f);
                    }
                    break;
                case "lightning_imbuement":
                    // Universal hybrid link: preserve the base element and visibly add lightning.
                    r.HasLightningInfusion = true;
                    r.Damage *= 1.15f;
                    r.HasStun = true;
                    r.StunChance = System.Math.Max(r.StunChance, 0.35f);
                    r.StunDuration = System.Math.Max(r.StunDuration, 0.35f);
                    if (skill.Type == SkillType.Projectile)
                    {
                        r.ChainCount += 1;
                    }
                    break;
                case "fire_link":
                    r.HasBurn = true;
                    r.BurnDps = System.Math.Max(r.BurnDps, skill.BaseDamage * 0.3f);
                    r.BurnDuration = System.Math.Max(r.BurnDuration, 2.0f);
                    // Demo #2: meteor leaves burning sparks field.
                    if (skill.Type == SkillType.AreaAttack)
                    {
                        r.HasGroundFire = true;
                        r.GroundFireDps = System.Math.Max(r.GroundFireDps, skill.BaseDamage * 0.2f);
                        r.GroundFireDuration = System.Math.Max(r.GroundFireDuration, 1.5f);
                        r.GroundFireRadius = System.Math.Max(r.GroundFireRadius, 3.0f);
                    }
                    break;
                case "cold_touch":
                    r.Damage *= 1.1f;
                    r.HasChill = true;
                    r.ChillSlow = System.Math.Max(r.ChillSlow, 0.38f);
                    r.ChillDuration = System.Math.Max(r.ChillDuration, 1.4f);
                    break;
                case "venom_seal":
                    r.HasPoison = true;
                    r.PoisonDps = System.Math.Max(r.PoisonDps, skill.BaseDamage * 0.26f);
                    r.PoisonDuration = System.Math.Max(r.PoisonDuration, 3f);
                    break;
                case "execution_mark":
                    r.ExecuteThreshold = System.Math.Max(r.ExecuteThreshold, 0.35f);
                    r.ExecuteMultiplier = System.Math.Max(r.ExecuteMultiplier, 1.75f);
                    break;
                case "chain_extension":
                    r.ChainCount += 2;
                    if (skill.Type == SkillType.AreaAttack && !r.HasGroundFire)
                    {
                        r.HasGroundFire = true;
                        r.GroundFireDps = System.Math.Max(r.GroundFireDps, skill.BaseDamage * 0.15f);
                        r.GroundFireDuration = System.Math.Max(r.GroundFireDuration, 1.5f);
                        r.GroundFireRadius = System.Math.Max(r.GroundFireRadius, 2.5f);
                    }
                    break;
                case "stun_impacts":
                    r.HasStun = true;
                    r.StunChance = System.Math.Max(r.StunChance, 0.5f);
                    r.StunDuration = System.Math.Max(r.StunDuration, 0.5f);
                    break;
                case "persist_aura":
                    r.DurationMultiplier *= 1.8f;
                    break;
                case "aether_pulse_link":
                    // The spam pulse accelerates a linked spell at the cost of impact.
                    r.CooldownMultiplier *= 0.44f;
                    r.Damage *= 0.40f;
                    break;
            }
        }

        return r;
    }
}
