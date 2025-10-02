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

        // Place a default cube (1x1x1, centered at 'snapped')
        placedBlock = GameObject.Instantiate(BlockToPlace, snapped, Quaternion.identity);
        //placedBlock.transform.position = snapped;
        //placedBlock.transform.localScale = Vector3.one;

        // Optional: put it on a dedicated layer/tag
        // placedBlock.layer = LayerMask.NameToLayer("Blocks");
        // placedBlock.tag = "Block";

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
        }
        else
        {
            target = ray.origin + ray.direction * maxDistance;
        }

        return TryPlaceBlockWorld(target, blockingMask, blockToPlace, skin);
    }

    // --- Helpers ---

    // Rounds to the nearest whole number with .5 going away from zero (more intuitive grid snap than banker's rounding)
    private static float RoundAwayFromZero(float v)
    {
        float s = Mathf.Sign(v);
        return s * Mathf.Floor(Mathf.Abs(v) + 0.5f);
    }
}
