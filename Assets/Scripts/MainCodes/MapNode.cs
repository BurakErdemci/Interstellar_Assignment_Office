using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MapNode : MonoBehaviour
{
    [Header("UI Components")]
    public Image statusIcon;       
    public Slider timerSlider;     
    public Button nodeButton;      
    public Image expiryRing;       

    [Header("Visuals")]
    public Sprite iconAlert;       
    public Sprite iconCheck;       
    public Sprite iconWorking;     

    [Header("Data")]
    public MissionData missionData; 
    private AgentData assignedAgent; 
    
    private enum NodeState { Available, Traveling, InProgress, Completed }
    private NodeState currentState = NodeState.Available;

    [Header("Settings")]
    public float timeToDisappear = 15f; 
    private float currentExpiryTimer;

    private bool agentIsIncoming = false;

    public void Setup(MissionData mission)
    {
        missionData = mission;
        currentExpiryTimer = timeToDisappear;
        
        ChangeState(NodeState.Available);
        
        nodeButton.onClick.RemoveAllListeners();
        nodeButton.onClick.AddListener(OnNodeClicked);
    }

    void Update()
    {
        // 1. KİLİT KONTROLÜ
        if (MapManager.Instance.isInteractionLocked) return;

        // 2. NORMAL SAYIM
        if (currentState == NodeState.Available && !agentIsIncoming)
        {
            currentExpiryTimer -= Time.deltaTime;

            if (expiryRing != null)
                expiryRing.fillAmount = currentExpiryTimer / timeToDisappear;

            if (currentExpiryTimer <= 0)
            {
                HandleExpiration();
            }
        }
    }

    public void StopExpiration()
    {
        agentIsIncoming = true; 
        if(expiryRing != null) expiryRing.gameObject.SetActive(false); 
    }
    
    public void SetTraveling()
    {
        ChangeState(NodeState.Traveling);
    }

    void HandleExpiration()
    {
        nodeButton.interactable = false;
        MapManager.Instance.OnMissionExpired(this);
    }

    void OnNodeClicked()
    {
        if (MapManager.Instance.isInteractionLocked) return;

        switch (currentState)
        {
            case NodeState.Available:
                MapManager.Instance.OpenDetailPanel(this);
                break;
            case NodeState.Traveling: // Yoldayken tıklanmasın
            case NodeState.InProgress:
                Debug.Log("Ajan çalışıyor...");
                break;
            case NodeState.Completed:
                MapManager.Instance.ResolveMission(this, assignedAgent);
                break;
        }
    }

    public void StartTimer(AgentData agent)
    {
        assignedAgent = agent;
        ChangeState(NodeState.InProgress);
        StartCoroutine(TimerRoutine());
    }
    
    IEnumerator TimerRoutine()
    { float duration = 5f; 
        float timer = 0f;

        // YENİ: Akıllı Replik Çağırma
        if (assignedAgent != null)
        {
            // Ajan'a soruyoruz: "Bu görev tipi (missionData.category) için bir lafın var mı?"
            string quote = assignedAgent.GetWorkingQuote(missionData.category);

            // Eğer boş dönmediyse konuş
            if (quote != "...")
            {
                yield return new WaitForSeconds(1f);
                NotificationManager.Instance.ShowMessage(quote, assignedAgent.faceIcon);
            }
        }


        // SAYAÇ DÖNGÜSÜ
        while (timer < duration)
        {
            timer += Time.deltaTime;
            timerSlider.value = timer / duration; 
            yield return null;
        }

        ChangeState(NodeState.Completed);
    } 

    void ChangeState(NodeState newState)
    {
        currentState = newState;
        switch (newState)
        {
            case NodeState.Available:
                statusIcon.sprite = iconAlert;
                timerSlider.gameObject.SetActive(false);
                if(expiryRing != null) expiryRing.gameObject.SetActive(true);
                break;

            case NodeState.Traveling:
                if(expiryRing != null) expiryRing.gameObject.SetActive(false);
                break;

            case NodeState.InProgress:
                statusIcon.sprite = iconWorking;
                timerSlider.gameObject.SetActive(true);
                if(expiryRing != null) expiryRing.gameObject.SetActive(false);
                break;

            case NodeState.Completed:
                statusIcon.sprite = iconCheck;
                timerSlider.gameObject.SetActive(false);
                if(expiryRing != null) expiryRing.gameObject.SetActive(false);
                break;
        }
    }
}