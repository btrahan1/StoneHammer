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

        public async Task SpawnVoxel(VoxelAsset asset, string name, bool isPlayer = false, object? transform = null)
        {
            await _jsRuntime.InvokeVoidAsync("stoneHammer.spawnVoxel", asset, name, isPlayer, transform);
        }

        public async Task SpawnRecipe(ProceduralAsset asset, string name, object? transform = null)
        {
            await _jsRuntime.InvokeVoidAsync("stoneHammer.spawnRecipe", asset, name, transform);
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
