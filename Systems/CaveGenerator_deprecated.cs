using System.Collections.Generic;
using StoneHammer.Systems;

namespace StoneHammer.Systems
{
    public static class CaveGenerator
    {
        public static ProceduralAsset Generate(int depth)
        {
            return depth switch
            {
                1 => GenerateLevel1(),
                2 => GenerateLevel2(),
                _ => GenerateLevel1()
            };
        }

        private static ProceduralAsset GenerateLevel1()
        {
            var asset = new ProceduralAsset
            {
                Name = "Goblin Den (Level 1)",
                Type = "Procedural",
                Parts = new List<ProceduralPart>
                {
                    // Floor (Dirt/Mud)
                    new ProceduralPart { Id = "floor", Shape = "Box", Position = new float[] { 0, -0.5f, 0 }, Scale = new float[] { 200, 1, 200 }, ColorHex = "#5D4037", Material = "Dirt" },
                    // Roof (High Cave Ceiling)
                    new ProceduralPart { Id = "roof", Shape = "Box", Position = new float[] { 0, 40, 0 }, Scale = new float[] { 200, 1, 200 }, ColorHex = "#3E2723", Material = "Stone" },
                    // Rough Walls
                    new ProceduralPart { Id = "wall_n", Shape = "Box", Position = new float[] { 0, 20, 100 }, Scale = new float[] { 200, 40, 10 }, ColorHex = "#4E342E", Material = "Stone" },
                    new ProceduralPart { Id = "wall_s", Shape = "Box", Position = new float[] { 0, 20, -100 }, Scale = new float[] { 200, 40, 10 }, ColorHex = "#4E342E", Material = "Stone" },
                    new ProceduralPart { Id = "wall_e", Shape = "Box", Position = new float[] { 100, 20, 0 }, Scale = new float[] { 10, 40, 200 }, ColorHex = "#4E342E", Material = "Stone" },
                    new ProceduralPart { Id = "wall_w", Shape = "Box", Position = new float[] { -100, 20, 0 }, Scale = new float[] { 10, 40, 200 }, ColorHex = "#4E342E", Material = "Stone" },
                },
                Children = new List<ChildAsset>
                {
                     // Goblin Camp
                     new ChildAsset { Path = "assets/goblin.json", Name = "Goblin_Lvl1_G1_A", Transform = new { Position = new float[] { -10, 0, 20 }, Rotation = new float[] { 0, 135, 0 } } },
                     new ChildAsset { Path = "assets/goblin.json", Name = "Goblin_Lvl1_G1_B", Transform = new { Position = new float[] { 10, 0, 20 }, Rotation = new float[] { 0, -135, 0 } } },
                     new ChildAsset { Path = "assets/goblin.json", Name = "Goblin_Lvl1_G1_C", Transform = new { Position = new float[] { 0, 0, 30 }, Rotation = new float[] { 0, 180, 0 } } },

                     // Spider Ambush
                     new ChildAsset { Path = "assets/spider.json", Name = "Spider_Lvl1_G2_A", Transform = new { Position = new float[] { -20, 0, 60 }, Rotation = new float[] { 0, 45, 0 } } },
                     new ChildAsset { Path = "assets/spider.json", Name = "Spider_Lvl1_G2_B", Transform = new { Position = new float[] { 20, 0, 60 }, Rotation = new float[] { 0, -45, 0 } } },
                     new ChildAsset { Path = "assets/spider.json", Name = "Spider_Lvl1_G2_C", Transform = new { Position = new float[] { 0, 0, 70 }, Rotation = new float[] { 0, 180, 0 } } },
                     
                     // Exit
                     new ChildAsset { Path = "assets/exit_crystal.json", Name = "CaveExit", Transform = new { Position = new float[] { 0, 5, -80 } } }
                }
            };
            
            // Random Stalagmites
            var rng = new System.Random();
            for(int i=0; i<30; i++)
            {
                float x = rng.Next(-90, 90);
                float z = rng.Next(-90, 90);
                asset.Parts.Add(new ProceduralPart 
                { 
                    Id = $"stalagmite_{i}", 
                    Shape = "Cone", 
                    Position = new float[] { x, 0, z }, 
                    Scale = new float[] { 2, rng.Next(5, 15), 2 }, 
                    ColorHex = "#6D4C41", 
                    Material = "Stone" 
                });
            }
            
            return asset;
        }

        private static ProceduralAsset GenerateLevel2()
        {
             // Placeholder for deeper levels
             return GenerateLevel1();
        }
    }
}
