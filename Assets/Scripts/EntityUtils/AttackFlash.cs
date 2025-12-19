using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This component handles flashing the object's materials to a specified color when it attacks,
/// </summary>
public class AttackFlash : MonoBehaviour
{
    [Header("Attack Flash")]
    [SerializeField] private Renderer[] renderers; // leave empty to auto-find in children
    [SerializeField] private Color flashColor = Color.orange; // color to flash to on hit
    [SerializeField, Min(0.01f)] private float flashDuration = 0.12f; // duration of the flash
    [SerializeField] private AnimationCurve flashCurve = AnimationCurve.EaseInOut(0, 1, 1, 0); // curve for flash interpolation

    /// <summary>
    /// This struct holds data for each renderer we want to flash.
    /// </summary>
    private struct RendData
    {
        public Renderer r; // the renderer component
        public int colorId; // which property we're overriding
        public Color baseColor; // original material color
        public bool valid; // whether this entry is valid
    }

    // Cache of renderers and their data
    private readonly List<RendData> _rends = new(); // initialized empty
    private MaterialPropertyBlock _mpb; // shared property block for setting colors
    private Coroutine attackFlashRoutine; // reference to the active flash coroutine

    // Common shader property IDs
    private static readonly int _BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int _ColorID = Shader.PropertyToID("_Color");
    private static readonly int _EmissionID = Shader.PropertyToID("_EmissionColor");

    #region Attack Flash

    /// <summary>
    /// This builds the cache of renderers and their base colors to flash.
    /// </summary>
    public void BuildRendererCache()
    {
        // Initialize MaterialPropertyBlock if null
        _mpb ??= new MaterialPropertyBlock();
        _rends.Clear(); // clear existing cache

        // Auto-find if not assigned
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);

        // Populate the cache
        foreach (var r in renderers) // iterate through each renderer
        {
            if (!r) continue; // skip null renderers
            var mat = r.sharedMaterial; // get shared material
            if (!mat) continue; // skip if no material

            // Determine which color property to use
            int prop = 0; // property ID to use
            if (mat.HasProperty(_BaseColorID)) prop = _BaseColorID; // check for _BaseColor
            else if (mat.HasProperty(_ColorID)) prop = _ColorID; // check for _Color
            else if (mat.HasProperty(_EmissionID)) prop = _EmissionID; // check for _EmissionColor

            // If a valid property was found, store its base color
            if (prop != 0)
            {
                var baseCol = mat.GetColor(prop); // get the base color
                _rends.Add(new RendData { r = r, colorId = prop, baseColor = baseCol, valid = true }); // add to cache
            }
        }
    }

    /// <summary>
    /// This triggers the attack flash effect.
    /// </summary>
    public void TriggerAttackFlash()
    {
        if (_rends.Count == 0) return;
        if (attackFlashRoutine != null) StopCoroutine(attackFlashRoutine);
        attackFlashRoutine = StartCoroutine(AttackFlashRoutine());
    }

    /// <summary>
    /// This coroutine handles the attack flash animation.
    /// </summary>
    /// <returns></returns>
    IEnumerator AttackFlashRoutine()
    {
        float t = 0f;

        // Animate from flashCurve value (1->0 by default)
        while (t < flashDuration)
        {
            float a = flashCurve.Evaluate(t / flashDuration); // 1..0
            for (int i = 0; i < _rends.Count; i++) // iterate cached renderers
            {
                var rd = _rends[i]; // get renderer data
                if (!rd.valid) continue; // skip invalid entries

                // Blend base color toward flash color
                Color c = Color.Lerp(rd.baseColor, flashColor, a); // interpolate color
                _mpb.SetColor(rd.colorId, c); // set color in property block
                rd.r.SetPropertyBlock(_mpb); // apply property block to renderer
            }

            t += Time.deltaTime; // increment timer
            yield return null; // wait for next frame
        }

        // Restore original colors
        for (int i = 0; i < _rends.Count; i++) // iterate cached renderers
        {
            var rd = _rends[i]; // get renderer data
            if (!rd.valid) continue; // skip invalid entries

            _mpb.SetColor(rd.colorId, rd.baseColor); // restore base color
            rd.r.SetPropertyBlock(_mpb); // apply property block to renderer
        }

        attackFlashRoutine = null; // clear coroutine reference
    }
    #endregion
}
