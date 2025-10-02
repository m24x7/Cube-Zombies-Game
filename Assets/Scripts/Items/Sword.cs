using UnityEngine;

public class Sword : MonoBehaviour
{
    [SerializeField] private int damage = 25;
    public int Damage { get { return damage; } }

    [SerializeField] private float range = 1.5f;
    public float Range { get { return range; } }

    [SerializeField] private float swingSpeed = 20f;
    public float SwingSpeed { get { return swingSpeed; } }
}
