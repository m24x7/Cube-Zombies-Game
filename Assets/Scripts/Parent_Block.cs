using UnityEngine;

public class Parent_Block : MonoBehaviour
{
    private void Start()
    {
        NavMeshUtils.Instance.UpdateNavMesh();
    }
    public void Break()
    {
        GetComponent<Collider>().enabled = false;
        GetComponent<Renderer>().enabled = false;
        NavMeshUtils.Instance.UpdateNavMesh();
        Destroy(gameObject);
    }
}
