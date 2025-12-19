using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// This class handles the movement behavior of an enemy AI
/// </summary>
[RequireComponent(typeof(EnemyController))]
public class EnemyMovement : MonoBehaviour
{
    private EnemyController enemyAI; // Reference to the EnemyController component

    [SerializeField] private float repathInterval = 0.2f; // Time interval for repathing
    private float timer; // Timer to track repathing intervals

    /// <summary>
    /// Awake is called when the script instance is being loaded
    /// </summary>
    private void Awake()
    {
        // Get reference to EnemyController component
        enemyAI = GetComponent<EnemyController>();
    }

    /// <summary>
    /// OnEnable is called when the object becomes enabled and active
    /// </summary>
    private void OnEnable() => NavMeshUtils.OnNavMeshUpdated += HandleNavMeshChanged; // Subscribe to NavMesh update events

    /// <summary>
    /// OnDisable is called when the behaviour becomes disabled or inactive
    /// </summary>
    private void OnDisable() => NavMeshUtils.OnNavMeshUpdated -= HandleNavMeshChanged; // Unsubscribe from NavMesh update events

    /// <summary>
    /// Update is called once per frame
    /// </summary>
    void Update()
    {
        // Pause check
        if (Time.timeScale == 0f) return;

        // Ensure we have a target and NavMeshAgent
        if (!enemyAI.Target || !enemyAI.Agent) return;

        // Repathing logic
        timer += Time.deltaTime; // Increment timer
        if (timer >= repathInterval) // Time to recalculate path
        {
            enemyAI.Agent.SetDestination(enemyAI.Target.position); // Update destination
            timer = 0f; // Reset timer
        }

        // State-based movement behavior
        switch (enemyAI.EnemyState)
        {
            case EnemyState.Chase:
                enemyAI.Agent.isStopped = false; // Ensure the agent is moving
                //enemyAI.Agent.SetDestination(enemyAI.Target.position); // Move towards the target
                break;

            case EnemyState.AttackBlock:
                // Stop to perform attacks
                enemyAI.Agent.isStopped = true; // Ensure the agent is stopped
                break;
            case EnemyState.AttackPlayer:
                // Stop to perform attacks
                enemyAI.Agent.isStopped = true; // Ensure the agent is stopped
                break;
        }
    }

    /// <summary>
    /// This method handles NavMesh changes by recalculating the path to the target
    /// </summary>
    private void HandleNavMeshChanged()
    {
        // Ensure we have a valid enemy AI, agent, and target
        if (!enemyAI || !enemyAI.Agent || !enemyAI.Target) return;

        // Recalculate path to target
        var dest = enemyAI.Target.position;
        enemyAI.Agent.ResetPath(); // Clear existing path
        enemyAI.Agent.SetDestination(dest); // Set new destination
    }
}
