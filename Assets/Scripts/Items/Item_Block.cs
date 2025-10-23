using UnityEngine;

public class Item_Block : MonoBehaviour, I_Item
{
    // Item Definition Reference
    [SerializeField] private ItemDefinition_Block itemDef;
    public ScriptableObject ItemDef { get { return itemDef; } }


    // Item Identity
    private string itemName = "Block_Generic";
    public string Name { get { return itemName; } }
    

    [SerializeField] private ItemsEnum itemID;
    public ItemsEnum ItemID { get { return itemID; } }


    [SerializeField] private ItemTypeEnum typeID;
    public ItemTypeEnum TypeID { get { return typeID; } }

    // Item Icon
    [SerializeField] private Sprite itemIcon;
    public Sprite Icon { get { return itemIcon; } }

    private void Start()
    {
        if (itemDef != null)
        {
            itemName = itemDef.Name;
            itemID = itemDef.itemEnum;
            typeID = itemDef.itemType;
        }
    }
}
