using UnityEngine;

public class Parent_Entity : MonoBehaviour
{
    // Stat Resources
    [SerializeField] protected Resource_Health health;
    public Resource_Health Health { get => health; }

    [SerializeField] protected float moveSpeed;
    public float MoveSpeed { get => moveSpeed; }

    virtual public void Heal(int heal) { health.UpdateVal(heal); if (health.Cur > health.Max) health.Cur = health.Max; }
    virtual public void TakeDamage(int damage) { Health.UpdateVal(-damage); if (health.Cur <= 0) Die(); }
    virtual public void Die() { Debug.Log("Parent_Entity: Die not implemented"); }
}
