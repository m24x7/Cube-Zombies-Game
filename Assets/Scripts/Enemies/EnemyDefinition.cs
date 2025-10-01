using UnityEngine;

[CreateAssetMenu(fileName = "Enemy_", menuName = "Enemies/EnemyDefinition")]
public class EnemyDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id = "zombie_basic";

    [Header("Prefab & Stats")]
    public GameObject prefab;         // Must have NavMeshAgent + EnemyHealth
    public int baseHealth = 100;
    public float baseSpeed = 3.0f;    // NavMeshAgent.speed baseline
    public int killReward = 50;       // Currency per kill
}
