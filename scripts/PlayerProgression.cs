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

public enum PassiveStat
{
    Power,
    Vitality,
    Haste,
    Mana,
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
    private readonly HashSet<int> _allocatedPassiveNodes = new() { 0 };

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
    public int PassivePowerRanks { get; private set; }
    public int PassiveVitalityRanks { get; private set; }
    public int PassiveHasteRanks { get; private set; }
    public int PassiveManaRanks { get; private set; }

    public float DamageMultiplier => (HeroClass switch { HeroClass.Aetherist => 1.12f, HeroClass.Runeblade => 1.08f, _ => 1.03f })
        + (Level - 1) * 0.003f + PowerRanks * 0.01f + PassivePowerRanks * 0.005f;
    public float CooldownMultiplier => Mathf.Max(0.45f,
        (HeroClass == HeroClass.Aetherist ? 0.95f : 1f) - HasteRanks * 0.004f - PassiveHasteRanks * 0.002f);
    public float HealthBonus => (HeroClass == HeroClass.Warden ? 30f : (HeroClass == HeroClass.Runeblade ? 10f : 0f))
        + VitalityRanks * 2f + PassiveVitalityRanks * 3f;
    public float ManaBonus => (HeroClass == HeroClass.Aetherist ? 30f : 0f) + PassiveManaRanks * 2f;
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

    public bool IsPassiveAllocated(int nodeId)
    {
        return _allocatedPassiveNodes.Contains(nodeId);
    }

    public bool CanAllocatePassive(int nodeId, int parentId)
    {
        return PassivePoints > 0
            && !_allocatedPassiveNodes.Contains(nodeId)
            && _allocatedPassiveNodes.Contains(parentId);
    }

    public bool AllocatePassive(int nodeId, int parentId, PassiveStat stat, int ranks)
    {
        if (ranks <= 0 || !CanAllocatePassive(nodeId, parentId))
        {
            return false;
        }
        PassivePoints--;
        _allocatedPassiveNodes.Add(nodeId);
        switch (stat)
        {
            case PassiveStat.Power: PassivePowerRanks += ranks; break;
            case PassiveStat.Vitality: PassiveVitalityRanks += ranks; break;
            case PassiveStat.Haste: PassiveHasteRanks += ranks; break;
            case PassiveStat.Mana: PassiveManaRanks += ranks; break;
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
