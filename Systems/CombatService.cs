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

        // Loot State
        public bool LootOpen { get; private set; }
        public int CurrentLootGold { get; private set; }
        public List<CharacterModels.InventoryItem> CurrentLootItems { get; private set; } = new List<CharacterModels.InventoryItem>();

        // Auto-Battle
        public bool IsAutoBattling { get; private set; }

        public event Action? OnStateChanged;

        public void ToggleAutoBattle()
        {
            IsAutoBattling = !IsAutoBattling;
            if (IsAutoBattling && Phase == CombatPhase.Input)
            {
                _ = AutoFightLoop();
            }
            OnStateChanged?.Invoke();
        }

        private async Task AutoFightLoop()
        {
             while(IsAutoBattling && IsFighting)
             {
                 if (Phase == CombatPhase.Input)
                 {
                     // Queue Actions for all heroes
                     foreach(var hero in Heroes.Where(h => h.HP > 0))
                     {
                         // Simple AI: Basic Attack first living enemy
                         // (Future: Healers heal if low HP)
                         var target = Enemies.OrderBy(e => e.HP).FirstOrDefault(e => e.HP > 0);
                         
                         if (target != null)
                         {
                             hero.QueuedAction = new CombatAction { Type = "Attack", Target = target };
                         }
                     }
                     
                     // Trigger Round
                     await ExecuteRound();
                     await Task.Delay(1000); // Pacing
                 }
                 else
                 {
                     await Task.Delay(500);
                 }
             }
        }

        private CityBridge _bridge;
        public IJSRuntime JS { get; private set; }
        private AssetManager _assets;
        private CharacterService _charService;

        public void OpenLootChest()
        {
            if (LootOpen) return;
            
            // Random Generate
            var rng = new Random();
            CurrentLootGold = rng.Next(20, 100);
            CurrentLootItems.Clear();
            
            // 30% chance for item
            if (rng.NextDouble() > 0.7)
            {
                CurrentLootItems.Add(new CharacterModels.InventoryItem 
                { 
                    Name = "Rusty Sword", 
                    Type = CharacterModels.ItemType.Weapon, 
                    ValidSlot = CharacterModels.EquipmentSlot.MainHand,
                    Bonuses = new Dictionary<string, int> { { "Strength", 1 } },
                    Icon = "🗡️",
                    Description = "Better than nothing."
                });
            }
             else if (rng.NextDouble() > 0.5)
            {
               CurrentLootItems.Add(new CharacterModels.InventoryItem 
                { 
                    Name = "Minor Potion", 
                    Type = CharacterModels.ItemType.Consumable, 
                    Icon = "🧪",
                    Description = "Heals 10 HP."
                });
            }

            LootOpen = true;
            OnStateChanged?.Invoke();
        }

        public async Task CloseLoot()
        {
            LootOpen = false;
            OnStateChanged?.Invoke();

            if (Phase == CombatPhase.Victory)
            {
                await EndCombat();
            }
        }

        public void ClearLoot()
        {
            CurrentLootGold = 0;
            CurrentLootItems.Clear();
        }

        public CombatService(CityBridge bridge, IJSRuntime js, AssetManager assets, CharacterService charService)
        {
            _bridge = bridge;
            JS = js;
            _assets = assets;
            _charService = charService;
        }

        // Instanced Combat State
        private string _returnToScene = "";
        private float _returnX = 0;
        private float _returnZ = 0;
        private string _engagedGroup = "";
        
        // Persistent Dead List (Ideally moved to a separate service later)
        private HashSet<string> _defeatedGroups = new HashSet<string>();

        public bool IsGroupDefeated(string groupName) => _defeatedGroups.Contains(groupName);

        public async Task StartCombat(string targetActorName)
        {
            if (IsFighting) return;

            IsFighting = true;
            // Phase = CombatPhase.Input; // Defer until spawned
            CombatLog = "Preparing for Battle...";
            
            // 1. Capture State
            // We assume the JS tracks current building. We need to ask JS or AssetManager?
            // AssetManager tracks _currentDepth but not full name string publicly easily.
            // For now, let's assume we are in "Crypt_Depth_" + depth.
            // A better way: Pass current scene from JS in StartCombat args? 
            // Or just default to restoring the logical location.
            // Temporary: Assume Crypt L1 if uncertain.
            _returnToScene = "Crypt_Depth_1"; // Default
            
            // Improve ID Parsing
            string baseName = targetActorName.Replace("voxel_", "");

            // Robust Cleaning: Remove all known body parts to find the Root Actor
            string[] knownParts = { 
                "_ribs", "_skull", "_head", "_torso", 
                "_arm_l", "_arm_r", "_leg_l", "_leg_r", 
                "_eye_l", "_eye_r", 
                "_sword_handle", "_sword_blade", "_sword_guard",
                "_helmet", "_hammer_handle", "_hammer_head",
                "_dagger_handle", "_dagger_blade",
                "_staff_handle", "_staff_gem",
                "_mace_handle", "_mace_head", "_circlet", "_hood", "_hat"
            };

            foreach(var part in knownParts)
            {
                if (baseName.Contains(part)) 
                {
                    baseName = baseName.Split(new[] { part }, StringSplitOptions.None)[0];
                    break; // Assume only one part suffix
                }
            }

            // 2. Strip suffix _A, _B, _C to get Group ID
             if (baseName.EndsWith("_A") || baseName.EndsWith("_B") || baseName.EndsWith("_C"))
            {
                baseName = baseName.Substring(0, baseName.Length - 2);
            }
            _engagedGroup = baseName; // e.g., "Skeleton_Lvl1_G1"

            CombatLog = $"Engaging {_engagedGroup}!";
            OnStateChanged?.Invoke();

            // 2. Transition to Arena
            await _assets.EnterBuilding("BattleArena");
            await Task.Delay(500); // Wait for scene clear/load

            // 3. Spawn Heroes (Party) - Left Side
            Heroes.Clear();
            
            // Fallback: If Party is empty, use Player
            var combatParticipants = _charService.Party.Any() ? _charService.Party : new List<CharacterModels.CharacterData> { _charService.Player };

            int hIndex = 0;
            foreach(var member in combatParticipants)
            {
                string heroId = "Hero_" + member.Name.Replace(" ", "");
                Heroes.Add(new CombatEntity 
                { 
                    Name = member.Name, 
                    HP = member.CurrentHP, 
                    MaxHP = member.Stats.MaxHP,
                    IsHero = true,
                    SourceCharacter = member,
                    ModelId = heroId 
                });
                
                // Spawn Visual
                string assetPath = "assets/player.json"; // Default/Fighter
                switch(member.Class)
                {
                    case CharacterModels.CharacterClass.Rogue: assetPath = "assets/rogue.json"; break;
                    case CharacterModels.CharacterClass.Mage: assetPath = "assets/mage.json"; break;
                    case CharacterModels.CharacterClass.Healer: assetPath = "assets/healer.json"; break;
                }

                await _assets.SpawnAsset(assetPath, heroId, false, new { Position = new float[] { -20, 0, (hIndex * 8) - 4 }, Rotation = new float[] { 0, 90, 0 } });
                
                // Attach Weapon Visuals
                if (member.Equipment.TryGetValue(CharacterModels.EquipmentSlot.MainHand, out var weapon) && weapon != null)
                {
                    if (weapon.Name.ToLower().Contains("bow"))
                    {
                        await JS.InvokeVoidAsync("stoneHammer.attachAsset", "assets/bow.json", heroId, "arm_r");
                    }
                }
                
                hIndex++;
            }

            // 4. Spawn Enemies (Group) - Right Side
            Enemies.Clear();
            string[] suffixes = { "A", "B", "C" }; // Assume 3 per group
            
            for(int i=0; i<3; i++)
            {
                string suffix = suffixes[i];
                string enemyId = $"{_engagedGroup}_{suffix}"; // e.g. Skeleton_Lvl1_G1_A
                
                var enemy = new CombatEntity 
                { 
                    Name = $"Skeleton {suffix}", 
                    HP = 40, 
                    MaxHP = 40,
                    IsHero = false,
                    XPValue = 25,
                    ModelId = enemyId
                };
                
                Enemies.Add(enemy);
                
                // Spawn Visual
                await _assets.SpawnAsset("assets/skeleton.json", enemyId, false, new { Position = new float[] { 20, 0, (i * 8) - 4 }, Rotation = new float[] { 0, -90, 0 } });
            }

            await JS.InvokeVoidAsync("stoneHammer.rotateCameraToBattle", "ArenaCenter"); // Adjust camera logic if needed
            
            // Ready to fight!
            Phase = CombatPhase.Input;
            CombatLog = "Battle Start!";
            OnStateChanged?.Invoke();
        }

        public async Task EndCombat()
        {
            // Called after Loot
            LootOpen = false;
            IsFighting = false;
            
            // Record Victory
            _defeatedGroups.Add(_engagedGroup);
            
            // Cleanup: The re-generated world will spawn the mobs again.
            // We must mark them as dead in AssetManager so they are skipped during generation.
            string[] suffixes = { "A", "B", "C" };
            foreach(var s in suffixes)
            {
                 _assets.MarkAsDead($"{_engagedGroup}_{s}");
            }
            
            // Return to World (This will regenerate the scene, now respecting the dead list)
            await _assets.EnterBuilding(_returnToScene);
            
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
            // 1. Deduct Cost (if Hero)
            if (actor.IsHero && actor.SourceCharacter != null)
            {
                var skill = actor.SourceCharacter.Skills.FirstOrDefault(s => s.Name == action.Type);
                if (skill != null)
                {
                    if (actor.SourceCharacter.CurrentMana < skill.ManaCost)
                    {
                        CombatLog = $"{actor.Name} fizzles! Not enough resource.";
                        OnStateChanged?.Invoke();
                        await Task.Delay(500);
                        return;
                    }
                    actor.SourceCharacter.CurrentMana -= skill.ManaCost;
                }
            }

            if (action.Type == "Attack" && action.Target != null)
            {
                await PerformAttack(actor, action.Target, 1.0f);
            }
            else if (action.Type == "Defend")
            {
                CombatLog = $"{actor.Name} is guarding.";
                OnStateChanged?.Invoke();
                // Logic for reducing damage next turn? (Not implemented yet)
            }
            // --- FIGHTER SKILLS ---
            else if (action.Type == "Power Strike" && action.Target != null)
            {
                CombatLog = $"{actor.Name} uses Power Strike!";
                await PerformAttack(actor, action.Target, 1.5f, "Attack");
            }
            else if (action.Type == "Block")
            {
                 CombatLog = $"{actor.Name} hunkers down!";
                 OnStateChanged?.Invoke();
            }
            // --- ROGUE SKILLS ---
            else if (action.Type == "Backstab" && action.Target != null)
            {
                CombatLog = $"{actor.Name} Backstabs!";
                await PerformAttack(actor, action.Target, 2.0f, "Attack");
            }
            // --- MAGE SKILLS ---
            else if (action.Type == "Fireball" && action.Target != null)
            {
                CombatLog = $"{actor.Name} casts Fireball!";
                await PerformMagic(actor, action.Target, 2.5f, "Cast_Fire", "🔥");
            }
            else if (action.Type == "Ice Bolt" && action.Target != null)
            {
                CombatLog = $"{actor.Name} casts Ice Bolt!";
                await PerformMagic(actor, action.Target, 1.5f, "Cast_Ice", "❄️");
            }
            // --- HEALER SKILLS ---
            else if (action.Type == "Heal" && action.Target != null)
            {
                 await PerformHeal(actor, action.Target);
            }
            else if (action.Type == "Smite" && action.Target != null)
            {
                CombatLog = $"{actor.Name} Smites the enemy!";
                await PerformMagic(actor, action.Target, 1.8f, "Cast_Smite", "⚡");
            }
            
            await Task.Delay(500); 
        }

        private async Task PerformAttack(CombatEntity actor, CombatEntity target, float multiplier, string anim = "Attack")
        {
             string actualAnim = anim;
             // Check Weapon for Ranged override
             if (anim == "Attack" && actor.SourceCharacter != null)
             {
                 if (actor.SourceCharacter.Equipment.TryGetValue(CharacterModels.EquipmentSlot.MainHand, out var weapon) && weapon != null)
                 {
                     if (weapon.Name.ToLower().Contains("bow") || weapon.Name.ToLower().Contains("wand"))
                     {
                         actualAnim = "Shoot";
                     }
                 }
             }

             OnStateChanged?.Invoke();
             await PlayAnim(actor, actualAnim, target);
             
             // SFX: Attack
             string sfx = actualAnim == "Shoot" ? "attack_range" : "attack_melee";
             await JS.InvokeVoidAsync("stoneHammer.audio.playSound", sfx);

             int damage = (int)(CalculateDamage(actor) * multiplier);
             
             await PlayAnim(target, "Hit");
             await JS.InvokeVoidAsync("stoneHammer.flashTarget", target.ModelId, "#FF0000", 200);
             if (multiplier > 1.2f) await JS.InvokeVoidAsync("stoneHammer.shakeCamera", 0.5, 200);
             
             // SFX: Impact
             await JS.InvokeVoidAsync("stoneHammer.audio.playSound", "impact_flesh");
             
             target.HP -= damage;
             if (target.IsHero && target.SourceCharacter != null) target.SourceCharacter.CurrentHP = target.HP;
             
             // Visual Feedback
             target.LastDamageAmount = damage;
             target.LastDamageType = multiplier > 1.0f ? "Crit" : "Phys";
             target.LastDamageTime = DateTime.Now;

             CombatLog = $"{target.Name} took {damage} damage!";
             OnStateChanged?.Invoke();
             
             if (target.HP <= 0) await HandleDeath(target);
        }

        private async Task PerformMagic(CombatEntity actor, CombatEntity target, float multiplier, string anim, string effectEmoji)
        {
             OnStateChanged?.Invoke();
             await PlayAnim(actor, anim, target);
             
             // SFX: Cast
             await JS.InvokeVoidAsync("stoneHammer.audio.playSound", "spell_fire"); // Generic magic sound

             // Magic Calc
             int damage = (int)(CalculateDamage(actor) * multiplier);
             
             CombatLog = $"{effectEmoji} {target.Name} hit for {damage}!";
             await PlayAnim(target, "Hit");
             await JS.InvokeVoidAsync("stoneHammer.flashTarget", target.ModelId, effectEmoji == "❄️" ? "#00FFFF" : "#FF5500", 300);
             await JS.InvokeVoidAsync("stoneHammer.shakeCamera", 0.3, 200);
             
             // SFX: Impact
             await JS.InvokeVoidAsync("stoneHammer.audio.playSound", "spell_fire"); // Reuse for explosion or specific hit

             target.HP -= damage;
             if (target.IsHero && target.SourceCharacter != null) target.SourceCharacter.CurrentHP = target.HP;
             
             // Visual Feedback
             target.LastDamageAmount = damage;
             target.LastDamageType = "Magic";
             target.LastDamageTime = DateTime.Now;

             OnStateChanged?.Invoke();
             
             if (target.HP <= 0) await HandleDeath(target);
        }

        private async Task PerformHeal(CombatEntity actor, CombatEntity target)
        {
                 CombatLog = $"{actor.Name} casts Heal on {target.Name}!";
                 OnStateChanged?.Invoke();
                 // Determine Animation
                 string animType = "Cast_Heal"; // Default for healing
                 await PlayAnim(actor, animType, target);
                 
                 // SFX: Heal
                 await JS.InvokeVoidAsync("stoneHammer.audio.playSound", "spell_fire"); // Placeholder

                 int healAmount = 20 + (actor.SourceCharacter?.Stats.Wisdom * 2 ?? 0);
                 target.HP = Math.Min(target.HP + healAmount, target.MaxHP);
                 if (target.IsHero && target.SourceCharacter != null) target.SourceCharacter.CurrentHP = target.HP;
                 
                 // Visual Feedback
                 target.LastDamageAmount = healAmount;
                 target.LastDamageType = "Heal";
                 target.LastDamageTime = DateTime.Now;

                 CombatLog = $"{target.Name} recovered {healAmount} HP!";
                 OnStateChanged?.Invoke();
        }

        private async Task PlayAnim(CombatEntity entity, string anim, CombatEntity? target = null)
        {
            if (!string.IsNullOrEmpty(entity.ModelId))
            {
                await JS.InvokeVoidAsync("stoneHammer.playCombatAnimation", entity.ModelId, anim, target?.ModelId);
                await Task.Delay(800);
            }
        }
        
        private async Task HandleDeath(CombatEntity target)
        {
             CombatLog = $"{target.Name} fell!";
             await JS.InvokeVoidAsync("stoneHammer.flashTarget", target.ModelId, "#FFFFFF", 500);
             await JS.InvokeVoidAsync("stoneHammer.shakeCamera", 1.0, 400);

             await PlayAnim(target, "Die");
             // SFX: Death (Could add specific later)
             await Task.Delay(200);
        }

        private int CalculateDamage(CombatEntity entity)
        {
            if (!entity.IsHero || entity.SourceCharacter == null) return 5; // Base mob damage

            return Math.Max(1, entity.SourceCharacter.GetTotalAttack());
        }

        private async Task HandleVictory()
        {
            CombatLog = "VICTORY!";
            Phase = CombatPhase.Victory;
            
            // SFX: Victory Fanfare?
            await JS.InvokeVoidAsync("stoneHammer.audio.playSound", "impact_flesh"); // Placeholder cheer

            // XP REWARD
            int totalXP = Enemies.Sum(e => e.XPValue);
            foreach(var member in _charService.Party)
            {
                _charService.GainXP(member, totalXP);
            }
            CombatLog += $" Party gained {totalXP} XP!";
            OnStateChanged?.Invoke();

            // Remove visuals
            foreach(var enemy in Enemies)
            {
                 var id = !string.IsNullOrEmpty(enemy.ModelId) ? enemy.ModelId : enemy.Name;
                 await JS.InvokeVoidAsync("stoneHammer.removeActor", id); 
            }

            await Task.Delay(1000);
            
            // Fix: Position should come from first enemy
            await _assets.SpawnAsset("assets/loot_chest.json", "Loot Chest", new { Position = new float[] { 0, 0, 15 } }); 
            
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
        public string ModelId { get; set; } = ""; // The JS scene node ID
        public int XPValue { get; set; } = 0;
        
        public CharacterModels.CharacterData? SourceCharacter { get; set; }
        public CombatAction? QueuedAction { get; set; }
        
        // Visual Feedback State
        public int LastDamageAmount { get; set; }
        public string LastDamageType { get; set; } = "Phys"; // Phys, Crit, Magic, Heal
        public DateTime LastDamageTime { get; set; }
    }

    public class CombatAction
    {
        public string Type { get; set; } = "Attack"; // Attack, Magic, Item, Run
        public CombatEntity? Target { get; set; }
    }
}
