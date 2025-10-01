using UnityEngine;

public class Parent_ResourceEnergy : MonoBehaviour
{
    // Maximum Resource Amount
    [SerializeField] private int max = -1;
    public int Max { get => max; }

    // Current Resource Amount
    [SerializeField] private int cur = -1;
    public int Cur { get => cur; set => cur = value; }

    protected void Awake()
    {
        cur = max;
    }

    virtual public void UpdateVal(int x) { cur += x; }
}
