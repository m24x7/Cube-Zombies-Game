using System.Collections.Generic;
using UnityEngine;

public class BuildingSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private BuildPhaseManager phaseManager; // optional

    [Header("Blocks / Selection")]
    [SerializeField] private List<BlockDefinition> blocks = new List<BlockDefinition>();
    [SerializeField] private int selectedIndex = 0;

    [Header("Placement Settings")]
    [SerializeField] private float maxPlaceDistance = 8f;
    [SerializeField] private LayerMask placeOnMask; // e.g., Default + Environment
    [SerializeField] private LayerMask removableMask; // e.g., "Blocks"
    [SerializeField] private KeyCode rotateLeftKey = KeyCode.Q;
    [SerializeField] private KeyCode rotateRightKey = KeyCode.E;

    [Header("Input (legacy example)")]
    [SerializeField] private KeyCode placeKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode removeKey = KeyCode.Mouse1;
    [SerializeField] private KeyCode nextBlockKey = KeyCode.Alpha2;
    [SerializeField] private KeyCode prevBlockKey = KeyCode.Alpha1;

    private BuildCurrency currency;
    private GameObject ghost;
    private Material[] originalGhostMats;
    private bool canPlaceHere;
    private Quaternion userRotation = Quaternion.identity;

    void Awake()
    {
        currency = GetComponent<BuildCurrency>();
        if (!playerCamera) playerCamera = Camera.main;
        SpawnGhost();
        ApplySelectedBlockToGhost();
    }

    void Update()
    {
        if (blocks.Count == 0) return;

        HandleSelectionHotkeys();
        HandleRotationInput();
        UpdateGhostPoseAndValidity();

        // Place
        if (Input.GetKeyDown(placeKey))
            TryPlace();

        // Remove
        if (Input.GetKeyDown(removeKey))
            TryRemove();
    }

    // --- Selection / Rotation ---

    void HandleSelectionHotkeys()
    {
        // Example: 1/2 cycle between two blocks; expand as needed.
        if (Input.GetKeyDown(prevBlockKey)) SetSelectedIndex(Mathf.Max(0, selectedIndex - 1));
        if (Input.GetKeyDown(nextBlockKey)) SetSelectedIndex(Mathf.Min(blocks.Count - 1, selectedIndex + 1));
    }

    void HandleRotationInput()
    {
        if (Input.GetKeyDown(rotateLeftKey))
            userRotation = Quaternion.Euler(0f, -90f, 0f) * userRotation;

        if (Input.GetKeyDown(rotateRightKey))
            userRotation = Quaternion.Euler(0f, 90f, 0f) * userRotation;
    }

    public void SetSelectedIndex(int idx)
    {
        if (idx == selectedIndex || idx < 0 || idx >= blocks.Count) return;
        selectedIndex = idx;
        ApplySelectedBlockToGhost();
    }

    public void SetSelectedBlock(BlockDefinition def)
    {
        int idx = blocks.IndexOf(def);
        if (idx >= 0) SetSelectedIndex(idx);
    }

    // --- Ghost ---

    void SpawnGhost()
    {
        if (ghost != null) Destroy(ghost);
        if (blocks.Count == 0 || blocks[selectedIndex] == null || blocks[selectedIndex].prefab == null) return;

        ghost = Instantiate(blocks[selectedIndex].prefab);
        ghost.name = "[GHOST] " + blocks[selectedIndex].id;

        // Disable colliders on ghost
        foreach (var col in ghost.GetComponentsInChildren<Collider>()) col.enabled = false;

        // Swap materials to ghost material
        var rends = ghost.GetComponentsInChildren<Renderer>();
        originalGhostMats = new Material[rends.Length];
        for (int i = 0; i < rends.Length; i++)
        {
            originalGhostMats[i] = rends[i].sharedMaterial;
            if (blocks[selectedIndex].ghostMaterial)
                rends[i].sharedMaterial = blocks[selectedIndex].ghostMaterial;
        }
    }

    void ApplySelectedBlockToGhost()
    {
        SpawnGhost();
        userRotation = Quaternion.identity; // reset rotation per selection
    }

    // --- Raycast / Pose / Validation ---

    void UpdateGhostPoseAndValidity()
    {
        if (!ghost) return;

        var def = blocks[selectedIndex];
        if (def == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out var hit, maxPlaceDistance, placeOnMask, QueryTriggerInteraction.Ignore))
        {
            // Base orientation
            Quaternion surfaceRot = def.alignToSurfaceNormal
                ? Quaternion.LookRotation(Vector3.ProjectOnPlane(playerCamera.transform.forward, hit.normal), hit.normal)
                : Quaternion.Euler(0f, cameraFlatYaw(playerCamera), 0f);

            Quaternion rotation = surfaceRot * userRotation;

            // Snap position to grid
            Vector3 cell = Vector3.one * def.cellSize;
            Vector3 pos = SnapToGrid(hit.point + hit.normal * (def.cellSize * 0.5f), cell, rotation);

            ghost.transform.SetPositionAndRotation(pos, rotation);

            // Validate overlap
            canPlaceHere = def.allowOverlap || !OverlapsForbidden(def, pos, rotation);

            TintGhost(canPlaceHere ? Color.green : Color.red, 0.5f);
        }
        else
        {
            // Hide ghost just in front of camera to avoid confusion
            ghost.transform.position = playerCamera.transform.position + playerCamera.transform.forward * 1.5f;
            ghost.transform.rotation = playerCamera.transform.rotation;
            canPlaceHere = false;
            TintGhost(Color.red, 0.3f);
        }
    }

    float cameraFlatYaw(Camera cam)
    {
        var fwd = cam.transform.forward; fwd.y = 0f; fwd.Normalize();
        return Quaternion.LookRotation(fwd, Vector3.up).eulerAngles.y;
    }

    Vector3 SnapToGrid(Vector3 worldPos, Vector3 cell, Quaternion rotation)
    {
        // Convert to local grid space aligned with rotation, snap, convert back
        Matrix4x4 toLocal = Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one).inverse;
        Vector3 local = toLocal.MultiplyPoint3x4(worldPos);

        local.x = Mathf.Round(local.x / cell.x) * cell.x;
        local.y = Mathf.Round(local.y / cell.y) * cell.y;
        local.z = Mathf.Round(local.z / cell.z) * cell.z;

        return Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one).MultiplyPoint3x4(local);
    }

    bool OverlapsForbidden(BlockDefinition def, Vector3 pos, Quaternion rot)
    {
        Vector3 half = Vector3.Scale((Vector3)def.sizeInCells, Vector3.one * def.cellSize) * 0.5f;
        var hits = Physics.OverlapBox(pos, half * 0.99f, rot, def.forbiddenOverlap, QueryTriggerInteraction.Ignore);
        return hits.Length > 0;
    }

    void TintGhost(Color color, float alpha)
    {
        if (!ghost) return;
        foreach (var r in ghost.GetComponentsInChildren<Renderer>())
        {
            if (r.sharedMaterial && r.sharedMaterial.HasProperty("_Color"))
            {
                var c = color; c.a = alpha;
                r.sharedMaterial.color = c;
            }
        }
    }

    // --- Place / Remove ---

    void TryPlace()
    {
        if (phaseManager && !phaseManager.IsBuildPhase) return;
        var def = blocks[selectedIndex];
        if (!def || !ghost || !canPlaceHere) return;

        if (!currency.TrySpend(def.cost)) return;

        var go = Instantiate(def.prefab, ghost.transform.position, ghost.transform.rotation);
        // Put the placed object on a "Blocks" layer if you have one:
        // go.layer = LayerMask.NameToLayer("Blocks");
    }

    void TryRemove()
    {
        // Shoot a ray to find a placed block on removableMask
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out var hit, maxPlaceDistance, removableMask, QueryTriggerInteraction.Ignore))
        {
            // Optionally ensure the object is one of our buildables (by tag or component)
            var root = hit.collider.attachedRigidbody ? hit.collider.attachedRigidbody.gameObject : hit.collider.gameObject;
            currency.Add(CurrentBlockRefund(root));
            Destroy(root);
        }
    }

    int CurrentBlockRefund(GameObject placed)
    {
        // Simple strategy: refund from currently selected type if tag matches.
        // For robust projects, store a small component on placed blocks with a reference to BlockDefinition.
        var tagMatches = (blocks[selectedIndex] != null && placed.CompareTag(blocks[selectedIndex].id));
        return tagMatches ? blocks[selectedIndex].refund : Mathf.RoundToInt((blocks[selectedIndex]?.refund ?? 25) * 0.5f);
    }
}
