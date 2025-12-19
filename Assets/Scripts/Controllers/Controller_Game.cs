using UnityEngine;

/// <summary>
/// This class manages the overall game state
/// </summary>
public class Controller_Game : MonoBehaviour
{
    /// Player references
    [SerializeField] private GameObject PlayerPrefab; // The player prefab to instantiate
    [SerializeField] private GameObject PlayerInstance; // The instantiated player
    public GameObject GetPlayer() => PlayerInstance; // Public getter for the player instance
    [SerializeField] private GameObject PlayerSpawnPoint; // The spawn point for the player

    //[SerializeField] private bool IsBuildEnabled = false;


    // Make it a singleton for easy access
    public static Controller_Game Instance { get; private set; }


    /// <summary>
    /// Awake is called when the script instance is being loaded
    /// </summary>
    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
        }
    }

    /// <summary>
    /// Start is called once before the first execution of Update after the MonoBehaviour is created
    /// </summary>
    void Start()
    {
        // Instantiate the player at the spawn point
        if (PlayerInstance == null)
        {
            PlayerInstance = Instantiate(PlayerPrefab, PlayerSpawnPoint.transform.position, Quaternion.identity);
        }
    }
}
