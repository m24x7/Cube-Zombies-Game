using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(I_EnemyAgent))]
public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float repathInterval = 0.2f;

    private NavMeshAgent agent;
    private float timer;

    void Awake() => agent = GetComponent<NavMeshAgent>();

    public void SetTarget(Transform t) => target = t;

    /// <summary>
    /// Update is called once per frame
    /// </summary>
    void Update()
    {
        if (Time.timeScale == 0f) return;
        if (!target || !agent) return;

        timer += Time.deltaTime;
        if (timer >= repathInterval)
        {
            timer = 0f;
            agent.SetDestination(target.position);
        }
    }
}
