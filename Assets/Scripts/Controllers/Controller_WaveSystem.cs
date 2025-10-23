using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Controller_WaveSystem : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform player;         // for EnemyChasePlayer
    //[SerializeField] private BuildCurrency currency;   // your existing bank
    //[SerializeField] private BuildPhaseManager phase;  // your existing phase gate
    [SerializeField] private UI_Manager uiManager;
    [SerializeField] private Controller_QuestUI questUI;

    [Header("Spawn Points")]
    [SerializeField] private List<SpawnPoint> spawnPoints = new();

    [Header("Waves")]
    [SerializeField] private List<WaveDefinition> waveSequence = new();

    [Tooltip("If true, when you run out of author-defined waves, waves keep scaling procedurally.")]
    [SerializeField] private bool endlessBeyondLastDefined = true;

    [Header("Procedural Fallback (kicks in after last defined wave)")]
    [SerializeField] private EnemyDefinition fallbackEnemy;
    [SerializeField, Min(1)] private int fallbackStartCount = 10;
    [SerializeField, Min(0)] private int fallbackAddPerWave = 4;
    [SerializeField, Min(0.1f)] private float fallbackSpawnInterval = 0.8f;
    [SerializeField, Min(1)] private int fallbackAliveCap = 20;
    [SerializeField] private float healthPerWave = 1.25f;  // 25% tougher per wave
    [SerializeField] private float speedPerWave = 1.02f;  // +2% speed per wave
    [SerializeField] private float minSpawnInterval = 0.25f;
    [SerializeField] private float intermissionSeconds = 18f;

    public Action noMoreWaves;

    public int TotalPossiblePoints { get => CalcTotalPosiblePoints(); }

    // Runtime
    public int CurrentWaveNumber { get; private set; } = 0; // 1-based
    public int TotalWavesDefined => waveSequence.Count;
    public int WavesRemaining => Mathf.Max(0, waveSequence.Count - CurrentWaveNumber);
    public int EnemiesAlive { get; private set; } = 0;
    public int EnemiesToSpawnThisWave { get; private set; } = 0;
    public int EnemiesSpawnedThisWave { get; private set; } = 0;
    public bool IsSpawning { get; private set; } = false;

    private Coroutine waveRoutine;

    void OnEnable()
    {
        TestEnemyController.OnEntityDeath += HandleEnemyKilled;
    }

    void OnDisable()
    {
        TestEnemyController.OnEntityDeath -= HandleEnemyKilled;
    }

    void Start()
    {
        //if (!currency) currency = FindFirstObjectByType<BuildCurrency>();
        //if (!phase) phase = FindFirstObjectByType<BuildPhaseManager>();
        if (!player && playerCamera) player = playerCamera.transform;
        StartCoroutine(StartFirstWaveWithShortBuild());
    }

    IEnumerator StartFirstWaveWithShortBuild()
    {
        //Debug.Log("Wave System: Starting first wave after short build phase.");
        // Give player a brief build window at game start
        //if (phase) phase.SetBuildPhase(true);
        yield return new WaitForSeconds(5f);
        StartNextWave();
    }

    public void StartNextWave()
    {
        //Debug.Log("Wave System: Starting wave " + (CurrentWaveNumber + 1));
        if (waveRoutine != null) StopCoroutine(waveRoutine);

        uiManager.UpdateWave();

        if (CurrentWaveNumber == waveSequence.Count)
        {
            questUI.Quests[0].GetComponent<Quest>().CompleteObjective();

            if (!endlessBeyondLastDefined)
            {
                
                noMoreWaves?.Invoke();
                return; // no more waves
            }
        }

        CurrentWaveNumber++;

        waveRoutine = StartCoroutine(RunWave());
    }

    IEnumerator RunWave()
    {
        //Debug.Log("Wave System: Running wave " + CurrentWaveNumber);
        //if (WavesRemaining == 0) questUI.Quests[0].GetComponent<Quest>().CompleteObjective();
        var spec = GetWaveSpec(CurrentWaveNumber);

        // Enter combat phase
        //if (phase) phase.SetBuildPhase(false);

        IsSpawning = true;
        EnemiesAlive = 0;
        EnemiesSpawnedThisWave = 0;
        EnemiesToSpawnThisWave = spec.totalCount;

        float nextSpawnAt = 0f;

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

            var entry = PickEntryWeighted(spec.entries);
            if (entry == null || entry.enemy == null)
            {
                yield return null;
                continue;
            }

            if (TrySpawn(entry.enemy, spec.healthMult, spec.speedMult))
            {
                EnemiesSpawnedThisWave++;
                EnemiesAlive++;
                nextSpawnAt = Time.time + spec.spawnInterval;
            }
            else
            {
                // If no valid spawn point, skip a frame
                yield return null;
            }
        }

        IsSpawning = false;

        // Wait for cleanup: all enemies dead
        while (EnemiesAlive > 0) yield return null;

        // Intermission/build
        //if (phase) phase.SetBuildPhase(true);
        float wait = spec.intermissionAfter;
        while (wait > 0f)
        {
            wait -= Time.deltaTime;
            yield return null;
        }

        // Next wave
        StartNextWave();
    }

    // --- Spec building ---

    class WaveEntryRuntime { public EnemyDefinition enemy; public int count; public float weight; }

    class WaveSpec
    {
        public List<WaveEntryRuntime> entries = new();
        public float spawnInterval;
        public int aliveCap;
        public int totalCount;
        public float intermissionAfter;
        public float healthMult;
        public float speedMult;
    }

    WaveSpec GetWaveSpec(int waveNumber)
    {
        if (waveNumber - 1 < waveSequence.Count && waveSequence[waveNumber - 1] != null)
        {
            var w = waveSequence[waveNumber - 1];
            var spec = new WaveSpec
            {
                spawnInterval = Mathf.Max(0.05f, w.spawnIntervalSeconds),
                aliveCap = Mathf.Max(1, w.concurrentAliveCap),
                intermissionAfter = Mathf.Max(0f, w.intermissionSeconds),
                healthMult = Mathf.Max(0.1f, w.healthMultiplier),
                speedMult = Mathf.Max(0.1f, w.speedMultiplier),
            };

            foreach (var e in w.entries.Where(e => e.enemy))
            {
                spec.entries.Add(new WaveEntryRuntime
                {
                    enemy = e.enemy,
                    count = Mathf.Max(0, e.count),
                    weight = Mathf.Max(0.0001f, e.weight)
                });
                spec.totalCount += Mathf.Max(0, e.count);
            }

            // Safety fallback if designer forgot entries
            if (spec.totalCount == 0 && fallbackEnemy)
            {
                spec.entries.Add(new WaveEntryRuntime { enemy = fallbackEnemy, count = 8, weight = 1f });
                spec.totalCount = 8;
            }
            return spec;
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

    WaveEntryRuntime PickEntryWeighted(List<WaveEntryRuntime> list)
    {
        // Consume counts gradually (without building a big queue)
        float totalW = 0f;
        foreach (var e in list) if (e.count > 0) totalW += e.weight;
        if (totalW <= 0f) return null;

        float r = UnityEngine.Random.value * totalW;
        float acc = 0f;
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            if (e.count <= 0) continue;
            acc += e.weight;
            if (r <= acc)
            {
                e.count--; // consume one spawn of this type
                return e;
            }
        }
        return null;
    }

    // --- Spawning ---

    bool TrySpawn(EnemyDefinition def, float healthMult, float speedMult)
    {
        //Debug.Log("Wave System: Trying to Spawn enemy " + def.id);

        //Debug.Log("Wave System: Picking spawn point for enemy " + def.id);
        var sp = PickSpawnPointWeighted();
        if (sp == null)
        {
            //Debug.Log("No Spawn Point Selected");
            return false;
        }

        //Debug.Log("Wave System: Getting spawn position for enemy " + def.id);
        if (!sp.TryGetSpawnPosition(out var pos))
        {
            //Debug.Log("No Spawn Position Selected");
            return false;
        }

        //Debug.Log("Wave System: Spawning enemy " + def.id + " at " + pos);
        var go = Instantiate(def.prefab, pos, Quaternion.identity);
        var hp = go.GetComponent<TestEnemyController>();
        if (!hp) { Destroy(go); return false; }
        hp.Initialize(def, healthMult, speedMult);

        var chase = go.GetComponent<EnemyChasePlayer>();
        if (chase && player) chase.SetTarget(player);

        return true;
    }

    SpawnPoint PickSpawnPointWeighted()
    {
        float total = 0f;
        foreach (var s in spawnPoints) total += s ? s.Weight : 0f;
        if (total <= 0f) return null;

        float r = UnityEngine.Random.value * total, acc = 0f;
        foreach (var s in spawnPoints)
        {
            if (!s) continue;
            acc += s.Weight;
            if (r <= acc) return s;
        }
        return spawnPoints.FirstOrDefault(sp => sp && sp.Weight > 0f);
    }

    // --- Events ---

    void HandleEnemyKilled(TestEnemyController e)
    {
        EnemiesAlive = Mathf.Max(0, EnemiesAlive - 1);
        //if (currency) currency.Add(e.reward);

        player.GetComponent<Controller_Player>().Points += e.reward;
        uiManager.UpdateScore();
    }

    private int CalcTotalPosiblePoints()
    {
        if (waveSequence == null || waveSequence.Count == 0) return 0;
        return waveSequence.Sum(wave => wave.entries.Sum(entry => entry.enemy ? entry.count * entry.enemy.killReward : 0));
    }
}
