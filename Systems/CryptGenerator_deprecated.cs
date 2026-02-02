using System.Collections.Generic;
using StoneHammer.Systems;

namespace StoneHammer.Systems
{
    public static class CryptGenerator
    {
        public static ProceduralAsset Generate(int depth)
        {
            return depth switch
            {
                1 => GenerateLevel1(),
                2 => GenerateLevel2(),
                3 => GenerateLevel3(),
                _ => GenerateLevel1() // Fallback
            };
        }

        private static ProceduralAsset GenerateLevel1()
        {
            var asset = new ProceduralAsset
            {
                Name = "The Vestibule (Level 1)",
                Type = "Procedural",
                Parts = new List<ProceduralPart>
                {
                    // Floor (Brick Texture - Dark Slate Blue) - OLD: Stone/#CBD5E0 -> NEW: Brick/#334155
                    new ProceduralPart { Id = "floor", Shape = "Box", Position = new float[] { 0, -0.5f, 0 }, Scale = new float[] { 180, 1, 240 }, ColorHex = "#334155", Material = "Brick" },
                    // Roof (Hight 60)
                    new ProceduralPart { Id = "roof", Shape = "Box", Position = new float[] { 0, 60, 0 }, Scale = new float[] { 180, 1, 240 }, ColorHex = "#718096", Material = "Stone" },
                    // Walls (Adjusted positions for 3x size)
                    new ProceduralPart { Id = "wall_n", Shape = "Box", Position = new float[] { 0, 30, 120 }, Scale = new float[] { 180, 60, 1 }, ColorHex = "#A0AEC0", Material = "Stone" },
                    new ProceduralPart { Id = "wall_s", Shape = "Box", Position = new float[] { 0, 30, -120 }, Scale = new float[] { 180, 60, 1 }, ColorHex = "#A0AEC0", Material = "Stone" },
                    new ProceduralPart { Id = "wall_e", Shape = "Box", Position = new float[] { 90, 30, 0 }, Scale = new float[] { 1, 60, 240 }, ColorHex = "#A0AEC0", Material = "Stone" },
                    new ProceduralPart { Id = "wall_w", Shape = "Box", Position = new float[] { -90, 30, 0 }, Scale = new float[] { 1, 60, 240 }, ColorHex = "#A0AEC0", Material = "Stone" },
                    // New: Entrance Stairs (Green/Up is meaningless here, but for consistency)
                    new ProceduralPart { Id = "stairs_up", Shape = "Box", Position = new float[] { 0, 0, -110 }, Scale = new float[] { 20, 10, 20 }, ColorHex = "#48BB78", Material = "Stone" },
                    // Exit Stairs (Blue/Down)
                    new ProceduralPart { Id = "stairs_down", Shape = "Box", Position = new float[] { 0, 0, 110 }, Scale = new float[] { 20, 10, 20 }, ColorHex = "#4299E1", Material = "Stone" }
                },
                Children = new List<ChildAsset>
                {
                     // Mobs - Group 1 (Near Entrance) - Tight Formation
                     new ChildAsset { Path = "assets/skeleton.json", Name = "Skeleton_Lvl1_G1_A", Transform = new { Position = new float[] { -5, 0, 20 }, Rotation = new float[] { 0, 135, 0 } } },
                     new ChildAsset { Path = "assets/skeleton.json", Name = "Skeleton_Lvl1_G1_B", Transform = new { Position = new float[] { 5, 0, 20 }, Rotation = new float[] { 0, -135, 0 } } },
                     new ChildAsset { Path = "assets/skeleton.json", Name = "Skeleton_Lvl1_G1_C", Transform = new { Position = new float[] { 0, 0, 25 }, Rotation = new float[] { 0, 180, 0 } } },

                     // Mobs - Group 2 (Guarding Exit) - Tight Formation
                     new ChildAsset { Path = "assets/skeleton.json", Name = "Skeleton_Lvl1_G2_A", Transform = new { Position = new float[] { -5, 0, 80 }, Rotation = new float[] { 0, 135, 0 } } },
                     new ChildAsset { Path = "assets/skeleton.json", Name = "Skeleton_Lvl1_G2_B", Transform = new { Position = new float[] { 5, 0, 80 }, Rotation = new float[] { 0, -135, 0 } } },
                     new ChildAsset { Path = "assets/skeleton.json", Name = "Skeleton_Lvl1_G2_C", Transform = new { Position = new float[] { 0, 0, 85 }, Rotation = new float[] { 0, 180, 0 } } },
                     
                     // Interactables
                     new ChildAsset { Path = "assets/stairs.json", Name = "StairsDown", Transform = new { Position = new float[] { 0, 0, 110 }, Rotation = new float[] { 0, 0, 0 } } },
                     new ChildAsset { Path = "assets/exit_crystal.json", Name = "CryptExit", Transform = new { Position = new float[] { 0, 10, -100 } } }
                }
            };
            return asset;
        }

