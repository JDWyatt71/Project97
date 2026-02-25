using UnityEngine;
using System.Collections.Generic;
using System;

public class Inventory : MonoBehaviour
{
    private Dictionary<ItemSO, int> inventory = new Dictionary<ItemSO, int>();
    public Dictionary<ItemSO, int> GetInventory()
    {
        return inventory;
    }
    public event EventHandler inventoryChanged;

    private void SetupDefaultEmptyInventory()
    {
        inventory = new Dictionary<ItemSO, int>();

        ItemSO[] itemSOs = Resources.LoadAll<ItemSO>("");

        foreach (ItemSO itemSO in itemSOs)
        {
            inventory.Add(itemSO, 0);
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
    public void UseItem()
    {
    }
    public int HowMuchOfItem(ItemSO item)
    {
        return inventory[item];
    }
}
