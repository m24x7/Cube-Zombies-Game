using UnityEngine;

public class BuildCurrency : MonoBehaviour
{
    [SerializeField] private int points = 500;

    public int Points => points;

    public bool TrySpend(int amount)
    {
        if (points < amount) return false;
        points -= amount;
        return true;
    }

    public void Add(int amount) => points += Mathf.Max(0, amount);
}
