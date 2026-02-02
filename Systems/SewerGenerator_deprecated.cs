using System.Collections.Generic;
using StoneHammer.Systems;

namespace StoneHammer.Systems
{
    public static class SewerGenerator
    {
        public static ProceduralAsset Generate(int depth)
        {
            return GenerateLevel1();
        }

        private static ProceduralAsset GenerateLevel1()
        {
            var asset = new ProceduralAsset
            {
                Name = "The Sewers (Level 1)",
                Type = "Procedural",
                Parts = new List<ProceduralPart>
                {
                    // Floor (Concrete with Sludge Channel)
                    new ProceduralPart { Id = "floor_l", Shape = "Box", Position = new float[] { -20, -0.5f, 0 }, Scale = new float[] { 40, 1, 200 }, ColorHex = "#2F4F2F", Material = "Stone" },
                    new ProceduralPart { Id = "floor_r", Shape = "Box", Position = new float[] { 20, -0.5f, 0 }, Scale = new float[] { 40, 1, 200 }, ColorHex = "#2F4F2F", Material = "Stone" },
                    
                    // Sludge River center
                    new ProceduralPart { Id = "sludge", Shape = "Box", Position = new float[] { 0, -0.8f, 0 }, Scale = new float[] { 20, 1, 200 }, ColorHex = "#1a3300", Material = "Glow" },

                    // Roof (Arched Tunnel look - approximated with Box for now)
                    new ProceduralPart { Id = "roof", Shape = "Box", Position = new float[] { 0, 15, 0 }, Scale = new float[] { 80, 1, 200 }, ColorHex = "#333333", Material = "Stone" },
                    
                    // Walls
                    new ProceduralPart { Id = "wall_w", Shape = "Box", Position = new float[] { -40, 7, 0 }, Scale = new float[] { 2, 20, 200 }, ColorHex = "#444444", Material = "Brick" },
                    new ProceduralPart { Id = "wall_e", Shape = "Box", Position = new float[] { 40, 7, 0 }, Scale = new float[] { 2, 20, 200 }, ColorHex = "#444444", Material = "Brick" },
                    new ProceduralPart { Id = "wall_n", Shape = "Box", Position = new float[] { 0, 7, 100 }, Scale = new float[] { 80, 20, 2 }, ColorHex = "#444444", Material = "Brick" },
                    new ProceduralPart { Id = "wall_s", Shape = "Box", Position = new float[] { 0, 7, -100 }, Scale = new float[] { 80, 20, 2 }, ColorHex = "#444444", Material = "Brick" },
                },
                Children = new List<ChildAsset>
                {
                     // Slime Pack
                     new ChildAsset { Path = "assets/slime.json", Name = "Slime_Lvl1_G1_A", Transform = new { Position = new float[] { -15, 0, 20 } } },
                     new ChildAsset { Path = "assets/slime.json", Name = "Slime_Lvl1_G1_B", Transform = new { Position = new float[] { -10, 0, 25 } } },
                     new ChildAsset { Path = "assets/slime.json", Name = "Slime_Lvl1_G1_C", Transform = new { Position = new float[] { -20, 0, 25 } } },

                     // Rat Pack
                     new ChildAsset { Path = "assets/rat.json", Name = "Rat_Lvl1_G2_A", Transform = new { Position = new float[] { 20, 0, 60 } } },
                     new ChildAsset { Path = "assets/rat.json", Name = "Rat_Lvl1_G2_B", Transform = new { Position = new float[] { 25, 0, 65 } } },
                     new ChildAsset { Path = "assets/rat.json", Name = "Rat_Lvl1_G2_C", Transform = new { Position = new float[] { 15, 0, 65 } } },
                     
                     // Exit
                     new ChildAsset { Path = "assets/exit_crystal.json", Name = "SewerExit", Transform = new { Position = new float[] { 0, 2, -80 } } }
                }
            };
            
            // Random Pipes
            var rng = new System.Random();
            for(int i=0; i<10; i++)
            {
                float z = rng.Next(-80, 80);
                bool left = rng.NextDouble() > 0.5;
                float x = left ? -38 : 38;
                
                asset.Parts.Add(new ProceduralPart 
                { 
                    Id = $"pipe_{i}", 
                    Shape = "Cylinder", 
                    Position = new float[] { x, 8, z }, 
                    Rotation = new float[] { 0, 0, 90 },
                    Scale = new float[] { 2, 6, 2 }, 
                    ColorHex = "#6D4C41", 
                    Material = "Metal" 
                });
            }
            
            return asset;
        }
    }
}
