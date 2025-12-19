using System.IO;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// The different states an enemy can be in
/// </summary>
public enum EnemyState
{
    Chase,
    AttackBlock,
    AttackPlayer
}

/// <summary>
/// This class manages the state machine for an enemy AI
/// </summary>
public class EnemyStateMachine : MonoBehaviour
{
    #region Enemy States
    [SerializeField] private EnemyState currentState = EnemyState.Chase; // Default starting state
    public EnemyState GetState => currentState; // Public getter for current state

    private EnemyState previousState; // To track previous state
    public EnemyState GetPreviousState => previousState; // Public getter for previous state
    #endregion

    private EnemyController enemyAI; // Reference to the EnemyController

    /// <summary>
    /// Start is called once before the first execution of Update after the MonoBehaviour is created
    /// </summary>
    void Start()
    {
        enemyAI = GetComponent<EnemyController>(); // Get reference to the EnemyController
    }

    ///// <summary>
    ///// Update is called once per frame
    ///// </summary>
    //void Update()
    //{

    //}

    /// <summary>
    /// Update the enemy state based on player distance
    /// </summary>
    public void UpdateState()
    {
        // Get player transform
        Transform playerTransform = Controller_Game.Instance.GetPlayer().transform;

        // If no player, do nothing
        if (playerTransform == null) return;

        // Calculate distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // State transition logic
        switch (currentState)
        {
            case EnemyState.Chase:
                // Transition to AttackBlock if blocks are in the path
                if (enemyAI.BlockInPath)
                {
                    enemyAI.AttackCooldownRemaining = enemyAI.BlockAttackCooldown; // give attack buffer when entering block attack state
                    SetState(EnemyState.AttackBlock);
                    return;
                }

                // Transition to AttackPlayer if in range
                if (distanceToPlayer <= enemyAI.PlayerAttackDistance)
                {
                    SetState(EnemyState.AttackPlayer);
                    return;
                }

                break;

            case EnemyState.AttackBlock:

                // Transition to chase or attack player if no block in path
                if (!enemyAI.BlockInPath)
                {
                    // If player in range, attack player
                    if (distanceToPlayer <= enemyAI.PlayerAttackDistance)
                    {
                        SetState(EnemyState.AttackPlayer);
                        return;
                    }

                    // Otherwise, chase player
                    SetState(EnemyState.Chase);
                    return;
                }

                break;

            case EnemyState.AttackPlayer:
                // Transition to AttackBlock if block detected in path
                if (enemyAI.BlockInPath)
                {
                    enemyAI.AttackCooldownRemaining = enemyAI.BlockAttackCooldown; // give attack buffer when entering block attack state
                    SetState(EnemyState.AttackBlock);
                    return;
                }

                // Transition to Chase if player out of attack range
                if (distanceToPlayer > enemyAI.PlayerAttackDistance * 1.5f)
                {
                    SetState(EnemyState.Chase);
                    return;
                }
                break;
        }
    }

    /// <summary>
    ///  Set the current enemy state
    /// </summary>
    /// <param name="newState"></param>
    public void SetState(EnemyState newState)
    {
        // Avoid redundant state changes
        if (currentState == newState) return;

        // Update states
        previousState = currentState;
        currentState = newState;

        // Update the EnemyController's state
        enemyAI.EnemyState = currentState;
    }

    /// <summary>
    /// This method chooses and performs an action based on the current state
    /// </summary>
    public void ChooseAction()
    {   
        switch (enemyAI.EnemyState)
        {
            case EnemyState.Chase:
                // Movement handled in EnemyMovement module
                break;
            case EnemyState.AttackBlock: // Currently the same as AttackPlayer, will be different if we add animations and more block types
                PerformAttack();
                break;
            case EnemyState.AttackPlayer: // Currently the same as AttackBlock, will be different if we add animations
                PerformAttack();
                break;
        }
    }

    /// <summary>
    /// This method performs an attack based on the current state
    /// </summary>
    public void PerformAttack()
    {
        // Check if can attack
        if (!enemyAI.CanAttack) return;

        float attackCooldown = 0f;

        // Perform attack based on state
        if (currentState == EnemyState.AttackPlayer) // attack player
        {
            GameObject player = Controller_Game.Instance.GetPlayer();
            if (player != null)
            {
                // Apply damage to player
                //Debug.Log("Zombie attacks player!");
                player.GetComponent<Controller_Player>()?.TakeDamage(Mathf.RoundToInt(enemyAI.PlayerAttackDamage));
            }
            attackCooldown = enemyAI.PlayerAttackCooldown;
        }
        else if (currentState == EnemyState.AttackBlock) // attack block
        {
            // break block in front of enemy
            //Debug.Log("Zombie attacks block!");
            enemyAI.AttackBlock();
            attackCooldown = enemyAI.BlockAttackCooldown;
        }

        enemyAI.AttackFlash.TriggerAttackFlash(); // Trigger attack flash effect

        // Set attack cooldown
        enemyAI.AttackCooldownRemaining = attackCooldown;
    }
}
