using System;
using System.Collections.Generic;
using Godot;

namespace Sigilwoven;

public enum HeroClass
{
    Runeblade,
    Aetherist,
    Warden,
}

public enum ParagonStat
{
    Power,
    Vitality,
    Haste,
}

/// <summary>Persistent-in-session character progression and unlock ownership.</summary>
public partial class PlayerProgression : Node
{
    public const int MaxLevel = 300;

    private readonly HashSet<string> _unlockedLinks = new(StringComparer.Ordinal)
    {
        "fire_link",
        "lightning_form",
        "chain_extension",
    };

    public HeroClass HeroClass { get; private set; } = HeroClass.Runeblade;
    public int Level { get; private set; } = 1;
    public int Experience { get; private set; }
    public int PassivePoints { get; private set; }
    public int ParagonLevel { get; private set; }
    public int ParagonExperience { get; private set; }
    public int ParagonPoints { get; private set; }
    public int PowerRanks { get; private set; }
    public int VitalityRanks { get; private set; }
    public int HasteRanks { get; private set; }

    public float DamageMultiplier => 1f + (Level - 1) * 0.003f + PowerRanks * 0.01f;
    public float CooldownMultiplier => Mathf.Max(0.55f, 1f - HasteRanks * 0.004f);
    public int ExperienceToNextLevel => Level >= MaxLevel ? 0 : CalculateLevelRequirement(Level);
    public int ExperienceToNextParagon => 1200 + ParagonLevel * 180;

    public event Action? Changed;
    public event Action<string>? LinkUnlocked;

    public void GainExperience(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        if (Level < MaxLevel)
        {
            Experience += amount;
            while (Level < MaxLevel && Experience >= ExperienceToNextLevel)
            {
                Experience -= ExperienceToNextLevel;
                Level++;
                PassivePoints++;
            }
            if (Level >= MaxLevel && Experience > 0)
            {
                ParagonExperience += Experience;
                Experience = 0;
            }
        }
        else
        {
            ParagonExperience += amount;
        }

        while (Level >= MaxLevel && ParagonExperience >= ExperienceToNextParagon)
        {
            ParagonExperience -= ExperienceToNextParagon;
            ParagonLevel++;
            ParagonPoints++;
        }
        Changed?.Invoke();
    }

    public bool IsLinkUnlocked(string linkId)
    {
        return _unlockedLinks.Contains(linkId);
    }

    public bool UnlockLink(string linkId)
    {
        if (LinkCatalog.ById(linkId) == null || !_unlockedLinks.Add(linkId))
        {
            return false;
        }
        LinkUnlocked?.Invoke(linkId);
        Changed?.Invoke();
        return true;
    }

    public SkillLinkData? UnlockNextLink()
    {
        foreach (SkillLinkData link in LinkCatalog.All)
        {
            if (UnlockLink(link.Id))
            {
                return link;
            }
        }
        return null;
    }

    public bool SpendParagonPoint(ParagonStat stat)
    {
        if (ParagonPoints <= 0)
        {
            return false;
        }
        ParagonPoints--;
        switch (stat)
        {
            case ParagonStat.Power: PowerRanks++; break;
            case ParagonStat.Vitality: VitalityRanks++; break;
            case ParagonStat.Haste: HasteRanks++; break;
        }
        Changed?.Invoke();
        return true;
    }

    public void SetClass(HeroClass heroClass)
    {
        if (Level == 1 && Experience == 0)
        {
            HeroClass = heroClass;
            Changed?.Invoke();
        }
    }

    private static int CalculateLevelRequirement(int level)
    {
        return 80 + level * 24 + Mathf.RoundToInt(Mathf.Pow(level, 1.32f) * 7f);
    }
}
