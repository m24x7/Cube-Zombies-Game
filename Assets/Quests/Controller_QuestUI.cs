using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Controller_QuestUI : MonoBehaviour
{
    [SerializeField] private GameObject questUIPrefab;
    [SerializeField] private Transform questUIParent;
    [SerializeField] private GameObject questLogPanel;
    [SerializeField] private GameObject questDetailsPanel;

    [SerializeField] private List<GameObject> quests;
    public List<GameObject> Quests { get { return quests; } set { quests = value; } }

    [SerializeField] private int maxQuests = 3;
    public int MaxQuests { get { return maxQuests; } }

    private void Start()
    {
        AddQuest(ObjectiveType.Survive);
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.timeScale == 0f) return;

        if (quests.Count <= 0)
        {
            questDetailsPanel.SetActive(false);
        }
        else// if (!questDetailsPanel.activeSelf)
        {
            questDetailsPanel.SetActive(true);
        }
    }

    public void AddQuest(ObjectiveType type)
    {
        if (quests.Count >= maxQuests)
        {
            Debug.Log("Cannot add more quests. Maximum limit reached.");
            return;
        }
        GameObject questUIInstance = Instantiate(questUIPrefab, questDetailsPanel.transform);
        Quest questComponent = questUIInstance.GetComponent<Quest>();
        questComponent.Type = type;
        if (questComponent != null)
        {
            questComponent.UpdateText();
            quests.Add(questUIInstance);
        }
        else
        {
            Debug.LogError("The questUIPrefab does not have a Quest component.");
        }
    }
}
