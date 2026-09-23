using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Sigilwoven;

public partial class PlayerInventory : Node
{
    public const int Capacity = 24;
    private const string SavePath = "user://inventory.json";
    public List<ItemData> Items { get; } = new();
    public Dictionary<EquipmentSlot, ItemData> Equipped { get; } = new();
    public event Action? Changed;
    public int SalvageDust { get; private set; }
    public int SigilShards { get; private set; }
    public int SigilCores { get; private set; }
    public ItemRarity MinimumVisibleRarity { get; private set; } = ItemRarity.Common;
    public HeroClass ActiveClass { get; private set; } = HeroClass.Runeblade;

    public float DamagePercent => Sum(ItemStat.DamagePercent);
    public float HealthBonus => Sum(ItemStat.MaxHealth);
    public float ManaBonus => Sum(ItemStat.MaxMana);
    public float CooldownReduction => Mathf.Min(45f, Sum(ItemStat.CooldownReduction));
    public float Armor => Sum(ItemStat.Armor);
    public float FireResistance => Mathf.Min(75f, Sum(ItemStat.FireResistance));
    public float ColdResistance => Mathf.Min(75f, Sum(ItemStat.ColdResistance));
    public float LightningResistance => Mathf.Min(75f, Sum(ItemStat.LightningResistance));
    public float PoisonResistance => Mathf.Min(75f, Sum(ItemStat.PoisonResistance));
    public float CriticalChance => Mathf.Min(65f, 5f + Sum(ItemStat.CriticalChance));
    public float CriticalDamage => 150f + Sum(ItemStat.CriticalDamage);
    public float ElementalPenetration => Mathf.Min(60f, Sum(ItemStat.ElementalPenetration));

    public override void _Ready()
    {
        LoadInventory();
    }

    public bool TryAdd(ItemData item)
    {
        if (Items.Count >= Capacity)
        {
            return false;
        }
        Items.Add(item);
        CommitChanges();
        return true;
    }

    public bool Equip(ItemData item)
    {
        if (!item.CanEquip(ActiveClass)) return false;
        if (!Items.Remove(item))
        {
            return false;
        }
        if (Equipped.TryGetValue(item.Slot, out ItemData? previous))
        {
            Items.Add(previous);
        }
        Equipped[item.Slot] = item;
        CommitChanges();
        return true;
    }

    public void SetActiveClass(HeroClass heroClass)
    {
        ActiveClass = heroClass;
        Changed?.Invoke();
    }

    public bool TrySpendDust(int amount)
    {
        if (amount <= 0 || SalvageDust < amount) return false;
        SalvageDust -= amount;
        CommitChanges();
        return true;
    }

    public void GrantCurrency(int dust = 0, int shards = 0, int cores = 0)
    {
        SalvageDust += Mathf.Max(0, dust);
        SigilShards += Mathf.Max(0, shards);
        SigilCores += Mathf.Max(0, cores);
        CommitChanges();
    }

    public bool Unequip(EquipmentSlot slot)
    {
        if (Items.Count >= Capacity || !Equipped.Remove(slot, out ItemData? item)) return false;
        Items.Add(item);
        CommitChanges();
        return true;
    }

    public void SortInventory()
    {
        Items.Sort((left, right) =>
        {
            int rarity = right.Rarity.CompareTo(left.Rarity);
            if (rarity != 0) return rarity;
            int power = right.GearScore.CompareTo(left.GearScore);
            return power != 0 ? power : string.Compare(left.Name, right.Name, StringComparison.Ordinal);
        });
        CommitChanges();
    }

    public int SalvageBelowRare()
    {
        int salvaged = 0;
        for (int i = Items.Count - 1; i >= 0; i--)
        {
            if (Items[i].Rarity >= ItemRarity.Rare) continue;
            ItemData item = Items[i];
            Items.RemoveAt(i);
            SalvageDust += item.Rarity == ItemRarity.Magic ? 5 : 2;
            salvaged++;
        }
        if (salvaged > 0) CommitChanges();
        return salvaged;
    }

    public bool Salvage(ItemData item)
    {
        if (!Items.Remove(item))
        {
            return false;
        }
        SalvageDust += item.Rarity switch
        {
            ItemRarity.Common => 2,
            ItemRarity.Magic => 5,
            ItemRarity.Rare => 12,
            ItemRarity.Sigilforged => 24,
            ItemRarity.Unique => 40,
            _ => 1,
        };
        if (item.Rarity >= ItemRarity.Rare) SigilShards += item.Rarity == ItemRarity.Rare ? 1 : 3;
        if (item.Rarity == ItemRarity.Unique) SigilCores++;
        CommitChanges();
        return true;
    }

    public void CycleLootFilter()
    {
        MinimumVisibleRarity = MinimumVisibleRarity switch
        {
            ItemRarity.Common => ItemRarity.Magic,
            ItemRarity.Magic => ItemRarity.Rare,
            _ => ItemRarity.Common,
        };
        CommitChanges();
    }

    public bool ShouldShow(ItemData item) => item.Rarity >= MinimumVisibleRarity;

