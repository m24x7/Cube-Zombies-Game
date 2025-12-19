using UnityEngine;


public class EnemyStateMachine : MonoBehaviour
{
    #region Enemy States
    public enum EnemyState
    {
        Search,
        Chase,
        Attack
    }

    [SerializeField] private EnemyState currentState = EnemyState.Search;
    public EnemyState GetState => currentState;

    private EnemyState previousState;
    public EnemyState GetPreviousState => previousState;
    #endregion

    #region Parameters

    [SerializeField] private float chaseDistance = 15f;
    public float GetChaseDistance => chaseDistance;

    [SerializeField] private float attackDistance = 1.5f;
    public float GetAttackDistance => attackDistance;

    [SerializeField] private float attackCooldown = 1.5f;
    public float GetAttackCooldown => attackCooldown;
    public bool CanAttack => attackCooldownRemaining <= 0;

    [SerializeField] private float attackDamage = 5f;
    public float GetAttackDamage => attackDamage;

    private float attackCooldownRemaining = 0f;
    #endregion

    private I_EnemyAgent enemyAI;

    /// <summary>
    /// Start is called once before the first execution of Update after the MonoBehaviour is created
    /// </summary>
    void Start()
    {
        enemyAI = GetComponent<I_EnemyAgent>();
    }

    /// <summary>
    /// Update is called once per frame
    /// </summary>
    void Update()
    {
        attackCooldownRemaining -= Time.deltaTime;
        UpdateState();
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
            case EnemyState.Search:
                if (distanceToPlayer < chaseDistance)
                {
                    SetState(EnemyState.Chase);
                }
                break;

            case EnemyState.Chase:
                if (distanceToPlayer > chaseDistance * 1.5f)
                {
                    SetState(EnemyState.Search);
                }
                else if (distanceToPlayer < attackDistance)
                {
                    SetState(EnemyState.Attack);
                }
                break;

            case EnemyState.Attack:
                if (distanceToPlayer > attackDistance * 1.5f)
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
        if (!CanAttack) return;

        GameObject player = Controller_Game.Instance?.GetPlayer();
        if (player != null)
        {
            // Apply damage to player
            Debug.Log("Zombie attacks player!");
            player.GetComponent<Controller_Player>()?.TakeDamage(Mathf.RoundToInt(attackDamage));
        }

        attackCooldownRemaining = attackCooldown;
    }
}
