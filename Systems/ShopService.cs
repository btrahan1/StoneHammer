using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.JSInterop;

namespace StoneHammer.Systems
{
    public class ShopService
    {
        private readonly CharacterService _charService;
        private readonly IJSRuntime _js;

        public bool IsOpen { get; private set; }
        public List<CharacterModels.InventoryItem> Stock { get; private set; } = new List<CharacterModels.InventoryItem>();

        public event Action? OnStateChanged;

        public ShopService(CharacterService charService, IJSRuntime js)
        {
            _charService = charService;
            _js = js;
        }

        public void OpenShop()
        {
            if (IsOpen) return;
            
            GenerateDailyStock();
            IsOpen = true;
            OnStateChanged?.Invoke();
        }

        public void CloseShop()
        {
            IsOpen = false;
            OnStateChanged?.Invoke();
        }

        private void GenerateDailyStock()
        {
            Stock.Clear();
            var rng = new Random();

            // Consumables
            Stock.Add(new CharacterModels.InventoryItem 
            { 
                Name = "Minor Potion", Type = CharacterModels.ItemType.Consumable, 
                Icon = "🧪", Value = 20, Description = "Restores 10 HP." 
            });
            Stock.Add(new CharacterModels.InventoryItem 
            { 
                Name = "Health Potion", Type = CharacterModels.ItemType.Consumable, 
                Icon = "💖", Value = 50, Description = "Restores 30 HP." 
            });

            // Weapons
            var weapons = new List<CharacterModels.InventoryItem>
            {
                new CharacterModels.InventoryItem { Name = "Bronze Sword", Type = CharacterModels.ItemType.Weapon, ValidSlot = CharacterModels.EquipmentSlot.MainHand, Icon = "⚔️", Value = 100, Bonuses = new Dictionary<string, int> { { "Strength", 3 } }, Description = "Standard issue." },
                new CharacterModels.InventoryItem { Name = "Iron Dagger", Type = CharacterModels.ItemType.Weapon, ValidSlot = CharacterModels.EquipmentSlot.MainHand, Icon = "🗡️", Value = 80, Bonuses = new Dictionary<string, int> { { "Dexterity", 3 } }, Description = "Fast and sharp." },
                new CharacterModels.InventoryItem { Name = "Oak Staff", Type = CharacterModels.ItemType.Weapon, ValidSlot = CharacterModels.EquipmentSlot.MainHand, Icon = "🪄", Value = 120, Bonuses = new Dictionary<string, int> { { "Intelligence", 3 } }, Description = "Arcane focus." },
                new CharacterModels.InventoryItem { Name = "Cleric Mace", Type = CharacterModels.ItemType.Weapon, ValidSlot = CharacterModels.EquipmentSlot.MainHand, Icon = "🔨", Value = 110, Bonuses = new Dictionary<string, int> { { "Wisdom", 3 } }, Description = "For smiting." }
            };

            // Armor/Offhand
            var armor = new List<CharacterModels.InventoryItem>
            {
                new CharacterModels.InventoryItem { Name = "Leather Vest", Type = CharacterModels.ItemType.Armor, ValidSlot = CharacterModels.EquipmentSlot.Chest, Icon = "🦺", Value = 80, Bonuses = new Dictionary<string, int> { { "Defense", 2 } }, Description = "Light protection." },
                new CharacterModels.InventoryItem { Name = "Wooden Shield", Type = CharacterModels.ItemType.Armor, ValidSlot = CharacterModels.EquipmentSlot.OffHand, Icon = "🛡️", Value = 60, Bonuses = new Dictionary<string, int> { { "Defense", 1 }, { "Block", 5 } }, Description = "Basic defense." },
                new CharacterModels.InventoryItem { Name = "Chainmail", Type = CharacterModels.ItemType.Armor, ValidSlot = CharacterModels.EquipmentSlot.Chest, Icon = "⛓️", Value = 250, Bonuses = new Dictionary<string, int> { { "Defense", 4 } }, Description = "Heavy protection." }
            };

            // Randomly select a few
            foreach(var w in weapons.OrderBy(x => rng.Next()).Take(3)) Stock.Add(w);
            foreach(var a in armor.OrderBy(x => rng.Next()).Take(2)) Stock.Add(a);
        }

        public void BuyItem(CharacterModels.CharacterData buyer, CharacterModels.InventoryItem item)
        {
            if (buyer == null) return;

            // POOLED GOLD LOGIC
            // 1. Check Total Wealth
            int totalGold = _charService.Party.Sum(p => p.Gold);
            if (totalGold < item.Value) return; // Cannot afford

            // 2. Deduct Cost (Spread across party, starting with richest or player)
            int remainingCost = item.Value;
            
            // Sort by gold descending to drain richest first
            foreach(var member in _charService.Party.OrderByDescending(p => p.Gold))
            {
                if (remainingCost <= 0) break;
                
                int take = Math.Min(member.Gold, remainingCost);
                _charService.AddGold(member, -take);
                remainingCost -= take;
            }

            // 3. Give Item
            _charService.AddItem(buyer, item); 
            OnStateChanged?.Invoke();
        }

        public void SellItem(CharacterModels.CharacterData seller, CharacterModels.InventoryItem item)
        {
            if (seller == null) return;
            // implicitly seller passed via item lookup in previous version, now explicit is better

            int sellValue = item.Value / 2;
            _charService.RemoveItem(seller, item);
            _charService.AddGold(seller, sellValue);
            
            OnStateChanged?.Invoke();
        }
    }
}
