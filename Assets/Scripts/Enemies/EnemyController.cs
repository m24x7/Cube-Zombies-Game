using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : Parent_Entity, I_EnemyAgent
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

        // Update State
        StateMachine.UpdateState();
    }

    private void FixedUpdate()
    {
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
    private bool IsBlockInPath()
    {
        // Raycast at two heights to detect blocks in front of enemy
        Vector3 r1 = transform.position;
        if (Physics.Raycast(r1, transform.forward, out RaycastHit hit, blockDetectionRange))
        {
            return hit.collider.CompareTag("Block");
        }
        Vector3 r2 = transform.position + transform.up;
        if (Physics.Raycast(r2, transform.forward, out hit, blockDetectionRange))
        {
            return hit.collider.CompareTag("Block");
        }

        // No blocks detected
        return false;
    }
    #endregion

    #region Actions
    public void AttackBlock()
    {
               Debug.Log("EnemyController: Attacking Block for " + BlockAttackDamage + " damage.");
        attackCooldownRemaining = BlockAttackCooldown;
        // Raycast to find block in front
        Vector3 rayOrigin = transform.position + transform.up * 0.5f;
        if (Physics.Raycast(rayOrigin, transform.forward, out RaycastHit hit, BlockDetectionRange))
        {
            if (hit.collider.CompareTag("Block"))
            {
                Parent_Block block = hit.collider.GetComponent<Parent_Block>();
                if (block != null)
                {
                    block.Break();
                }
            }
        }
    }
    #endregion
}
