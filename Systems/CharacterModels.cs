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

        public class Skill
        {
            public string Id { get; set; } = System.Guid.NewGuid().ToString();
            public string Name { get; set; } = "Unknown Skill";
            public string Description { get; set; } = "";
            public string Icon { get; set; } = "✨";
            public int ManaCost { get; set; } = 0;
            public int Cooldown { get; set; } = 0;
            public bool IsPassive { get; set; } = false;
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

        public class InventoryItem
        {
            public string Id { get; set; } = System.Guid.NewGuid().ToString();
            public string Name { get; set; } = "Unknown Item";
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
        }

        public class SaveGame
        {
            public string Name { get; set; } = "AutoSave";
            public System.DateTime Created { get; set; } = System.DateTime.Now;
            public List<CharacterData> Party { get; set; } = new List<CharacterData>();
        }
    }
}
