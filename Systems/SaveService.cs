using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using static StoneHammer.Systems.CharacterModels;

namespace StoneHammer.Systems
{
    public class SaveService
    {
        private readonly IJSRuntime _js;
        private readonly CharacterService _charService;
        private const string KeyPrefix = "stonehammer_save_";

        public SaveService(IJSRuntime js, CharacterService charService)
        {
            _js = js;
            _charService = charService;
        }

        public async Task<List<string>> GetSaveFiles()
        {
            // We can't easily iterate localStorage keys in C# without a JS helper.
            // For now, let's store a "manifest" or just rely on a hardcoded list if we wanted, 
            // but a JS helper is better.
            // Let's use a simple JS script to find keys starting with prefix.
            var keys = await _js.InvokeAsync<List<string>>("stoneHammer.storage.getSaves", KeyPrefix);
            return keys.Select(k => k.Replace(KeyPrefix, "")).ToList();
        }

        public async Task SaveGame(string saveName)
        {
            var save = new SaveGame
            {
                Name = saveName,
                Created = DateTime.Now,
                Party = _charService.Party 
            };

            var json = JsonSerializer.Serialize(save);
            await _js.InvokeVoidAsync("localStorage.setItem", KeyPrefix + saveName, json);
        }

        public async Task LoadGame(string saveName)
        {
            var json = await _js.InvokeAsync<string>("localStorage.getItem", KeyPrefix + saveName);
            if (string.IsNullOrEmpty(json)) return;

            var save = JsonSerializer.Deserialize<SaveGame>(json);
            if (save != null && save.Party != null)
            {
                _charService.LoadParty(save.Party);
            }
        }

        public async Task<string?> GetLatestSave()
        {
            var keys = await _js.InvokeAsync<List<string>>("stoneHammer.storage.getSaves", KeyPrefix);
            if (keys == null || keys.Count == 0) 
            {
                Console.WriteLine("[SaveService] No saves found.");
                return null;
            }

            SaveGame? latest = null;

            foreach (var key in keys)
            {
                var json = await _js.InvokeAsync<string>("localStorage.getItem", key);
                if (string.IsNullOrEmpty(json)) continue;

                try 
                {
                    var save = JsonSerializer.Deserialize<SaveGame>(json);
                    if (save != null)
                    {
                        Console.WriteLine($"[SaveService] Found '{save.Name}' Created: {save.Created}");
                        if (latest == null || save.Created > latest.Created)
                        {
                            latest = save;
                        }
                    }
                }
                catch (Exception ex)
                { 
                    Console.WriteLine($"[SaveService] Error reading {key}: {ex.Message}");
                }
            }

            if (latest != null) Console.WriteLine($"[SaveService] Selected Latest: {latest.Name}");
            return latest?.Name;
        }
    }
}
