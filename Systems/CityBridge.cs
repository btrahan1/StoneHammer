using System.Text.Json;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace StoneHammer.Systems
{
    public class CityBridge
    {
        private readonly IJSRuntime _jsRuntime;

        public CityBridge(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task SpawnVoxel(VoxelAsset asset, string name, bool isPlayer = false, object? transform = null, Dictionary<string, object>? metadata = null)
        {
            await _jsRuntime.InvokeVoidAsync("stoneHammer.spawnVoxel", asset, name, isPlayer, transform, metadata);
        }

        public async Task SpawnRecipe(ProceduralAsset asset, string name, object? transform = null, Dictionary<string, object>? metadata = null)
        {
            await _jsRuntime.InvokeVoidAsync("stoneHammer.spawnRecipe", asset, name, transform, metadata);
        }

        public async Task ClearAll()
        {
            await _jsRuntime.InvokeVoidAsync("stoneHammer.clearAll");
        }

        public async Task EnterDesert()
        {
            await _jsRuntime.InvokeVoidAsync("stoneHammer.enterDesert");
        }
    }
}
