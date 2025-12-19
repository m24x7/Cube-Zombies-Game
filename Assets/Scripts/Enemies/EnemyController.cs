using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// This class controls the behavior of an enemy entity
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : Parent_Entity
{
    #region Modules

    [SerializeField] private EnemyStateMachine stateMachine; // state machine module
    public EnemyStateMachine StateMachine => stateMachine; // public getter

    [SerializeField] private EnemyMovement movement; // movement module
    public EnemyMovement Movement => movement; // public getter

    //[SerializeField] private EnemyDecisionMaking Decision;
    //public EnemyDecisionMaking GetDecision => Decision;
    //[SerializeField] private EnemyActions Actions;
    //public EnemyActions GetActions => Actions;
    #endregion

    #region Settings
    [Header("Perception Settings")]
    [SerializeField] private float attackRangeStart = 2f; // distance to start attack behaviors
    public float AttackRangeStart => attackRangeStart; // public getter


    [Header("AttackBlocks Settings")]
    [SerializeField] private float blockDetectionRange = 1.5f; // distance to detect blocks in front of enemy
    public float BlockDetectionRange => blockDetectionRange; // public getter

    [SerializeField] private int blockAttackDamage = 1; // damage dealt to blocks
    public int BlockAttackDamage => blockAttackDamage; // public getter

    [SerializeField] private float blockAttackCooldown = 20f; // cooldown between block attacks
    public float BlockAttackCooldown => blockAttackCooldown; // public getter


    [Header("AttackPlayer Settings")]
    [SerializeField] private float playerAttackRange = 1.5f; // distance to attack player
    public float PlayerAttackDistance => playerAttackRange; // public getter

    [SerializeField] private float playerAttackDamage = 10f; // damage dealt to player
    public float PlayerAttackDamage => playerAttackDamage; // public getter

    [SerializeField] private float playerAttackCooldown = 30f; // cooldown between player attacks
    public float PlayerAttackCooldown => playerAttackCooldown; // public getter
    #endregion

    private float attackCooldownRemaining = 0f; // time remaining until next attack
    public float AttackCooldownRemaining // public getter and setter
    {
        get => attackCooldownRemaining;
        set => attackCooldownRemaining = value;
    }

    #region Perception
    [Header("Perception Values")]
    [SerializeField] private EnemyState enemyState; // current state of the enemy
    public EnemyState EnemyState // public getter and setter
    {
        get => enemyState;
        set => enemyState = value;
    }

    [SerializeField] private Transform target; // usually the player
    public Transform Target // public getter and setter
    {
        get => target;
        set => target = value;
    }
    public void SetTarget(Transform t) => Target = t; // public method to set target

    [SerializeField] private bool blockInPath = false; // whether there is a block in front of the enemy
    public bool BlockInPath // public getter and private setter
    {
        get => blockInPath;
        private set => blockInPath = value;
    }

    //[SerializeField] private float maxDetourFactor = 3f; // how much longer a detour may be vs straight line
    //[SerializeField] private bool prefersBreakingPath = false;
    //public bool PrefersBreakingPath => prefersBreakingPath;

    public bool CanAttack => attackCooldownRemaining <= 0; // whether the enemy can attack
    #endregion

    public static event Action<EnemyController> OnEntityDeath; // Notify when enemy dies

    [Header("Runtime values (initialized by spawner)")]
    public EnemyDefinition definition; // definition used to initialize this enemy
    public int reward; // reward given to player on death

    private NavMeshAgent agent; // reference to NavMeshAgent component
    public NavMeshAgent Agent => agent; // public getter

    [Header("Flash Effects")]
    [SerializeField] private HitFlash hitFlash; // reference to HitFlash component
    [SerializeField] private AttackFlash attackFlash; // reference to AttackFlash component
    public AttackFlash AttackFlash => attackFlash; // public getter


    #region Sounds
    private float randSoundTimer = 0; // timer for random ambient sounds
    private string[] sounds = { "Sounds/Creature 1-21", "Sounds/Zombie 1 - Short 1-01" }; // ambient sound clips
    private string[] hurtSounds = { "Sounds/Hit Generic 2-1" }; // hurt sound clips
    #endregion

    /// <summary>
    /// Awake is called when the script instance is being loaded
    /// </summary>
    void Awake()
    {
        // Get required components
        if (stateMachine == null) stateMachine = GetComponent<EnemyStateMachine>(); // state machine
        if (movement == null) movement = GetComponent<EnemyMovement>(); // movement module
        if (agent == null) agent = GetComponent<NavMeshAgent>(); // nav mesh agent
        if (hitFlash == null) hitFlash = GetComponent<HitFlash>(); // hit flash
        if (attackFlash == null) attackFlash = GetComponent<AttackFlash>(); // attack flash

        // Initialize Flash Effects
        hitFlash.BuildRendererCache();
        attackFlash.BuildRendererCache();
    }

    /// <summary>
    /// This method is called by WaveDirector after Instantiate
    /// </summary>
    /// <param name="def"></param>
    /// <param name="healthMult"></param>
    /// <param name="speedMult"></param>
    public void Initialize(EnemyDefinition def, float healthMult, float speedMult)
    {
        // Set definition and initialize stats
        definition = def;
        Health.Init(Mathf.RoundToInt(def.baseHealth * Mathf.Max(0.1f, healthMult)));
        reward = def.killReward;

        // Set movement speed
        if (agent) agent.speed = def.baseSpeed * Mathf.Max(0.1f, speedMult);

        // Initialize random sound timer
        randSoundTimer = UnityEngine.Random.Range(0, 10);
    }

    /// <summary>
    /// This method is called once per frame
    /// </summary>
    private void Update()
    {
        // Update attack cooldown
        attackCooldownRemaining -= Time.deltaTime;

        // Update perception
        BlockInPath = IsBlockInPath();

        // Update State
        StateMachine.UpdateState();

        // Choose Action
        StateMachine.ChooseAction();
    }

    /// <summary>
    /// FixedUpdate is called at a fixed interval and is independent of frame rate
    /// </summary>
    private void FixedUpdate()
    {
        AmbientSounds(); // Play ambient sounds
    }

    #region HP Methods
    /// <summary>
    /// This method applies damage to the enemy.
    /// </summary>
    /// <param name="damage"></param>
    /// <param name="ignoreInvincibility"></param>
    public override void TakeDamage(int damage, bool ignoreInvincibility = false)
    {
        // Apply damage using base method
        base.TakeDamage(damage);

        // Trigger hit flash
        hitFlash.TriggerHitFlash();

        Debug.Log("TestEnemyController: Took " + damage + " damage. Current health: " + Health.Cur);

        // Play hurt sound
        AudioSource.PlayClipAtPoint(
            Resources.Load<AudioClip>(hurtSounds[UnityEngine.Random.Range(0, hurtSounds.Length)]),
            transform.position,
            0.4f
        );
    }

    /// <summary>
    /// This method handles the enemy's death.
    /// </summary>
    protected override void Die()
    {
        Debug.Log("TestEnemyController: Died.");
        OnEntityDeath?.Invoke(this); // Notify subscribers of death
        Destroy(gameObject); // Destroy enemy game object
    }
    #endregion

    /// <summary>
    /// This method plays random ambient sounds at intervals.
    /// </summary>
    private void AmbientSounds()
    {
        // Update timer
        if (randSoundTimer > 0)
        {
            randSoundTimer -= Time.fixedDeltaTime;
        }
        else // Play random sound
        {
            AudioSource.PlayClipAtPoint(
                Resources.Load<AudioClip>(sounds[UnityEngine.Random.Range(0, sounds.Length)]),
                transform.position,
                0.2f
            );
            randSoundTimer = UnityEngine.Random.Range(10, 30); // Reset timer
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
        if (Physics.Raycast(r1, transform.forward, out RaycastHit hit, blockDetectionRange, LayerMask.GetMask("Blocks")))
        {
            Debug.Log("EnemyController: detected waist height block");
            return true;
        }

        Vector3 r2 = transform.position + transform.up;
        Debug.Log("EnemyController: checking head height block");
        if (Physics.Raycast(r2, transform.forward, out hit, blockDetectionRange, LayerMask.GetMask("Blocks")))
        {
            Debug.Log("EnemyController: detected head height block");
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
        if (Physics.Raycast(r1, transform.forward, out RaycastHit hit, blockDetectionRange, LayerMask.GetMask("Blocks")))
        {
            // If we hit a block, deal damage to it
            //if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Block"))
            {
                Debug.Log("EnemyController: Attacking head height block " + hit.collider.gameObject.name);
                Parent_Block block = hit.collider.GetComponent<Parent_Block>();
                if (block != null)
                {
                    block.TakeDamage(BlockAttackDamage); // Deal damage to block
                    attackCooldownRemaining = BlockAttackCooldown; // Set attack cooldown
                    return;
                }
            }
        }

        // Raycast at waist height to detect blocks in front of enemy
        Vector3 r2 = transform.position;
        if (Physics.Raycast(r2, transform.forward, out hit, blockDetectionRange, LayerMask.GetMask("Blocks")))
        {
            // If we hit a block, deal damage to it
            //if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Block"))
            {
                Debug.Log("EnemyController: Attacking waist height block " + hit.collider.gameObject.name);
                Parent_Block block = hit.collider.GetComponent<Parent_Block>();
                if (block != null)
                {
                    block.TakeDamage(BlockAttackDamage); // Deal damage to block
                    attackCooldownRemaining = BlockAttackCooldown; // Set attack cooldown
                    return;
                }
            }
        }
    }
    #endregion
}
