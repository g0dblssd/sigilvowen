using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Godot;

namespace Sigilwoven;

public enum ItemRarity
{
    Common,
    Magic,
    Rare,
    Sigilforged,
    Unique,
}

public enum EquipmentSlot
{
    Weapon,
    Focus,
    Helm,
    Chest,
    Gloves,
    Boots,
    Belt,
    Ring,
    Amulet,
}

public enum ItemStat
{
    DamagePercent,
    MaxHealth,
    MaxMana,
    CooldownReduction,
}

public sealed class ItemAffix
{
    public ItemStat Stat { get; }
    public float Value { get; }
    public string DisplayName { get; }

    public ItemAffix(ItemStat stat, float value, string displayName)
    {
        Stat = stat;
        Value = value;
        DisplayName = displayName;
    }

    public string Format()
    {
        return Stat switch
        {
            ItemStat.DamagePercent => $"+{Value:0.#}% damage",
            ItemStat.MaxHealth => $"+{Value:0} maximum health",
            ItemStat.MaxMana => $"+{Value:0} maximum mana",
            ItemStat.CooldownReduction => $"+{Value:0.#}% cooldown recovery",
            _ => $"+{Value:0.#} {DisplayName}",
        };
    }
}

public sealed class ItemData
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Name { get; init; } = "Unnamed Relic";
    public EquipmentSlot Slot { get; init; }
    public ItemRarity Rarity { get; init; }
    public int ItemLevel { get; init; } = 1;
    public List<ItemAffix> Affixes { get; init; } = new();

    [JsonIgnore]
    public Color RarityColor => Rarity switch
    {
        ItemRarity.Magic => new Color(0.3f, 0.55f, 1f),
        ItemRarity.Rare => new Color(1f, 0.82f, 0.16f),
        ItemRarity.Sigilforged => new Color(0.72f, 0.22f, 1f),
        ItemRarity.Unique => new Color(1f, 0.38f, 0.08f),
        _ => new Color(0.82f, 0.84f, 0.88f),
    };

    public float GetStat(ItemStat stat)
    {
        float total = 0f;
        foreach (ItemAffix affix in Affixes)
        {
            if (affix.Stat == stat)
            {
                total += affix.Value;
            }
        }
        return total;
    }

    public string BuildTooltip()
    {
        string text = $"{Name}\n{Rarity.ToString().ToUpperInvariant()} {Slot.ToString().ToUpperInvariant()}  •  ITEM LEVEL {ItemLevel}";
        foreach (ItemAffix affix in Affixes)
        {
            text += $"\n  {affix.Format()}";
        }
        return text;
    }
}

public static class ItemGenerator
{
    private static readonly string[] WeaponBases = { "Rune Blade", "Aether Cleaver", "Woven Scepter" };
    private static readonly string[] FocusBases = { "Sigil Focus", "Crystal Ward", "Aether Codex" };
    private static readonly string[] HelmBases = { "Raider Crown", "Runed Visor", "Vault Hood" };
    private static readonly string[] ChestBases = { "Warden Plate", "Aether Raiment", "Sigil Harness" };
    private static readonly string[] GloveBases = { "Runic Grips", "Hexweave Gloves", "Stone Gauntlets" };
    private static readonly string[] BootBases = { "Vaultwalkers", "Aether Treads", "Raider Boots" };
    private static readonly string[] BeltBases = { "Sigil Girdle", "Woven Sash", "Guardian Chain" };
    private static readonly string[] RingBases = { "Aether Loop", "Ember Band", "Frost Seal" };
    private static readonly string[] AmuletBases = { "Woven Eye", "Vault Talisman", "Crystal Pendant" };
    private static readonly string[] RarePrefixes = { "Warden's", "Runebound", "Unbroken", "Stormwoven", "Graveborn" };
    private static readonly string[] RareSuffixes = { "of the Vault", "of Embers", "of Echoes", "of the Deep", "of Binding" };

