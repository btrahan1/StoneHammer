using System.Collections.Generic;
using System.Linq;
using StoneHammer.Systems;

namespace StoneHammer.Systems
{
    public static class UniversalDungeonGenerator
    {
        public static ProceduralAsset Generate(DungeonRecipe recipe, int depth)
        {
            // Future: Vary logic by depth
            return GenerateLevel(recipe, depth);
        }

        private static ProceduralAsset GenerateLevel(DungeonRecipe recipe, int depth)
        {
            var asset = new ProceduralAsset
            {
                Name = $"{recipe.Name} (Level {depth})",
                Type = "Procedural",
                Parts = new List<ProceduralPart>(),
                Children = new List<ChildAsset>()
            };

            switch (recipe.LayoutType)
            {
                case DungeonLayoutType.Rooms:
                    GenerateRoomsLayout(asset, recipe);
                    break;
                case DungeonLayoutType.Cave:
                    GenerateCaveLayout(asset, recipe);
                    break;
                case DungeonLayoutType.Tunnel:
                    GenerateTunnelLayout(asset, recipe);
                    break;
            }

            // Populate Enemies
            PopulateEnemies(asset, recipe);

            // Exit
            asset.Children.Add(new ChildAsset 
            { 
                Path = "assets/exit_crystal.json", 
                Name = "DungeonExit", 
                Transform = new { Position = new float[] { 0, 5, -80 } } 
            });

            return asset;
        }

        private static void GenerateRoomsLayout(ProceduralAsset asset, DungeonRecipe recipe)
        {
            // Room & Corridor Generation
            var rng = new System.Random();
            var rooms = new List<RoomRect>();
            int mapWidth = 200;
            int mapDepth = 200;
            int roomCount = rng.Next(8, 15);

            // 1. Place Rooms
            for (int i = 0; i < roomCount; i++)
            {
                int w = rng.Next(15, 30);
                int d = rng.Next(15, 30);
                int x = rng.Next(-mapWidth / 2, mapWidth / 2 - w);
                int z = rng.Next(-mapDepth / 2, mapDepth / 2 - d);

                var newRoom = new RoomRect { X = x, Z = z, W = w, D = d };
                
                // Simple overlap check
                bool overlaps = rooms.Any(r => r.Intersects(newRoom, 5));
                if (!overlaps)
                {
                    rooms.Add(newRoom);
                }
            }

            // Ensure we have at least one room (Start)
            if (!rooms.Any()) rooms.Add(new RoomRect { X = -10, Z = -10, W = 20, D = 20 });

            // 2. Build Rooms
            foreach (var room in rooms)
            {
                // Floor
                asset.Parts.Add(new ProceduralPart 
                { 
                    Id = $"room_floor_{room.X}_{room.Z}", Shape = "Box", 
                    Position = new[] { room.CenterX, -0.5f, room.CenterZ }, 
                    Scale = new[] { (float)room.W, 1, (float)room.D }, 
                    ColorHex = recipe.Theme.FloorColor, Material = recipe.Theme.FloorMaterial 
                });

                // Ceiling
                asset.Parts.Add(new ProceduralPart 
                { 
                    Id = $"room_ceil_{room.X}_{room.Z}", Shape = "Box", 
                    Position = new[] { room.CenterX, 10, room.CenterZ }, 
                    Scale = new[] { (float)room.W, 1, (float)room.D }, 
                    ColorHex = recipe.Theme.WallColor, Material = recipe.Theme.WallMaterial 
                });

                // Walls (North/South/East/West) - Simplified as blocks around
                // North
                asset.Parts.Add(new ProceduralPart { Id = $"wall_n_{room.X}", Shape = "Box", Position = new[] { room.CenterX, 5, room.Z + room.D/2f }, Scale = new[] { (float)room.W, 10, 1 }, ColorHex = recipe.Theme.WallColor, Material = recipe.Theme.WallMaterial });
                // South
                asset.Parts.Add(new ProceduralPart { Id = $"wall_s_{room.X}", Shape = "Box", Position = new[] { room.CenterX, 5, room.Z - room.D/2f }, Scale = new[] { (float)room.W, 10, 1 }, ColorHex = recipe.Theme.WallColor, Material = recipe.Theme.WallMaterial });
                // East
                asset.Parts.Add(new ProceduralPart { Id = $"wall_e_{room.X}", Shape = "Box", Position = new[] { room.X + room.W/2f, 5, room.CenterZ }, Scale = new[] { 1, 10, (float)room.D }, ColorHex = recipe.Theme.WallColor, Material = recipe.Theme.WallMaterial });
                // West
                asset.Parts.Add(new ProceduralPart { Id = $"wall_w_{room.X}", Shape = "Box", Position = new[] { room.X - room.W/2f, 5, room.CenterZ }, Scale = new[] { 1, 10, (float)room.D }, ColorHex = recipe.Theme.WallColor, Material = recipe.Theme.WallMaterial });
            }

            // 3. Connect Rooms (Corridors)
            for (int i = 0; i < rooms.Count - 1; i++)
            {
                ConnectRooms(asset, rooms[i], rooms[i+1], recipe);
            }

            // 4. Store spawn points for enemies (Centers of rooms)
            // Hack: Attach to asset metadata? Or just assume spawns function will find floor.
            // For now, let's keep it simple.
        }

