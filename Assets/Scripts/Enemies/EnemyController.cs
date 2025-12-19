using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : Parent_Entity
{
    #region Modules

    [SerializeField] private EnemyStateMachine stateMachine;
    public EnemyStateMachine StateMachine => stateMachine;

    [SerializeField] private EnemyMovement movement;
    public EnemyMovement Movement => movement;

    //[SerializeField] private EnemyDecisionMaking Decision;
    //public EnemyDecisionMaking GetDecision => Decision;
    //[SerializeField] private EnemyActions Actions;
    //public EnemyActions GetActions => Actions;
    #endregion

    //[SerializeField] private float attackDistance = 1.5f;
    //public float AttackDistance => attackDistance;

    //[SerializeField] private float attackCooldown = 1.5f;
    //public float AttackCooldown => attackCooldown;


    [SerializeField] private float attackDamage = 5f;
    public float AttackDamage => attackDamage;

    #region Settings
    [Header("Perception Settings")]
    [SerializeField] private float attackRangeStart = 2f;
    public float AttackRangeStart => attackRangeStart;
    [SerializeField] private float rayPerceptionRange = 2f;
    public float RayPerceptionRange => rayPerceptionRange;

    [Header("AttackBlocks Settings")]
    [SerializeField] private float blockDetectionRange = 1.5f;
    public float BlockDetectionRange => blockDetectionRange;
    [SerializeField] private float blockAttackDamage = 3f;
    public float BlockAttackDamage => blockAttackDamage;
    [SerializeField] private float blockAttackCooldown = 1f;
    public float BlockAttackCooldown => blockAttackCooldown;

    [Header("AttackPlayer Settings")]
    [SerializeField] private float playerAttackRange = 1.5f;
    public float PlayerAttackDistance => playerAttackRange;
    [SerializeField] private float playerAttackDamage = 5f;
    public float PlayerAttackDamage => playerAttackDamage;
    [SerializeField] private float playerAttackCooldown = 1.5f;
    public float PlayerAttackCooldown => playerAttackCooldown;
    #endregion

    private float attackCooldownRemaining = 0f;
    public float AttackCooldownRemaining
    {
        get => attackCooldownRemaining;
        set => attackCooldownRemaining = value;
    }

    #region Perception
    [SerializeField] private EnemyState enemyState;
    public EnemyState EnemyState
    {
        get => enemyState;
        set => enemyState = value;
    }

    [SerializeField] private Transform target;
    public Transform Target
    {
        get => target;
        set => target = value;
    }
    public void SetTarget(Transform t) => Target = t;

    [SerializeField] private bool blockInPath = false;
    public bool BlockInPath
    {
        get => blockInPath;
        private set => blockInPath = value;
    }
    [SerializeField] private float maxDetourFactor = 3f; // how much longer a detour may be vs straight line
    [SerializeField] private bool prefersBreakingPath = false;
    public bool PrefersBreakingPath => prefersBreakingPath;

    public bool CanAttack => attackCooldownRemaining <= 0;
    #endregion

    public static event Action<EnemyController> OnEntityDeath;

    [Header("Runtime values (initialized by spawner)")]
    public EnemyDefinition definition;
    public int reward;

    private NavMeshAgent agent;
    public NavMeshAgent Agent => agent;

    //[Header("Hit Flash")]
    [SerializeField] private HitFlash hitFlash;


    #region Sounds
    private float randSoundTimer = 0;
    private string[] sounds = { "Sounds/Creature 1-21", "Sounds/Zombie 1 - Short 1-01" };
    private string[] hurtSounds = { "Sounds/Hit Generic 2-1" };
    #endregion

    void Awake()
    {
        if (stateMachine == null) stateMachine = GetComponent<EnemyStateMachine>();
        if (movement == null) movement = GetComponent<EnemyMovement>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (hitFlash == null) hitFlash = GetComponent<HitFlash>();

        hitFlash.BuildRendererCache();
    }

    // Called by WaveDirector after Instantiate
    public void Initialize(EnemyDefinition def, float healthMult, float speedMult)
    {
        definition = def;
        Health.Init(Mathf.RoundToInt(def.baseHealth * Mathf.Max(0.1f, healthMult)));
        reward = def.killReward;

        if (agent) agent.speed = def.baseSpeed * Mathf.Max(0.1f, speedMult);

        randSoundTimer = UnityEngine.Random.Range(0, 10);
    }

    private void Update()
    {
        attackCooldownRemaining -= Time.deltaTime;

        // Update perception
        BlockInPath = IsBlockInPath();
        //ShouldBreakBlocksToReachPlayer();

        // Update State
        StateMachine.UpdateState();

        // Choose Action
        StateMachine.ChooseAction();
    }

    private void FixedUpdate()
    {
        // Play ambient sounds
        AmbientSounds();
    }

    #region HP Methods
    public override void TakeDamage(int damage, bool ignoreInvincibility = false)
    {
        base.TakeDamage(damage);
        hitFlash.TriggerHitFlash();
        Debug.Log("TestEnemyController: Took " + damage + " damage. Current health: " + Health.Cur);

        // Play hurt sound
        AudioSource.PlayClipAtPoint(
            Resources.Load<AudioClip>(hurtSounds[UnityEngine.Random.Range(0, hurtSounds.Length)]),
            transform.position,
            0.4f
        );
    }

    protected override void Die()
    {
        Debug.Log("TestEnemyController: Died.");
        OnEntityDeath?.Invoke(this);
        Destroy(gameObject);
    }
    #endregion

    /// <summary>
    /// This method plays random ambient sounds at intervals.
    /// </summary>
    private void AmbientSounds()
    {
        if (randSoundTimer > 0)
        {
            randSoundTimer -= Time.fixedDeltaTime;
        }
        else
        {
            AudioSource.PlayClipAtPoint(
                Resources.Load<AudioClip>(sounds[UnityEngine.Random.Range(0, sounds.Length)]),
                transform.position,
                0.2f
            );
            randSoundTimer = UnityEngine.Random.Range(10, 30);
        }
    }

    #region Perception
    /// <summary>
    /// This method checks if there is a block in front of the enemy within blockDetectionRange.
    /// </summary>
    /// <returns></returns>
    private bool IsBlockInPath()
    {
        // Raycast at two heights to detect blocks in front of enemy
        Vector3 r1 = transform.position;
        Debug.Log("EnemyController: checking waist height block");
        if (Physics.Raycast(r1, transform.forward, out RaycastHit hit, blockDetectionRange, LayerMask.NameToLayer("Block")))
        {
            //Debug.Log("EnemyController: detected waist height block");
            return true;
        }

        Vector3 r2 = transform.position + transform.up;
        Debug.Log("EnemyController: checking head height block");
        if (Physics.Raycast(r2, transform.forward, out hit, blockDetectionRange, LayerMask.NameToLayer("Block")))
        {
            //Debug.Log("EnemyController: detected head height block");
            return true;
        }

        // No blocks detected
        return false;
    }

    ///// <summary>
    ///// Tells whether the enemy should break blocks to reach the player,
    ///// </summary>
    ///// <returns></returns>
    //public bool ShouldBreakBlocksToReachPlayer()
    //{
    //    var player = Controller_Game.Instance.GetPlayer();
    //    if (player == null || agent == null) return false;

    //    // Straight‑line distance
    //    float directDistance = Vector3.Distance(transform.position, player.transform.position);

    //    // 1) Try to get a navmesh path
    //    NavMeshPath path = new NavMeshPath();
    //    bool hasPath = agent.CalculatePath(player.transform.position, path);

    //    float walkCost = Mathf.Infinity;
    //    if (hasPath && path.status == NavMeshPathStatus.PathComplete)
    //    {
    //        float length = 0f;
    //        for (int i = 1; i < path.corners.Length; i++) length += Vector3.Distance(path.corners[i - 1], path.corners[i]);

    //        // Treat path length as “time” by dividing by agent speed
    //        float speed = Mathf.Max(0.01f, agent.speed);
    //        walkCost = length / speed;

    //        // if detour is ridiculously long vs straight line, treat as invalid
    //        if (length > directDistance * maxDetourFactor) walkCost = Mathf.Infinity;
    //    }

    //    // 2) Estimate block‑breaking cost
    //    int hitsPerBlock = 5; // currently hardcoded as we only have 1 block type
    //    float breakCost = hitsPerBlock * BlockAttackCooldown;

    //    bool shouldBreak = breakCost < walkCost || !hasPath || path.status != NavMeshPathStatus.PathComplete;

    //    prefersBreakingPath = shouldBreak;
    //    return shouldBreak;
    //}
    #endregion

    #region Actions
    /// <summary>
    /// Attack the block in front of the enemy, if any.
    /// First it tries to damage the block at head height, then at waist height.
    /// </summary>
    public void AttackBlock()
    {
        // Raycast at head height to detect blocks in front of enemy
        Vector3 r1 = transform.position + transform.up;
        if (Physics.Raycast(r1, transform.forward, out RaycastHit hit, blockDetectionRange))
        {
            // If we hit a block, deal damage to it
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Blocks"))
            {
                //Debug.Log("EnemyController: Attacking head height block " + hit.collider.gameObject.name);
                Parent_Block block = hit.collider.GetComponent<Parent_Block>();
                if (block != null)
                {
                    block.TakeDamage(1);
                    attackCooldownRemaining = BlockAttackCooldown;
                    return;
                }
            }
        }

        // Raycast at waist height to detect blocks in front of enemy
        Vector3 r2 = transform.position;
        if (Physics.Raycast(r2, transform.forward, out hit, blockDetectionRange))
        {
            // If we hit a block, deal damage to it
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Blocks"))
            {
                //Debug.Log("EnemyController: Attacking waist height block " + hit.collider.gameObject.name);
                Parent_Block block = hit.collider.GetComponent<Parent_Block>();
                if (block != null)
                {
                    block.TakeDamage(1);
                    attackCooldownRemaining = BlockAttackCooldown;
                    return;
                }
            }
        }
    }
    #endregion
}
