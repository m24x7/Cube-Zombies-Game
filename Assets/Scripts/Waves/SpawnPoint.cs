using UnityEngine;
using UnityEngine.AI;

public class SpawnPoint : MonoBehaviour
{
    [SerializeField, Min(0f)] private float sampleRadius = 2f;
    [SerializeField, Range(0f, 10f)] private float weight = 1f;
    [SerializeField] private bool isActive = true;

    public float Weight => isActive ? weight : 0f;

    public bool TryGetSpawnPosition(out Vector3 pos)
    {
        if (NavMesh.SamplePosition(transform.position, out var hit, sampleRadius, NavMesh.AllAreas))
        {
            pos = hit.position;
            return true;
        }
        pos = transform.position;
        return false;
    }

    public void SetActive(bool active) => isActive = active;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isActive ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, sampleRadius);
    }
}
