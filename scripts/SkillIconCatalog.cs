using Godot;

namespace Sigilwoven;

/// <summary>Stable ID-based lookup into the 6x6 production icon atlas.</summary>
public static class SkillIconCatalog
{
    private const int Columns = 6;
    private const int CellSize = 209;
    private static Texture2D? _atlas;
    private static Texture2D? _classAtlas;

    public static Texture2D Get(string skillId)
    {
        _atlas ??= GD.Load<Texture2D>("res://assets/ui/skill-icon-atlas-v3.png");
        int index = SkillCatalog.All.FindIndex(skill => skill.Id == skillId);
        if (index >= 35 && index < 47)
        {
            _classAtlas ??= GD.Load<Texture2D>("res://assets/ui/class-skill-icon-atlas-v2.png");
            int classIndex = index - 35;
            float width = _classAtlas.GetWidth() / 4f;
            float height = _classAtlas.GetHeight() / 3f;
            return new AtlasTexture { Atlas = _classAtlas, Region = new Rect2((classIndex % 4) * width, (classIndex / 4) * height, width, height) };
        }
        if (index < 0 || index >= 35) index = 35;
        return new AtlasTexture
        {
            Atlas = _atlas,
            Region = new Rect2((index % Columns) * CellSize, (index / Columns) * CellSize, CellSize, CellSize),
        };
    }
}
