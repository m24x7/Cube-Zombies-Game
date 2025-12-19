using Unity.AI.Navigation;
using UnityEngine;

/// <summary>
/// This class provides utility functions for NavMesh operations
/// </summary>
public class NavMeshUtils : MonoBehaviour
{
    // Reference to the NavMeshSurface component
    private NavMeshSurface navMeshSurface;

    // Minimum interval between NavMesh updates
    [SerializeField] private float minUpdateInterval = 0.25f;

    // Flag to force NavMesh update
    [SerializeField] private bool forceUpdate = false;
    public bool ForceUpdate { set => forceUpdate = value; }

    // Make it a singleton for easy access
    public static NavMeshUtils Instance { get; private set; }

    /// <summary>
    /// Awake is called when the script instance is being loaded
    /// </summary>
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
        }

        // Get reference to NavMeshSurface
        navMeshSurface = GetComponent<NavMeshSurface>();
    }

    /// <summary>
    /// Start is called once before the first execution of Update after the MonoBehaviour is created
    /// </summary>
    void Start()
    {
        navMeshSurface.BuildNavMesh(); // Initial build of the NavMesh
    }

    //// Update is called once per frame
    //void Update()
    //{
    //    if (forceUpdate)
    //    {
    //        UpdateNavMesh();
    //        forceUpdate = false;
    //    }
    //}

    /// <summary>
    /// This method updates the NavMesh data
    /// </summary>
    public void UpdateNavMesh()
    {
        var data = navMeshSurface.navMeshData;

        #region Attempted Partial Update (Disabled)
        //if (data == null)
        //{
        //    // Fallback: full rebuild if no data yet
        //    navMeshSurface.BuildNavMesh();
        //    return;
        //}

        //// Collect sources restricted to bounds:
        //var sources = new System.Collections.Generic.List<NavMeshBuildSource>();
        //NavMeshBuilder.CollectSources(
        //    navMeshSurface.transform,
        //    navMeshSurface,
        //    navMeshSurface.layerMask,
        //    navMeshSurface.useGeometry,
        //    navMeshSurface.defaultArea,
        //    sources
        //);

        //var settings = navMeshSurface.GetBuildSettings();

        //NavMeshBuilder.UpdateNavMeshData(
        //    data,
        //    settings,
        //    sources,
        //    _pendingBounds
        //);

        //navMeshSurface.UpdateNavMesh(navMeshSurface.navMeshData);
        #endregion

        navMeshSurface.BuildNavMesh();
    }
}
