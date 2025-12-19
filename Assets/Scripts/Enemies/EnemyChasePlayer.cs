using UnityEngine;
using UnityEngine.AI;

public class EnemyChasePlayer : MonoBehaviour
{
    [SerializeField] private Transform target; // assign Player
    [SerializeField] private float repathInterval = 0.2f;

    private NavMeshAgent agent;
    private float timer;

    void Awake() => agent = GetComponent<NavMeshAgent>();

    public void SetTarget(Transform t) => target = t;

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
