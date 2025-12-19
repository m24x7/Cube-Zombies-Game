using Unity.AI.Navigation;
using UnityEngine;

public class Parent_Block : MonoBehaviour
{
    [SerializeField] private int hitPoints = 3;

    private void Start()
    {
        NavMeshUtils.Instance.UpdateNavMesh();
    }

    /// <summary>
    /// This method applies damage to the block.
    /// </summary>
    /// <param name="damage"></param>
    public void TakeDamage(int damage)
    {
        hitPoints -= damage;
        //Debug.Log("Parent_Block: Took " + damage + " damage, remaining hit points: " + hitPoints);

        if (hitPoints <= 0)
        {
            Break();
        }
    }

    /// <summary>
    /// This method handles the breaking of the block.
    /// </summary>
    public void Break()
    {
        //Debug.Log("Parent_Block: Breaking block " + gameObject.name);

        // Disable components before destroying to avoid issues during NavMesh update
        GetComponent<Collider>().enabled = false;
        GetComponent<Renderer>().enabled = false;
        GetComponent<NavMeshModifierVolume>().enabled = false;

        // Update the NavMesh to reflect the removed block
        NavMeshUtils.Instance.UpdateNavMesh();

        // Finally, destroy the block GameObject
        Destroy(gameObject);
    }
}
