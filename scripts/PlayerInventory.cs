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

    public float DamagePercent => Sum(ItemStat.DamagePercent);
    public float HealthBonus => Sum(ItemStat.MaxHealth);
    public float ManaBonus => Sum(ItemStat.MaxMana);
    public float CooldownReduction => Mathf.Min(45f, Sum(ItemStat.CooldownReduction));

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
        Equipped.TryGetValue(candidate.Slot, out ItemData? current);
        string text = current == null ? "EMPTY SLOT — ALL VALUES ARE GAINS" : $"VS {current.Name.ToUpperInvariant()}";
        foreach (ItemStat stat in Enum.GetValues<ItemStat>())
        {
            float delta = candidate.GetStat(stat) - (current?.GetStat(stat) ?? 0f);
            if (Mathf.Abs(delta) < 0.05f) continue;
            string sign = delta > 0f ? "+" : "";
            string suffix = stat is ItemStat.DamagePercent or ItemStat.CooldownReduction ? "%" : "";
            text += $"\n{sign}{delta:0.#}{suffix} {stat}";
        }
        return text;
    }

    public string BuildEquipmentSummary()
    {
        return $"GEAR  +{DamagePercent:0.#}% DMG  •  +{HealthBonus:0} HP  •  +{ManaBonus:0} MANA  •  +{CooldownReduction:0.#}% CDR     MATERIALS  {SalvageDust} DUST  •  {SigilShards} SHARDS  •  {SigilCores} CORES";
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
