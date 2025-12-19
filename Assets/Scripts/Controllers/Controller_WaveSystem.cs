using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Controller_WaveSystem : MonoBehaviour
{
    // Make it a singleton for easy access
    public static Controller_WaveSystem Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private Camera playerCamera; // for EnemyChasePlayer
    [SerializeField] private Transform player; // for EnemyChasePlayer
    //[SerializeField] private BuildCurrency currency; // your existing bank
    //[SerializeField] private BuildPhaseManager phase; // your existing phase gate
    [SerializeField] private UI_Manager uiManager; // for updating wave/score UI
    [SerializeField] private Controller_QuestUI questUI; // for completing quest objective

    [Header("Spawn Points")]
    [SerializeField] private List<SpawnPoint> spawnPoints = new(); // list of spawn points

    [Header("Waves")]
    [SerializeField] private List<WaveDefinition> waveSequence = new(); // list of author-defined waves

    [Tooltip("If true, when you run out of author-defined waves, waves keep scaling procedurally.")]
    [SerializeField] private bool endlessBeyondLastDefined = true; // endless mode

    [Header("Procedural Fallback (kicks in after last defined wave)")]
    [SerializeField] private EnemyDefinition fallbackEnemy; // enemy type to spawn in fallback waves
    [SerializeField, Min(1)] private int fallbackStartCount = 10; // starting count for fallback waves
    [SerializeField, Min(0)] private int fallbackAddPerWave = 4; // additional count per wave
    [SerializeField, Min(0.1f)] private float fallbackSpawnInterval = 0.8f; // starting spawn interval
    [SerializeField, Min(1)] private int fallbackAliveCap = 20; // starting alive cap
    [SerializeField] private float healthPerWave = 1.25f; // 25% tougher per wave
    [SerializeField] private float speedPerWave = 1.02f; // +2% speed per wave
    [SerializeField] private float minSpawnInterval = 0.25f; // minimum spawn interval
    [SerializeField] private float intermissionSeconds = 18f; // intermission time for fallback waves

    public Action noMoreWaves; // event for no more waves

    public int TotalPossiblePoints { get => CalcTotalPosiblePoints(); } // total possible points from all waves

    // Runtime
    public int CurrentWaveNumber { get; private set; } = 0; // 1-based
    public int TotalWavesDefined => waveSequence.Count; // total defined waves
    public int WavesRemaining => Mathf.Max(0, waveSequence.Count - CurrentWaveNumber); // waves left
    public int EnemiesAlive { get; private set; } = 0; // current alive enemies
    public int EnemiesToSpawnThisWave { get; private set; } = 0; // total to spawn this wave
    public int EnemiesSpawnedThisWave { get; private set; } = 0; // spawned so far this wave
    public bool IsSpawning { get; private set; } = false; // whether currently spawning
    
    private Coroutine waveRoutine; // current wave coroutine

    /// <summary>
    /// Awake is called when the script instance is being loaded
    /// </summary>
    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
        }
    }

    /// <summary>
    /// OnEnable is called when the object becomes enabled and active
    /// </summary>
    void OnEnable()
    {
        // Subscribe to enemy death event
        EnemyController.OnEntityDeath += HandleEnemyKilled;
    }

    /// <summary>
    /// OnDisable is called when the behaviour becomes disabled or inactive
    /// </summary>
    void OnDisable()
    {
        // Unsubscribe from enemy death event
        EnemyController.OnEntityDeath -= HandleEnemyKilled;
    }

    /// <summary>
    /// Start is called once before the first execution of Update after the MonoBehaviour is created
    /// </summary>
    void Start()
    {
        //if (!currency) currency = FindFirstObjectByType<BuildCurrency>();
        //if (!phase) phase = FindFirstObjectByType<BuildPhaseManager>();

        if (!player && playerCamera) player = playerCamera.transform; // fallback to camera transform
        StartCoroutine(StartFirstWaveWithShortBuild()); // start first wave after short build phase
    }

    /// <summary>
    /// This starts the first wave after a short build phase
    /// </summary>
    /// <returns></returns>
    IEnumerator StartFirstWaveWithShortBuild()
    {
        //Debug.Log("Wave System: Starting first wave after short build phase.");
        // Give player a brief build window at game start
        //if (phase) phase.SetBuildPhase(true);

        yield return new WaitForSeconds(5f); // wait 5 seconds
        StartNextWave(); // start first wave
    }

    /// <summary>
    /// This starts the next wave
    /// </summary>
    public void StartNextWave()
    {
        //Debug.Log("Wave System: Starting wave " + (CurrentWaveNumber + 1));
        if (waveRoutine != null) StopCoroutine(waveRoutine); // stop existing wave routine

        uiManager.UpdateWave(); // update wave UI

        // Check for end of waves
        if (CurrentWaveNumber == waveSequence.Count)
        {
            // Complete quest objective if applicable
            questUI.Quests[0].GetComponent<Quest>().CompleteObjective();

            // If no endless mode, signal no more waves
            if (!endlessBeyondLastDefined)
            {
                
                noMoreWaves?.Invoke(); // signal no more waves
                return; // no more waves
            }
        }

        // Increment wave number
        CurrentWaveNumber++;

        // Start wave routine
        waveRoutine = StartCoroutine(RunWave());
    }

    /// <summary>
    /// This runs the current wave
    /// </summary>
    /// <returns></returns>
    IEnumerator RunWave()
    {
        //Debug.Log("Wave System: Running wave " + CurrentWaveNumber);
        //if (WavesRemaining == 0) questUI.Quests[0].GetComponent<Quest>().CompleteObjective();

        var spec = GetWaveSpec(CurrentWaveNumber); // get wave spec

        // Enter combat phase
        //if (phase) phase.SetBuildPhase(false);

        // Initialize wave state
        IsSpawning = true;
        EnemiesAlive = 0;
        EnemiesSpawnedThisWave = 0;
        EnemiesToSpawnThisWave = spec.totalCount;

        float nextSpawnAt = 0f; // next spawn time

        // While more to spawn
        while (EnemiesSpawnedThisWave < spec.totalCount)
        {
            // Respect alive cap
            while (EnemiesAlive >= spec.aliveCap) yield return null;

            // Wait spawn interval
            if (Time.time < nextSpawnAt)
            {
                yield return null;
                continue;
            }

            // Pick entry to spawn
            var entry = PickEntryWeighted(spec.entries);
            if (entry == null || entry.enemy == null) // no valid entry
            {
                yield return null;
                continue;
            }

            // Try to spawn
            if (TrySpawn(entry.enemy, spec.healthMult, spec.speedMult))
            {
                EnemiesSpawnedThisWave++; // increment spawned count
                EnemiesAlive++; // increment alive count
                nextSpawnAt = Time.time + spec.spawnInterval; // schedule next spawn
            }
            else
            {
                // If no valid spawn point, skip a frame
                yield return null;
            }
        }

        IsSpawning = false; // done spawning for this wave

        // Wait for cleanup: all enemies dead
        while (EnemiesAlive > 0) yield return null;

        // Intermission/build
        //if (phase) phase.SetBuildPhase(true);
        float wait = spec.intermissionAfter; // intermission time
        while (wait > 0f) // countdown
        {
            wait -= Time.deltaTime; // wait
            yield return null; // next frame
        }

        // Next wave
        StartNextWave();
    }

