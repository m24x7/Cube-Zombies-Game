using UnityEngine;


public class EnemyStateMachine : MonoBehaviour
{
    #region Enemy States
    public enum EnemyState
    {
        Chase,
        AttackBlock,
        AttackPlayer
    }

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
        Transform playerTransform = Controller_Game.Instance?.GetPlayer().transform;
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        switch (currentState)
        {
            case EnemyState.Chase:

                // Transition to AttackBlock or AttackPlayer if in range
                if (distanceToPlayer <= enemyAI.AttackRangeStart)
                {
                    // Prioritize attacking block if in path
                    if (enemyAI.BlockInPath)
                    {
                        SetState(EnemyState.AttackBlock);
                        return;
                    }
                    SetState(EnemyState.AttackPlayer);
                    return;
                }

                break;

            case EnemyState.AttackBlock:

                // Transition to Chase or AttackPlayer if no more blocks in the path
                if (!enemyAI.BlockInPath)
                {
                    // Prioritize attacking player if in range
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
                if (enemyAI.BlockInPath)
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

        //if (enemyAI != null)
        //{
        //    enemyAI.OnStateChange(newState);
        //}
    }

    public void PerformAttack()
    {
        if (!enemyAI.CanAttack) return;

        float attackCooldown = 0f;

        if (currentState == EnemyState.AttackPlayer)
        {
            GameObject player = Controller_Game.Instance?.GetPlayer();
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
            // Attack block logic here
            Debug.Log("Zombie attacks block!");
            attackCooldown = enemyAI.BlockAttackCooldown;
        }


        enemyAI.AttackCooldownRemaining = attackCooldown;
    }
}
