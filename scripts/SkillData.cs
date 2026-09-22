namespace Sigilwoven;

public enum SkillType
{
    Summon,
    Projectile,
    AreaAttack,
    Buff,
    Movement
}

public enum DamageElement
{
    Fire,
    Cold,
    Lightning,
    Poison,
    Physical
}

public class SkillData
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public SkillType Type { get; set; }
    public DamageElement DamageElement { get; set; }
    public float BaseDamage { get; set; }
    public string[] Tags { get; set; } = System.Array.Empty<string>();
    public float CastTime { get; set; }
    public float Cooldown { get; set; }
    public float ManaCost { get; set; }
    // Runtime ownership of charges belongs to SkillCaster. Catalog entries stay immutable.
    public const int MaxLinkCharges = 2;

    public SkillData(string id, string displayName, SkillType type, DamageElement element, float baseDamage, string[] tags, float castTime, float cooldown, float manaCost)
    {
        Id = id;
        DisplayName = displayName;
        Type = type;
        DamageElement = element;
        BaseDamage = baseDamage;
        Tags = tags;
        CastTime = castTime;
        Cooldown = cooldown;
        ManaCost = manaCost;
    }
}