    public string BuildComparison(ItemData candidate)
    {
        if (!candidate.CanEquip(ActiveClass)) return $"CLASS LOCKED — {ActiveClass.ToString().ToUpperInvariant()} CANNOT EQUIP {candidate.WeaponType.ToString().ToUpperInvariant()}";
        Equipped.TryGetValue(candidate.Slot, out ItemData? current);
        float scoreDelta = EstimateCombatValue(candidate) - (current == null ? 0f : EstimateCombatValue(current));
        string verdict = current == null ? "EMPTY SLOT — PURE UPGRADE" : scoreDelta >= 0f ? $"▲ ESTIMATED BUILD GAIN +{scoreDelta:0.#}" : $"▼ ESTIMATED BUILD LOSS {scoreDelta:0.#}";
        string text = current == null ? verdict : $"VS {current.Name.ToUpperInvariant()}\n{verdict}";
        foreach (ItemStat stat in Enum.GetValues<ItemStat>())
        {
            float delta = candidate.GetStat(stat) - (current?.GetStat(stat) ?? 0f);
            if (Mathf.Abs(delta) < 0.05f) continue;
            string sign = delta > 0f ? "+" : "";
            string suffix = stat is ItemStat.DamagePercent or ItemStat.CooldownReduction or ItemStat.FireResistance or ItemStat.ColdResistance or ItemStat.LightningResistance or ItemStat.PoisonResistance or ItemStat.CriticalChance or ItemStat.CriticalDamage or ItemStat.ElementalPenetration ? "%" : "";
            text += $"\n{sign}{delta:0.#}{suffix} {stat}";
        }
        return text;
    }

    private static float EstimateCombatValue(ItemData item)
    {
        return item.GetStat(ItemStat.DamagePercent) * 2.2f
            + item.GetStat(ItemStat.CriticalChance) * 2f
            + item.GetStat(ItemStat.CriticalDamage) * 0.35f
            + item.GetStat(ItemStat.CooldownReduction) * 1.6f
            + item.GetStat(ItemStat.MaxHealth) * 0.12f
            + item.GetStat(ItemStat.Armor) * 0.08f
            + (item.GetStat(ItemStat.FireResistance) + item.GetStat(ItemStat.ColdResistance) + item.GetStat(ItemStat.LightningResistance) + item.GetStat(ItemStat.PoisonResistance)) * 0.45f
            + item.GetStat(ItemStat.ElementalPenetration) * 1.5f;
    }

    public string BuildEquipmentSummary()
    {
        return $"GEAR  +{DamagePercent:0.#}% DMG  •  {CriticalChance:0.#}% CRIT  •  {Armor:0} ARMOR  •  +{HealthBonus:0} HP  •  +{CooldownReduction:0.#}% CDR";
    }

    private float Sum(ItemStat stat)
    {
        float total = 0f;
        foreach (ItemData item in Equipped.Values)
        {
            total += item.GetStat(stat);
        }
        return total;
    }

    private void CommitChanges()
    {
        SaveInventory();
        Changed?.Invoke();
    }

    private void SaveInventory()
    {
        try
        {
            var data = new InventorySaveData
            {
                Version = 1,
                Items = Items.ToArray(),
                Equipped = new List<ItemData>(Equipped.Values).ToArray(),
                SalvageDust = SalvageDust,
                SigilShards = SigilShards,
                SigilCores = SigilCores,
                MinimumVisibleRarity = MinimumVisibleRarity,
            };
            using FileAccess? file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
            if (file == null)
            {
                GD.PushWarning($"[Inventory] Could not open save file: {FileAccess.GetOpenError()}");
                return;
            }
            file.StoreString(JsonSerializer.Serialize(data));
        }
        catch (Exception exception)
        {
            GD.PushWarning($"[Inventory] Save failed: {exception.Message}");
        }
    }

    private void LoadInventory()
    {
        if (!FileAccess.FileExists(SavePath))
        {
            return;
        }
        try
        {
            using FileAccess? file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
            InventorySaveData? data = file == null ? null : JsonSerializer.Deserialize<InventorySaveData>(file.GetAsText());
            if (data == null || data.Version != 1)
            {
                return;
            }
            Items.Clear();
            Equipped.Clear();
            foreach (ItemData item in data.Items)
            {
                if (Items.Count < Capacity) Items.Add(item);
            }
            foreach (ItemData item in data.Equipped)
            {
                if (Enum.IsDefined(item.Slot)) Equipped[item.Slot] = item;
            }
            SalvageDust = Mathf.Max(0, data.SalvageDust);
            SigilShards = Mathf.Max(0, data.SigilShards);
            SigilCores = Mathf.Max(0, data.SigilCores);
            MinimumVisibleRarity = data.MinimumVisibleRarity is ItemRarity.Common or ItemRarity.Magic or ItemRarity.Rare
                ? data.MinimumVisibleRarity : ItemRarity.Common;
            GD.Print($"[Inventory] Loaded {Items.Count} stored and {Equipped.Count} equipped items.");
        }
        catch (Exception exception)
        {
            GD.PushWarning($"[Inventory] Load failed: {exception.Message}");
        }
    }

    private sealed class InventorySaveData
    {
        public int Version { get; set; }
        public ItemData[] Items { get; set; } = Array.Empty<ItemData>();
        public ItemData[] Equipped { get; set; } = Array.Empty<ItemData>();
        public int SalvageDust { get; set; }
        public int SigilShards { get; set; }
        public int SigilCores { get; set; }
        public ItemRarity MinimumVisibleRarity { get; set; }
    }
}
