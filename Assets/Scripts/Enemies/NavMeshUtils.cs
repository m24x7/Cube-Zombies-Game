using Unity.AI.Navigation;
//using UnityEditor.AI;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshUtils : MonoBehaviour
{
    private NavMeshSurface navMeshSurface;

    [SerializeField] private float minUpdateInterval = 0.25f;

    [SerializeField] private bool forceUpdate = false;
    public bool ForceUpdate { set => forceUpdate = value; }

    // Make it a singleton for easy access
    public static NavMeshUtils Instance { get; private set; }

    // Awake is called when the script instance is being loaded
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
        }

        navMeshSurface = GetComponent<NavMeshSurface>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        navMeshSurface.BuildNavMesh();
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

    public void UpdateNavMesh()
    {
        var data = navMeshSurface.navMeshData;
        if (data == null)
        {
            // Fallback: full rebuild if no data yet
            navMeshSurface.BuildNavMesh();
            return;
        }

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

        navMeshSurface.BuildNavMesh();
    }
}
