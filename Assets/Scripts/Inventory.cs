using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    //[SerializeField] private Dictionary<string , GameObject> inventory;
    //public Dictionary<string , GameObject> InventoryItems { get { return inventory; } }
    [SerializeField] private List<GameObject> inventory = new List<GameObject>();
    public List<GameObject> InventoryItems { get { return inventory; } }

    [SerializeField] private int maxInventorySize = 4;

    public void AddItem(GameObject item)
    {
        if (inventory.Count == maxInventorySize) return;

        if (item.GetComponent<I_Item>() != null)
        {
            inventory.Add(item);
            //inventory.Add(item.GetComponent<I_Item>().Name, item);
        }
        
    }

    //public GameObject GetItemByIndex(int index)
    //{
    //    if (inventory.Count == 0) return null;
    //    if (index < 0 || index >= inventory.Count) return null;

    //    int i = 0;
    //    foreach (var item in inventory.Values)
    //    {
    //        if (i == index)
    //        {
    //            return item;
    //        }
    //        i++;
    //    }

    //    return null;
    //}
}
