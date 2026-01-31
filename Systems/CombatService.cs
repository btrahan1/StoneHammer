using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace StoneHammer.Systems
{
    public class CombatService
    {
        public bool IsFighting { get; private set; }
        public string CombatLog { get; private set; } = "";
        public CombatPhase Phase { get; private set; } = CombatPhase.Input;

        public List<CombatEntity> Heroes { get; private set; } = new List<CombatEntity>();
        public List<CombatEntity> Enemies { get; private set; } = new List<CombatEntity>();

        public event Action? OnStateChanged;

        public void LogLoot(int gold)
        {
            CombatLog = $"You found {gold} Gold!";
            OnStateChanged?.Invoke();
        }

        private CityBridge _bridge;
        private IJSRuntime _js;
        private AssetManager _assets;
        private CharacterService _charService;

        public CombatService(CityBridge bridge, IJSRuntime js, AssetManager assets, CharacterService charService)
        {
            _bridge = bridge;
            _js = js;
            _assets = assets;
            _charService = charService;
        }

        public async Task StartCombat(string enemyGroupName)
        {
            IsFighting = true;
            Phase = CombatPhase.Input;
            CombatLog = $"Encounter started: {enemyGroupName}!";
            
            // 1. Initialize Heroes from Party
            Heroes.Clear();
            foreach(var member in _charService.Party)
            {
                Heroes.Add(new CombatEntity 
                { 
                    Name = member.Name, 
                    HP = member.Stats.MaxHP, // Should probably track current HP in CharacterData
                    MaxHP = member.Stats.MaxHP,
                    IsHero = true,
                    SourceCharacter = member,
                    ModelId = "Hero_" + member.Name.Replace(" ", "") // Assume unique ID or mapping
                });
            }

            // 2. Initialize Enemies (Mock Group for now)
            Enemies.Clear();
            int enemyCount = 3; // Fixed group size for v19
            for(int i=0; i<enemyCount; i++)
            {
                Enemies.Add(new CombatEntity 
                { 
                    Name = $"{enemyGroupName} {char.ConvertFromUtf32(65+i)}", // A, B, C
                    HP = 40, 
                    MaxHP = 40,
                    IsHero = false,
                    ModelId = $"Enemy_{i}"
                });
            }

            // 3. Camera & Spawning (Mock)
            await _js.InvokeVoidAsync("stoneHammer.rotateCameraToBattle", enemyGroupName);
            // In a real implementation, we would spawn the enemy meshes here if they aren't already.

            OnStateChanged?.Invoke();
        }

        public void QueueAction(CombatEntity actor, string actionType, CombatEntity? target = null)
        {
            if (Phase != CombatPhase.Input) return;

            actor.QueuedAction = new CombatAction 
            { 
                Type = actionType, 
                Target = target 
            };
            
            // Auto-target random enemy if attack and no target
            if (actionType == "Attack" && target == null)
            {
                 actor.QueuedAction.Target = Enemies.FirstOrDefault(e => e.HP > 0);
            }

            OnStateChanged?.Invoke();
        }

        public async Task ExecuteRound()
        {
            if (Phase != CombatPhase.Input) return;
            
            // 1. Check if all heroes define actions? 
            // For now, we allow partial execution (skip idle heroes) or strictly wait.
            // Let's assume user clicks "FIGHT" to commit.
            
            Phase = CombatPhase.Execution;
            CombatLog = "Round Start!";
            OnStateChanged?.Invoke();

            // 2. Queue Enemy Actions (Simple AI)
            foreach(var enemy in Enemies.Where(e => e.HP > 0))
            {
                var target = Heroes.Where(h => h.HP > 0).OrderBy(x => Guid.NewGuid()).FirstOrDefault();
                enemy.QueuedAction = new CombatAction { Type = "Attack", Target = target };
            }

            // 3. Merge and Sort Participants by Speed (Random for now)
            var allParticipants = new List<CombatEntity>();
            allParticipants.AddRange(Heroes.Where(h => h.HP > 0));
            allParticipants.AddRange(Enemies.Where(e => e.HP > 0));
            
            // Random Initiative
            var rng = new Random();
            allParticipants = allParticipants.OrderByDescending(p => rng.Next(20)).ToList();

            // 4. Play out Turns
            foreach(var actor in allParticipants)
            {
                if (actor.HP <= 0) continue; // Died during round
                
                var action = actor.QueuedAction;
                if (action == null) continue;

                // Validate Target
                if (action.Target != null && action.Target.HP <= 0)
                {
                    // Retarget if target dead
                    if (actor.IsHero) action.Target = Enemies.FirstOrDefault(e => e.HP > 0);
                    else action.Target = Heroes.FirstOrDefault(h => h.HP > 0);
                }

                if (action.Target == null && action.Type == "Attack") 
                {
                    CombatLog = $"{actor.Name} has no target!";
                    OnStateChanged?.Invoke();
                    await Task.Delay(500);
                    continue;
                }

                // Execute
                await PerformAction(actor, action);
                
                // Check End Condition mid-round
                if (Heroes.All(h => h.HP <= 0) || Enemies.All(e => e.HP <= 0)) break;
            }

            // 5. Cleanup & Next Round
            foreach(var p in allParticipants) p.QueuedAction = null;
            
            if (Heroes.All(h => h.HP <= 0))
            {
                CombatLog = "DEFEAT...";
                IsFighting = false;
            }
            else if (Enemies.All(e => e.HP <= 0))
            {
                await HandleVictory();
            }
            else
            {
                Phase = CombatPhase.Input;
                CombatLog = "Command Phase. Choose actions.";
            }
            
            OnStateChanged?.Invoke();
        }

        private async Task PerformAction(CombatEntity actor, CombatAction action)
        {
            if (action.Type == "Attack" && action.Target != null)
            {
                CombatLog = $"{actor.Name} attacks {action.Target.Name}!";
                OnStateChanged?.Invoke();
                
                // Anim (Mock)
                // await _js.InvokeVoidAsync("stoneHammer.playCombatAnimation", actor.ModelId, "Attack");
                await Task.Delay(800); // Wait for anim
                
                int damage = 10; // Calc based on stats later
                action.Target.HP -= damage;
                
                CombatLog = $"{action.Target.Name} took {damage} damage!";
                OnStateChanged?.Invoke();
                
                if (action.Target.HP <= 0)
                {
                    CombatLog = $"{action.Target.Name} fell!";
                }
            }
            else if (action.Type == "Heal")
            {
                actor.HP = Math.Min(actor.MaxHP, actor.HP + 20);
                CombatLog = $"{actor.Name} cast Heal!";
                OnStateChanged?.Invoke();
                await Task.Delay(800);
            }
            
            await Task.Delay(500); // Pace
        }

        private async Task HandleVictory()
        {
            CombatLog = "VICTORY!";
            Phase = CombatPhase.Victory;
            OnStateChanged?.Invoke();

            // 1. Remove all enemies visuals
            foreach(var enemy in Enemies)
            {
                 // Play die anim if not already?
                 // Remove actor
                 // In v15 we used "stoneHammer.removeActor" with the Name.
                 // We need to make sure the name matches the node. 
                 // Our spawn logic used "Name" as ID effectively? 
                 // Let's assume the ModelId is the key if we set it, or Name if not.
                 var id = !string.IsNullOrEmpty(enemy.ModelId) ? enemy.ModelId : enemy.Name;
                 await _js.InvokeVoidAsync("stoneHammer.removeActor", enemy.Name); 
            }

            await Task.Delay(1000);

            // 2. Spawn Loot Chest
            // We just pick a spot near the center of battle or the player
            await _assets.SpawnAsset("assets/loot_chest.json", "Loot Chest", new { Position = new float[] { 0, 0, 15 } }); 
            
            CombatLog = "A Loot Chest appeared!";
            OnStateChanged?.Invoke();
            
            await Task.Delay(2000);
            IsFighting = false;
            OnStateChanged?.Invoke();
        }
    }

    public enum CombatPhase
    {
        Input,
        Execution,
        Victory,
        Defeat
    }

    public class CombatEntity
    {
        public string Name { get; set; } = "";
        public int HP { get; set; }
        public int MaxHP { get; set; }
        public bool IsHero { get; set; }
        public string ModelId { get; set; } = "";
        
        public CharacterModels.CharacterData? SourceCharacter { get; set; }
        public CombatAction? QueuedAction { get; set; }
    }

    public class CombatAction
    {
        public string Type { get; set; } = "Attack"; // Attack, Magic, Item, Run
        public CombatEntity? Target { get; set; }
    }
}
