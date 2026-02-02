using System.Collections.Generic;

namespace StoneHammer.Systems
{
    public class VoxelPart
    {
        public string Id { get; set; } = ""; // Was missing, needed for bones
        public string Name { get; set; } = "";
        public float[] Offset { get; set; } = new float[3];
        public float[] Dimensions { get; set; } = new float[3];
        public float[] Pivot { get; set; } = new float[3];
        public float[] Rotation { get; set; } = new float[3];
        public string? HexColor { get; set; }
        public string? ParentLimb { get; set; } // New: For hierarchical rigs
        public int[] TextureOffset { get; set; } = new int[2];
        public int[]? TextureDimensions { get; set; }
    }

    public class VoxelAsset
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "Voxel";
        public List<VoxelPart> Parts { get; set; } = new();
        public VoxelProceduralColors? ProceduralColors { get; set; }
        public VoxelTextures? Textures { get; set; }
        public string? Skin { get; set; } // Base64 texture
        public string? Description { get; set; }
        public List<ChildAsset> Children { get; set; } = new();
    }

    public class VoxelProceduralColors
    {
        public string Skin { get; set; } = "#F1C27D";
        public string Shirt { get; set; } = "#FFFFFF";
        public string Pants { get; set; } = "#1A1A1A";
        public string Eyes { get; set; } = "#222222";
    }

    public class VoxelTextures
    {
        public string[]? Face { get; set; }
        public string[]? Hat { get; set; }
        public string[]? Chest { get; set; }
        public string[]? Arms { get; set; }
        public string[]? Legs { get; set; }
    }

    public class ProceduralPart
    {
        public string Id { get; set; } = "";
        public string Shape { get; set; } = "Box";
        public float[] Position { get; set; } = new float[3];
        public float[] Rotation { get; set; } = new float[3];
        public float[] Scale { get; set; } = new float[3] { 1, 1, 1 };
        public string ColorHex { get; set; } = "#FFFFFF";
        public string Material { get; set; } = "Plastic";
        public string? ParentId { get; set; }
        public string Operation { get; set; } = "Union";
    }

    public class ProceduralAsset
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "Procedural";
        public List<ProceduralPart> Parts { get; set; } = new();
        public List<TimelineEvent> Timeline { get; set; } = new();
        public List<ChildAsset> Children { get; set; } = new();
    }

    public class ChildAsset
    {
        public string Path { get; set; } = "";
        public string Name { get; set; } = "";
        public object? Transform { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class TimelineEvent
    {
        public double Time { get; set; }
        public string Action { get; set; } = ""; // Move, Rotate, Scale, Color
        public string TargetId { get; set; } = "";
        public object? Value { get; set; }
        public double Duration { get; set; } = 1.0;
    }

    // --- Data-Driven Dungeon Models ---

    public class TownRecipe
    {
        public List<StaticBuildingPlacement> StaticBuildings { get; set; } = new();
        public List<DungeonEntrancePlacement> DungeonEntrances { get; set; } = new();
        public DungeonTheme Theme { get; set; } = new();
    }

    public class StaticBuildingPlacement
    {
        public string AssetPath { get; set; } = "";
        public string Name { get; set; } = ""; // Unique ID for interaction
        public float[] Position { get; set; } = new float[3];
        public float[] Rotation { get; set; } = new float[3];
        public string? ActionId { get; set; } // e.g., "OpenShop"
    }

    public class DungeonEntrancePlacement
    {
        public string DungeonId { get; set; } = ""; // Links to DungeonRecipe.Id
        public string EntranceAssetPath { get; set; } = "";
        public float[] Position { get; set; } = new float[3];
        public float[] Rotation { get; set; } = new float[3];
    }

    public class DungeonRecipe
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public DungeonLayoutType LayoutType { get; set; } = DungeonLayoutType.Rooms;
        public DungeonTheme Theme { get; set; } = new();
        public List<DungeonEnemy> Enemies { get; set; } = new();
        public int DefaultDepth { get; set; } = 1;
    }

    public enum DungeonLayoutType
    {
        Rooms,
        Cave,
        Tunnel
    }

    public class DungeonTheme
    {
        public string WallColor { get; set; } = "#555555";
        public string WallMaterial { get; set; } = "Stone";
        public string FloorColor { get; set; } = "#444444";
        public string FloorMaterial { get; set; } = "Stone";
        public string AtmosphereColor { get; set; } = "#000000"; // For fog/skybox
    }

    public class DungeonEnemy
    {
        public string AssetPath { get; set; } = "";
        public string NamePrefix { get; set; } = "Enemy";
        public int HP { get; set; } = 20;
        public int XP { get; set; } = 10;
        public float SpawnChance { get; set; } = 1.0f; // Relative weight
    }
}
