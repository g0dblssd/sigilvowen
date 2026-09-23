using System;
using System.Collections.Generic;
using System.Text.Json;
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
    public const int PassiveNodeCount = 1009;
    private const string SavePath = "user://progression.json";

    private readonly HashSet<string> _unlockedLinks = new(StringComparer.Ordinal)
    {
        "fire_link",
        "lightning_form",
        "chain_extension",
    };
    private readonly HashSet<int> _allocatedPassiveNodes = new() { 0 };
    private readonly HashSet<string> _rewardUnlockedSkills = new(StringComparer.Ordinal);

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

    public override void _Ready()
    {
        LoadProgress();
    }

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
        CommitChanges();
    }

    public bool IsLinkUnlocked(string linkId)
    {
        return _unlockedLinks.Contains(linkId);
    }

    public int GetSkillRequiredLevel(string skillId)
    {
        return skillId switch
        {
            "ember_imp" => 3,
            "spark_bolt" => 4,
            "lightning_wisp" => 5,
            "stone_shot" => 6,
            "frost_spider" => 7,
            "stone_skin" => 8,
            "venom_fang" => 10,
            "dread_hound" => 12,
            "frost_armor" => 14,
            "firestorm" => 15,
            "thunderstorm" => 18,
            "toxic_cloud" => 20,
            "earthquake" => 22,
            "plague_nova" => 24,
            "phoenix" => 25,
            "arc_surge" => 28,
            _ => 1,
        };
    }

    public bool IsSkillUnlocked(string skillId)
    {
        return Level >= GetSkillRequiredLevel(skillId) || _rewardUnlockedSkills.Contains(skillId);
    }

    public bool UnlockSkill(string skillId)
    {
        if (SkillCatalog.ById(skillId) == null || IsSkillUnlocked(skillId) || !_rewardUnlockedSkills.Add(skillId))
        {
            return false;
        }
        CommitChanges();
        return true;
    }

    public SkillData? UnlockNextSkill()
    {
        foreach (SkillData skill in SkillCatalog.All)
        {
            if (GetSkillRequiredLevel(skill.Id) > 1 && UnlockSkill(skill.Id))
            {
                return skill;
            }
        }
        return null;
    }

    public bool UnlockLink(string linkId)
    {
        if (LinkCatalog.ById(linkId) == null || !_unlockedLinks.Add(linkId))
        {
            return false;
        }
        SaveProgress();
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
        CommitChanges();
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
        CommitChanges();
        return true;
    }

    public void SetClass(HeroClass heroClass)
    {
        if (Level == 1 && Experience == 0)
        {
            HeroClass = heroClass;
            CommitChanges();
        }
    }

    public void ChooseClassForRun(HeroClass heroClass)
    {
        if (!Enum.IsDefined(heroClass) || HeroClass == heroClass)
        {
            return;
        }
        HeroClass = heroClass;
        CommitChanges();
    }

    private void CommitChanges()
    {
        SaveProgress();
        Changed?.Invoke();
    }

    private void SaveProgress()
    {
        try
        {
            var data = new ProgressionSaveData
            {
                Version = 1,
                HeroClass = HeroClass,
                Level = Level,
                Experience = Experience,
                PassivePoints = PassivePoints,
                ParagonLevel = ParagonLevel,
                ParagonExperience = ParagonExperience,
                ParagonPoints = ParagonPoints,
                PowerRanks = PowerRanks,
                VitalityRanks = VitalityRanks,
                HasteRanks = HasteRanks,
                PassivePowerRanks = PassivePowerRanks,
                PassiveVitalityRanks = PassiveVitalityRanks,
                PassiveHasteRanks = PassiveHasteRanks,
                PassiveManaRanks = PassiveManaRanks,
                UnlockedLinks = new List<string>(_unlockedLinks).ToArray(),
                AllocatedPassiveNodes = new List<int>(_allocatedPassiveNodes).ToArray(),
                RewardUnlockedSkills = new List<string>(_rewardUnlockedSkills).ToArray(),
            };
            using FileAccess? file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
            if (file == null)
            {
                GD.PushWarning($"[Progression] Could not open save file: {FileAccess.GetOpenError()}");
                return;
            }
            file.StoreString(JsonSerializer.Serialize(data));
        }
        catch (Exception exception)
        {
            GD.PushWarning($"[Progression] Save failed: {exception.Message}");
        }
    }

    private void LoadProgress()
    {
        if (!FileAccess.FileExists(SavePath))
        {
            return;
        }
        try
        {
            using FileAccess? file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
            ProgressionSaveData? data = file == null
                ? null
                : JsonSerializer.Deserialize<ProgressionSaveData>(file.GetAsText());
            if (data == null || data.Version != 1)
            {
                GD.PushWarning("[Progression] Ignoring unsupported or empty save data.");
                return;
            }

            HeroClass = Enum.IsDefined(data.HeroClass) ? data.HeroClass : HeroClass.Runeblade;
            Level = Mathf.Clamp(data.Level, 1, MaxLevel);
            Experience = Mathf.Max(0, data.Experience);
            PassivePoints = Mathf.Max(0, data.PassivePoints);
            ParagonLevel = Mathf.Max(0, data.ParagonLevel);
            ParagonExperience = Mathf.Max(0, data.ParagonExperience);
            ParagonPoints = Mathf.Max(0, data.ParagonPoints);
            PowerRanks = Mathf.Max(0, data.PowerRanks);
            VitalityRanks = Mathf.Max(0, data.VitalityRanks);
            HasteRanks = Mathf.Max(0, data.HasteRanks);
            PassivePowerRanks = Mathf.Max(0, data.PassivePowerRanks);
            PassiveVitalityRanks = Mathf.Max(0, data.PassiveVitalityRanks);
            PassiveHasteRanks = Mathf.Max(0, data.PassiveHasteRanks);
            PassiveManaRanks = Mathf.Max(0, data.PassiveManaRanks);

            _unlockedLinks.Clear();
            _unlockedLinks.Add("fire_link");
            _unlockedLinks.Add("lightning_form");
            _unlockedLinks.Add("chain_extension");
            foreach (string linkId in data.UnlockedLinks)
            {
                if (LinkCatalog.ById(linkId) != null)
                {
                    _unlockedLinks.Add(linkId);
                }
            }

            _allocatedPassiveNodes.Clear();
            _allocatedPassiveNodes.Add(0);
            foreach (int nodeId in data.AllocatedPassiveNodes)
            {
                if (nodeId > 0 && nodeId < PassiveNodeCount)
                {
                    _allocatedPassiveNodes.Add(nodeId);
                }
            }
            _rewardUnlockedSkills.Clear();
            foreach (string skillId in data.RewardUnlockedSkills)
            {
                if (SkillCatalog.ById(skillId) != null)
                {
                    _rewardUnlockedSkills.Add(skillId);
                }
            }
            GD.Print($"[Progression] Loaded level {Level}, paragon {ParagonLevel}, links {_unlockedLinks.Count}.");
        }
        catch (Exception exception)
        {
            GD.PushWarning($"[Progression] Load failed, using defaults: {exception.Message}");
        }
    }

    private static int CalculateLevelRequirement(int level)
    {
        return 80 + level * 24 + Mathf.RoundToInt(Mathf.Pow(level, 1.32f) * 7f);
    }

    private sealed class ProgressionSaveData
    {
        public int Version { get; set; }
        public HeroClass HeroClass { get; set; }
        public int Level { get; set; }
        public int Experience { get; set; }
        public int PassivePoints { get; set; }
        public int ParagonLevel { get; set; }
        public int ParagonExperience { get; set; }
        public int ParagonPoints { get; set; }
        public int PowerRanks { get; set; }
        public int VitalityRanks { get; set; }
        public int HasteRanks { get; set; }
        public int PassivePowerRanks { get; set; }
        public int PassiveVitalityRanks { get; set; }
        public int PassiveHasteRanks { get; set; }
        public int PassiveManaRanks { get; set; }
        public string[] UnlockedLinks { get; set; } = System.Array.Empty<string>();
        public int[] AllocatedPassiveNodes { get; set; } = System.Array.Empty<int>();
        public string[] RewardUnlockedSkills { get; set; } = System.Array.Empty<string>();
    }
}
