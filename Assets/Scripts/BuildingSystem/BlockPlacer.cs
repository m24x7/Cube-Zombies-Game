using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// This class provides methods to place and destroy blocks in a voxel-based world.
/// </summary>
public class BlockPlacer
{
    /// <summary>
    /// Attempts to place a 1x1x1 default Cube at the nearest whole-number position.
    /// Returns true if placed; false if blocked.
    /// </summary>
    public static bool TryPlaceBlockWorld(
        Vector3 desiredWorldPos,
        LayerMask blockingMask, // things that should prevent placement (Environment, Blocks, etc.)
        GameObject BlockToPlace,
        float skin = 0.49f // half-extent minus a hair so touching surfaces don't count as overlap
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

        if (blocked) return false; // can't place here

        // Place a default cube (1x1x1, centered at 'snapped')
        placedBlock = GameObject.Instantiate(BlockToPlace, snapped, Quaternion.identity);

        // Set layer and tag
        placedBlock.layer = LayerMask.NameToLayer("Blocks");
        // placedBlock.tag = "Block";

        // Play placement sound
        AudioSource.PlayClipAtPoint(
            Resources.Load<AudioClip>("Sounds/footstep_grass_003"),
            snapped,
            1f
        );

        return true; // placed
    }

    /// <summary>
    /// Raycasts from the camera center out to maxDistance. If it hits, place a cube
    /// near the hit point (snapped to whole numbers).
    /// </summary>
    public static bool TryPlaceBlockFromCamera(
        Camera cam,
        float maxDistance,
        LayerMask blockingMask,
        GameObject blockToPlace,
        float skin = 0.49f
    )
    {
        if (!cam) return false; // no camera, can't place

        // Build a ray from the center of the screen (crosshair)
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        // Nudge origin slightly forward so we don't start inside the player collider
        ray.origin += ray.direction * 0.06f;

        // Raycast to find placement point
        Vector3 target;
        if (Physics.Raycast(ray, out var hit, maxDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            // Bias slightly along the normal so we snap onto the intended cell, not inside the surface
            target = hit.point + hit.normal * 0.01f;
            return TryPlaceBlockWorld(target, blockingMask, blockToPlace, skin); // try to place
        }

        return false; // no hit, can't place
    }

    #region Helpers
    /// <summary>
    /// Rounds the specified value to the nearest whole number, with midpoint values (.5) rounded away from zero.
    /// </summary>
    /// <remarks>This method uses "round half away from zero" semantics, which differs from the default
    /// "banker's rounding" (round half to even) used by some rounding functions. For example, both 1.5 and -1.5 are
    /// rounded to 2 and -2, respectively.</remarks>
    /// <param name="v">The value to round.</param>
    /// <returns>The nearest whole number to <paramref name="v"/>, with values exactly halfway between two integers rounded away
    /// from zero.</returns>
    private static float RoundAwayFromZero(float v)
    {
        float s = Mathf.Sign(v); // get the sign of v
        return s * Mathf.Floor(Mathf.Abs(v) + 0.5f); // round away from zero
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
        if (!cam) return false; // no camera, can't destroy

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

        if (hits == null || hits.Length == 0) return false; // no hits
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance)); // sort by distance

        // Process each hit in order
        foreach (var h in hits)
        {
            var col = h.collider; // get the collider we hit
            if (!col) continue; // safety check

            // Skip our own body/weapon colliders
            if (playerRoot && col.transform.root == playerRoot) continue;

            // If it's on the Blocks layer, destroy it and stop
            if (col.gameObject.layer == blocksLayer)
            {
                // Destroy the block via its Parent_Block script
                col.GetComponent<Parent_Block>().Break();

                // Play destruction sound
                AudioSource.PlayClipAtPoint(
                    Resources.Load<AudioClip>("Sounds/footstep_grass_004"),
                    h.point,
                    1f
                );

                return true; // block found and destroyed
            }

            // Any solid non-block object blocks line of sight -> stop
            if (!col.isTrigger) return false;

            // Otherwise (it’s a trigger that isn’t a block), keep checking further hits
        }

        return false; // no block found to destroy
    }
    #endregion
}
