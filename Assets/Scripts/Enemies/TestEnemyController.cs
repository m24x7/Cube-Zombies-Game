using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class TestEnemyController : Parent_Entity
{
    public static event Action<TestEnemyController> OnEntityDeath;

    [Header("Runtime values (initialized by spawner)")]
    public EnemyDefinition definition;
    public int reward;

    private NavMeshAgent agent;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    // Called by WaveDirector after Instantiate
    public void Initialize(EnemyDefinition def, float healthMult, float speedMult)
    {
        definition = def;
        Health.Init(Mathf.RoundToInt(def.baseHealth * Mathf.Max(0.1f, healthMult)));
        reward = def.killReward;

        if (agent)
            agent.speed = def.baseSpeed * Mathf.Max(0.1f, speedMult);
    }

    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);
        Debug.Log("TestEnemyController: Took " + damage + " damage. Current health: " + Health.Cur);
    }

    protected override void Die()
    {
        Debug.Log("TestEnemyController: Died.");
        OnEntityDeath?.Invoke(this);
        Destroy(gameObject);
    }
}
