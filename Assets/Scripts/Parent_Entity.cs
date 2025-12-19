using System.Collections;
using UnityEngine;

/// <summary>
/// This is the parent class for all entities in the game (player, enemies, etc.)
/// </summary>
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

    protected Coroutine attackCoroutine; // reference to the attack coroutine

    /// <summary>
    /// This method heals the entity by the specified amount
    /// </summary>
    /// <param name="heal"></param>
    virtual public void Heal(int heal) { Health.UpdateVal(heal); if (health.Cur > health.Max) health.Cur = health.Max; }

    /// <summary>
    /// This method applies damage to the entity
    /// </summary>
    /// <param name="damage"></param>
    /// <param name="ignoreInvincibility"></param>
    virtual public void TakeDamage(int damage, bool ignoreInvincibility = false) { Health.UpdateVal(-damage); if (health.Cur <= 0) Die(); }

    /// <summary>
    /// This method handles the death of the entity
    /// </summary>
    virtual protected void Die() { Debug.Log("Parent_Entity: Die not implemented"); }

    /// <summary>
    /// This method performs an attack
    /// </summary>
    virtual public void Attack() {; }

    /// <summary>
    /// This coroutine handles the attack timing
    /// </summary>
    /// <returns></returns>
    virtual protected IEnumerator AttackRoutine()
    {
        yield return null;
    }
}
