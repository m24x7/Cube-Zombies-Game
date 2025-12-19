using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyController))]
public class EnemyMovement : MonoBehaviour
{
    private EnemyController enemyAI;

    
    [SerializeField] private float repathInterval = 0.2f;
    private float timer;

    /// <summary>
    /// Start is called once before the first execution of Update after the MonoBehaviour is created
    /// </summary>
    void Start()
    {
        enemyAI = GetComponent<EnemyController>();
    }

    /// <summary>
    /// Update is called once per frame
    /// </summary>
    void Update()
    {
        if (Time.timeScale == 0f) return;
        if (!enemyAI.Target || !enemyAI.Agent) return;

        timer += Time.deltaTime;
        if (timer >= repathInterval)
        {
            timer = 0f;
            enemyAI.Agent.SetDestination(enemyAI.Target.position);
        }
    }
}
