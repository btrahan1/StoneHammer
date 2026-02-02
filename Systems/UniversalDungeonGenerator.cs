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
                case DungeonLayoutType.OpenFloor:
                    GenerateOpenFloorLayout(asset, recipe, depth);
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
            int mapWidth = 400;
            int mapDepth = 400;
            int roomCount = rng.Next(8, 15);

            // 0. Global Foundation (Bedrock)
            // Added to prevent falling into void and provide visual "ground" outside rooms
            asset.Parts.Add(new ProceduralPart 
            { 
                Id = "dungeon_foundation", 
                Shape = "Box", 
                Position = new[] { 0f, -2.0f, 0f }, 
                Scale = new[] { (float)mapWidth, 1f, (float)mapDepth }, 
                ColorHex = "#3E2723", // Dark Brown/Dirt
                Material = "Stone" // Basic Stone
            });

            // 1. Place Rooms
            for (int i = 0; i < roomCount; i++)
            {
                int w = rng.Next(30, 60);
                int d = rng.Next(30, 60);
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

            // 2. Identify Connections (Doors)
            // We connect strictly sequentially for now
            for (int i = 0; i < rooms.Count - 1; i++)
            {
                ConnectRooms(asset, rooms[i], rooms[i+1], recipe);
            }

            // 3. Build Rooms (With Walls respecting Doors)
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

                // Walls
                GenerateSmartWall(asset, room.X, room.Z + room.D/2f, room.W, "North", room, recipe); // North
                GenerateSmartWall(asset, room.X, room.Z - room.D/2f, room.W, "South", room, recipe); // South
                GenerateSmartWall(asset, room.X + room.W/2f, room.Z, room.D, "East", room, recipe); // East
                GenerateSmartWall(asset, room.X - room.W/2f, room.Z, room.D, "West", room, recipe); // West
            }

            // 4. Spawn Enemies in Rooms (Skip start room 0)
            if (recipe.Enemies.Any())
            {
                 for(int i=1; i<rooms.Count; i++) // Skip Start Room
                 {
                     var room = rooms[i];
                     // Spawn 1-3 enemies per room
                     int count = rng.Next(1, 4);
                     for(int j=0; j<count; j++)
                     {
                         var enemy = recipe.Enemies[rng.Next(recipe.Enemies.Count)];
                         
                         // Random position within room (padding 2 units)
                         float ex = room.CenterX + rng.Next(-(room.W/2)+2, (room.W/2)-2);
                         float ez = room.CenterZ + rng.Next(-(room.D/2)+2, (room.D/2)-2);

                         asset.Children.Add(new ChildAsset 
                         { 
                            Path = enemy.AssetPath, 
                            Name = $"{enemy.NamePrefix}_{i}_{j}", 
                            Transform = new { Position = new float[] { ex, 0, ez }, Rotation = new float[] { 0, rng.Next(0, 360), 0 } },
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

        private static void ConnectRooms(ProceduralAsset asset, RoomRect r1, RoomRect r2, DungeonRecipe recipe)
        {
            // L-Shaped Corridor logic
            float x1 = r1.CenterX; float z1 = r1.CenterZ;
            float x2 = r2.CenterX; float z2 = r2.CenterZ;
            float corrWidth = 8f;

            // Determine Door Requests
            // 1. Exit r1 (Horizontal segment starts here)
            // It punches through either East or West wall of r1.
            if (x2 > x1) r1.AddDoor("East", z1, corrWidth);
            else r1.AddDoor("West", z1, corrWidth);

            // 2. Enter r2 (Vertical segment ends here)
            // It punches through either North or South wall of r2.
            if (z2 > z1) r2.AddDoor("South", x2, corrWidth);
            else r2.AddDoor("North", x2, corrWidth);

            // Draw Corridor Floor
            // X-Segment
            float width = System.Math.Abs(x2 - x1) + corrWidth; 
            float centerX = (x1 + x2) / 2;
            asset.Parts.Add(new ProceduralPart { Id = $"corr_h_{x1}", Shape = "Box", Position = new[] { centerX, -0.4f, z1 }, Scale = new[] { width, 1, corrWidth }, ColorHex = recipe.Theme.FloorColor, Material = recipe.Theme.FloorMaterial });

            // Z-Segment
            float depth = System.Math.Abs(z2 - z1) + corrWidth;
            float centerZ = (z1 + z2) / 2;
            // Note: The vertical segment technically overlaps the horizontal one at the corner. This is fine.
            asset.Parts.Add(new ProceduralPart { Id = $"corr_v_{z1}", Shape = "Box", Position = new[] { x2, -0.4f, centerZ }, Scale = new[] { corrWidth, 1, depth }, ColorHex = recipe.Theme.FloorColor, Material = recipe.Theme.FloorMaterial });
        }

        private static void GenerateSmartWall(ProceduralAsset asset, float wallX, float wallZ, int length, string side, RoomRect room, DungeonRecipe recipe)
        {
            // Filter doors relevant to this side
            var doors = room.Doors.Where(d => d.Side == side).OrderBy(d => d.Pos).ToList();
            
            float wallHeight = 20f;
            float wallY = 10f;
            float thick = 1f;

            // Wall geometry variables depend on orientation
            bool isHoriz = (side == "North" || side == "South");
            
            // "Start" and "End" of the wall in world coords along the wall's axis
            // For North/South, axis is X. Range: [CenterX - W/2, CenterX + W/2]
            // For East/West, axis is Z. Range: [CenterZ - D/2, CenterZ + D/2]
            float start = isHoriz ? (room.CenterX - room.W/2f) : (room.CenterZ - room.D/2f);
            float end = isHoriz ? (room.CenterX + room.W/2f) : (room.CenterZ + room.D/2f);

            float currentPos = start;

            foreach(var door in doors)
            {
                // Gap Start/End
                float gapStart = door.Pos - (door.Size / 2f);
                float gapEnd = door.Pos + (door.Size / 2f);

                // Build Wall Segment from currentPos to gapStart
                if (gapStart > currentPos)
                {
                    float segLen = gapStart - currentPos;
                    float segCenter = currentPos + (segLen / 2f);
                    
                    if (isHoriz)
                        asset.Parts.Add(new ProceduralPart { Id = $"wall_{side}_{segCenter}", Shape = "Box", Position = new[] { segCenter, wallY, wallZ }, Scale = new[] { segLen, wallHeight, thick }, ColorHex = recipe.Theme.WallColor, Material = recipe.Theme.WallMaterial });
                    else
                        asset.Parts.Add(new ProceduralPart { Id = $"wall_{side}_{segCenter}", Shape = "Box", Position = new[] { wallX, wallY, segCenter }, Scale = new[] { thick, wallHeight, segLen }, ColorHex = recipe.Theme.WallColor, Material = recipe.Theme.WallMaterial });
                }

                // Create Lintel (Above Door)
                // Height: 4 units? Door is usually 5-6 units high. Wall is 20.
                // Let's say Door Height is 8. Lintel starts at 8 + (20-8)/2 = 14?
                // Size: 20 - 8 = 12.
                float lintelHeight = 12f;
                float lintelY = 8f + (lintelHeight/2f); // 8 + 6 = 14
                
                 if (isHoriz)
                        asset.Parts.Add(new ProceduralPart { Id = $"lintel_{side}_{door.Pos}", Shape = "Box", Position = new[] { door.Pos, lintelY, wallZ }, Scale = new[] { door.Size, lintelHeight, thick }, ColorHex = recipe.Theme.WallColor, Material = recipe.Theme.WallMaterial });
                    else
                        asset.Parts.Add(new ProceduralPart { Id = $"lintel_{side}_{door.Pos}", Shape = "Box", Position = new[] { wallX, lintelY, door.Pos }, Scale = new[] { thick, lintelHeight, door.Size }, ColorHex = recipe.Theme.WallColor, Material = recipe.Theme.WallMaterial });


                currentPos = gapEnd;
            }

            // Final Segment
            if (currentPos < end)
            {
                float segLen = end - currentPos;
                float segCenter = currentPos + (segLen / 2f);
                 if (isHoriz)
                        asset.Parts.Add(new ProceduralPart { Id = $"wall_{side}_{segCenter}", Shape = "Box", Position = new[] { segCenter, wallY, wallZ }, Scale = new[] { segLen, wallHeight, thick }, ColorHex = recipe.Theme.WallColor, Material = recipe.Theme.WallMaterial });
                    else
                        asset.Parts.Add(new ProceduralPart { Id = $"wall_{side}_{segCenter}", Shape = "Box", Position = new[] { wallX, wallY, segCenter }, Scale = new[] { thick, wallHeight, segLen }, ColorHex = recipe.Theme.WallColor, Material = recipe.Theme.WallMaterial });
            }
        }

        private class RoomRect
        {
            public int X, Z, W, D;
            public float CenterX => X; 
            public float CenterZ => Z;
            public List<DoorDef> Doors { get; set; } = new List<DoorDef>();

            public void AddDoor(string side, float pos, float size)
            {
                Doors.Add(new DoorDef { Side = side, Pos = pos, Size = size });
            }

            public bool Intersects(RoomRect other, int buffer)
            {
                return (System.Math.Abs(X - other.X) * 2 < (W + other.W) + buffer) &&
                       (System.Math.Abs(Z - other.Z) * 2 < (D + other.D) + buffer);
            }
        }

        private class DoorDef
        {
            public string Side; // North, South, East, West
            public float Pos;   // Coordinate along the wall
            public float Size;
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

            // Identify Open Cells and Prepare Wall Map for Optimization
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (!map[x, y])
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
            
            // OPTIMIZATION: Greedy Meshing for Walls
            // Instead of spawning 1x1 blocks, we merge adjacent wall cells into larger rectangles.
            OptimizeCaveWalls(asset, map, width, height, scale, recipe.Theme.WallColor, recipe.Theme.WallMaterial);

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

        private static void OptimizeCaveWalls(ProceduralAsset asset, bool[,] map, int width, int height, int scale, string color, string material)
        {
            // Simple RLE (Run-Length Encoding) along X-axis
            // We iterate row by row. If we find a wall, we check the next cell.
            // If it's also a wall, we extend the current block.
            // If not, we finalize the block and start looking for the next one.
            // Note: A full 2D greedy mesh (merging rects) is better but 1D RLE is often sufficient for 90% reduction.
            
            bool[,] visited = new bool[width, height];

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (map[x, z] && !visited[x, z])
                    {
                        // Start of a wall segment
                        int startX = x;
                        int length = 0;

                        // Extend along X
                        while (x < width && map[x, z] && !visited[x, z])
                        {
                            visited[x, z] = true;
                            length++;
                            x++;
                        }

                        // Create the merged wall part
                        float centerX = (startX + (length - 1) / 2f - width / 2f) * scale;
                        float centerZ = (z - height / 2f) * scale;
                        
                        // Scale.X expands by length
                        float scaleX = length * scale;
                        float scaleZ = scale;

                        asset.Parts.Add(new ProceduralPart
                        {
                            Id = $"wall_opt_{z}_{startX}",
                            Shape = "Box",
                            Position = new[] { centerX, 7.5f, centerZ }, // Higher position (15/2)
                            Scale = new[] { scaleX, 15f, scaleZ }, // Taller Walls
                            ColorHex = color,
                            Material = material
                        });
                    }
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
            
            // 0. Safety Foundation (Bedrock to prevent falling)
            // Fix for Falling Through World in Sewers
            int mapSize = 400;
            asset.Parts.Add(new ProceduralPart 
            { 
                Id = "tunnel_foundation", 
                Shape = "Box", 
                Position = new[] { 0f, -4.0f, 0f }, // Slightly lower to clear sludge channels
                Scale = new[] { (float)mapSize, 1f, (float)mapSize }, 
                ColorHex = "#1a0f0a", // Darker Mud/Dirt
                Material = "Stone" 
            });

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
        private static void GenerateOpenFloorLayout(ProceduralAsset asset, DungeonRecipe recipe, int depth)
        {
             // Specialized Logic for Elemental Sanctum
             // Depth 1: Earth, 2: Wind, 3: Fire, 4: Water
             
             string floorColor = recipe.Theme.FloorColor;
             string atmos = recipe.Theme.AtmosphereColor;
             string mobPrefix = "Skeleton";
             string mobAsset = "assets/skeleton.json";
             int hp = 50; int xp = 25;
             
             // Override based on Floor Config (Per-Depth)
             // Check for specific floor config
             var floorConfig = recipe.Floors?.FirstOrDefault(f => f.Depth == depth);
             
             List<DungeonEnemy> availableEnemies = recipe.Enemies;

             if (floorConfig != null)
             {
                 if (floorConfig.Theme != null)
                 {
                     if (!string.IsNullOrEmpty(floorConfig.Theme.FloorColor)) floorColor = floorConfig.Theme.FloorColor;
                     if (!string.IsNullOrEmpty(floorConfig.Theme.AtmosphereColor)) atmos = floorConfig.Theme.AtmosphereColor;
                 }
                 
                 if (floorConfig.Enemies != null && floorConfig.Enemies.Count > 0)
                 {
                     availableEnemies = floorConfig.Enemies;
                 }
             }
             
             // Pick main mob for this floor
             if (availableEnemies != null && availableEnemies.Count > 0)
             {
                 var enemyDef = availableEnemies[0]; // Simplification: Pick first defined enemy for this floor
                 mobPrefix = enemyDef.NamePrefix;
                 mobAsset = enemyDef.AssetPath;
                 hp = enemyDef.HP;
                 xp = enemyDef.XP;
             }

             // 1. Huge Room
             int width = 200;
             int depthD = 200;
             
             // Floor (Bedrock + Floor)
             asset.Parts.Add(new ProceduralPart { Id = "huge_bedrock", Shape = "Box", Position = new[] { 0f, -4f, 0f }, Scale = new[] { 220f, 2f, 220f }, ColorHex = "#1a1a1a", Material = "Stone" });
             
             asset.Parts.Add(new ProceduralPart 
             { 
                 Id = "open_floor_main", Shape = "Box", 
                 Position = new[] { 0f, -2f, 0f }, 
                 Scale = new[] { (float)width, 2f, (float)depthD }, 
                 ColorHex = floorColor, 
                 Material = "Stone" 
             });

             // Walls (Perimeter)
             float wallH = 40f;
             // N
             asset.Parts.Add(new ProceduralPart { Id = "wall_N", Shape = "Box", Position = new[] { 0f, 10f, depthD/2f }, Scale = new[] { (float)width, wallH, 4f }, ColorHex = recipe.Theme.WallColor, Material = recipe.Theme.WallMaterial });
             // S
             asset.Parts.Add(new ProceduralPart { Id = "wall_S", Shape = "Box", Position = new[] { 0f, 10f, -depthD/2f }, Scale = new[] { (float)width, wallH, 4f }, ColorHex = recipe.Theme.WallColor, Material = recipe.Theme.WallMaterial });
             // E
             asset.Parts.Add(new ProceduralPart { Id = "wall_E", Shape = "Box", Position = new[] { width/2f, 10f, 0f }, Scale = new[] { 4f, wallH, (float)depthD }, ColorHex = recipe.Theme.WallColor, Material = recipe.Theme.WallMaterial });
             // W
             asset.Parts.Add(new ProceduralPart { Id = "wall_W", Shape = "Box", Position = new[] { -width/2f, 10f, 0f }, Scale = new[] { 4f, wallH, (float)depthD }, ColorHex = recipe.Theme.WallColor, Material = recipe.Theme.WallMaterial });

             // 2. Pillars
             for(int x = -40; x <= 40; x+=40)
             {
                 for(int z = -40; z <= 40; z+=40)
                 {
                     if (x == 0 && z == 0) continue; // Skip center spawn
                     asset.Parts.Add(new ProceduralPart { Id = $"pillar_{x}_{z}", Shape = "Cylinder", Position = new[] { (float)x, 10f, (float)z }, Scale = new[] { 4f, 30f, 4f }, ColorHex = recipe.Theme.WallColor, Material = "Brick" });
                 }
             }

             // 3. Spawns (One huge Boss Element? Or multiple?)
             // User said "Huge room, with that elemental type as the mob".
             // Let's spawn 3 of them.
             var rng = new System.Random();
             for(int i=0; i<3; i++)
             {
                 asset.Children.Add(new ChildAsset 
                 { 
                    Path = mobAsset, 
                    Name = $"{mobPrefix}_{i}", 
                    Transform = new { Position = new float[] { rng.Next(-30,30), 0, rng.Next(-30,30) } },
                    Metadata = new Dictionary<string, object> 
                    {
                        { "isEnemy", true },
                        { "hp", hp },
                        { "xp", xp },
                        { "returnScene", $"DungeonEntrance_{recipe.Id}_Depth_{depth}" }, // Return to this depth? Or entrance? Bestiary default is entrance.
                        { "assetPath", mobAsset },
                        { "faction", "Elemental" }
                    }
                 });
             }


             // 4. Portal Logic
             if (depth < 4)
             {
                 // Next Level Portal
                 asset.Children.Add(new ChildAsset 
                 { 
                     Path = "assets/exit_crystal.json", 
                     Name = "LevelPortal", 
                     Transform = new { Position = new float[] { 0, 5, -(depthD/2f) + 20 } },
                     Metadata = new Dictionary<string, object> 
                     { 
                         { "action", "EnterDungeon" },
                         { "target", $"DungeonEntrance_{recipe.Id}_Depth_{depth+1}" }
                     }
                 });
             }
             else
             {
                 // Final Exit
                 asset.Children.Add(new ChildAsset 
                 { 
                     Path = "assets/exit_crystal.json", 
                     Name = "DungeonExit", 
                     Transform = new { Position = new float[] { 0, 5, -(depthD/2f) + 20 } } 
                 });
             }
        }
    }
}
