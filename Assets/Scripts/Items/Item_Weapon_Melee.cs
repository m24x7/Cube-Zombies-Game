using UnityEngine;

public class Item_Weapon_Melee: MonoBehaviour, I_Item
{
    // Item Definition Reference
    [SerializeField] private ItemDefinition_Weapon_Melee itemDef = null;
    public ScriptableObject ItemDef { get { return itemDef; } }


    // Item Identity
    [SerializeField] private string itemName;
    public string Name { get { return itemName; } }


    [SerializeField] private ItemsEnum itemID;
    public ItemsEnum ItemID { get { return itemID; } }


    [SerializeField] private ItemTypeEnum typeID;
    public ItemTypeEnum TypeID { get { return typeID; } }


    // Weapon Specific Stats
    [SerializeField] private int damage = 25;
    public int Damage { get { return damage; } }

    [SerializeField] private float range = 1.5f;
    public float Range { get { return range; } }

    [SerializeField] private float swingSpeed = 20f;
    public float SwingSpeed { get { return swingSpeed; } }

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
            damage = itemDef.Damage;
            range = itemDef.AttackRange;
            swingSpeed = itemDef.AttackSpeed;
        }
    }
}
