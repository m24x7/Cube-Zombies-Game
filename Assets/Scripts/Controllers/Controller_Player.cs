using System;
using UnityEngine;

public class Controller_Player : Parent_Entity
{
    //[SerializeField] private Controller_Build buildController;


    public Action OnHealthChange;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    override public void Heal(int heal)
    {
        base.Heal(heal);

        OnHealthChange?.Invoke();
    }

    override public void TakeDamage(int damage)
    {
        base.TakeDamage(damage);

        OnHealthChange?.Invoke();
    }

    override public void Die()
    {
        Debug.Log("Player Died");
        // Add death logic here (e.g., respawn, game over screen, etc.)
    }
}
