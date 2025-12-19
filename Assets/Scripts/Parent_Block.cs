using Unity.AI.Navigation;
using UnityEngine;

public class Parent_Block : MonoBehaviour
{
    private void Start()
    {
        NavMeshUtils.Instance.UpdateNavMesh();
    }
    public void Break()
    {
        Debug.Log("Parent_Block: Breaking block " + gameObject.name);
        GetComponent<Collider>().enabled = false;
        GetComponent<Renderer>().enabled = false;
        GetComponent<NavMeshModifierVolume>().enabled = false;
        NavMeshUtils.Instance.UpdateNavMesh();
        Destroy(gameObject);
    }
}
