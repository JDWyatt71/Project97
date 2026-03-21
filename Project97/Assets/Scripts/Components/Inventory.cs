using UnityEngine;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;

public class Inventory : MonoBehaviour
{
    private Dictionary<ItemSO, int> inventory = new Dictionary<ItemSO, int>();
    public Dictionary<ItemSO, int> GetInventory()
    {
        return inventory;
    }
    public event EventHandler inventoryChanged;

    public void SetupInventory(Difficulty difficulty)
    {
        switch (difficulty)
        {
            case Difficulty.Easy:
                inventory = new() {
                    { AssetsDatabase.I.items[0], 3 }, //Bandage
                    { AssetsDatabase.I.items[1], 3 }, //Energy bar
                    { AssetsDatabase.I.items[2], 6 }, //Medecine
                    { AssetsDatabase.I.items[3], 3 }, //Sandals
                    { AssetsDatabase.I.items[4], 3 }, //Vitamins
                    { AssetsDatabase.I.items[5], 3 }, //Water
                };
                break;

            case Difficulty.Normal:
                inventory = new() {
                    { AssetsDatabase.I.items[0], 2 }, //Bandage
                    { AssetsDatabase.I.items[1], 2 }, //Energy bar
                    { AssetsDatabase.I.items[2], 5 }, //Medecine
                    { AssetsDatabase.I.items[3], 2 }, //Sandals
                    { AssetsDatabase.I.items[4], 2 }, //Vitamins
                    { AssetsDatabase.I.items[5], 2 }, //Water
                };
                break;

            case Difficulty.Hard:
                inventory = new() {
                    { AssetsDatabase.I.items[0], 1 }, //Bandage
                    { AssetsDatabase.I.items[1], 1 }, //Energy bar
                    { AssetsDatabase.I.items[2], 4 }, //Medecine
                    { AssetsDatabase.I.items[3], 1 }, //Sandals
                    { AssetsDatabase.I.items[4], 1 }, //Vitamins
                    { AssetsDatabase.I.items[5], 1 }, //Water
                };
                break;
        }

       
    }
    public void SetItem(ItemSO item, int amount)
    {
        inventory[item] = amount;
        inventoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public void AddItems(Dictionary<ItemSO, int> items)
    {
        foreach (ItemSO item in items.Keys)
        {
            AddItem(item, items[item]);
        }
    }
    public void AddItem(ItemSO item, int amount = 1)
    {
        //Works when values doesn't exist for a key.
        inventory[item] = inventory.GetValueOrDefault(item) + amount; 
        
        inventoryChanged?.Invoke(this, EventArgs.Empty);
    }
    public void RemoveItems(Dictionary<ItemSO, int> items)
    {
        foreach (ItemSO item in items.Keys)
        {
            RemoveItem(item, items[item]);
        }
    }
    public void RemoveItem(ItemSO item, int amount = 1)
    {
        inventory[item] -= amount;
        inventoryChanged?.Invoke(this, EventArgs.Empty);
    }
    public bool HasAmountOfItem(ItemSO item, int amount = 1)
    {
        return (inventory[item] >= amount);
        
    }
    public bool HasItems(Dictionary<ItemSO, int> items)
    {
        foreach (ItemSO item in items.Keys)
        {
            if (!HasAmountOfItem(item, items[item])) return false;
        }
        return true;
    }
    public void TransferItemsTo(Dictionary<ItemSO, int> items, Inventory otherInventory) //Transfers items from this inventory to another inputted inventory
    {
        RemoveItems(items); //RemoveItems from here

        otherInventory.AddItems(items); //Add items to other inventory
    }
    public void UseItem(ItemSO item, Character character)
    {
        foreach (ItemEffect effect in item.effects)
        {
            switch (effect)
            {
                case ItemEffect.IAttack:
                    character.ChangeBonusAttack(15);
                    break;

                case ItemEffect.IEvasion:
                    character.ChangeBonusEvasion(15);
                    break;

                case ItemEffect.IAP:
                    character.ChangeBonusAP(2);
                    break;

                case ItemEffect.IAccuracy:
                    character.ChangeBonusAccuracy(15);
                    break;

                case ItemEffect.HealHP:
                    double relativeHPgain = Math.Round(character.healthSystem.GetMaxHealth()/2d, 1);
                    print("heal"+character.healthSystem.GetHealth() / 2d);
                    character.healthSystem.
                        Heal(Convert.ToInt32(relativeHPgain));
                    break;

                case ItemEffect.HealStatus:
                    character.HealEffects();
                    break;
            }
        }
        RemoveItem(item);
    }
    public int HowMuchOfItem(ItemSO item)
    {
        return inventory[item];
    }
}