        private static ProceduralAsset GenerateLevel2()
        {
            var asset = new ProceduralAsset
            {
                Name = "The Catacombs (Level 2)",
                Type = "Procedural",
                Parts = new List<ProceduralPart>
                {
                    // Floor - OLD: Stone/#CBD5E0 -> NEW: Brick/#334155
                    new ProceduralPart { Id = "floor", Shape = "Box", Position = new float[] { 0, -0.5f, 0 }, Scale = new float[] { 300, 1, 300 }, ColorHex = "#334155", Material = "Brick" },
                    // Roof (Height: 24)
                    new ProceduralPart { Id = "roof", Shape = "Box", Position = new float[] { 0, 24, 0 }, Scale = new float[] { 300, 1, 300 }, ColorHex = "#718096", Material = "Stone" },
                    // Walls
                    new ProceduralPart { Id = "wall_n", Shape = "Box", Position = new float[] { 0, 12, 150 }, Scale = new float[] { 300, 24, 1 }, ColorHex = "#A0AEC0", Material = "Stone" },
                    new ProceduralPart { Id = "wall_s", Shape = "Box", Position = new float[] { 0, 12, -150 }, Scale = new float[] { 300, 24, 1 }, ColorHex = "#A0AEC0", Material = "Stone" },
                    new ProceduralPart { Id = "wall_e", Shape = "Box", Position = new float[] { 150, 12, 0 }, Scale = new float[] { 1, 24, 300 }, ColorHex = "#A0AEC0", Material = "Stone" },
                    new ProceduralPart { Id = "wall_w", Shape = "Box", Position = new float[] { -150, 12, 0 }, Scale = new float[] { 1, 24, 300 }, ColorHex = "#A0AEC0", Material = "Stone" }
                },
                Children = new List<ChildAsset>
                {
                    // Stairs Up (Green)
                    new ChildAsset { Path = "assets/stairs_up.json", Name = "StairsUp", Transform = new { Position = new float[] { -130, 0, -130 }, Rotation = new float[] { 0, 180, 0 } } },
                    // Stairs Down (Blue)
                    new ChildAsset { Path = "assets/stairs_down.json", Name = "StairsDown", Transform = new { Position = new float[] { 130, 0, 130 } } }
                }
            };

            // Procedural Maze Pillars (More pillars for larger space)
            var random = new System.Random(12345); 
            for (int i = 0; i < 60; i++) // Increased from 20 to 60
            {
                float x = random.Next(-130, 130);
                float z = random.Next(-130, 130);
                
                asset.Parts.Add(new ProceduralPart 
                { 
                    Id = $"pillar_{i}", 
                    Shape = "Box", 
                    Position = new float[] { x, 12, z }, 
                    Scale = new float[] { 6, 24, 6 }, 
                    ColorHex = "#A0AEC0", 
                    Material = "Stone" 
                });
            }

            return asset;
        }

        private static ProceduralAsset GenerateLevel3()
        {
             var asset = new ProceduralAsset
            {
                Name = "The Pit (Level 3)",
                Type = "Procedural",
                Parts = new List<ProceduralPart>
                {
                    // Floor - OLD: Stone/#E53E3E -> NEW: Brick/#334155 (Uniformity request)
                    new ProceduralPart { Id = "floor", Shape = "Box", Position = new float[] { 0, -0.5f, 0 }, Scale = new float[] { 150, 1, 150 }, ColorHex = "#334155", Material = "Brick" }, 
                     // Roof (Height 45)
                    new ProceduralPart { Id = "roof", Shape = "Box", Position = new float[] { 0, 45, 0 }, Scale = new float[] { 150, 1, 150 }, ColorHex = "#9B2C2C", Material = "Stone" },
                },
                Children = new List<ChildAsset>
                {
                    // Stairs Up (Green)
                    new ChildAsset { Path = "assets/stairs_up.json", Name = "StairsUp", Transform = new { Position = new float[] { 0, 0, -60 }, Rotation = new float[] { 0, 180, 0 } } }
                }
            };
            return asset;
        }
    }
}
