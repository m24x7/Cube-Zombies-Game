using UnityEngine;

public class Controller_Game : MonoBehaviour
{
    [SerializeField] private GameObject PlayerPrefab;
    [SerializeField] private GameObject PlayerInstance;
    [SerializeField] private GameObject PlayerSpawnPoint;

    [SerializeField] private bool IsBuildEnabled = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (PlayerInstance == null)
        {
            PlayerInstance = Instantiate(PlayerPrefab, PlayerSpawnPoint.transform.position, Quaternion.identity);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }



    private void OnPlayerDeath()
    {

    }
}
