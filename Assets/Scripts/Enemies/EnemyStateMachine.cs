using UnityEngine;


public class EnemyStateMachine : MonoBehaviour
{
    public enum EnemyState
    {
        Search,
        Chase,
        Attack
    }

    [SerializeField] private EnemyState currentState = EnemyState.Search;

    [SerializeField] private float chaseDistance = 15f;
    public float GetChaseDistance => chaseDistance;

    [SerializeField] private float attackDistance = 1.5f;
    public float GetAttackDistance => attackDistance;

    [SerializeField] private float attackCooldown = 1.5f;
    public float GetAttackCooldown => attackCooldown;

    [SerializeField] private float attackDamage = 5f;
    public float GetAttackDamage => attackDamage;

    private EnemyState previousState;
    private float attackCooldownRemaining = 0f;
    private I_EnemyAgent enemyAI;

    //    // Start is called once before the first execution of Update after the MonoBehaviour is created
    //    void Start()
    //    {
    //        enemyAI = GetComponent<I_EnemyAgent>();
    //    }

    //    // Update is called once per frame
    //    void Update()
    //    {
    //        attackCooldownRemaining -= Time.deltaTime;
    //        UpdateState();
    //    }
    //    public void UpdateState()
    //    {
    //        Transform playerTransform = Controller_Game.Instance?.GetPlayer().transform;
    //        if (playerTransform == null) return;

    //        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

    //        switch (currentState)
    //        {
    //            case EnemyState.Search:
    //                if (distanceToPlayer < chaseDistance)
    //                {
    //                    SetState(EnemyState.Chase);
    //                }
    //                break;

    //            case EnemyState.Chase:
    //                if (distanceToPlayer > chaseDistance * 1.5f)
    //                {
    //                    SetState(EnemyState.Search);
    //                }
    //                else if (distanceToPlayer < attackDistance)
    //                {
    //                    SetState(EnemyState.Attack);
    //                }
    //                break;

    //            case EnemyState.Attack:
    //                if (distanceToPlayer > attackDistance * 1.5f)
    //                {
    //                    SetState(EnemyState.Chase);
    //                }
    //                break;
    //        }
    //    }

    //    public void SetState(EnemyState newState)
    //    {
    //        if (currentState == newState) return;

    //        previousState = currentState;
    //        currentState = newState;

    //        if (enemyAI != null)
    //        {
    //            enemyAI.OnStateChange(newState);
    //        }
    //    }

    //    public EnemyState GetState() => currentState;

    //    public EnemyState GetPreviousState() => previousState;

    //    public bool CanAttack()
    //    {
    //        return attackCooldownRemaining <= 0;
    //    }

    //    public void PerformAttack()
    //    {
    //        if (!CanAttack()) return;

    //        Transform playerTransform = Controller_Game.Instance?.GetPlayer().transform;
    //        if (playerTransform != null)
    //        {
    //            // Apply damage to player
    //            Controller_Player player = playerTransform.GetComponent<Controller_Player>();
    //            if (player != null)
    //            {
    //                // You can extend PlayerController to have a TakeDamage method
    //                Debug.Log("Zombie attacks player!");
    //            }
    //        }

    //        attackCooldownRemaining = attackCooldown;
    //    }
}
