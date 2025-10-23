using UnityEngine;

[CreateAssetMenu(fileName = "Item_Weapon_Melee_", menuName = "Items/Weapons/MeleeDefinition")]
public class ItemDefinition_Weapon_Melee : ScriptableObject
{
    [Header("Identity")]
    public string Name = "Melee_Generic";
    public ItemsEnum itemEnum = ItemsEnum.MeleeGeneric;
    public ItemTypeEnum itemType = ItemTypeEnum.Weapon;

    [Header("Prefab & Stats")]
    public GameObject prefab = null;         // Must have Weapon_Melee script
    public ItemRarityEnum Rarity = ItemRarityEnum.Common;
    public int Damage = 0;
    public float AttackRange = 0f;
    public float AttackSpeed = 0f;
}
