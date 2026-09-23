using Godot;

namespace Sigilwoven;

/// <summary>CC0 photographic PBR materials with matching albedo, OpenGL normal and roughness maps.</summary>
public static class RealisticMaterialCatalog
{
    public static StandardMaterial3D GraveyardGround(float tiles = 14f) =>
        Create("graveyard-cobblestone", new Color(0.58f, 0.56f, 0.52f), tiles, 0f);

    public static StandardMaterial3D CryptWall(float tiles = 5f) =>
        Create("crypt-wall", new Color(0.62f, 0.61f, 0.58f), tiles, 0f);

    public static StandardMaterial3D ForgedIron(float tiles = 1f) =>
        Create("forged-iron", new Color(0.56f, 0.54f, 0.5f), tiles, 0.92f);

    private static StandardMaterial3D Create(string name, Color tint, float tiles, float metallic)
    {
        string root = $"res://assets/textures/pbr/{name}";
        return new StandardMaterial3D
        {
            AlbedoColor = tint,
            AlbedoTexture = GD.Load<Texture2D>($"{root}-albedo.jpg"),
            NormalEnabled = true,
            NormalTexture = GD.Load<Texture2D>($"{root}-normal.jpg"),
            NormalScale = 1f,
            Roughness = 1f,
            RoughnessTexture = GD.Load<Texture2D>($"{root}-roughness.jpg"),
            RoughnessTextureChannel = BaseMaterial3D.TextureChannel.Red,
            Metallic = metallic,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic,
            Uv1Scale = new Vector3(tiles, tiles, 1f),
        };
    }
}
