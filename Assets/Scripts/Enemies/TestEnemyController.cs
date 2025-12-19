using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class TestEnemyController : Parent_Entity, I_EnemyAgent
{
    #region Modules
    [SerializeField] private EnemyMovement Movement;
    public EnemyMovement GetMovement => Movement;
    [SerializeField] private EnemyPerception Perception;
    public EnemyPerception GetPerception => Perception;
    [SerializeField] private EnemyDecisionMaking Decision;
    public EnemyDecisionMaking GetDecision => Decision;
    [SerializeField] private EnemyActions Actions;
    public EnemyActions GetActions => Actions;
    #endregion

    public static event Action<TestEnemyController> OnEntityDeath;

    [Header("Runtime values (initialized by spawner)")]
    public EnemyDefinition definition;
    public int reward;

    private NavMeshAgent agent;

    //[Header("Hit Flash")]
    [SerializeField] private HitFlash hitFlash;


    #region Sounds
    private float randSoundTimer = 0;
    private string[] sounds = { "Sounds/Creature 1-21", "Sounds/Zombie 1 - Short 1-01" };
    private string[] hurtSounds = { "Sounds/Hit Generic 2-1" };
    #endregion

    void Awake()
    {
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

    private void FixedUpdate()
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

    public override void TakeDamage(int damage)
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
}
