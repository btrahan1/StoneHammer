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
            
            var recruit = new CharacterData 
            { 
                Name = "Recruit " + (Party.Count + 1),
                Class = CharacterClass.Fighter
            };
            
            Party.Add(recruit);
            SetClass(recruit, CharacterClass.Fighter); // Init stats
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
            
            OnCharacterUpdated?.Invoke();
        }

        public void AddSkill(CharacterData target, Skill skill)
        {
            target.Skills.Add(skill);
            OnCharacterUpdated?.Invoke();
        }
    }
}
