namespace Sigilwoven;

/// <summary>Single source of truth for 3D collision layers and masks.</summary>
public static class PhysicsLayers
{
    public const uint World = 1u << 0;
    public const uint Player = 1u << 1;
    public const uint Enemy = 1u << 2;
    public const uint Ally = 1u << 3;
    public const uint PlayerProjectile = 1u << 4;
    public const uint Pickup = 1u << 5;
}
