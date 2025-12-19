using System.IO;
using Unity.VisualScripting;
using UnityEngine;

public enum EnemyState
    {
        Chase,
        AttackBlock,
        AttackPlayer
    }
public class EnemyStateMachine : MonoBehaviour
{
    #region Enemy States
    [SerializeField] private EnemyState currentState = EnemyState.Chase;
    public EnemyState GetState => currentState;

    private EnemyState previousState;
    public EnemyState GetPreviousState => previousState;
    #endregion

    private EnemyController enemyAI;

    /// <summary>
    /// Start is called once before the first execution of Update after the MonoBehaviour is created
    /// </summary>
    void Start()
    {
        enemyAI = GetComponent<EnemyController>();
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
        Transform playerTransform = Controller_Game.Instance.GetPlayer().transform;
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        switch (currentState)
        {
            case EnemyState.Chase:
                // Only attack blocks if they are in path AND breaking makes sense
                if (enemyAI.BlockInPath) //&& enemyAI.PrefersBreakingPath)
                {
                    SetState(EnemyState.AttackBlock);
                    return;
                }

                if (distanceToPlayer <= enemyAI.PlayerAttackDistance)
                {
                    SetState(EnemyState.AttackPlayer);
                    return;
                }

                break;

            case EnemyState.AttackBlock:

                // Transition to chase or attack player if no block in path
                if (!enemyAI.BlockInPath) //|| !enemyAI.PrefersBreakingPath)
                {
                    if (distanceToPlayer <= enemyAI.PlayerAttackDistance)
                    {
                        SetState(EnemyState.AttackPlayer);
                        return;
                    }

                    SetState(EnemyState.Chase);
                    return;
                }

                break;

            case EnemyState.AttackPlayer:
                // Transition to AttackBlock if block detected in path
                if (enemyAI.BlockInPath) //&& enemyAI.PrefersBreakingPath)
                {
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
        if (currentState == newState) return;

        previousState = currentState;
        currentState = newState;

        enemyAI.EnemyState = currentState;
    }

    public void ChooseAction()
    {   
        switch (enemyAI.EnemyState)
        {
            case EnemyState.Chase:
                // Movement handled in EnemyMovement module
                break;
            case EnemyState.AttackBlock: // Currently the same as AttackPlayer, will be different if we add animations
                PerformAttack();
                break;
            case EnemyState.AttackPlayer:
                PerformAttack();
                break;
        }
    }

    public void PerformAttack()
    {
        if (!enemyAI.CanAttack) return;

        float attackCooldown = 0f;

        if (currentState == EnemyState.AttackPlayer)
        {
            GameObject player = Controller_Game.Instance.GetPlayer();
            if (player != null)
            {
                // Apply damage to player
                Debug.Log("Zombie attacks player!");
                player.GetComponent<Controller_Player>()?.TakeDamage(Mathf.RoundToInt(enemyAI.AttackDamage));
            }
            attackCooldown = enemyAI.PlayerAttackCooldown;
        }
        else if (currentState == EnemyState.AttackBlock)
        {
            // break block in front
            Debug.Log("Zombie attacks block!");
            enemyAI.AttackBlock();
            attackCooldown = enemyAI.BlockAttackCooldown;
        }
        enemyAI.AttackCooldownRemaining = attackCooldown;
    }
}
