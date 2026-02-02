using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using System.Text.Json;

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
            
            CurrentLootGold = new Random().Next(20, 100);
            CurrentLootItems.Clear();
            
            // Generate 1-3 items
            int count = new Random().Next(1, 4);
            for(int i=0; i<count; i++)
            {
                var rarity = RollRarity();
                var item = GenerateLootItem(rarity);
                if (item != null) CurrentLootItems.Add(item);
            }

            LootOpen = true;
            OnStateChanged?.Invoke();
        }

        private CharacterModels.ItemRarity RollRarity()
        {
            double roll = new Random().NextDouble();
            if (roll > 0.99) return CharacterModels.ItemRarity.Legendary; // 1%
            if (roll > 0.95) return CharacterModels.ItemRarity.Epic;      // 4%
            if (roll > 0.80) return CharacterModels.ItemRarity.Rare;      // 15%
            if (roll > 0.50) return CharacterModels.ItemRarity.Uncommon;  // 30%
            return CharacterModels.ItemRarity.Common;                     // 50%
        }

        private CharacterModels.InventoryItem GenerateLootItem(CharacterModels.ItemRarity rarity)
        {
            var rng = new Random();
            var type = (CharacterModels.ItemType)rng.Next(0, 3); // Weapon, Armor, Consumable
            
            var item = new CharacterModels.InventoryItem { Rarity = rarity, Type = type };
            
            // Name & Stat Gen
            string prefix = "";
            string baseName = "";
            
            if (type == CharacterModels.ItemType.Weapon)
            {
                string[] weapons = { "Sword", "Axe", "Mace", "Dagger", "Staff", "Bow" };
                baseName = weapons[rng.Next(weapons.Length)];
                item.ValidSlot = CharacterModels.EquipmentSlot.MainHand;
                item.Icon = "⚔️";
                
                int damage = 1;
                switch(rarity)
                {
                    case CharacterModels.ItemRarity.Common: damage = 1; break;
                    case CharacterModels.ItemRarity.Uncommon: damage = 2; prefix = "Sharp "; break;
                    case CharacterModels.ItemRarity.Rare: damage = 3; prefix = "Honed "; item.Bonuses["Strength"] = 1; break;
                    case CharacterModels.ItemRarity.Epic: damage = 5; prefix = "Runed "; item.Bonuses["Strength"] = 2; item.Bonuses["Crit"] = 1; break;
                    case CharacterModels.ItemRarity.Legendary: damage = 8; prefix = "Godly "; item.Bonuses["Strength"] = 5; item.Bonuses["All"] = 2; break;
                }
                
                // Class-specific stats
                if (baseName == "Dagger") { item.Bonuses["Dexterity"] = damage; item.Icon = "🗡️"; }
                else if (baseName == "Staff") { item.Bonuses["Intelligence"] = damage; item.Icon = "🪄"; }
                else if (baseName == "Bow") { item.Bonuses["Dexterity"] = damage; item.Icon = "🏹"; }
                else { item.Bonuses["Strength"] = damage; } // Default melee
            }
            else if (type == CharacterModels.ItemType.Armor)
            {
                string[] armors = { "Helmet", "Chestplate", "Boots" };
                baseName = armors[rng.Next(armors.Length)];
                if (baseName == "Helmet") { item.ValidSlot = CharacterModels.EquipmentSlot.Head; item.Icon = "⛑️"; }
                else if (baseName == "Chestplate") { item.ValidSlot = CharacterModels.EquipmentSlot.Chest; item.Icon = "👕"; }
                else if (baseName == "Boots") { item.ValidSlot = CharacterModels.EquipmentSlot.Feet; item.Icon = "🥾"; }


                int def = 1;
                switch(rarity)
                {
                    case CharacterModels.ItemRarity.Common: def = 1; break;
                    case CharacterModels.ItemRarity.Uncommon: def = 2; prefix = "Sturdy "; break;
                    case CharacterModels.ItemRarity.Rare: def = 3; prefix = "Reinforced "; item.Bonuses["Constitution"] = 1; break;
                    case CharacterModels.ItemRarity.Epic: def = 5; prefix = "Enchanted "; item.Bonuses["Constitution"] = 3; break;
                    case CharacterModels.ItemRarity.Legendary: def = 8; prefix = "Dragon "; item.Bonuses["Constitution"] = 5; item.Bonuses["Defense"] = 5; break;
                }
                item.Bonuses["Defense"] = def;
            }
            else
            {
                // Consumable
                baseName = "Potion";
                item.Icon = "🧪";
                if (rarity >= CharacterModels.ItemRarity.Rare) { baseName = "Elixir"; item.Icon = "🏺"; }
                item.Description = "Restores HP/Mana";
            }

            item.Name = $"{prefix}{baseName}";
            if (string.IsNullOrEmpty(item.Description)) item.Description = $"{rarity} Quality.";
            
            // Value Calculation
            item.Value = (int)rarity * 50 + rng.Next(10, 50);
            
            return item;
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

        public async Task StartCombat(string targetActorName, JsonElement? metadata = null)
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
            // Improve ID Parsing
            string baseName = targetActorName.Replace("voxel_", "");
            
            // v21.1: Context-Aware Return
            // In the future, pass the scene name from JS. For now, infer from mob type.
            if (metadata.HasValue && metadata.Value.TryGetProperty("returnScene", out var rs))
            {
                 _returnToScene = rs.ToString();
            }
            else if (baseName.Contains("Goblin") || baseName.Contains("Spider"))
            {
                _returnToScene = "GoblinCave";
            }
            else if (baseName.Contains("Slime") || baseName.Contains("Rat"))
            {
                _returnToScene = "Sewer";
            }
            else if (baseName.Contains("Snake") || baseName.Contains("Beetle"))
            {
                _returnToScene = "DungeonEntrance_thehole";
            }
            else
            {
                _returnToScene = "Crypt_Depth_1"; // Default to Crypt for Skellies
            }

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

            // v27.2: Custom Combat Atmosphere
            await JS.InvokeVoidAsync("stoneHammer.setAtmosphere", "#e6ccb3"); // Sandy Brown

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
                    HP = 40, MaxHP = 40, XPValue = 25,
                    IsHero = false, ModelId = enemyId
                };

                if (metadata.HasValue)
                {
                    // Data-Driven Stats
                    enemy.Name = $"{_engagedGroup} {suffix}"; // Generic name
                    if (metadata.Value.TryGetProperty("hp", out var h)) enemy.HP = enemy.MaxHP = h.GetInt32();
                    if (metadata.Value.TryGetProperty("xp", out var x)) enemy.XPValue = x.GetInt32();
                }
                else if (_engagedGroup.Contains("Goblin")) 
                {
                    enemy.Name = $"Goblin {suffix}";
                    enemy.HP = 25; enemy.MaxHP = 25; enemy.XPValue = 15;
                }
                else if (_engagedGroup.Contains("Spider"))
                {
                    enemy.Name = $"Spider {suffix}";
                    enemy.HP = 15; enemy.MaxHP = 15; enemy.XPValue = 10;
                }
                else if (_engagedGroup.Contains("Slime"))
                {
                    enemy.Name = $"Slime {suffix}";
                    enemy.HP = 35; enemy.MaxHP = 35; enemy.XPValue = 20;
                }
                else if (_engagedGroup.Contains("Rat"))
                {
                    enemy.Name = $"Giant Rat {suffix}";
                    enemy.HP = 10; enemy.MaxHP = 10; enemy.XPValue = 5;
                }
                else if (_engagedGroup.Contains("Snake"))
                {
                    enemy.Name = $"Giant Snake {suffix}";
                    enemy.HP = 50; enemy.MaxHP = 50; enemy.XPValue = 40;
                }
                else if (_engagedGroup.Contains("Beetle"))
                {
                    enemy.Name = $"Giant Beetle {suffix}";
                    enemy.HP = 80; enemy.MaxHP = 80; enemy.XPValue = 60;
                }

                
                Enemies.Add(enemy);
                
                
                // Spawn Visual
                string assetPath = "assets/skeleton.json";

                if (metadata.HasValue && metadata.Value.TryGetProperty("assetPath", out var ap))
                {
                    assetPath = ap.ToString();
                }
                else if (_engagedGroup.Contains("Goblin")) assetPath = "assets/goblin.json";
                else if (_engagedGroup.Contains("Spider")) assetPath = "assets/spider.json";
                else if (_engagedGroup.Contains("Slime")) assetPath = "assets/slime.json";
                else if (_engagedGroup.Contains("Slime")) assetPath = "assets/slime.json"; // Dupe implicit logic
                else if (_engagedGroup.Contains("Rat")) assetPath = "assets/rat.json";
                else if (_engagedGroup.Contains("Snake")) assetPath = "assets/snake.json";
                else if (_engagedGroup.Contains("Beetle")) assetPath = "assets/beetle.json";
                
                await _assets.SpawnAsset(assetPath, enemyId, false, new { Position = new float[] { 20, 0, (i * 8) - 4 }, Rotation = new float[] { 0, -90, 0 } });
            }

            // v27.3: Normalize Camera & UI (Moved After Spawning)
            await JS.InvokeVoidAsync("stoneHammer.combat.init", Enemies.Select(e => e.ModelId).ToArray());
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


    // Helper for JsonElement parsing
    public static int GetInt(object o)
    {
       if (o is int i) return i;
       if (o is long l) return (int)l;
       if (o is JsonElement je && je.ValueKind == JsonValueKind.Number) return je.GetInt32();
       return 0; // fallback
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
    
    // Moved to CombatService class
}
