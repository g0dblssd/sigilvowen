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
    Berserker,
    Necromancer,
    Shadowstalker,
    Templar,
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
    private readonly Dictionary<string, int> _skillMastery = new(StringComparer.Ordinal);

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

    public float DamageMultiplier => (HeroClass switch
        {
            HeroClass.Aetherist => 1.12f,
            HeroClass.Runeblade => 1.08f,
            HeroClass.Berserker => 1.16f,
            HeroClass.Necromancer => 1.06f,
            HeroClass.Shadowstalker => 1.1f,
            HeroClass.Templar => 1.02f,
            _ => 1.03f,
        })
        + (Level - 1) * 0.0024f + ParagonPowerBonus + PassivePowerRanks * 0.005f;
    public float CooldownMultiplier => Mathf.Max(0.45f,
        (HeroClass == HeroClass.Aetherist ? 0.95f : 1f) - HasteRanks * 0.0025f - PassiveHasteRanks * 0.002f);
    public float HealthBonus => (HeroClass switch { HeroClass.Warden => 30f, HeroClass.Runeblade => 10f, HeroClass.Berserker => 20f, HeroClass.Templar => 45f, HeroClass.Necromancer => -10f, _ => 0f })
        + (Level - 1) * 0.85f + VitalityRanks * 3f + PassiveVitalityRanks * 3f;
    public float ManaBonus => (HeroClass switch { HeroClass.Aetherist => 30f, HeroClass.Necromancer => 22f, HeroClass.Templar => 12f, _ => 0f }) + PassiveManaRanks * 2f;
    public int ExperienceToNextLevel => Level >= MaxLevel ? 0 : CalculateLevelRequirement(Level);
    public int ExperienceToNextParagon => 6000 + ParagonLevel * 450;
    private float ParagonPowerBonus => Mathf.Min(PowerRanks, 50) * 0.008f + Mathf.Max(0, PowerRanks - 50) * 0.003f;

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

    public int ApplyDeathPenalty()
    {
        if (Level >= MaxLevel)
        {
            int paragonLost = Mathf.Min(ParagonExperience, Mathf.Max(0, Mathf.RoundToInt(ExperienceToNextParagon * 0.05f)));
            ParagonExperience -= paragonLost;
            if (paragonLost > 0) CommitChanges();
            return paragonLost;
        }
        if (Level <= 1 || Experience <= 0) return 0;
        int lost = Mathf.Min(Experience, Mathf.Max(12, Mathf.RoundToInt(ExperienceToNextLevel * 0.08f)));
        Experience -= lost;
        CommitChanges();
        return lost;
    }

    public bool IsLinkUnlocked(string linkId)
    {
        return _unlockedLinks.Contains(linkId);
    }

    public int GetSkillRequiredLevel(string skillId)
    {
        return skillId switch
        {
            "fireball" or "aether_pulse" or "rune_cleave" or "void_lance" or "split_arrow" or "war_cry" or "bone_spear" or "fan_of_knives" or "holy_smite" => 1,
            "frostbolt" => 15,
            "spark_bolt" => 25,
            "ember_imp" => 30,
            "venom_fang" or "blade_vortex" or "gravity_well" or "venom_knives" or "seismic_rage" or "raise_legion" or "smoke_bomb" or "consecrated_ground" => 35,
            "flame_dash" => 40,
            "chain_lightning" => 45,
            "stone_shot" => 50,
            "stone_skin" => 55,
            "frost_spider" => 60,
            "frost_armor" => 70,
            "lightning_wisp" => 75,
            "ice_nova" => 80,
            "dread_hound" or "blood_lunge" or "arcane_echo" or "shadow_step" or "executioner_leap" or "corpse_bloom" or "death_mark" or "judgment_bell" => 90,
            "firestorm" => 100,
            "haste_aura" => 110,
            "thunderstorm" => 120,
            "toxic_cloud" => 125,
            "meteor" => 135,
            "earthquake" => 140,
            "bone_golem" => 150,
            "plague_nova" => 160,
            "lightning_elemental" => 180,
            "arc_surge" => 200,
            "phoenix" => 240,
            _ => 1,
        };
    }

    public int GetSkillRank(string skillId) => 1 + Mathf.Min(19, GetSkillMastery(skillId) / 24);
    public int GetSkillMastery(string skillId) => _skillMastery.TryGetValue(skillId, out int value) ? value : 0;
    public float GetSkillDamageMultiplier(string skillId) => 1f + (GetSkillRank(skillId) - 1) * 0.055f;

    public void GainSkillMastery(string skillId, int amount = 1)
    {
        if (amount <= 0 || SkillCatalog.ById(skillId) == null) return;
        int oldRank = GetSkillRank(skillId);
        _skillMastery[skillId] = GetSkillMastery(skillId) + amount;
        if (GetSkillRank(skillId) != oldRank || _skillMastery[skillId] % 4 == 0) CommitChanges();
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
                SkillMastery = new Dictionary<string, int>(_skillMastery),
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
            _skillMastery.Clear();
            foreach (var pair in data.SkillMastery)
            {
                if (SkillCatalog.ById(pair.Key) != null) _skillMastery[pair.Key] = Mathf.Max(0, pair.Value);
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
        // ~616k total XP from 1 to 300. Combined with level-scaled monster XP,
        // this targets a long campaign rather than tens of thousands of flat-XP kills.
        return 90 + level * 7 + Mathf.RoundToInt(Mathf.Pow(level, 1.18f) * 2.4f);
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
        public Dictionary<string, int> SkillMastery { get; set; } = new();
    }
}
