using UnityEngine;

[CreateAssetMenu(fileName = "Item_Block_", menuName = "Items/Blocks/BlockDefinition")]
public class ItemDefinition_Block : ScriptableObject
{
    [Header("Identity")]
    public string Name = "Block_Generic";
    public ItemsEnum itemEnum = ItemsEnum.BasicBlock;
    public ItemTypeEnum itemType = ItemTypeEnum.Block;

    [Header("Prefab & Stats")]
    public GameObject prefab = null; // Requires Block script attached
}