#region Spec building

    /// <summary>
    /// This defines a single wave entry at runtime
    /// </summary>
    class WaveEntryRuntime
    {
        public EnemyDefinition enemy; // enemy type
        public int count; // number to spawn
        public float weight; // weight for selection
    }

    /// <summary>
    /// This defines the full spec for a wave at runtime
    /// </summary>
    class WaveSpec
    {
        public List<WaveEntryRuntime> entries = new(); // list of entries
        public float spawnInterval; // time between spawns
        public int aliveCap; // max concurrent alive
        public int totalCount; // total to spawn
        public float intermissionAfter; // intermission time after wave
        public float healthMult; // health multiplier
        public float speedMult; // speed multiplier
    }

    /// <summary>
    /// This builds the wave spec for the given wave number
    /// </summary>
    /// <param name="waveNumber"></param>
    /// <returns></returns>
    WaveSpec GetWaveSpec(int waveNumber)
    {
        // Author-defined wave
        if (waveNumber - 1 < waveSequence.Count && waveSequence[waveNumber - 1] != null)
        {
            // Build from definition
            var w = waveSequence[waveNumber - 1]; // 0-based index
            var spec = new WaveSpec
            {
                spawnInterval = Mathf.Max(0.05f, w.spawnIntervalSeconds),
                aliveCap = Mathf.Max(1, w.concurrentAliveCap),
                intermissionAfter = Mathf.Max(0f, w.intermissionSeconds),
                healthMult = Mathf.Max(0.1f, w.healthMultiplier),
                speedMult = Mathf.Max(0.1f, w.speedMultiplier),
            }; // copy basic params

            // Copy entries
            foreach (var e in w.entries.Where(e => e.enemy)) // skip null enemy refs
            {
                // Add entry with safety clamps
                spec.entries.Add(new WaveEntryRuntime
                {
                    enemy = e.enemy, // copy enemy ref
                    count = Mathf.Max(0, e.count), // clamp count
                    weight = Mathf.Max(0.0001f, e.weight) // clamp weight
                });
                spec.totalCount += Mathf.Max(0, e.count); // accumulate total count
            }

            // Safety fallback if designer forgot entries
            if (spec.totalCount == 0 && fallbackEnemy)
            {
                spec.entries.Add(new WaveEntryRuntime { enemy = fallbackEnemy, count = 8, weight = 1f });
                spec.totalCount = 8; // set total count
            }

            return spec; // return built spec
        }

        // Procedural fallback (endless)
        if (endlessBeyondLastDefined && fallbackEnemy != null)
        {
            int n = waveNumber - waveSequence.Count; // 1..∞ after authored waves
            var count = Mathf.Max(1, fallbackStartCount + (n - 1) * fallbackAddPerWave);
            var spawn = Mathf.Max(minSpawnInterval, fallbackSpawnInterval - 0.03f * (n - 1));
            var cap = Mathf.Min(fallbackAliveCap + (n - 1) * 2, 150);

            float hMult = Mathf.Pow(healthPerWave, Mathf.Max(0, waveNumber - 1)); // multiplicative scale
            float sMult = Mathf.Min(2.5f, Mathf.Pow(speedPerWave, Mathf.Max(0, waveNumber - 1)));

            var spec = new WaveSpec
            {
                spawnInterval = spawn,
                aliveCap = cap,
                intermissionAfter = intermissionSeconds,
                healthMult = hMult,
                speedMult = sMult,
                totalCount = count
            };
            spec.entries.Add(new WaveEntryRuntime { enemy = fallbackEnemy, count = count, weight = 1f });
            return spec;
        }

        // If no fallback, just repeat the last defined wave
        return GetWaveSpec(Mathf.Clamp(waveNumber, 1, waveSequence.Count));
    }
    #endregion

    /// <summary>
    /// This picks a wave entry from the given list based on weights and remaining counts
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    WaveEntryRuntime PickEntryWeighted(List<WaveEntryRuntime> list)
    {
        // Consume counts gradually (without building a big queue)
        float totalW = 0f; // sum weights
        foreach (var e in list) if (e.count > 0) totalW += e.weight; // only count those with remaining spawns
        if (totalW <= 0f) return null; // no entries left

        // Pick based on weight
        float r = UnityEngine.Random.value * totalW; // random value in [0, totalW]
        float acc = 0f; // accumulated weight
        for (int i = 0; i < list.Count; i++) // iterate entries
        {
            var e = list[i]; // current entry

            if (e.count <= 0) continue; // skip if none left

            acc += e.weight; // accumulate weight

            if (r <= acc) // check if selected
            {
                e.count--; // consume one spawn of this type

                return e; // return selected entry
            }
        }

        return null; // should not reach here
    }

    #region Spawning
    /// <summary>
    /// This tries to spawn an enemy of the given definition with scaled stats
    /// </summary>
    /// <param name="def"></param>
    /// <param name="healthMult"></param>
    /// <param name="speedMult"></param>
    /// <returns></returns>
    bool TrySpawn(EnemyDefinition def, float healthMult, float speedMult)
    {
        //Debug.Log("Wave System: Trying to Spawn enemy " + def.id);

        //Debug.Log("Wave System: Picking spawn point for enemy " + def.id);

        var sp = PickSpawnPointWeighted(); // pick spawn point based on weights

        // if no spawn point, fail
        if (sp == null)
        {
            //Debug.Log("No Spawn Point Selected");
            return false;
        }

        //Debug.Log("Wave System: Getting spawn position for enemy " + def.id);

        // get spawn position from spawn point
        if (!sp.TryGetSpawnPosition(out var pos))
        {
            //Debug.Log("No Spawn Position Selected");
            return false;
        }

        //Debug.Log("Wave System: Spawning enemy " + def.id + " at " + pos);

        var go = Instantiate(def.prefab, pos, Quaternion.identity); // instantiate enemy prefab

        var hp = go.GetComponent<EnemyController>(); // get EnemyController

        // if no EnemyController, destroy and fail
        if (!hp) { Destroy(go); return false; }

        hp.Initialize(def, healthMult, speedMult); // init with scaled stats

        hp.SetTarget(player); // set player as target for chasing

        return true; // spawned successfully
    }

    /// <summary>
    /// This picks a spawn point based on their weights
    /// </summary>
    /// <returns></returns>
    SpawnPoint PickSpawnPointWeighted()
    {
        float total = 0f; // sum weights

        // Calculate total weight
        foreach (var s in spawnPoints) total += s ? s.Weight : 0f;

        // If no weight, return null
        if (total <= 0f) return null;

        // Pick based on weight
        float r = UnityEngine.Random.value * total, acc = 0f;

        // Find selected spawn point
        foreach (var s in spawnPoints)
        {
            // Skip nulls
            if (!s) continue;

            // Accumulate weight
            acc += s.Weight;

            // Check if selected
            if (r <= acc) return s;
        }
        return spawnPoints.FirstOrDefault(sp => sp && sp.Weight > 0f);
    }
    #endregion

    #region Events
    /// <summary>
    /// This is called when an enemy is killed
    /// </summary>
    /// <param name="e"></param>
    void HandleEnemyKilled(EnemyController e)
    {
        EnemiesAlive = Mathf.Max(0, EnemiesAlive - 1); // decrement alive count
        //if (currency) currency.Add(e.reward);

        // Award points to player
        player.GetComponent<Controller_Player>().Points += e.reward;

        // Update UI
        uiManager.UpdateScore();
    }
    #endregion

    /// <summary>
    /// This calculates the total possible points from all defined waves
    /// </summary>
    /// <returns></returns>
    private int CalcTotalPosiblePoints()
    {
        if (waveSequence == null || waveSequence.Count == 0) return 0; // no waves defined

        // Sum up all possible points from all waves
        return waveSequence.Sum(wave => wave.entries.Sum(entry => entry.enemy ? entry.count * entry.enemy.killReward : 0));
    }
}
