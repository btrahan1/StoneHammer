using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace StoneHammer.Systems
{
    public class AssetManager
    {
        private readonly HttpClient _http;
        private readonly CityBridge _bridge;
        private readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public AssetManager(HttpClient http, CityBridge bridge)
        {
            _http = http;
            _bridge = bridge;
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

        public async Task EnterBuilding(string buildingName)
        {
            await this.ClearAll();

            if (buildingName.Contains("Guild"))
            {
                await SpawnAsset("assets/guild_interior.json", "Guild Interior");
            }
            else if (buildingName.Contains("Desert"))
            {
                await SpawnAsset("assets/desert_interior.json", "Desert Interior");
            }
            else if (buildingName.Contains("Lodge"))
            {
                await SpawnAsset("assets/lodge_interior.json", "Lodge Interior");
            }
            else
            {
                await SpawnAsset("assets/sandbox_interior.json", "Sandbox Interior");
            }
            
            // v11.0: Child assets like Exit Crystals are now loaded via JSON nesting
            await SpawnPlayer(0, 0);
        }

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
            await SpawnBartender(x + 5, z + 5);
            await SpawnAsset("assets/table.json", "Street Table 1", false, new { Position = new[] { 5, 0, 5 } });
            await SpawnAsset("assets/table.json", "Street Table 2", false, new { Position = new[] { -5, 0, 5 } });
        }

        public async Task ClearAll() => await _bridge.ClearAll();

        private async Task SpawnAsset(string path, string name, bool isPlayer = false, object? transform = null)
        {
            try 
            {
                System.Console.WriteLine($"[AssetManager] Requesting spawn for: {name} from {path}");

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
