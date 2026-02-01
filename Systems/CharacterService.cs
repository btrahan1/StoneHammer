using System;
using System.Collections.Generic;
using System.Linq;
using static StoneHammer.Systems.CharacterModels;

namespace StoneHammer.Systems
{
    public class CharacterService
    {
        public CharacterData Player => Party[0]; // Main Character
        public List<CharacterData> Party { get; private set; } = new List<CharacterData>();
        
        // Event to notify UI of changes
        public event Action? OnCharacterUpdated;

        public void LoadParty(List<CharacterData> loadedParty)
        {
            Party = loadedParty;
            OnCharacterUpdated?.Invoke();
        }

        public CharacterService()
        {
            Initialize();
        }

        public void ResetGame()
        {
            Initialize();
            OnCharacterUpdated?.Invoke();
        }

        private void Initialize()
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
            SetClass(mainChar, CharacterClass.Fighter);
        }

        public void RecruitMember()
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
            SetClass(recruit, newClass); // Init stats
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

        public void SetClass(CharacterData target, CharacterClass newClass)
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
                    AddSkill(target, new Skill { Name = "Power Strike", Description = "A heavy blow.", ManaCost = 5, Icon = "⚔️" });
                    AddSkill(target, new Skill { Name = "Block", Description = "Reduce damage.", ManaCost = 0, Icon = "🛡️" });
                    break;
                case CharacterClass.Rogue:
                    target.Stats.Dexterity = 16;
                    target.Stats.Charisma = 14;
                    AddSkill(target, new Skill { Name = "Backstab", Description = "Critical hit from behind.", ManaCost = 10, Icon = "🗡️" });
                    AddSkill(target, new Skill { Name = "Stealth", Description = "Become invisible.", ManaCost = 5, Icon = "👻" });
                    break;
                case CharacterClass.Healer:
                    target.Stats.Wisdom = 16;
                    target.Stats.Constitution = 12;
                    AddSkill(target, new Skill { Name = "Heal", Description = "Restore HP.", ManaCost = 8, Icon = "💖" });
                    AddSkill(target, new Skill { Name = "Smite", Description = "Holy damage.", ManaCost = 6, Icon = "⚡" });
                    break;
                case CharacterClass.Mage:
                    target.Stats.Intelligence = 16;
                    target.Stats.Wisdom = 14;
                    AddSkill(target, new Skill { Name = "Fireball", Description = "Explosive damage.", ManaCost = 12, Icon = "🔥" });
                    AddSkill(target, new Skill { Name = "Ice Bolt", Description = "Freeze enemy.", ManaCost = 8, Icon = "❄️" });
                    break;
            }
            
            // Recalc Max Resource
            target.MaxMana = target.GetMaxResource();
            target.CurrentMana = target.MaxMana;
            
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

            // Restore HP/Mana on Level Up
            target.MaxMana = target.GetMaxResource();
            target.CurrentMana = target.MaxMana;
            // MaxHP grows automatically via Constitution property in StatBlock
        }
    }
}
