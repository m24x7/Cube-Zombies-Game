using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class TestEnemyController : Parent_Entity
{
    public static event Action<TestEnemyController> OnEntityDeath;

    [Header("Runtime values (initialized by spawner)")]
    public EnemyDefinition definition;
    public int reward;

    private NavMeshAgent agent;

    // ---- HIT FLASH SETTINGS ----
    [Header("Hit Flash")]
    [SerializeField] private Renderer[] renderers;        // leave empty to auto-find in children
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField, Min(0.01f)] private float flashDuration = 0.12f;
    [SerializeField] private AnimationCurve flashCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    private struct RendData
    {
        public Renderer r;
        public int colorId;      // which property we're overriding
        public Color baseColor;  // original material color
        public bool valid;
    }

    private readonly List<RendData> _rends = new();
    private MaterialPropertyBlock _mpb;
    private Coroutine damageFlashRoutine;

    // Common shader property IDs
    private static readonly int _BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int _ColorID = Shader.PropertyToID("_Color");
    private static readonly int _EmissionID = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        BuildRendererCache();
    }

    // Called by WaveDirector after Instantiate
    public void Initialize(EnemyDefinition def, float healthMult, float speedMult)
    {
        definition = def;
        Health.Init(Mathf.RoundToInt(def.baseHealth * Mathf.Max(0.1f, healthMult)));
        reward = def.killReward;

        if (agent) agent.speed = def.baseSpeed * Mathf.Max(0.1f, speedMult);
    }

    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);
        TriggerHitFlash();
        Debug.Log("TestEnemyController: Took " + damage + " damage. Current health: " + Health.Cur);
    }

    protected override void Die()
    {
        Debug.Log("TestEnemyController: Died.");
        OnEntityDeath?.Invoke(this);
        Destroy(gameObject);
    }

    // ---------- Hit Flash ----------

    void BuildRendererCache()
    {
        _mpb ??= new MaterialPropertyBlock();
        _rends.Clear();

        // Auto-find if not assigned
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);

        foreach (var r in renderers)
        {
            if (!r) continue;
            var mat = r.sharedMaterial;            // read-only; won’t instantiate
            if (!mat) continue;

            int prop = 0;
            if (mat.HasProperty(_BaseColorID)) prop = _BaseColorID;
            else if (mat.HasProperty(_ColorID)) prop = _ColorID;
            else if (mat.HasProperty(_EmissionID)) prop = _EmissionID;

            if (prop != 0)
            {
                var baseCol = mat.GetColor(prop);
                _rends.Add(new RendData { r = r, colorId = prop, baseColor = baseCol, valid = true });
            }
        }
    }

    void TriggerHitFlash()
    {
        if (_rends.Count == 0) return;
        if (damageFlashRoutine != null) StopCoroutine(damageFlashRoutine);
        damageFlashRoutine = StartCoroutine(HitFlashRoutine());
    }

    IEnumerator HitFlashRoutine()
    {
        float t = 0f;

        // Animate from flashCurve value (1→0 by default)
        while (t < flashDuration)
        {
            float a = flashCurve.Evaluate(t / flashDuration); // 1..0
            for (int i = 0; i < _rends.Count; i++)
            {
                var rd = _rends[i];
                if (!rd.valid) continue;

                // Blend base color toward flash color
                Color c = Color.Lerp(rd.baseColor, flashColor, a);
                _mpb.SetColor(rd.colorId, c);
                rd.r.SetPropertyBlock(_mpb);
            }

            t += Time.deltaTime;
            yield return null;
        }

        // Restore original colors
        for (int i = 0; i < _rends.Count; i++)
        {
            var rd = _rends[i];
            if (!rd.valid) continue;

            _mpb.SetColor(rd.colorId, rd.baseColor);
            rd.r.SetPropertyBlock(_mpb);
        }

        damageFlashRoutine = null;
    }
}
