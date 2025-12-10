using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatRowUI : MonoBehaviour
{
    public TextMeshProUGUI statNameText;
    public TextMeshProUGUI statValueText;
    public Button plusButton;

    private AgentData currentAgent;
    private StatType myStatType;

    public void Setup(AgentData agent, StatType type)
    {
        currentAgent = agent;
        myStatType = type;

        statNameText.text = type.ToString();
        UpdateVisuals();

        // Butona basınca ne olacağını ayarla
        plusButton.onClick.RemoveAllListeners();
        plusButton.onClick.AddListener(() => 
        {
            agent.UpgradeStat(type);
            UpdateVisuals();
            // Parent'a (Manager'a) haber verip puan yazısını güncelletebiliriz
            CharacterProfileManager.Instance.UpdatePointsUI();
        });
    }

    public void UpdateVisuals()
    {
        int val = currentAgent.GetSkillValue(myStatType);
        statValueText.text = val.ToString();

        // Eğer puan yoksa veya stat 10 olduysa butonu kapat
        if (currentAgent.availableStatPoints > 0 && val < 10)
        {
            plusButton.interactable = true;
        }
        else
        {
            plusButton.interactable = false;
        }
    }
}