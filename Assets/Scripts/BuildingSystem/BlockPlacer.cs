using Unity.VisualScripting;
using UnityEngine;

public class BlockPlacer
{
    /// <summary>
    /// Attempts to place a 1x1x1 default Cube at the nearest whole-number position.
    /// Returns true if placed; false if blocked.
    /// </summary>
    public static bool TryPlaceBlockWorld(
        Vector3 desiredWorldPos,
        LayerMask blockingMask,               // things that should prevent placement (Environment, Blocks, etc.)
        GameObject BlockToPlace,
        float skin = 0.49f                    // half-extent minus a hair so touching surfaces don't count as overlap
    )
    {
        GameObject placedBlock = null;

        // Snap to whole-number grid (center of a default cube is at integer coords)
        Vector3 snapped = new Vector3(
            RoundAwayFromZero(desiredWorldPos.x),
            RoundAwayFromZero(desiredWorldPos.y),
            RoundAwayFromZero(desiredWorldPos.z)
        );

        // Check occupancy using an OverlapBox at the cube’s center with half-extents ~0.5
        Vector3 half = new Vector3(skin, skin, skin);
        bool blocked = Physics.CheckBox(
            snapped, half, Quaternion.identity,
            blockingMask, QueryTriggerInteraction.Ignore
        );

        if (blocked) return false;

        //Require support: ground directly below OR any face neighbor
        //if (!HasSupport(snapped, blockingMask, skin))
        //    return false;

        // Place a default cube (1x1x1, centered at 'snapped')
        placedBlock = GameObject.Instantiate(BlockToPlace, snapped, Quaternion.identity);
        //placedBlock.transform.position = snapped;
        //placedBlock.transform.localScale = Vector3.one;

        // Optional: put it on a dedicated layer/tag
        placedBlock.layer = LayerMask.NameToLayer("Blocks");
        // placedBlock.tag = "Block";

        AudioSource.PlayClipAtPoint(
            Resources.Load<AudioClip>("Sounds/footstep_grass_003"),
            snapped,
            1f
        );

        return true;
    }

    /// <summary>
    /// Raycasts from the camera center out to maxDistance. If it hits, place a cube
    /// near the hit point (snapped to whole numbers). If it misses, place at
    /// the far point along the ray (snapped).
    /// </summary>
    public static bool TryPlaceBlockFromCamera(
        Camera cam,
        float maxDistance,
        LayerMask blockingMask,
        GameObject blockToPlace,
        float skin = 0.49f
    )
    {
        //placedBlock = null;
        if (!cam) return false;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        // Nudge origin slightly forward so we don't start inside the player collider
        ray.origin += ray.direction * 0.06f;

        Vector3 target;
        if (Physics.Raycast(ray, out var hit, maxDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            // Bias slightly along the normal so we snap onto the intended cell, not inside the surface
            target = hit.point + hit.normal * 0.01f;
            return TryPlaceBlockWorld(target, blockingMask, blockToPlace, skin);
        }

        return false;
    }

    // --- Helpers ---

    // Rounds to the nearest whole number with .5 going away from zero (more intuitive grid snap than banker's rounding)
    private static float RoundAwayFromZero(float v)
    {
        float s = Mathf.Sign(v);
        return s * Mathf.Floor(Mathf.Abs(v) + 0.5f);
    }

    /// <summary>
    /// Destroys the first "block" the camera is looking at.
    /// Returns true if a block was destroyed.
    /// </summary>
    /// <param name="cam">Player's camera (first-person camera)</param>
    /// <param name="maxDistance">Max distance to check</param>
    /// <param name="hitMask">
    /// Layers the ray should interact with (include Environment + Blocks, exclude Player).
    /// </param>
    /// <param name="blocksLayer">Layer index for Blocks (e.g., LayerMask.NameToLayer("Blocks"))</param>
    /// <param name="playerRoot">Root transform of the player (for self-filtering)</param>
    public static bool TryDestroyBlockFromCamera(
        Camera cam,
        float maxDistance,
        LayerMask hitMask,
        int blocksLayer,
        Transform playerRoot
    )
    {
        if (!cam) return false;

        // Build a ray from the center of the screen (crosshair)
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        // Push origin slightly forward so we don't start inside the player's collider
        ray.origin += ray.direction * 0.06f;

        // Get every hit along the path, nearest first
        var hits = Physics.RaycastAll(
            ray,
            maxDistance,
            hitMask,
            QueryTriggerInteraction.Collide // in case blocks use trigger colliders
        );

        if (hits == null || hits.Length == 0) return false;
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var h in hits)
        {
            var col = h.collider;
            if (!col) continue;

            // Skip our own body/weapon colliders
            if (playerRoot && col.transform.root == playerRoot) continue;

            // If it's on the Blocks layer, destroy it and stop
            if (col.gameObject.layer == blocksLayer)
            {
                // If blocks are nested, destroy the top-level block object.
                // Adjust this if your block prefab uses a different hierarchy.
                var blockRoot = col.attachedRigidbody ? col.attachedRigidbody.gameObject : col.transform.root.gameObject;
                Object.Destroy(blockRoot);

                AudioSource.PlayClipAtPoint(
                    Resources.Load<AudioClip>("Sounds/footstep_grass_004"),
                    h.point,
                    1f
                );

                return true;
            }

            // Any solid non-block object blocks line of sight → stop
            if (!col.isTrigger)
                return false;

            // Otherwise (it’s a trigger that isn’t a block), keep checking further hits
        }

        return false;
    }
}
