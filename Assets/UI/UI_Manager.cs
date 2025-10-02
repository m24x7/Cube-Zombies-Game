using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
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
        if (GameUI_UIToolKit != null)
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

            PlayerController.OnHealthChange += UpdateHealth;


            pauseMenu.SetEnabled(false);
            pauseMenu.style.opacity = 0;

            UpdateHealth();
            UpdateScore();
            UpdateWave();
        }

        if (Hotbar != null)
        {
            slots = Hotbar.transform.childCount;

            for (int i = 0; i < slots; i++)
            {
                hotbarSlots.Add(Hotbar.transform.GetChild(i).gameObject);
            }

            SelectHotbarSlot(0);
        }
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
        if (pauseMenu.enabledInHierarchy)
        {
            pauseMenu.SetEnabled(false);
            pauseMenu.style.opacity = 0;

            Time.timeScale = 1f;

            UnityEngine.Cursor.visible = false;
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            pauseMenu.SetEnabled(true);
            pauseMenu.style.opacity = 0.5f;

            Time.timeScale = 0f;

            UnityEngine.Cursor.visible = true;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
        }
    }

    public void SelectHotbarSlot(int slot)
    {
        if (slot < 0 || slot >= slots) return;
        hotbarSlots[selectedSlot].GetComponent<UnityEngine.UI.Image>().color = Color.white;
        selectedSlot = slot;
        hotbarSlots[selectedSlot].GetComponent<UnityEngine.UI.Image>().color = Color.yellow;
    }

    private void ResumeButton()
    {
        TogglePauseMenu();
    }
    private void MainMenuButton()
    {
        // Load Main Menu Scene
    }
    private void QuitButton()
    {
               Application.Quit();
    }
}
