using TMPro;
using UnityEngine;

public class WallBuy : MonoBehaviour
{
    [SerializeField] private GameObject item = null;
    public GameObject Item { get { return item; } }


    [SerializeField] private int cost = 0;
    public int Cost { get { return cost; } }

    [SerializeField] private GameObject wallBuyCanvas;
    public GameObject WallBuyCanvas { get { return wallBuyCanvas; } }

    [SerializeField] TextMeshProUGUI wallBuyText;

    private int purchaseCooldown = 50;

    private void Start()
    {
        if (item != null && wallBuyText != null)
        {
            wallBuyText.text = $"Buy {item.GetComponent<Item_Weapon_Melee>().Name} for {cost} Points";
        }
    }

    private void Update()
    {
        if (purchaseCooldown > 0)
        {
            purchaseCooldown--;
        }
    }
    //private bool isItemRendered = false;

    //public GameObject BuyItem(int curPoints)
    //{
    //    if (!CanBuy(curPoints)) return null;

    //    return itemToBuy;
    //}

    public bool CanBuy(int curPoints)
    {
        if (purchaseCooldown > 0) return false;
        return curPoints >= cost;
    }
}
