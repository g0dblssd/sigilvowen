using System.Collections.Generic;

namespace Sigilwoven;

public static class LinkCatalog
{
    public static readonly List<SkillLinkData> All = new()
    {
        new SkillLinkData("lightning_form", "Lightning Form", "Summon takes lightning form: lightning damage, 50% chance to stun 0.5s, glowing cyan body."),
        new SkillLinkData("lightning_imbuement", "Lightning Imbuement", "Any attack becomes a visible hybrid: keeps its base element, gains lightning sparks, +15% damage and 35% stun."),
        new SkillLinkData("fire_link", "Fire Link", "Adds burning: hits ignite, meteor leaves burning sparks dealing fire damage over 1.5s."),
        new SkillLinkData("cold_touch", "Cold Touch", "Adds cold: +10% damage and a real 38% chill for 1.4s."),
        new SkillLinkData("venom_seal", "Venom Seal", "Hits infect enemies for 26% base damage per second over 3s; stacks with burning."),
        new SkillLinkData("execution_mark", "Execution Mark", "Hits against enemies under 35% health deal 75% more damage."),
        new SkillLinkData("chain_extension", "Chain Extension", "Shots chain to +2 targets; area hits leave a short burning field."),
        new SkillLinkData("stun_impacts", "Stun Impacts", "Hits gain 50% chance to stun for 0.5s."),
        new SkillLinkData("persist_aura", "Persist Aura", "Buffs last 80% longer; summons gain +30% lifetime flavor."),
        new SkillLinkData("aether_pulse_link", "Aether Pulse Link", "Links the zero-damage spam pulse: cooldown -56%, but final damage -60%."),
    };

    public static SkillLinkData? ById(string id)
    {
        foreach (var l in All)
        {
            if (l.Id == id)
            {
                return l;
            }
        }
        return null;
    }
}