        private static void ConnectRooms(ProceduralAsset asset, RoomRect r1, RoomRect r2, DungeonRecipe recipe)
        {
            // L-Shaped Corridor
            // Horizontal then Vertical
            float x1 = r1.CenterX; float z1 = r1.CenterZ;
            float x2 = r2.CenterX; float z2 = r2.CenterZ;

            // X-Segment
            float width = System.Math.Abs(x2 - x1) + 4; // +4 for overlap/width
            float centerX = (x1 + x2) / 2;
            asset.Parts.Add(new ProceduralPart { Id = $"corr_h_{x1}", Shape = "Box", Position = new[] { centerX, -0.4f, z1 }, Scale = new[] { width, 1, 4 }, ColorHex = recipe.Theme.FloorColor, Material = recipe.Theme.FloorMaterial });
            // Ceiling
             asset.Parts.Add(new ProceduralPart { Id = $"corr_h_ceil_{x1}", Shape = "Box", Position = new[] { centerX, 8, z1 }, Scale = new[] { width, 1, 4 }, ColorHex = recipe.Theme.WallColor, Material = recipe.Theme.WallMaterial });


            // Z-Segment
            float depth = System.Math.Abs(z2 - z1) + 4;
            float centerZ = (z1 + z2) / 2;
            asset.Parts.Add(new ProceduralPart { Id = $"corr_v_{z1}", Shape = "Box", Position = new[] { x2, -0.4f, centerZ }, Scale = new[] { 4, 1, depth }, ColorHex = recipe.Theme.FloorColor, Material = recipe.Theme.FloorMaterial });
             // Ceiling
             asset.Parts.Add(new ProceduralPart { Id = $"corr_v_ceil_{z1}", Shape = "Box", Position = new[] { x2, 8, centerZ }, Scale = new[] { 4, 1, depth }, ColorHex = recipe.Theme.WallColor, Material = recipe.Theme.WallMaterial });
        }

        private class RoomRect
        {
            public int X, Z, W, D;
            public float CenterX => X; 
            public float CenterZ => Z;

            public bool Intersects(RoomRect other, int buffer)
            {
                return (System.Math.Abs(X - other.X) * 2 < (W + other.W) + buffer) &&
                       (System.Math.Abs(Z - other.Z) * 2 < (D + other.D) + buffer);
            }
        }

        private static void GenerateCaveLayout(ProceduralAsset asset, DungeonRecipe recipe)
        {
            // Cellular Automata
            int width = 40; int height = 40; // Grid cells (each 5x5 units)
            int scale = 5;
            bool[,] map = new bool[width, height];
            var rng = new System.Random();

            // 1. Init Random
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    map[x, y] = rng.NextDouble() < 0.45;

            // 2. Smooth (4 iterations)
            for (int i = 0; i < 4; i++)
            {
                bool[,] newMap = new bool[width, height];
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        int neighbors = GetNeighbors(map, x, y, width, height);
                        if (map[x, y]) newMap[x, y] = neighbors >= 4;
                        else newMap[x, y] = neighbors >= 5;
                    }
                }
                map = newMap;
                
