using UnityEngine;

[RequireComponent(typeof(Parent_Entity))]
public class Resource_Health : Parent_ResourceEnergy
{
    public override void UpdateVal(int x)
    {
        base.UpdateVal(x);

        if (Cur <= 0)
        {
            Die();
        }
    }

    private void Die()
    {

    }
}
