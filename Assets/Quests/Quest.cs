using TMPro;
using UnityEngine;

public enum ObjectiveType
{
    Kill,
    Collect,
    Explore,
    Survive
}

public class Quest : MonoBehaviour
{
    [SerializeField] private GameObject QuestPanel;
    [SerializeField] private TextMeshProUGUI Text;
    [SerializeField] private ObjectiveType objectiveType;
    public ObjectiveType Type { get { return objectiveType; } set { objectiveType = value; } }

    [SerializeField] private int objectiveCount = 0;
    [SerializeField] private int currentCount = 0;

    //[SerializeField] private bool isCompleted = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateText();
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.timeScale == 0f) return;

        UpdateText();
    }

    public void UpdateText()
    {
        switch (objectiveType)
        {
            case ObjectiveType.Kill:
                Text.text = "Objective: " + currentCount + " / " + objectiveCount;
                break;
            case ObjectiveType.Collect:
                Text.text = "Objective: Collect 5 items";
                break;
            case ObjectiveType.Explore:
                Text.text = "Objective: Explore the area";
                break;
            case ObjectiveType.Survive:
                Text.text = "Objective: Survive";
                break;
            default:
                Text.text = "Objective: Unknown";
                break;
        }
    }

    public void CompleteObjective()
    {
        //isCompleted = true;


        Controller_QuestUI Controller = gameObject.GetComponentsInParent<Controller_QuestUI>()[0];
        int index = 0;
        foreach (GameObject quest in Controller.Quests)
        {
            if (quest == gameObject)
            {
                Controller.Quests.RemoveAt(index);
                Destroy(gameObject);
                return;
            }
            index++;
        }
    }
}
