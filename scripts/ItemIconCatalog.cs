using Godot;

namespace Sigilwoven;

public static class ItemIconCatalog
{
    private const float CellSize = 313.5f;
    private static Texture2D? _atlas;

    public static Texture2D Get(EquipmentSlot slot)
    {
        _atlas ??= GD.Load<Texture2D>("res://assets/ui/equipment-icon-atlas-v1.png");
        int index = slot switch
        {
            EquipmentSlot.Weapon => 0,
            EquipmentSlot.Focus => 1,
            EquipmentSlot.Helm => 2,
            EquipmentSlot.Chest => 3,
            EquipmentSlot.Gloves => 4,
            EquipmentSlot.Boots => 5,
            EquipmentSlot.Belt => 6,
            EquipmentSlot.Ring => 7,
            EquipmentSlot.Amulet => 8,
            _ => 15,
        };
        return new AtlasTexture
        {
            Atlas = _atlas,
            Region = new Rect2((index % 4) * CellSize, (index / 4) * CellSize, CellSize, CellSize),
        };
    }
}