    public static ItemData Generate(int itemLevel, float rarityBonus = 0f)
    {
        itemLevel = Mathf.Max(1, itemLevel);
        EquipmentSlot slot = (EquipmentSlot)GD.RandRange(0, Enum.GetValues<EquipmentSlot>().Length - 1);
        float roll = GD.Randf() + rarityBonus;
        ItemRarity rarity = roll switch
        {
            >= 1.12f => ItemRarity.Unique,
            >= 1.01f => ItemRarity.Sigilforged,
            >= 0.76f => ItemRarity.Rare,
            >= 0.38f => ItemRarity.Magic,
            _ => ItemRarity.Common,
        };
        int affixCount = rarity switch
        {
            ItemRarity.Common => 0,
            ItemRarity.Magic => GD.RandRange(1, 2),
            ItemRarity.Rare => GD.RandRange(3, 4),
            ItemRarity.Sigilforged => GD.RandRange(4, 5),
            ItemRarity.Unique => 5,
            _ => 0,
        };

        var affixes = new List<ItemAffix>();
        var available = new List<ItemStat> { ItemStat.DamagePercent, ItemStat.MaxHealth, ItemStat.MaxMana, ItemStat.CooldownReduction };
        for (int i = 0; i < affixCount; i++)
        {
            if (available.Count == 0)
            {
                available.AddRange(new[] { ItemStat.DamagePercent, ItemStat.MaxHealth, ItemStat.MaxMana, ItemStat.CooldownReduction });
            }
            int index = GD.RandRange(0, available.Count - 1);
            ItemStat stat = available[index];
            available.RemoveAt(index);
            affixes.Add(CreateAffix(stat, itemLevel, rarity));
        }

        string baseName = PickBase(slot);
        string name = rarity switch
        {
            ItemRarity.Magic => $"Charged {baseName}",
            ItemRarity.Rare => $"{RarePrefixes[GD.RandRange(0, RarePrefixes.Length - 1)]} {baseName} {RareSuffixes[GD.RandRange(0, RareSuffixes.Length - 1)]}",
            ItemRarity.Sigilforged => $"Sigilforged {baseName}",
            ItemRarity.Unique => $"Echo of {baseName}",
            _ => baseName,
        };
        return new ItemData { Name = name, Slot = slot, Rarity = rarity, ItemLevel = itemLevel, Affixes = affixes };
    }

    private static ItemAffix CreateAffix(ItemStat stat, int level, ItemRarity rarity)
    {
        float quality = 1f + (int)rarity * 0.12f;
        return stat switch
        {
            ItemStat.DamagePercent => new ItemAffix(stat, (GD.Randf() * 3.5f + 2.5f + level * 0.16f) * quality, "power"),
            ItemStat.MaxHealth => new ItemAffix(stat, Mathf.Round((GD.Randf() * 9f + 8f + level * 0.9f) * quality), "vitality"),
            ItemStat.MaxMana => new ItemAffix(stat, Mathf.Round((GD.Randf() * 5f + 5f + level * 0.45f) * quality), "aether"),
            ItemStat.CooldownReduction => new ItemAffix(stat, (GD.Randf() * 2.2f + 1.5f + level * 0.05f) * quality, "recovery"),
            _ => new ItemAffix(stat, 1f, stat.ToString()),
        };
    }

    private static string PickBase(EquipmentSlot slot)
    {
        string[] pool = slot switch
        {
            EquipmentSlot.Weapon => WeaponBases,
            EquipmentSlot.Focus => FocusBases,
            EquipmentSlot.Helm => HelmBases,
            EquipmentSlot.Chest => ChestBases,
            EquipmentSlot.Gloves => GloveBases,
            EquipmentSlot.Boots => BootBases,
            EquipmentSlot.Belt => BeltBases,
            EquipmentSlot.Ring => RingBases,
            _ => AmuletBases,
        };
        return pool[GD.RandRange(0, pool.Length - 1)];
    }
}
