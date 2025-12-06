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
    
    private enum NodeState { Available, InProgress,Traveling, Completed }
    private NodeState currentState = NodeState.Available;

    // DEĞİŞİKLİK: Artık Inspector'dan süreyi 10, 20, 30 diye elle girebilirsin.
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
  
    public void SetTraveling()
    {
        ChangeState(NodeState.Traveling);
    }

    void Update()
    {
        // 1. KİLİT KONTROLÜ GERİ GELDİ
        // Eğer MapManager kilitliyse (Panel açıksa, Çark dönüyorsa) -> SAYMA, DUR.
        if (MapManager.Instance.isInteractionLocked) return;

        // 2. NORMAL SAYIM
        // Müsaitse VE Ajan yolda değilse say
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

    // ... (Diğer fonksiyonlar: HandleExpiration, OnNodeClicked, StartTimer, StopExpiration AYNI KALSIN) ...
    // ... Sadece Update ve değişken kısmı değişti ...
    
    // Eksik kalmasın diye kopyalaman için diğer fonksiyonları kısaca yazıyorum:
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
            case NodeState.Available: MapManager.Instance.OpenDetailPanel(this); break;
            case NodeState.InProgress: Debug.Log("Ajan çalışıyor..."); break;
            case NodeState.Completed: MapManager.Instance.ResolveMission(this, assignedAgent); break;
        }
    }

    public void StopExpiration()
    {
        agentIsIncoming = true; 
        if(expiryRing != null) expiryRing.gameObject.SetActive(false); 
    }

    public void StartTimer(AgentData agent)
    {
        assignedAgent = agent;
        ChangeState(NodeState.InProgress);
        StartCoroutine(TimerRoutine());
    }

    IEnumerator TimerRoutine()
    {
        float duration = 5f; 
        float timer = 0f;
        while (timer < duration) { timer += Time.deltaTime; timerSlider.value = timer / duration; yield return null; }
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

            // --- BU KISMI EKLE ---
            case NodeState.Traveling:
                // Ajan yolda, sayaç durmalı, halka gizlenmeli
                if(expiryRing != null) expiryRing.gameObject.SetActive(false);
                break;
            // ---------------------

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