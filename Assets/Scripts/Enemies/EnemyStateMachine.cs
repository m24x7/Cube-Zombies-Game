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

    /// <summary>
    /// Update is called once per frame
    /// </summary>
    void Update()
    {

    }

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
                if (distanceToPlayer < enemyAI.AttackDistance)
                {
                    SetState(EnemyState.AttackPlayer);
                }
                break;

            case EnemyState.AttackBlock:
                //if (distanceToPlayer > chaseDistance)
                //{
                //    SetState(EnemyState.Chase);
                //}
                break;

            case EnemyState.AttackPlayer:
                if (distanceToPlayer > enemyAI.AttackDistance * 1.5f)
                {
                    SetState(EnemyState.Chase);
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

        GameObject player = Controller_Game.Instance?.GetPlayer();
        if (player != null)
        {
            // Apply damage to player
            Debug.Log("Zombie attacks player!");
            player.GetComponent<Controller_Player>()?.TakeDamage(Mathf.RoundToInt(enemyAI.AttackDamage));
        }

        enemyAI.AttackCooldownRemaining = enemyAI.AttackCooldown;
    }
}
