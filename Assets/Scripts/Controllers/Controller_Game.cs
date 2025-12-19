using UnityEngine;

public class Controller_Game : MonoBehaviour
{
    [SerializeField] private GameObject PlayerPrefab;
    [SerializeField] private GameObject PlayerInstance;
    public GameObject GetPlayer() => PlayerInstance;

    [SerializeField] private GameObject PlayerSpawnPoint;

    //[SerializeField] private bool IsBuildEnabled = false;


    // Make it a singleton for easy access
    public static Controller_Game Instance { get; private set; }


    // Awake is called when the script instance is being loaded
    private void Awake()
    {
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (PlayerInstance == null)
        {
            PlayerInstance = Instantiate(PlayerPrefab, PlayerSpawnPoint.transform.position, Quaternion.identity);
        }
    }

    //// Update is called once per frame
    //void Update()
    //{
        
    //}



    //private void OnPlayerDeath()
    //{

    //}
}
