using UnityEngine;

public interface I_Item
{
    public ScriptableObject ItemDef { get; }
    public string Name { get; }
    public ItemsEnum ItemID { get; }
    public ItemTypeEnum TypeID { get; }
    //public ItemRarityEnum RarityID { get; }
    //public GameObject Prefab { get; }
    

    public Sprite Icon { get; }
}
