using Godot;

namespace Sigilwoven;

public partial class LootDrop : Node3D
{
    public ItemData Item { get; private set; } = new();
    private float _age;
    private bool _collected;
    private PlayerController? _player;

    public static void Spawn(Node scene, Vector3 position, ItemData item)
    {
        var drop = new LootDrop { Item = item };
        scene.AddChild(drop);
        drop.GlobalPosition = position;
    }

    public override void _Ready()
    {
        AddToGroup("loot");
        Color color = Item.RarityColor;
        var material = new StandardMaterial3D
        {
            AlbedoColor = color,
            EmissionEnabled = true,
            Emission = color,
            EmissionEnergyMultiplier = Item.Rarity >= ItemRarity.Rare ? 3.5f : 1.8f,
        };
        var itemMesh = new MeshInstance3D
        {
            Mesh = BuildLootMesh(Item.Slot),
            Position = new Vector3(0f, 0.28f, 0f),
            RotationDegrees = new Vector3(8f, 25f, 4f),
        };
        itemMesh.SetSurfaceOverrideMaterial(0, material);
        AddChild(itemMesh);

        float beamHeight = Item.Rarity switch
        {
            ItemRarity.Unique => 5.5f,
            ItemRarity.Sigilforged => 4.5f,
            ItemRarity.Rare => 3.2f,
            _ => 1.5f,
        };
        var beam = new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.025f, BottomRadius = 0.12f, Height = beamHeight },
            Position = new Vector3(0f, beamHeight * 0.5f, 0f),
        };
        beam.SetSurfaceOverrideMaterial(0, material);
        AddChild(beam);

        var label = new Label3D
        {
            Text = $"[F] {Item.Name}\n{Item.Rarity.ToString().ToUpperInvariant()}  •  iLVL {Item.ItemLevel}",
            FontSize = 28,
            OutlineSize = 7,
            Position = new Vector3(0f, 0.85f, 0f),
            Modulate = color,
            NoDepthTest = true,
        };
        AddChild(label);
    }

    private static PrimitiveMesh BuildLootMesh(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Weapon => new BoxMesh { Size = new Vector3(0.13f, 0.12f, 0.9f) },
            EquipmentSlot.Focus => new SphereMesh { Radius = 0.24f, Height = 0.48f },
            EquipmentSlot.Helm => new CylinderMesh { TopRadius = 0.18f, BottomRadius = 0.3f, Height = 0.34f },
            EquipmentSlot.Chest => new BoxMesh { Size = new Vector3(0.48f, 0.55f, 0.2f) },
            EquipmentSlot.Gloves => new CapsuleMesh { Radius = 0.16f, Height = 0.4f },
            EquipmentSlot.Boots => new BoxMesh { Size = new Vector3(0.25f, 0.2f, 0.48f) },
            EquipmentSlot.Belt => new TorusMesh { InnerRadius = 0.19f, OuterRadius = 0.27f },
            EquipmentSlot.Ring => new TorusMesh { InnerRadius = 0.13f, OuterRadius = 0.21f },
            EquipmentSlot.Amulet => new PrismMesh { Size = new Vector3(0.35f, 0.42f, 0.15f) },
            _ => new BoxMesh { Size = new Vector3(0.34f, 0.12f, 0.52f) },
        };
    }

    public override void _Process(double delta)
    {
        _age += (float)delta;
        Position = new Vector3(Position.X, 0.12f + Mathf.Sin(_age * 2.4f) * 0.045f, Position.Z);
        _player ??= GetTree().GetFirstNodeInGroup("player") as PlayerController;
        if (_player != null && IsInstanceValid(_player))
        {
            Visible = _player.Inventory.ShouldShow(Item);
        }
        if (_age >= 90f)
        {
            QueueFree();
        }
    }

    public bool Collect(PlayerController player)
    {
        if (!player.Inventory.ShouldShow(Item))
        {
            return false;
        }
        if (_collected || !player.Inventory.TryAdd(Item))
        {
            if (!_collected)
            {
                player.ShowCombatMessage("INVENTORY FULL — OPEN [I]");
            }
            return false;
        }
        _collected = true;
        player.ShowCombatMessage($"LOOT: {Item.Name.ToUpperInvariant()}");
        QueueFree();
        return true;
    }
}
