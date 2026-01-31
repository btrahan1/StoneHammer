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
            var asset = new VoxelAsset
            {
                Name = "Master Mason",
                ProceduralColors = new VoxelProceduralColors { Skin = "#F1C27D", Shirt = "#4E342E", Pants = "#3E2723" }
            };
            await _bridge.SpawnVoxel(asset, "Player", true, new { Position = new float[] { x, 0, z } });
        }

        public async Task SpawnBartender(float x = 0, float z = 0)
        {
            var json = await _http.GetStringAsync("assets/bartender.json");
            var asset = JsonSerializer.Deserialize<VoxelAsset>(json, _options);
            if (asset != null) await _bridge.SpawnVoxel(asset, "Bartender", false, new { Position = new float[] { x, 0, z } });
        }

        public async Task SpawnBlackjackTable() => await SpawnAsset("assets/table.json", "Blackjack Table");

        public async Task SpawnTavern(float x = 0, float z = 0) => await SpawnAsset("assets/tavern.json", "Tavern", new { Position = new[] { x, 0, z } });
        public async Task SpawnGuild(float x = 0, float z = 0) => await SpawnAsset("assets/guild.json", "Guild", new { Position = new[] { x, 0, z } });
        public async Task SpawnStore(float x = 0, float z = 0) => await SpawnAsset("assets/general_store.json", "General Store", new { Position = new[] { x, 0, z } });

        public async Task GenerateTown()
        {
            await _bridge.ClearAll();

            // 1. "Main Street" Layout (Spacious & Open)
            // Tavern at the end of the road
            await SpawnTavern(0, 40); 
            
            // Guild and Store as anchor buildings
            await SpawnGuild(-25, 10);
            await SpawnStore(25, 10);

            // 2. Residential strip (Open spacing)
            for (int i = 0; i < 3; i++)
            {
                await SpawnAsset("assets/house.json", $"House_L_{i}", new { Position = new[] { -20f, 0, -15f - (i * 20f) }, Rotation = new[] { 0, 90f, 0 } });
                await SpawnAsset("assets/house.json", $"House_R_{i}", new { Position = new[] { 20f, 0, -15f - (i * 20f) }, Rotation = new[] { 0, -90f, 0 } });
            }

            // 3. NPCs, Player & Decor
            await SpawnPlayer(0, 0);
            await SpawnBartender(0, 30);
            await SpawnAsset("assets/table.json", "Street Table 1", new { Position = new[] { 5, 0, 5 } });
            await SpawnAsset("assets/table.json", "Street Table 2", new { Position = new[] { -5, 0, 5 } });
        }

        public async Task ClearAll() => await _bridge.ClearAll();

        private async Task SpawnAsset(string path, string name, object? transform = null)
        {
            var json = await _http.GetStringAsync(path);
            if (json.Contains("\"Voxel\""))
            {
                var asset = JsonSerializer.Deserialize<VoxelAsset>(json, _options);
                if (asset != null) await _bridge.SpawnVoxel(asset, name, false, transform);
            }
            else
            {
                var asset = JsonSerializer.Deserialize<ProceduralAsset>(json, _options);
                if (asset != null) await _bridge.SpawnRecipe(asset, name, transform);
            }
        }
    }
}
