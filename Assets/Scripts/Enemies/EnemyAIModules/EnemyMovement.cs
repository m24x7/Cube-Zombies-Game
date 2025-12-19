using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyController))]
public class EnemyMovement : MonoBehaviour
{
    private EnemyController enemyAI;

    
    [SerializeField] private float repathInterval = 0.2f;
    private float timer;

    /// <summary>
    /// Awake is called when the script instance is being loaded
    /// </summary>
    private void Awake()
    {
        enemyAI = GetComponent<EnemyController>();
    }

    /// <summary>
    /// Update is called once per frame
    /// </summary>
    void Update()
    {
        if (Time.timeScale == 0f) return;
        if (!enemyAI.Target || !enemyAI.Agent) return;

        timer += Time.deltaTime;
        if (timer >= repathInterval)
        {
            timer = 0f;
            enemyAI.Agent.SetDestination(enemyAI.Target.position);
        }

        switch (enemyAI.EnemyState)
        {
            case EnemyState.Chase:
                enemyAI.Agent.isStopped = false;
                enemyAI.Agent.SetDestination(enemyAI.Target.position);
                break;

            case EnemyState.AttackBlock:
                // Stop to perform attacks
                enemyAI.Agent.isStopped = true;
                break;
            case EnemyState.AttackPlayer:
                // Stop to perform attacks
                enemyAI.Agent.isStopped = true;
                break;
        }
    }
}
