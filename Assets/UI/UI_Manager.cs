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

    // TextMeshPro Variables
    [SerializeField] private GameObject GameUI_TextMeshPro;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Set UI Toolkit Vars
        healthLabel = UIDoc.rootVisualElement.Q<Label>("HealthLabel");
        healthBarMask = UIDoc.rootVisualElement.Q<VisualElement>("HealthBarMask");

        PlayerController.OnHealthChange += UpdateHealth;

        UpdateHealth();
    }

    // Update is called once per frame
    void Update()
    {
        //UpdateHealthText();
    }

    private void UpdateHealth()
    {
        float healthRatio = (float)PlayerController.Health.Cur / PlayerController.Health.Max;
        float healthPercent = Mathf.Lerp(0, 100, healthRatio);
        healthBarMask.style.width = Length.Percent(healthPercent);

        healthLabel.text = $"{PlayerController.Health.Cur}/{PlayerController.Health.Max}";
    }

    //private void UpdateHealthText()
    //{
    //    healthText.GetComponent<TextMeshProUGUI>().text = "Health: " + 
    //        playerObject.GetComponent<Resource_Health>().Cur.ToString() + " / " +
    //        playerObject.GetComponent<Resource_Health>().Max.ToString();
    //}
}
