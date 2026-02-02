using System;
using System.Collections.Generic;
using System.Linq;
using static StoneHammer.Systems.CharacterModels;
using System.Text.Json;

namespace StoneHammer.Systems
{
    public class CharacterService
    {
        public CharacterData Player => Party[0]; // Main Character
        public List<CharacterData> Party { get; private set; } = new List<CharacterData>();
        
        private HttpClient _http;
        
        // Event to notify UI of changes
        public event Action? OnCharacterUpdated;

        public void LoadParty(List<CharacterData> loadedParty)
        {
            Party = loadedParty;
            OnCharacterUpdated?.Invoke();
        }

        public CharacterService(HttpClient http)
        {
            _http = http;
            // InitializeAsync must be called manually or fire-and-forget (not ideal but ok for client-side Blazor if careful)
        }

        public async Task ResetGame()
        {
            await InitializeAsync();
            OnCharacterUpdated?.Invoke();
        }

        public async Task InitializeAsync()
        {
            Party.Clear();
            
            // Initialize with default player
            var mainChar = new CharacterData();
            Party.Add(mainChar);
            
            // Give starting items
            AddItem(mainChar, new InventoryItem 
            { 
                Name = "Stone Hammer", 
                Description = "A sturdy tool for a sturdy mason.", 
                Icon = "🔨", 
                Type = ItemType.Weapon, 
                ValidSlot = EquipmentSlot.MainHand,
                Bonuses = new Dictionary<string, int> { { "Strength", 2 } }
            });
            
            AddItem(mainChar, new InventoryItem 
            { 
                Name = "Work Helmet", 
                Description = "Safety first.", 
                Icon = "⛑️", 
                Type = ItemType.Armor, 
                ValidSlot = EquipmentSlot.Head,
                Bonuses = new Dictionary<string, int> { { "Defense", 1 } }
            });
            
            AddItem(mainChar, new InventoryItem 
            { 
                Name = "Health Potion", 
                Description = "Restores 20 HP.", 
                Icon = "🧪", 
                Type = ItemType.Consumable,
                Value = 10
            });
            
            // Auto Equip Starter Gear
            Equip(mainChar, mainChar.Inventory.FirstOrDefault(i => i.Name == "Stone Hammer"));
            Equip(mainChar, mainChar.Inventory.FirstOrDefault(i => i.Name == "Work Helmet"));
            
            // Set default class
            await SetClassAsync(mainChar, CharacterClass.Fighter);
        }

        public async Task RecruitMember()
        {
            if (Party.Count >= 4) return;
            
            // Cycle classes: 1=Rogue, 2=Healer, 3=Mage
            var newClass = (CharacterClass)(Party.Count % 4);
            if (newClass == CharacterClass.Fighter) newClass = CharacterClass.Rogue; // Avoid duping fighter immediately if possible

            var recruit = new CharacterData 
            { 
                Name = newClass.ToString() + " " + (Party.Count), // e.g. "Rogue 1"
                Class = newClass
            };
            
            Party.Add(recruit);
            await SetClassAsync(recruit, newClass); // Init stats
            OnCharacterUpdated?.Invoke();
        }

        public void AddItem(CharacterData target, InventoryItem item)
        {
            target.Inventory.Add(item);
            OnCharacterUpdated?.Invoke();
        }

        public void RemoveItem(CharacterData target, InventoryItem item)
        {
            target.Inventory.Remove(item);
            OnCharacterUpdated?.Invoke();
        }
        
        public void AddGold(CharacterData target, int amount)
        {
            target.Gold += amount;
            OnCharacterUpdated?.Invoke();
        }

        public void TransferItem(CharacterData from, CharacterData to, InventoryItem item)
        {
            if (from == to) return;
            if (!from.Inventory.Contains(item)) return;

            // Unequip if currently equipped
            if (item.ValidSlot.HasValue && from.Equipment.Values.Contains(item))
            {
                 var slot = item.ValidSlot.Value;
                 from.Equipment[slot] = null;
            }

            from.Inventory.Remove(item);
            to.Inventory.Add(item);
            
            OnCharacterUpdated?.Invoke();
        }

        public void Equip(CharacterData target, InventoryItem? item)
        {
            if (item == null || !item.ValidSlot.HasValue) return;

            var slot = item.ValidSlot.Value;
            
            // Unequip current if exists
            if (target.Equipment[slot] != null)
            {
                // Verify it's in inventory? It should be.
            }

            target.Equipment[slot] = item;
            OnCharacterUpdated?.Invoke();
        }

        public void Unequip(CharacterData target, EquipmentSlot slot)
        {
            target.Equipment[slot] = null;
            OnCharacterUpdated?.Invoke();
        }

