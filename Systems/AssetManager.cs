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
            PropertyNameCaseInsensitive = true
        };

        public AssetManager(IJSRuntime js, HttpClient http, CityBridge bridge) // Modified constructor
        {
            _js = js; // Initialized _js
            _http = http;
            _bridge = bridge;
        }

        // New SpawnAsset method as per instruction - Forwarding to main logic
        public async Task SpawnAsset(string path, string name, object transform)
        {
             await SpawnAsset(path, name, false, transform);
        }

        public async Task SpawnPlayer(float x = 0, float z = 0)
        {
            await SpawnAsset("assets/player.json", "Player", true, new { Position = new float[] { x, 0, z } });
        }

        public async Task SpawnBartender(float x = 0, float z = 0)
        {
            await SpawnAsset("assets/bartender.json", "Bartender", false, new { Position = new float[] { x, 0, z } });
        }

        public async Task SpawnBlackjackTable() => await SpawnAsset("assets/table.json", "Blackjack Table", false);

        public async Task SpawnTavern(float x = 0, float z = 0) => await SpawnAsset("assets/tavern.json", "Tavern", false, new { Position = new[] { x, 0, z } });
        public async Task SpawnGuild(float x = 0, float z = 0) => await SpawnAsset("assets/guild.json", "Guild", false, new { Position = new[] { x, 0, z }, isTrigger = true, triggerRadius = 5.0f });
        public async Task SpawnStore(float x = 0, float z = 0) => await SpawnAsset("assets/general_store.json", "General Store", false, new { Position = new[] { x, 0, z } });
        public async Task SpawnGuildMaster(float x = 0, float z = 0) => await SpawnAsset("assets/guild_master.json", "Guild Master Debug", false, new { Position = new[] { x, 0, z } }); // Debug helper

        public async Task ExitBuilding(float x, float z)
        {
            await GenerateTown(x, z);
        }

        public async Task GenerateTown(float x = 0, float z = 0)
        {
            await _bridge.ClearAll();

            // v10.1: Town Floor is now a JSON asset
            await SpawnAsset("assets/town_floor.json", "TownFloor");

            // 1. "Main Street" Layout
            await SpawnTavern(0, 40); 
            await SpawnGuild(-25, 10);
            await SpawnStore(25, 10);

            // 2. Residential strip
            for (int i = 0; i < 3; i++)
            {
                await SpawnAsset("assets/house.json", $"House_L_{i}", false, new { Position = new[] { -20f, 0, -15f - (i * 20f) }, Rotation = new[] { 0, 90f, 0 } });
                await SpawnAsset("assets/house.json", $"House_R_{i}", false, new { Position = new[] { 20f, 0, -15f - (i * 20f) }, Rotation = new[] { 0, -90f, 0 } });
            }

            // 3. NPCs, Player & Decor
            await SpawnPlayer(x, z);
            // await SpawnBartender(x + 5, z + 5);
            await SpawnAsset("assets/table.json", "Street Table 1", false, new { Position = new[] { 5, 0, 5 } });
            await SpawnAsset("assets/table.json", "Street Table 2", false, new { Position = new[] { -5, 0, 5 } });

            // v12.3: The Crypt Entrance (Behind Tavern)
            await SpawnAsset("assets/crypt_entrance.json", "CryptEntrance", false, new { Position = new[] { 0, 0, 60 } });

            // v14.0: The Desert Teleporter (East of Tavern)
            await SpawnAsset("assets/desert_entrance.json", "DesertEntrance", false, new { Position = new[] { 50, 0, 50 } });
        }

        private int _currentDepth = 0;

        public async Task EnterBuilding(string buildingName)
        {
            await this.ClearAll();

            if (buildingName.Contains("Guild"))
            {
                await SpawnAsset("assets/guild_interior.json", "Guild Interior");
            }
            else if (buildingName.Contains("Lodge"))
            {
                await SpawnAsset("assets/lodge_interior.json", "Lodge Interior");
            }
            else if (buildingName.Contains("Desert"))
            {
               // v14.0: Hybrid Desert Mode
               // The terrain is procedural JS, but we need an exit mechanism.
               // We will spawn the 'exit crystal' via the bridge, but let JS handle the world.
               
               System.Console.WriteLine("[AssetManager] Entering Procedural Desert...");
               await _bridge.EnterDesert();
               
               // Spawn just the exit crystal so user can leave
               await SpawnAsset("assets/exit_crystal.json", "DesertExit", false, new { Position = new float[] { 0, 10, 0 } });
            }
            else if (buildingName.Contains("Crypt"))
            {
                // v13.0: Procedural Dungeon Generation
                // format: "Crypt_Depth_X"
                if (buildingName.Contains("Depth"))
                {
                   var parts = buildingName.Split('_');
                   if (parts.Length == 3 && int.TryParse(parts[2], out int d))
                   {
                       _currentDepth = d;
                   }
                }
                else 
                {
                   _currentDepth = 1; // Default entry
                }

                System.Console.WriteLine($"[AssetManager] Entering Crypt Level {_currentDepth}");
                var levelAsset = CryptGenerator.Generate(_currentDepth);
                await SpawnGeneratedAsset(levelAsset, levelAsset.Name);
            }
            else if (buildingName == "BattleArena")
            {
                await SpawnAsset("assets/battle_arena.json", "BattleArena");
                // Do not spawn default player; CombatService handles unit placement
                return; 
            }
            else
            {
                await SpawnAsset("assets/sandbox_interior.json", "Sandbox Interior");
            }
            
            // v11.0: Child assets like Exit Crystals are now loaded via JSON nesting
            await SpawnPlayer(0, 0);
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
                 await SpawnAsset(child.Path, child.Name, false, child.Transform);
             }
        }

        // v20.0: Persistent Dead List
        private HashSet<string> _deadActors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        public void MarkAsDead(string actorId)
        {
            if (!_deadActors.Contains(actorId))
            {
                _deadActors.Add(actorId);
                System.Console.WriteLine($"[AssetManager] MarkAsDead: {actorId} (Total Dead: {_deadActors.Count})");
            }
        }

        public async Task SpawnAsset(string path, string name, bool isPlayer = false, object? transform = null)
        {
            try 
            {
                // Check Blacklist
                if (_deadActors.Contains(name))
                {
                    System.Console.WriteLine($"[AssetManager] BLOCKED: {name} is on the dead list.");
                    return;
                }

                System.Console.WriteLine($"[AssetManager] Spawning: {name}");

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
                bool isVoxel = json.Contains("\"Voxel\"", System.StringComparison.OrdinalIgnoreCase);

                if (isVoxel)
                {
                    System.Console.WriteLine($"[AssetManager] Identifed {name} as VOXEL (SpawnVoxel).");
                    var asset = JsonSerializer.Deserialize<VoxelAsset>(json, _options);
                    if (asset != null) 
                    {
                        await _bridge.SpawnVoxel(asset, name, isPlayer, transform);
                        foreach (var child in asset.Children)
                        {
                            await SpawnAsset(child.Path, child.Name, false, child.Transform);
                        }
                    }
                }
                else
                {
                    System.Console.WriteLine($"[AssetManager] Identifed {name} as PROCEDURAL (SpawnRecipe).");
                    var asset = JsonSerializer.Deserialize<ProceduralAsset>(json, _options);
                    if (asset != null) 
                    {
                        await _bridge.SpawnRecipe(asset, name, transform);
                        foreach (var child in asset.Children)
                        {
                            await SpawnAsset(child.Path, child.Name, false, child.Transform);
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