                // Ensure center is open (Start)
                map[width/2, height/2] = false; 
                map[width/2+1, height/2] = false;
                map[width/2, height/2+1] = false;
            }

            // 3. Render
            // Floor (Base plane)
            asset.Parts.Add(new ProceduralPart { Id = "cave_base", Shape = "Box", Position = new[] { 0f, -1f, 0f }, Scale = new[] { (float)width*scale, 1f, (float)height*scale }, ColorHex = recipe.Theme.FloorColor, Material = recipe.Theme.FloorMaterial });

            var openCells = new List<(int, int)>();

            // Walls (Extruded cells)
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (map[x, y])
                    {
                        // Map 0,0 is -Width/2, -Height/2
                        float worldX = (x - width / 2) * scale;
                        float worldZ = (y - height / 2) * scale;

                        asset.Parts.Add(new ProceduralPart 
                        {
                            Id = $"rock_{x}_{y}",
                            Shape = "Box", 
                            Position = new[] { worldX, 7.5f, worldZ }, // Higher position (15/2)
                            Scale = new[] { (float)scale, 15f, (float)scale }, // Taller Walls
                            ColorHex = recipe.Theme.WallColor,
                            Material = recipe.Theme.WallMaterial
                        });
                    }
                    else
                    {
                        // Open cell, candidate for spawning
                        // Don't spawn too close to start (center)
                        if(System.Math.Abs(x - width/2) > 2 || System.Math.Abs(y - height/2) > 2)
                        {
                            openCells.Add((x, y));
                        }
                    }
                }
            }

            // Ceiling (Higher up)
            asset.Parts.Add(new ProceduralPart { Id = "cave_roof", Shape = "Box", Position = new[] { 0f, 15f, 0f }, Scale = new[] { (float)width*scale, 1f, (float)height*scale }, ColorHex = recipe.Theme.AtmosphereColor, Material = "Stone" });

            // 4. Safe Spawning
            if (recipe.Enemies.Any() && openCells.Any())
            {
                 int spawnCount = System.Math.Min(openCells.Count / 10, 8); // Density rule
                 for(int i=0; i<spawnCount; i++)
                 {
                     int idx = rng.Next(openCells.Count);
                     var cell = openCells[idx];
                     openCells.RemoveAt(idx); // No overlap

                     float wsX = (cell.Item1 - width / 2) * scale;
                     float wsZ = (cell.Item2 - height / 2) * scale;

                     var enemy = recipe.Enemies[rng.Next(recipe.Enemies.Count)];
                     asset.Children.Add(new ChildAsset 
                     { 
                        Path = enemy.AssetPath, 
                        Name = $"{enemy.NamePrefix}_{i}", 
                        Transform = new { Position = new float[] { wsX, 0, wsZ }, Rotation = new float[] { 0, rng.Next(0, 360), 0 } },
                        Metadata = new Dictionary<string, object> 
                        {
                            { "isEnemy", true },
                            { "hp", enemy.HP },
                            { "xp", enemy.XP },
                            { "returnScene", $"DungeonEntrance_{recipe.Id}" },
                            { "assetPath", enemy.AssetPath }
                        }
                     });
                 }
            }
        }

        private static int GetNeighbors(bool[,] map, int x, int y, int w, int h)
        {
            int count = 0;
            for(int i=-1; i<=1; i++)
                for(int j=-1; j<=1; j++)
                {
                    if(i==0 && j==0) continue;
                    int nx = x+i; int ny = y+j;
                    if(nx < 0 || ny < 0 || nx >= w || ny >= h) count++; // Edges are walls
                    else if(map[nx,ny]) count++;
                }
            return count;
        }

        private static void GenerateTunnelLayout(ProceduralAsset asset, DungeonRecipe recipe)
        {
            // Drunkard's Walk Tunnel
            var rng = new System.Random();
            int walkerX = 0; int walkerY = 0;
            int steps = 60;
            var path = new HashSet<(int, int)>();
            path.Add((0,0));

            for(int i=0; i<steps; i++)
            {
                // Bias forward (Y+) mostly
                double roll = rng.NextDouble();
                if (roll < 0.5) walkerY++; 
                else if (roll < 0.7) walkerY--;
                else if (roll < 0.85) walkerX++;
                else walkerX--;

                path.Add((walkerX, walkerY));
            }

            int scale = 8; // Wider tunnels

            foreach (var cell in path)
            {
                float x = cell.Item1 * scale;
                float z = cell.Item2 * scale;

                // Floor
                asset.Parts.Add(new ProceduralPart { Id = $"t_floor_{x}_{z}", Shape = "Box", Position = new[] { x, -1.0f, z }, Scale = new[] { (float)scale, 2f, (float)scale }, ColorHex = recipe.Theme.FloorColor, Material = recipe.Theme.FloorMaterial });
                
                // Sludge channel in middle?
                asset.Parts.Add(new ProceduralPart { Id = $"t_sludge_{x}_{z}", Shape = "Box", Position = new[] { x, -1.2f, z }, Scale = new[] { (float)scale/2, 1.8f, (float)scale }, ColorHex = "#1a3300", Material = "Glow" });

                // Ceiling
                asset.Parts.Add(new ProceduralPart { Id = $"t_ceil_{x}_{z}", Shape = "Box", Position = new[] { x, 8f, z }, Scale = new[] { (float)scale, 2f, (float)scale }, ColorHex = recipe.Theme.WallColor, Material = "Stone" });

                // Walls? 
                // We need to check neighbors to decide walls.
                // Simplified: Just put columns at corners
                asset.Parts.Add(new ProceduralPart { Id = $"t_col_{x}_{z}", Shape = "Cylinder", Position = new[] { x + scale/2f, 3f, z + scale/2f }, Scale = new[] { 1f, 8f, 1f }, ColorHex = "#444", Material = "Brick" });
            }
            
            // Populate Enemies along the path
            if (recipe.Enemies.Any())
            {
                 var cellList = path.ToList();
                 for(int i=0; i<5; i++)
                 {
                     var randomCell = cellList[rng.Next(cellList.Count)];
                     var enemy = recipe.Enemies[rng.Next(recipe.Enemies.Count)];
                     asset.Children.Add(new ChildAsset 
                     { 
                        Path = enemy.AssetPath, 
                        Name = $"{enemy.NamePrefix}_{i}", 
                        Transform = new { Position = new float[] { randomCell.Item1 * scale, 0, randomCell.Item2 * scale } },
                        Metadata = new Dictionary<string, object> 
                        {
                            { "isEnemy", true },
                            { "hp", enemy.HP },
                            { "xp", enemy.XP },
                            { "returnScene", $"DungeonEntrance_{recipe.Id}" },
                            { "assetPath", enemy.AssetPath }
                        }
                     });
                 }
            }
        }

        private static void PopulateEnemies(ProceduralAsset asset, DungeonRecipe recipe)
        {
            // Generic fallback population if not handled by layout
            // This is now largely redundant if specific layouts handle it better, 
            // but kept for safety or generic noise.
            if(asset.Children.Count == 0 && recipe.Enemies.Any())
            {
                 var rng = new System.Random();
                 for(int i=0; i<3; i++)
                 {
                     var enemy = recipe.Enemies[rng.Next(recipe.Enemies.Count)];
                     asset.Children.Add(new ChildAsset 
                     { 
                        Path = enemy.AssetPath, 
                        Name = $"Fallback_{enemy.NamePrefix}_{i}", 
                        Transform = new { Position = new float[] { rng.Next(-10,10), 0, rng.Next(10,30) } },
                        Metadata = new Dictionary<string, object> 
                        {
                            { "isEnemy", true },
                            { "hp", enemy.HP },
                            { "xp", enemy.XP },
                            { "returnScene", $"DungeonEntrance_{recipe.Id}" },
                            { "assetPath", enemy.AssetPath }
                        }
                     });
                 }
            }
        }
    }
}
