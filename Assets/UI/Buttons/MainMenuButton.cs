using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuButton : MonoBehaviour
{
    public void OnButtonClick()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