        private Dictionary<CharacterClass, List<Skill>> _skillCache = new Dictionary<CharacterClass, List<Skill>>();

        public async Task<List<Skill>> GetClassSkillsAsync(CharacterClass cClass)
        {
            if (_skillCache.ContainsKey(cClass)) return _skillCache[cClass];

            try 
            {
                string jsonPath = $"assets/data/skills/skills_{cClass.ToString().ToLower()}.json";
                var json = await _http.GetStringAsync(jsonPath + "?v=" + DateTime.Now.Ticks);
                
                var options = new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                };
                options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

                var skills = JsonSerializer.Deserialize<List<Skill>>(json, options);
                if (skills != null) 
                {
                    _skillCache[cClass] = skills;
                    return skills;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[CharacterService] Failed to load skills for {cClass}: {ex.Message}");
            }
            return new List<Skill>();
        }

        public async Task SetClassAsync(CharacterData target, CharacterClass newClass)
        {
            target.Class = newClass;
            
            // Default Stats based on Class
            target.Stats = new StatBlock(); // Reset
            target.Skills.Clear();

            switch (newClass)
            {
                case CharacterClass.Fighter:
                    target.Stats.Strength = 16;
                    target.Stats.Constitution = 14;
                    break;
                case CharacterClass.Rogue:
                    target.Stats.Dexterity = 16;
                    target.Stats.Charisma = 14;
                    break;
                case CharacterClass.Healer:
                    target.Stats.Wisdom = 16;
                    target.Stats.Constitution = 12;
                    break;
                case CharacterClass.Mage:
                    target.Stats.Intelligence = 16;
                    target.Stats.Wisdom = 14;
                    break;
            }

            // Load Skills and Grant Starters
            var allSkills = await GetClassSkillsAsync(newClass);
            var starters = allSkills.Where(s => s.LevelRequirement <= 1 && s.GoldCost == 0).ToList();
            target.Skills.AddRange(starters);
            
            // Recalc Max Resource
            target.MaxMana = target.GetMaxResource();
            target.CurrentMana = target.MaxMana;
            target.CurrentHP = target.Stats.MaxHP;
            
            OnCharacterUpdated?.Invoke();
        }

        public bool CanLearnSkill(CharacterData character, Skill skill)
        {
            if (character.Skills.Any(s => s.Name == skill.Name)) return false; // Already known
            if (character.Level < skill.LevelRequirement) return false;
            // v30.1: Pooled Gold (Player pays for everyone)
            if (Player.Gold < skill.GoldCost) return false;
            return true;
        }

        public void LearnSkill(CharacterData character, Skill skill)
        {
            if (!CanLearnSkill(character, skill)) return;

            // v30.1: Pooled Gold
            Player.Gold -= skill.GoldCost;
            character.Skills.Add(skill);
            OnCharacterUpdated?.Invoke();
        }

        public void AddSkill(CharacterData target, Skill skill)
        {
            target.Skills.Add(skill);
            OnCharacterUpdated?.Invoke();
        }

        public void GainXP(CharacterData target, int amount)
        {
            target.CurrentXP += amount;

            // Level Up Logic
            // Curve: Level * 100 XP needed for next level
            // e.g. Lvl 1 needs 100 XP to get to Lvl 2.
            // Lvl 2 needs 200 XP to get to Lvl 3.
            while (true)
            {
                int xpNeeded = target.Level * 100;
                if (target.CurrentXP >= xpNeeded)
                {
                    target.CurrentXP -= xpNeeded;
                    LevelUp(target);
                }
                else
                {
                    break;
                }
            }
            
            OnCharacterUpdated?.Invoke();
        }

        public void Rest()
        {
            foreach(var member in Party)
            {
                member.CurrentHP = member.Stats.MaxHP;
                member.CurrentMana = member.MaxMana;
            }
            OnCharacterUpdated?.Invoke();
        }

        private void LevelUp(CharacterData target)
        {
            target.Level++;
            
            // Stat Growth
            // Simplified: +1 to Primary Stat, +1 to Con
            var stats = target.Stats;
            stats.Constitution++;
            
            switch(target.Class)
            {
                case CharacterClass.Fighter: stats.Strength++; break;
                case CharacterClass.Rogue: stats.Dexterity++; break;
                case CharacterClass.Healer: stats.Wisdom++; break;
                case CharacterClass.Mage: stats.Intelligence++; break;
            }

            // Restore HP/Mana on Level Up partially or fully? 
            // Let's heal them fully on level up for gratification
            target.MaxMana = target.GetMaxResource();
            target.CurrentMana = target.MaxMana;
            target.CurrentHP = target.Stats.MaxHP;
        }
    }
}
