using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class UI_Manager : MonoBehaviour
{
    [SerializeField] private Controller_Player PlayerController;

    // UI Toolkit Variables
    [SerializeField] private GameObject GameUI_UIToolKit;
    [SerializeField] private UIDocument UIDoc;
    private Label healthLabel;
    private VisualElement healthBarMask;
    private Label scoreLabel;

    private VisualElement pauseMenu;
    private Button resumeButton;
    private Button mainMenuButton;
    private Button quitButton;

    // Main Menu UI Toolkit Variables
    [SerializeField] private GameObject MainMenuUI_UIToolKit;
    [SerializeField] private UIDocument MainMenuUIDoc;
    private Button playButton;
    private Button instructionsButton;
    private Button gradingButton;
    private Button creditsButton;

    // End Game UI Toolkit Variables
    [SerializeField] private GameObject EndGameUI_UIToolKit;
    [SerializeField] private UIDocument EndGameUIDoc;
    private VisualElement endBG;
    private VisualElement endMenu;
    private VisualElement stats;
    private Button restartButton;
    private Button endMainMenuButton;
    private Button endQuitButton;
    private Label endText;
    private Label waveText;
    private Label timeText;
    private Label scoreText;
    private Label healthLost;
    private Label healthRank;

    private float secondsSurvived = 0f;

    // TextMeshPro Variables
    [SerializeField] private GameObject GameUI_TextMeshPro;
    [SerializeField] private TextMeshProUGUI WaveTMP;
    [SerializeField] private GameObject Hotbar;
    private List<GameObject> hotbarSlots = new List<GameObject>();
    private int slots;
    private int selectedSlot = 0;

    [SerializeField] private Controller_WaveSystem waveSystem;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (MainMenuUIDoc != null)
        {
            // Set UI Toolkit Vars
            playButton = MainMenuUIDoc.rootVisualElement.Q<Button>("Play");
            instructionsButton = MainMenuUIDoc.rootVisualElement.Q<Button>("Instructions");
            gradingButton = MainMenuUIDoc.rootVisualElement.Q<Button>("Grading");
            creditsButton = MainMenuUIDoc.rootVisualElement.Q<Button>("Credits");
            quitButton = MainMenuUIDoc.rootVisualElement.Q<Button>("Quit");


            playButton.clicked += () => SceneManager.LoadScene("TestScene");
            instructionsButton.clicked += () => SceneManager.LoadScene("InstructionsScene");
            gradingButton.clicked += () => SceneManager.LoadScene("GradingScene");
            creditsButton.clicked += () => SceneManager.LoadScene("CreditsScene");
            quitButton.clicked += QuitButton;

            UnityEngine.Cursor.visible = true;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
        }

        if (UIDoc != null)
        {
            // Set UI Toolkit Vars
            healthLabel = UIDoc.rootVisualElement.Q<Label>("HealthLabel");
            healthBarMask = UIDoc.rootVisualElement.Q<VisualElement>("HealthBarMask");
            scoreLabel = UIDoc.rootVisualElement.Q<Label>("ScoreLabel");

            pauseMenu = UIDoc.rootVisualElement.Q<VisualElement>("PauseMenu");
            resumeButton = UIDoc.rootVisualElement.Q<Button>("Resume");
            mainMenuButton = UIDoc.rootVisualElement.Q<Button>("MainMenu");
            quitButton = UIDoc.rootVisualElement.Q<Button>("Quit");

            resumeButton.clicked += ResumeButton;
            mainMenuButton.clicked += MainMenuButton;
            quitButton.clicked += QuitButton;

            if (PlayerController != null)
            {
                PlayerController.OnHealthChange += UpdateHealth;
                PlayerController.playerDie += ToggleLoseScreen;
            }

            if (waveSystem != null)
            {
                waveSystem.noMoreWaves += ToggleWinScreen;
            }


            pauseMenu.SetEnabled(false);
            pauseMenu.style.opacity = 0;

            UpdateHealth();
            UpdateScore();

            Time.timeScale = 1f;

            UnityEngine.Cursor.visible = false;
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        }

        if (EndGameUIDoc != null)
        {
            // Set UI Toolkit Vars
            endBG = EndGameUIDoc.rootVisualElement.Q<VisualElement>("BG");
            endMenu = EndGameUIDoc.rootVisualElement.Q<VisualElement>("EndMenu");
            stats = EndGameUIDoc.rootVisualElement.Q<VisualElement>("Stats");

            restartButton = EndGameUIDoc.rootVisualElement.Q<Button>("Replay");
            endMainMenuButton = EndGameUIDoc.rootVisualElement.Q<Button>("MainMenu");
            endQuitButton = EndGameUIDoc.rootVisualElement.Q<Button>("Quit");

            endText = EndGameUIDoc.rootVisualElement.Q<Label>("EndText");
            waveText = EndGameUIDoc.rootVisualElement.Q<Label>("WavesSurvived");
            timeText = EndGameUIDoc.rootVisualElement.Q<Label>("TimeSurvived");
            scoreText = EndGameUIDoc.rootVisualElement.Q<Label>("ScoreText");
            healthLost = EndGameUIDoc.rootVisualElement.Q<Label>("HealthLost");
            healthRank = EndGameUIDoc.rootVisualElement.Q<Label>("HealthRankText");

            restartButton.clicked += RestartScene;
            endMainMenuButton.clicked += MainMenuButton;
            endQuitButton.clicked += QuitButton;

            endBG.SetEnabled(false);
            endBG.style.opacity = 0f;

            //endMenu.SetEnabled(false);
            //endMenu.style.opacity = 0f;

            stats.SetEnabled(false);
            stats.style.opacity = 0f;
        }

        if (WaveTMP != null) UpdateWave();
        if (Hotbar != null)
        {
            slots = Hotbar.transform.childCount;
            Debug.Log($"Hotbar Slots: {slots}");

            for (int i = 0; i < slots; i++)
            {
                hotbarSlots.Add(Hotbar.transform.GetChild(i).gameObject);
            }

            SelectHotbarSlot(0);
        }

        //UnityEngine.Cursor.visible = false;
        //UnityEngine.Cursor.lockState = CursorLockMode.Locked;
    }

    private void UpdateHealth()
    {
        float healthRatio = (float)PlayerController.Health.Cur / PlayerController.Health.Max;
        float healthPercent = Mathf.Lerp(0, 100, healthRatio);
        healthBarMask.style.width = Length.Percent(healthPercent);

        healthLabel.text = $"{PlayerController.Health.Cur}/{PlayerController.Health.Max}";
    }
    public void UpdateScore()
    {
        scoreLabel.text = $"Score: {PlayerController.Points}";
    }
    public void UpdateWave()
    {
        WaveTMP.text = $"Waves Left: {waveSystem.WavesRemaining}";
    }

    public void TogglePauseMenu()
    {
        if (endMenu.enabledInHierarchy) return;

        if (pauseMenu.enabledInHierarchy)
        {
            pauseMenu.SetEnabled(false);
            pauseMenu.style.opacity = 0f;

            Time.timeScale = 1f;

            UnityEngine.Cursor.visible = false;
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            pauseMenu.SetEnabled(true);
            pauseMenu.style.opacity = 1f;

            Time.timeScale = 0f;

            UnityEngine.Cursor.visible = true;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
        }
    }

    public void ToggleEndScreen(bool win)
    {
        endBG.SetEnabled(true);
        endBG.style.opacity = 1f;

        //endMenu.SetEnabled(true);
        //endMenu.style.opacity = 1f;

        stats.SetEnabled(true);
        stats.style.opacity = 1f;

        EndGameUIDoc.sortingOrder = 10;

        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;

        if (win)
        {
            endText.text = "You Win!";
        }
        else
        {
            endText.text = "You Lose!";
        }

        waveText.text = $"Waves Survived: {waveSystem.CurrentWaveNumber - 1} out of {waveSystem.TotalWavesDefined}";
        timeText.text = $"Time Survived: {Mathf.FloorToInt(secondsSurvived / 60)}m {Mathf.FloorToInt(secondsSurvived % 60)}s";
        scoreText.text = $"Final Score: {PlayerController.Points} out of {waveSystem.TotalPossiblePoints}";
        healthLost.text = $"Health Lost: {PlayerController.HealthLost}";
        if (PlayerController.HealthLost <= 20) healthRank.text = $"Evasion Mastery: Grand Master";
        else if (PlayerController.HealthLost <= 50) healthRank.text = $"Evasion Mastery: Master";
        else if (PlayerController.HealthLost <= 80) healthRank.text = $"Evasion Mastery: Adept";
        else healthRank.text = $"Evasion Mastery: Novice";

        Time.timeScale = 0f;

    }

    public void ToggleWinScreen()
    {
        ToggleEndScreen(true);
    }
    public void ToggleLoseScreen()
    {
        ToggleEndScreen(false);
    }

    public void ToggleInstructions()
    {
        ////bool isInstructionsOpen = SceneManager.GetSceneByName("InstructionsScene").isLoaded;
        ////if (isInstructionsOpen)
        ////{
        ////    SceneManager.UnloadSceneAsync("InstructionsScene");

        ////    Time.timeScale = 1f;

        ////    UnityEngine.Cursor.visible = false;
        ////    UnityEngine.Cursor.lockState = CursorLockMode.Locked;

        ////    return;
        ////}
        ////else
        ////{
        ////    SceneManager.LoadScene("InstructionsScene", LoadSceneMode.Additive);
        ////    Time.timeScale = 0f;

        ////    UnityEngine.Cursor.visible = true;
        ////    UnityEngine.Cursor.lockState = CursorLockMode.None;

        ////    return;
        ////}

        var instructionsUI = GameObject.FindGameObjectWithTag("InstructionsUI");

        if (instructionsUI != null)
        {
            Time.timeScale = 1f;

            UnityEngine.Cursor.visible = false;
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            Destroy(instructionsUI);
            return;
        }
        else
        {
            Time.timeScale = 0f;
            UnityEngine.Cursor.visible = true;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            var instructions = Instantiate(Resources.Load("Instructions"));
            instructions.GetComponent<Canvas>().sortingOrder = 20;
            return;
        }
    }

    public void SelectHotbarSlot(int slot)
    {
        //Debug.Log($"Selecting Hotbar Slot: {slot}");
        if (slot < 0 || slot >= slots) return;
        hotbarSlots[selectedSlot].GetComponent<UnityEngine.UI.Image>().color = new Vector4(0, 0, 0, 100f / 255f);
        if (hotbarSlots[selectedSlot].GetComponent<UnityEngine.UI.Image>().sprite == null) hotbarSlots[selectedSlot].GetComponent<UnityEngine.UI.Image>().color = Color.clear;
        selectedSlot = slot;
        hotbarSlots[selectedSlot].GetComponent<UnityEngine.UI.Image>().color = Color.yellow;
        if (hotbarSlots[selectedSlot].GetComponent<UnityEngine.UI.Image>().sprite == null) hotbarSlots[selectedSlot].GetComponent<UnityEngine.UI.Image>().color = Color.clear;
    }

    public void UpdateHotbarSlots()
    {
        for (int i = 0; i < slots; i++)
        {
            if (i >= PlayerController.Inventory.InventoryItems.Count) break;
            var item = PlayerController.Inventory.InventoryItems[i];
            if (item != null)
            {
                hotbarSlots[i].transform.Find("Image").GetComponent<UnityEngine.UI.Image>().sprite = item.GetComponent<I_Item>().Icon;
                hotbarSlots[i].transform.Find("Image").GetComponent<UnityEngine.UI.Image>().color = Color.white;

                if (i == selectedSlot)
                {
                    hotbarSlots[i].GetComponent<UnityEngine.UI.Image>().color = Color.yellow;
                }
                else
                {
                    hotbarSlots[i].GetComponent<UnityEngine.UI.Image>().color = new Vector4(0, 0, 0, 100f/255f);
                }
            }
            else
            {
                hotbarSlots[i].GetComponent<UnityEngine.UI.Image>().sprite = null;
                hotbarSlots[i].GetComponent<UnityEngine.UI.Image>().color = Color.clear;
            }
        }
    }

    private void ResumeButton()
    {
        TogglePauseMenu();
    }
    private void MainMenuButton()
    {
        SceneManager.LoadScene("MainMenu");
    }
    private void QuitButton()
    {
        Application.Quit();
    }

    private void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    private void FixedUpdate()
    {
        secondsSurvived += Time.fixedDeltaTime;
    }
}
