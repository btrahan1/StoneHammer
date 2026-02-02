using System.Collections.Generic;

namespace StoneHammer.Systems
{
    public class CharacterModels
    {
        // Stats
        public class StatBlock
        {
            public int Strength { get; set; } = 10;
            public int Dexterity { get; set; } = 10;
            public int Constitution { get; set; } = 10;
            public int Intelligence { get; set; } = 10;
            public int Wisdom { get; set; } = 10;
            public int Charisma { get; set; } = 10;
            
            // Derived (Simplified for now)
            public int MaxHP => 20 + (Constitution * 2);
            public int Defense => 10 + (Dexterity / 2);
        }

        public enum CharacterClass
        {
            Fighter,
            Rogue,
            Healer,
            Mage
        }

        public enum SkillTargetType
        {
            SingleEnemy,
            AllEnemies,
            SingleAlly,
            AllAllies,
            Self
        }

        public enum SkillEffectType
        {
            Damage,
            Heal,
            Buff,
            Debuff
        }

        public class Skill
        {
            public string Id { get; set; } = System.Guid.NewGuid().ToString();
            public string Name { get; set; } = "Unknown Skill";
            public string Description { get; set; } = "";
            public string Icon { get; set; } = "✨";
            public int ManaCost { get; set; } = 0;
            public int Cooldown { get; set; } = 0;
            public bool IsPassive { get; set; } = false;
            
            // Data-Driven Properties
            public SkillTargetType TargetType { get; set; } = SkillTargetType.SingleEnemy;
            public SkillEffectType EffectType { get; set; } = SkillEffectType.Damage;
            public float Multiplier { get; set; } = 1.0f;
            public string Animation { get; set; } = "Attack";
            public string VfxColor { get; set; } = "#FFFFFF";
            public string Sfx { get; set; } = "attack_melee";
        }

        public enum EquipmentSlot
        {
            Head,
            Chest,
            Legs,
            Feet,
            MainHand,
            OffHand,
            Accessory
        }

        public enum ItemType
        {
            Weapon,
            Armor,
            Consumable,
            Material,
            Quest
        }

        public enum ItemRarity
        {
            Common,
            Uncommon,
            Rare,
            Epic,
            Legendary
        }

        public class InventoryItem
        {
            public string Id { get; set; } = System.Guid.NewGuid().ToString();
            public string Name { get; set; } = "Unknown Item";
            public ItemRarity Rarity { get; set; } = ItemRarity.Common;
            public string Description { get; set; } = "";
            public string Icon { get; set; } = "📦"; // Emoji fallback
            public ItemType Type { get; set; }
            public int Value { get; set; } = 0;
            public int Weight { get; set; } = 0;
            
            // Stats (Simplistic dictionary for bonuses)
            public Dictionary<string, int> Bonuses { get; set; } = new Dictionary<string, int>();

            public EquipmentSlot? ValidSlot { get; set; } // Null if not equippable
        }

        public class CharacterData
        {
            public string Name { get; set; } = "Master Mason";
            public CharacterClass Class { get; set; } = CharacterClass.Fighter;
            public int Level { get; set; } = 1;
            public int CurrentXP { get; set; } = 0;
            
            public StatBlock Stats { get; set; } = new StatBlock();
            
            // Equipment
            public Dictionary<EquipmentSlot, InventoryItem?> Equipment { get; set; } = new Dictionary<EquipmentSlot, InventoryItem?>();

            // Inventory & Skills
            public List<InventoryItem> Inventory { get; set; } = new List<InventoryItem>();
            public List<Skill> Skills { get; set; } = new List<Skill>();
            
            public int Gold { get; set; } = 0;
            public int CurrentMana { get; set; } = 20;
            public int MaxMana { get; set; } = 20;

            // Persistent Health
            public int CurrentHP { get; set; } = 20;

            public CharacterData()
            {
                // Init Slots
                foreach(EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
                {
                    Equipment[slot] = null;
                }
            }

            public int GetTotalDefense()
            {
                int total = Stats.Defense;
                foreach(var item in Equipment.Values)
                {
                    if (item != null && item.Bonuses.ContainsKey("Defense"))
                    {
                        total += item.Bonuses["Defense"];
                    }
                }
                return total;
            }
            public int GetTotalAttack()
            {
                // 1. Determine Primary Stat
                int baseStat = Stats.Strength;
                string relevantBonus = "Strength";
                
                switch(Class)
                {
                    case CharacterClass.Rogue: 
                        baseStat = Stats.Dexterity; 
                        relevantBonus = "Dexterity";
                        break;
                    case CharacterClass.Mage: 
                        baseStat = Stats.Intelligence; 
                        relevantBonus = "Intelligence";
                        break;
                    case CharacterClass.Healer: 
                        baseStat = Stats.Wisdom; 
                        relevantBonus = "Wisdom";
                        break;
                }
                
                int damage = baseStat;

                // 2. Add Weapon Bonus (Simplified: Scaled off primary stat bonus on weapon)
                if (Equipment.TryGetValue(EquipmentSlot.MainHand, out var weapon) && weapon != null)
                {
                     // If weapon has a bonus to our primary stat, it counts double as "Damage"
                     if (weapon.Bonuses.ContainsKey(relevantBonus))
                     {
                         damage += weapon.Bonuses[relevantBonus] * 2;
                     }
                     // If it has a generic "Damage" stat (future proofing)
                     if (weapon.Bonuses.ContainsKey("Damage"))
                     {
                         damage += weapon.Bonuses["Damage"];
                     }
                }
                
                return damage;
            }

            // RESOURCE SYSTEM
            public string GetResourceName()
            {
                switch(Class)
                {
                    case CharacterClass.Rogue: return "Energy";
                    case CharacterClass.Fighter: return "Energy";
                    case CharacterClass.Healer: return "Faith";
                    case CharacterClass.Mage: return "Mana";
                    default: return "Mana";
                }
            }

            public string GetResourceColor()
            {
                switch(Class)
                {
                    case CharacterClass.Rogue: return "#ffff00"; // Yellow
                    case CharacterClass.Fighter: return "#ffaa00"; // Orange
                    case CharacterClass.Healer: return "#aa00ff"; // Purple
                    case CharacterClass.Mage: return "#0088ff"; // Blue
                    default: return "gray";
                }
            }

            public int GetMaxResource()
            {
                switch(Class)
                {
                    case CharacterClass.Fighter: 
                         return Stats.Strength * 3;
                         
                    case CharacterClass.Rogue: 
                         return Stats.Dexterity * 3;
                    
                    case CharacterClass.Healer: 
                        return Stats.Wisdom * 3;
                    
                    case CharacterClass.Mage: 
                        return Stats.Intelligence * 3;
                        
                    default: return 20;
                }
            }
        }

        public class SaveGame
        {
            public string Name { get; set; } = "AutoSave";
            public System.DateTime Created { get; set; } = System.DateTime.Now;
            public List<CharacterData> Party { get; set; } = new List<CharacterData>();
        }
    }
}
