using UnityEngine;

/// <summary>
/// This enum represents the different states the enemy agent can be in.
/// </summary>
public enum EnemyState
{
    Search,
    Chase,
    Attack
}

[RequireComponent(typeof(I_EnemyAgent))]
public class EnemyDecisionMaking : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
