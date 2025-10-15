using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuButton : MonoBehaviour
{
    public void OnButtonClick()
    {
        if (SceneManager.GetSceneByName("TestScene").isLoaded) { GameObject.FindGameObjectWithTag("GameUI").GetComponent<UI_Manager>().ToggleInstructions(); return; }
        SceneManager.LoadScene("MainMenu");
    }
}
