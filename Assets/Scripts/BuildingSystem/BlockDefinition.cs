using UnityEngine;

[CreateAssetMenu(menuName = "Building/Block Definition", fileName = "Block_")]
public class BlockDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id = "block_id";

    [Header("Prefab & Visuals")]
    public GameObject prefab;                // The final placed object
    public Material ghostMaterial;           // Transparent material for preview

    [Header("Placement")]
    public Vector3Int sizeInCells = Vector3Int.one; // Dimensions, e.g., 1x1x1
    public bool alignToSurfaceNormal = true; // Stick to walls/ceilings if true
    public float cellSize = 1f;              // Grid size (default 1 meter)

    [Header("Validation")]
    public LayerMask forbiddenOverlap;       // Layers you cannot intersect
    public bool allowOverlap = false;        // If true, skips overlap checks

    [Header("Economy")]
    public int cost = 50;                    // Points to place
    public int refund = 35;                  // Points when removed
}
