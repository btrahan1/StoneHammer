using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace StoneHammer.Systems
{
    public class CombatService
    {
        public bool IsFighting { get; private set; }
        public string CombatLog { get; private set; } = "";
        
        public void LogLoot(int gold) 
        {
            CombatLog = $"You found {gold} Gold!";
            OnStateChanged?.Invoke();
        }

        public MobData CurrentEnemy { get; private set; }
        
        // Player Stats (Mock for now)
        public int PlayerHP { get; private set; } = 100;
        public int PlayerMaxHP { get; private set; } = 100;

        public event Action OnStateChanged;

        private CityBridge _bridge;
        private IJSRuntime _js;
        private AssetManager _assets;

        public CombatService(CityBridge bridge, IJSRuntime js, AssetManager assets)
        {
            _bridge = bridge;
            _js = js;
            _assets = assets;
        }

        public async Task StartCombat(string mobName)
        {
            IsFighting = true;
            CurrentEnemy = new MobData { Name = mobName, HP = 50, MaxHP = 50 };
            PlayerHP = 100;
            CombatLog = $"A wild {mobName} appeared!";
            
            // Dramatic Camera Zoom
            await _js.InvokeVoidAsync("stoneHammer.rotateCameraToBattle", mobName);
            
            OnStateChanged?.Invoke();
        }

        public async Task PlayerAction(string action)
        {
            if (!IsFighting) return;

            switch (action)
            {
                case "Attack":
                    int dmg = 10 + new Random().Next(5);
                    CurrentEnemy.HP -= dmg;
                    CombatLog = $"You hit {CurrentEnemy.Name} for {dmg} damage!";
                    
                    // Animate Player (Needs a player mesh name, usually just "Player")
                    await _js.InvokeVoidAsync("stoneHammer.playCombatAnimation", "Player", "Attack"); 
                    
                    // Animate Enemy Hit
                    await _js.InvokeVoidAsync("stoneHammer.playCombatAnimation", CurrentEnemy.Name, "Hit");
                    
                    break;
                case "Heal":
                    PlayerHP = Math.Min(PlayerMaxHP, PlayerHP + 20);
                    CombatLog = "You cast Heal! Recovered 20 HP.";
                    break;
                case "Run":
                    IsFighting = false;
                    CombatLog = "You ran away safely.";
                    OnStateChanged?.Invoke();
                    return;
            }

            OnStateChanged?.Invoke();

            // Check Win
            if (CurrentEnemy.HP <= 0)
            {
                await HandleVictory();
                return;
            }

            // Enemy Turn
            await EnemyTurn();
        }

        private async Task EnemyTurn()
        {
            CombatLog += " Enemy is thinking...";
            OnStateChanged?.Invoke();
            
            await Task.Delay(1000);

            // Animate Enemy Attack
            await _js.InvokeVoidAsync("stoneHammer.playCombatAnimation", CurrentEnemy.Name, "Attack");
            
            int dmg = 5 + new Random().Next(5);
            PlayerHP -= dmg;
            CombatLog = $"{CurrentEnemy.Name} attacks you for {dmg} damage!";
            
            // Animate Player Hit
            await _js.InvokeVoidAsync("stoneHammer.playCombatAnimation", "Player", "Hit");
            
            if (PlayerHP <= 0)
            {
                 CombatLog = "You have been defeated...";
                 // Handle death?
            }
            
            OnStateChanged?.Invoke();
        }

        private async Task HandleVictory()
        {
            CombatLog = "Victory! The enemy falls...";
            OnStateChanged?.Invoke();

            // Animate Death
            await _js.InvokeVoidAsync("stoneHammer.playCombatAnimation", CurrentEnemy.Name, "Die");
            await Task.Delay(2000); // 2s fade out time

            // Remove Mob
            await _js.InvokeVoidAsync("stoneHammer.removeActor", CurrentEnemy.Name);

            // Spawn Loot Chest
            // We need the mob's position. For now, assuming spawned near (0,0,0) relative to something?
            // Actually, we don't know the exact coords here easily without JS query. 
            // BUT, AssetManager has spawn methods.
            // Let's spawn it near the player or use a "SpawnAtLastEnemy" trick? 
            // Or just spawn it in front of the player (0,0,5)?
            
            // Hack for demo: Spawn Chest at fixed relative offset
            var chest = new {
               Id = "LootChest_" + DateTime.Now.Ticks,
               Type = "Voxel", 
               // We will leverage AssetManager to load loot_chest.json logic if exposed
               // Or manually call spawnVoxel via JS if AssetManager doesn't expose generic "SpawnAsset" nicely.
            };
            
            // Better: AssetManager.SpawnLootChest
            await _assets.SpawnAsset("assets/loot_chest.json", "Loot Chest", new { Position = new float[] { 0, 0, 25 } }); // Just generic placement for now
            
            CombatLog = "A Loot Chest appeared!";
            await Task.Delay(1000);
            
            IsFighting = false;
            OnStateChanged?.Invoke();
        }
    }

    public class MobData
    {
        public string Name { get; set; }
        public int HP { get; set; }
        public int MaxHP { get; set; }
    }
}
