using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace StoneHammer.Systems
{
    public class AssetManager
    {
        private readonly HttpClient _http;
        private readonly CityBridge _bridge;
        private readonly IJSRuntime _js; // Added for IJSRuntime
        private readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        public AssetManager(IJSRuntime js, HttpClient http, CityBridge bridge) // Modified constructor
        {
            _js = js; // Initialized _js
            _http = http;
            _bridge = bridge;
        }

        // New SpawnAsset method - Forwarding to main logic
        public async Task SpawnAsset(string path, string name, object transform)
        {
             await SpawnAsset(path, name, false, transform, null);
        }

        public async Task SpawnPlayer(float x = 0, float z = 0)
        {
            await SpawnAsset("assets/player.json", "Player", true, new { Position = new float[] { x, 0, z } }, null);
        }

        public async Task SpawnBartender(float x = 0, float z = 0)
        {
            await SpawnAsset("assets/bartender.json", "Bartender", false, new { Position = new float[] { x, 0, z } }, null);
        }

        public async Task SpawnBlackjackTable() => await SpawnAsset("assets/table.json", "Blackjack Table", false, null, null);

        public async Task SpawnTavern(float x = 0, float z = 0) => await SpawnAsset("assets/tavern.json", "Tavern", false, new { Position = new[] { x, 0, z } }, null);
        public async Task SpawnGuild(float x = 0, float z = 0) => await SpawnAsset("assets/guild.json", "Guild", false, new { Position = new[] { x, 0, z }, isTrigger = true, triggerRadius = 5.0f }, null);
        public async Task SpawnStore(float x = 0, float z = 0) => await SpawnAsset("assets/general_store.json", "General Store", false, new { Position = new[] { x, 0, z } }, null);
        public async Task SpawnGuildMaster(float x = 0, float z = 0) => await SpawnAsset("assets/guild_master.json", "Guild Master Debug", false, new { Position = new[] { x, 0, z } }, null); // Debug helper

        public async Task SpawnProp(string assetFile, string name, float x, float z)
        {
            // Auto-prepend assets/ if not present
            string path = assetFile.StartsWith("assets/") ? assetFile : $"assets/{assetFile}";
            await SpawnAsset(path, name, false, new { Position = new[] { x, 0, z } }, null);
        }

        public async Task ExitBuilding(float x, float z)
        {
            await GenerateTown(x, z);
        }

        // v23.0: Data-Driven Town Generation
        public async Task GenerateTown(float x = 0, float z = 0)
        {
            await _bridge.ClearAll();

            // 1. Load Town Recipe
            string json = await _http.GetStringAsync("assets/data/town.json?v=" + System.DateTime.Now.Ticks);
            var town = JsonSerializer.Deserialize<TownRecipe>(json, _options);

            if (town == null) return;
            
            // 1.5 Set Atmosphere
            string atmos = town.Theme?.AtmosphereColor ?? "#80b3ff";
            await _js.InvokeVoidAsync("stoneHammer.setAtmosphere", atmos);

            // 2. Town Floor
            await SpawnAsset("assets/town_floor.json", "TownFloor", false, null, null);

            // 3. Guards
            // await _js.InvokeVoidAsync("stoneHammer.spawnGuards"); // Guards initialized later in town.json loop? Actually logic below handles it.
            
            // 3. Static Buildings
            foreach (var b in town.StaticBuildings)
            {
                // Note: Rotation order in JSON [X, Y, Z]
                await SpawnAsset(b.AssetPath, b.Name, false, new { Position = b.Position, Rotation = b.Rotation }, b.Metadata);
                // If ActionId exists, we might need to handle triggers? 
                // Currently only triggers have specific handling.
                // TODO: Store ActionId in metadata or similar if needed for interaction.
            }

            // 4. Dungeon Entrances
            foreach (var d in town.DungeonEntrances)
            {
                // We encode the DungeonId into the Name so we know what to load on entry
                // Name format: "DungeonEntrance_{DungeonId}"
                // AssetPath should point to visual glTF/JSON
                string name = $"DungeonEntrance_{d.DungeonId}";
                await SpawnAsset(d.EntranceAssetPath, name, false, new { Position = d.Position, Rotation = d.Rotation, isTrigger = true, triggerRadius = 5.0f }, null);
            }

            // v33.0: Procedural Ring Walls
            if (town.RingWalls != null)
            {
                foreach (var ring in town.RingWalls)
                {
                    float anglePerSeg = 360f / ring.SegmentCount;
                    for (int i = 0; i < ring.SegmentCount; i++)
                    {
                        float currentAngle = i * anglePerSeg;
                        
                        // Check Gates
                        bool isGate = false;
                        foreach(var gateAngle in ring.GateAngles)
                        {
                            float diff = Math.Abs(currentAngle - gateAngle);
                            if (diff > 180) diff = 360 - diff; // Handle wrap around
                            if (diff < ring.GateWidthDegrees / 2f) 
                            {
                                isGate = true;
                                break;
                            }
                        }

                        if (isGate) continue;

                        // Calculate Math (Babylon Y is Up, Z is Forward)
                        // Angle 0 = North = +Z ? Or East = +X?
                        // Usually Math.Cos is X, Math.Sin is Z
                        float rad = currentAngle * (MathF.PI / 180f);
                        float px = MathF.Sin(rad) * ring.Radius;
                        float pz = MathF.Cos(rad) * ring.Radius;
                        
                        // Rotation: Wall should face center or tangent?
                        // Box is usually long along X? wall_tall is 20 along X.
                        // So to face tangent, we rotate Y.
                        // If at angle 0 (0, radius), tangent is X-axis (90 deg).
                        // Let's try Rotation = currentAngle.
                        float rotY = currentAngle; 

                        string name = $"RingWall_{ring.Radius}_{i}";
                        await SpawnAsset(ring.AssetPath, name, false, new { Position = new[]{px, ring.HeightOffset, pz}, Rotation = new[]{0, rotY, 0} }, null);
                    }
                }
            }


            
            // v35.0: Town NPCs (Guards)
            if (town.NPCs != null)
            {
                await _js.InvokeVoidAsync("stoneHammer.initAI");
                foreach (var npc in town.NPCs)
                {
                    // Spawn
                    await SpawnAsset(npc.AssetPath, npc.Name, false, new { Position = npc.Position, Rotation = npc.Rotation }, null);
                    
                    // Register Patrol
                    if (npc.PatrolPath != null && npc.PatrolPath.Count > 0)
                    {
                        await _js.InvokeVoidAsync("stoneHammer.registerPatrol", npc.Name, npc.PatrolPath, npc.PatrolSpeed);
                    }
                }
            }

            // 5. Player & Decor (Hardcoded for now / could be in recipe too)
            await SpawnPlayer(x, z);
            //await SpawnAsset("assets/table.json", "Street Table 1", false, new { Position = new[] { 5, 0, 5 } });
            //await SpawnAsset("assets/table.json", "Street Table 2", false, new { Position = new[] { -5, 0, 5 } });

            // v14.0: The Desert Teleporter (Legacy Hybrid)
            // Removed: Now in town.json
            // await SpawnAsset("assets/desert_entrance.json", "DesertEntrance", false, new { Position = new[] { 50, 0, 50 } });
            
            // v40.0: Initialize Labels AFTER all assets are spawned
            await _js.InvokeVoidAsync("stoneHammer.initLabels");
        }

        private int _currentDepth = 0;

        // v23.0: Data-Driven Dungeon Entry
        public async Task EnterBuilding(string buildingName, float? spawnX = null, float? spawnZ = null)
        {
            await this.ClearAll();

            // 1. Interiors (Tavern, Store, etc)
            // TODO: Move these to recipe too? For now keep them if they are simple assets.
            if (buildingName.Contains("The Rusty Mug") || buildingName == "Tavern") await SpawnAsset("assets/tavern_interior.json", "Tavern Interior");
            else if (buildingName.Contains("Hammer & Sickle") || buildingName == "General Store") await SpawnAsset("assets/general_store_interior.json", "Store Interior"); // No interior asset yet?
            else if (buildingName.Contains("Guild")) await SpawnAsset("assets/guild_interior.json", "Guild Interior");
            else if (buildingName.Contains("Lodge")) await SpawnAsset("assets/lodge_interior.json", "Lodge Interior");
            else if (buildingName == "BattleArena") 
            {
                await SpawnAsset("assets/battle_arena.json", "BattleArena");
                return;
            }
            else if (buildingName == "Desert")
            {
                await _bridge.EnterDesert();
                await SpawnAsset("assets/exit_crystal.json", "DesertExit", false, new { Position = new float[] { 0, 10, 0 } });
            }

            // 2. Dungeons
            // 2. Dungeons (Generic Fallback)
            else
            {
                string dungeonId = "";
                int depth = 1;

                // Parse ID
                if (buildingName.StartsWith("DungeonEntrance_"))
                {
                    dungeonId = buildingName.Replace("DungeonEntrance_", "");
                }
                else 
                {
                    // Fallback: Use the name itself as the ID (e.g. "Crypt", "GoblinCave")
                    dungeonId = buildingName;
                }

                // Strip Depth from ID if present (e.g. "elemental_Depth_2" -> "elemental")
                if (dungeonId.Contains("_Depth_"))
                {
                    dungeonId = dungeonId.Split(new[] { "_Depth_" }, StringSplitOptions.None)[0];
                }

                // Parse Depth (e.g. "crypt_Depth_2")
                if (buildingName.Contains("_Depth_"))
                {
                    var parts = buildingName.Split(new[]{"_Depth_"}, System.StringSplitOptions.None);
                    if (parts.Length > 1 && int.TryParse(parts[1], out int d)) depth = d;
                }
                
                _currentDepth = depth;

                // Load Recipe
                string recipePath = $"assets/data/dungeons/dungeon_{dungeonId}.json";

                try 
                {
                    string json = await _http.GetStringAsync(recipePath + "?v=" + System.DateTime.Now.Ticks);
                    
                    var recipe = JsonSerializer.Deserialize<DungeonRecipe>(json, _options);

                    if (recipe != null)
                    {
                        var dungeonAsset = UniversalDungeonGenerator.Generate(recipe, depth);
                        
                        // Set Atmosphere
                        string atmos = recipe.Theme?.AtmosphereColor ?? "#000000";
                        await _js.InvokeVoidAsync("stoneHammer.setAtmosphere", atmos);

                        await SpawnGeneratedAsset(dungeonAsset, dungeonAsset.Name);
                        await SpawnPlayer(spawnX ?? 0, spawnZ ?? 0);
                    }
                    else
                    {
                         System.Console.WriteLine($"[AssetManager] Recipe was null after deserialization!");
                    }
                }
                catch(System.Exception ex)
                {
                     System.Console.WriteLine($"[AssetManager] CRITICAL ERROR loading dungeon {dungeonId}: {ex.Message}");
                     System.Console.WriteLine($"[AssetManager] StackTrace: {ex.StackTrace}");
                     // Fallback
                     await SpawnAsset("assets/sandbox_interior.json", "Error Fallback");
                     await SpawnPlayer(spawnX ?? 0, spawnZ ?? 0);
                }
                return;
            }

            
            await SpawnPlayer(spawnX ?? 0, spawnZ ?? 0);
        }

        public async Task ClearAll()
        {
             _currentDepth = 0;
             await _bridge.ClearAll();
        }

        // v13.0: Overload for spawning C# generated assets directly
        private async Task SpawnGeneratedAsset(ProceduralAsset asset, string name, object? transform = null)
        {
             await _bridge.SpawnRecipe(asset, name, transform);
             foreach (var child in asset.Children)
             {
                 // Recursively spawn children (stairs, crystals, etc)
                 await SpawnAsset(child.Path, child.Name, false, child.Transform, child.Metadata);
             }
        }

        // v20.0: Persistent Dead List
        private HashSet<string> _deadActors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        public void MarkAsDead(string actorId)
        {
            if (!_deadActors.Contains(actorId))
            {
                _deadActors.Add(actorId);
            }
        }

        public async Task SpawnAsset(string path, string name, bool isPlayer = false, object? transform = null, Dictionary<string, object>? metadata = null)
        {
            try 
            {
                // Check Blacklist
                if (_deadActors.Contains(name))
                {
                    return;
                }

                string json;
                if (!string.IsNullOrEmpty(path))
                {
                    // v11.8: Add cache buster to prevent stale JSON (missing properties like Type: Voxel)
                    var url = path.Contains("?") ? path + "&" : path + "?";
                    url += "v=" + System.DateTime.Now.Ticks;
                    json = await _http.GetStringAsync(url);
                }
                else 
                {
                    System.Console.WriteLine($"Error: Asset {name} has no path and inline data is not yet supported in SpawnAsset.");
                    return;
                }

                // Robust check for Voxel type
                bool isVoxel = json.Contains("\"Voxel\"", System.StringComparison.OrdinalIgnoreCase) || 
                               json.Contains("\"Humanoid\"", System.StringComparison.OrdinalIgnoreCase);

                if (isVoxel)
                {
                    var asset = JsonSerializer.Deserialize<VoxelAsset>(json, _options);
                    if (asset != null) 
                    {
                        await _bridge.SpawnVoxel(asset, name, isPlayer, transform, metadata);
                        foreach (var child in asset.Children)
                        {
                            await SpawnAsset(child.Path, child.Name, false, child.Transform, null);
                        }
                    }
                }

                else
                {
                    var asset = JsonSerializer.Deserialize<ProceduralAsset>(json, _options);
                    if (asset != null) 
                    {
                        await _bridge.SpawnRecipe(asset, name, transform, metadata);
                        foreach (var child in asset.Children)
                        {
                            await SpawnAsset(child.Path, child.Name, false, child.Transform, child.Metadata);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"Error Spawning Asset {name} ({path}): {ex.Message}");
            }
        }
    }
}
