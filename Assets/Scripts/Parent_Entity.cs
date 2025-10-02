using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Parent_Entity : MonoBehaviour
{
    // Stat Resources
    [SerializeField] protected Resource_Health health;
    public Resource_Health Health { get => health; }

    //public static event Action<Parent_Entity> OnEntityDeath;

    //[SerializeField] protected float moveSpeed;
    //public float MoveSpeed { get => moveSpeed; }

    [Header("Attack (timing)")]
    [SerializeField] protected float attackWindup = 0.05f;  // delay before the hit happens
    [SerializeField] protected float attackActive = 0.02f;  // (optional) time the hit window stays "active"
    [SerializeField] protected float attackRecovery = 0.25f;  // time before you can attack again
    [SerializeField] protected LayerMask attackMask = ~0;     // which layers the ray can hit (env + enemies)

    protected Coroutine attackCoroutine;

    virtual public void Heal(int heal) { Health.UpdateVal(heal); if (health.Cur > health.Max) health.Cur = health.Max; }
    virtual public void TakeDamage(int damage) { Health.UpdateVal(-damage); if (health.Cur <= 0) Die(); }
    virtual protected void Die() { Debug.Log("Parent_Entity: Die not implemented"); }
    virtual public void Attack() {; }
    virtual protected IEnumerator AttackRoutine()
    {
        yield return null;
    }
}
