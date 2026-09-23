using Godot;

namespace Sigilwoven;

public partial class LootDrop : Node3D
{
    public ItemData Item { get; private set; } = new();
    private float _age;
    private bool _collected;
    private PlayerController? _player;
    private Label3D? _label;
    private Sprite3D? _icon;

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
        Node3D itemModel = BuildLootModel(Item.Slot, material);
        itemModel.Position = new Vector3(0f, 0.28f, 0f);
        itemModel.RotationDegrees = new Vector3(8f, 25f, 4f);
        AddChild(itemModel);

        _icon = new Sprite3D
        {
            Texture = ItemIconCatalog.Get(Item.Slot),
            Position = new Vector3(0f, 1.35f, 0f),
            PixelSize = 0.0022f,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            NoDepthTest = true,
            Modulate = color.Lerp(Colors.White, 0.68f),
        };
        AddChild(_icon);

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

        _label = new Label3D
        {
            Text = BuildGroundLabel(false),
            FontSize = 28,
            OutlineSize = 7,
            Position = new Vector3(0f, 2.05f, 0f),
            Modulate = color,
            NoDepthTest = true,
        };
        AddChild(_label);
    }

    private static Node3D BuildLootModel(EquipmentSlot slot, Material material)
    {
        var root = new Node3D();
        switch (slot)
        {
            case EquipmentSlot.Weapon:
                AddPart(root, new PrismMesh { Size = new Vector3(0.16f, 0.12f, 1.05f) }, material, new Vector3(0f, 0f, -0.18f));
                AddPart(root, new BoxMesh { Size = new Vector3(0.62f, 0.13f, 0.12f) }, material, new Vector3(0f, 0f, 0.38f));
                AddPart(root, new CylinderMesh { TopRadius = 0.07f, BottomRadius = 0.09f, Height = 0.48f, RadialSegments = 8 }, material, new Vector3(0f, 0f, 0.65f), new Vector3(Mathf.Pi * 0.5f, 0f, 0f));
                AddPart(root, new SphereMesh { Radius = 0.13f, Height = 0.22f }, material, new Vector3(0f, 0f, 0.93f));
                break;
            case EquipmentSlot.Focus:
                AddPart(root, new SphereMesh { Radius = 0.28f, Height = 0.56f }, material, Vector3.Zero);
                AddPart(root, new TorusMesh { InnerRadius = 0.34f, OuterRadius = 0.4f }, material, Vector3.Zero, new Vector3(0.35f, 0f, 0f));
                AddPart(root, new TorusMesh { InnerRadius = 0.42f, OuterRadius = 0.47f }, material, Vector3.Zero, new Vector3(0f, 0.55f, 0.32f));
                break;
            case EquipmentSlot.Helm:
                AddPart(root, new CylinderMesh { TopRadius = 0.19f, BottomRadius = 0.32f, Height = 0.4f, RadialSegments = 8 }, material, Vector3.Zero);
                AddPart(root, new BoxMesh { Size = new Vector3(0.38f, 0.09f, 0.32f) }, material, new Vector3(0f, 0.02f, -0.23f));
                AddPart(root, new PrismMesh { Size = new Vector3(0.08f, 0.32f, 0.08f) }, material, new Vector3(0f, 0.35f, 0f));
                break;
            case EquipmentSlot.Chest:
                AddPart(root, new BoxMesh { Size = new Vector3(0.58f, 0.66f, 0.25f) }, material, Vector3.Zero);
                AddPart(root, new BoxMesh { Size = new Vector3(0.12f, 0.58f, 0.29f) }, material, new Vector3(0f, 0f, -0.16f), new Vector3(0f, 0f, 0.7f));
                AddPart(root, new SphereMesh { Radius = 0.18f, Height = 0.28f }, material, new Vector3(-0.38f, 0.22f, 0f), scale: new Vector3(1.25f, 0.7f, 1f));
                AddPart(root, new SphereMesh { Radius = 0.18f, Height = 0.28f }, material, new Vector3(0.38f, 0.22f, 0f), scale: new Vector3(1.25f, 0.7f, 1f));
                break;
            case EquipmentSlot.Gloves:
                AddPart(root, new CapsuleMesh { Radius = 0.16f, Height = 0.44f }, material, new Vector3(-0.2f, 0f, 0f), new Vector3(0f, 0f, -0.32f));
                AddPart(root, new CapsuleMesh { Radius = 0.16f, Height = 0.44f }, material, new Vector3(0.2f, 0f, 0f), new Vector3(0f, 0f, 0.32f));
                break;
            case EquipmentSlot.Boots:
                AddPart(root, new BoxMesh { Size = new Vector3(0.25f, 0.34f, 0.52f) }, material, new Vector3(-0.18f, 0f, 0f));
                AddPart(root, new BoxMesh { Size = new Vector3(0.25f, 0.34f, 0.52f) }, material, new Vector3(0.18f, 0f, 0f));
                AddPart(root, new TorusMesh { InnerRadius = 0.13f, OuterRadius = 0.18f }, material, new Vector3(-0.18f, 0.2f, 0f));
                AddPart(root, new TorusMesh { InnerRadius = 0.13f, OuterRadius = 0.18f }, material, new Vector3(0.18f, 0.2f, 0f));
                break;
            case EquipmentSlot.Belt:
                AddPart(root, new TorusMesh { InnerRadius = 0.24f, OuterRadius = 0.32f }, material, Vector3.Zero, new Vector3(Mathf.Pi * 0.5f, 0f, 0f));
                AddPart(root, new BoxMesh { Size = new Vector3(0.2f, 0.24f, 0.1f) }, material, new Vector3(0f, 0f, -0.31f));
                break;
            case EquipmentSlot.Ring:
                AddPart(root, new TorusMesh { InnerRadius = 0.15f, OuterRadius = 0.23f }, material, Vector3.Zero, new Vector3(Mathf.Pi * 0.5f, 0f, 0f));
                AddPart(root, new PrismMesh { Size = new Vector3(0.2f, 0.23f, 0.16f) }, material, new Vector3(0f, 0.23f, 0f));
                break;
            case EquipmentSlot.Amulet:
                AddPart(root, new TorusMesh { InnerRadius = 0.3f, OuterRadius = 0.335f }, material, new Vector3(0f, 0.1f, 0f));
                AddPart(root, new PrismMesh { Size = new Vector3(0.34f, 0.48f, 0.15f) }, material, new Vector3(0f, -0.3f, 0f));
                AddPart(root, new SphereMesh { Radius = 0.09f, Height = 0.18f }, material, new Vector3(0f, -0.3f, -0.1f));
                break;
        }
        return root;
    }

    private static void AddPart(Node3D root, PrimitiveMesh mesh, Material material, Vector3 position, Vector3? rotation = null, Vector3? scale = null)
    {
        var part = new MeshInstance3D { Mesh = mesh, Position = position, Rotation = rotation ?? Vector3.Zero, Scale = scale ?? Vector3.One };
        part.SetSurfaceOverrideMaterial(0, material);
        root.AddChild(part);
    }

    public override void _Process(double delta)
    {
        _age += (float)delta;
        Position = new Vector3(Position.X, 0.12f + Mathf.Sin(_age * 2.4f) * 0.045f, Position.Z);
        _player ??= GetTree().GetFirstNodeInGroup("player") as PlayerController;
        if (_player != null && IsInstanceValid(_player))
        {
            Visible = _player.Inventory.ShouldShow(Item);
            float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);
            if (_label != null) _label.Text = BuildGroundLabel(distance <= 5f);
            if (_icon != null) _icon.Scale = Vector3.One * (distance <= 5f ? 1.18f : 0.9f);
        }
        if (_age >= 90f)
        {
            QueueFree();
        }
    }

    private string BuildGroundLabel(bool detailed)
    {
        string header = $"[F] {Item.Name}\n{Item.Rarity.ToString().ToUpperInvariant()} {Item.Slot.ToString().ToUpperInvariant()}  •  PWR {Item.GearScore}";
        if (!detailed) return header;
        if (_player != null && IsInstanceValid(_player))
        {
            string comparison = _player.Inventory.BuildComparison(Item);
            header += comparison.Contains("▲") || comparison.Contains("PURE UPGRADE") ? "  •  ▲ UPGRADE" : comparison.Contains("▼") ? "  •  ▼ SIDEGRADE" : "";
        }
        string affixes = "";
        int shown = Mathf.Min(2, Item.Affixes.Count);
        for (int i = 0; i < shown; i++) affixes += $"\n◆ {Item.Affixes[i].Format()}";
        if (Item.Affixes.Count > shown) affixes += $"\n+{Item.Affixes.Count - shown} MORE AFFIXES";
        return header + affixes;
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
