using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    [Header("Agent Movement")]
    public Transform headquartersLocation; 
    public GameObject agentVisualPrefab;   
    
    private List<AgentData> busyAgents = new List<AgentData>();
    private AgentData agentWaitingToReturn;
    private Vector3 returnStartPosition;

    [Header("Map Elements")]
    public Transform mapContainer;
    public GameObject nodePrefab;
    
    [Header("UI Panels")]
    public GameObject detailPanel;      
    public TextMeshProUGUI missionTitle;
    public TextMeshProUGUI missionBrief;
    public Transform agentButtonContainer;
    public GameObject agentButtonPrefab;
    public Button closeDetailButton;

    [Header("Wheel System")]
    public WheelController wheelController; 
    public Button closeWheelButton;         

    [Header("Data")]
    public MissionData[] allMissions; 
    public AgentData[] allAgents;

    private List<MissionData> activeMissionsOnMap = new List<MissionData>();
    private List<MissionData> completedMissionsToday = new List<MissionData>();

    private MapNode currentSelectedNode; 
    public bool isInteractionLocked = false; 

    [Header("Economy")]
    public int currentMoney = 1000; 
    public TextMeshProUGUI moneyText;
    public int rewardAmount = 200;  
    public int penaltyAmount = 100; 
    private int displayedMoney = 1000; 
    
    [Header("Level System")]
    public LevelData currentLevelData;

    private void Awake() { Instance = this; }

    private void Start()
    {
        // 1. Hafızayı Yükle
        if (GameSession.isGameStarted)
        {
            currentMoney = GameSession.savedMoney;
            completedMissionsToday = new List<MissionData>(GameSession.savedCompletedMissions);
        }
        else
        {
            GameSession.savedMoney = currentMoney;
        }

        // 2. UI Hazırlığı
        UpdateMoneyUI(true);
        detailPanel.SetActive(false);

        // 3. Butonları Bağla
        closeDetailButton.onClick.AddListener(() => 
        {
            detailPanel.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack).OnComplete(() => 
            {
                detailPanel.SetActive(false);
                isInteractionLocked = false; 
            });
        });

        closeWheelButton.onClick.AddListener(() => 
        {
            wheelController.CloseWheel();
            closeWheelButton.gameObject.SetActive(false);
            isInteractionLocked = false; 
            
            if (agentWaitingToReturn != null)
            {
                ReturnAgentToBase(agentWaitingToReturn, returnStartPosition);
                agentWaitingToReturn = null;
            }

            CheckEndDayCondition();
        });
        
        // 4. Minigame Dönüş Kontrolü
        CheckMinigameReturn();

        // 5. Oyun Akışını Başlat (Eğer Level Data varsa senaryoyu oynat, yoksa rastgele)
        if (currentLevelData != null)
        {
            StartCoroutine(PlayLevelRoutine());
        }
        else
        {
            StartCoroutine(MissionSpawnerRoutine());
        }
    }
    
    // --- BU FONKSİYON ARTIK DIŞARIDA (DOĞRUSU BU) ---
    void CheckMinigameReturn()
    {
        if (GameSession.returningFromMinigame)
        {
            Debug.Log("Minigame'den dönüldü. Sonuç işleniyor...");

            AgentData agent = GameSession.activeAgent;
            bool isWin = GameSession.minigameWin;

            if (isWin)
            {
                currentMoney += rewardAmount * 2; 
                Debug.Log("KRİZ ÇÖZÜLDÜ! Ekstra Ödül.");
            }
            else
            {
                currentMoney -= penaltyAmount;
                Debug.Log("KRİZ YÖNETİLEMEDİ! Ceza.");
            }
            UpdateMoneyUI();

            if (agent != null && busyAgents.Contains(agent))
            {
                busyAgents.Remove(agent);
            }
            
            if (agent != null)
            {
                ReturnAgentToBase(agent, Vector3.zero); 
            }

            GameSession.SaveMapState(currentMoney, completedMissionsToday);
            GameSession.ClearMinigameData();
        }
    }

    IEnumerator MissionSpawnerRoutine() // Yedek Rastgele Sistem
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);
            yield return new WaitWhile(() => isInteractionLocked);
            
            if (completedMissionsToday.Count + activeMissionsOnMap.Count < allMissions.Length)
            {
                SpawnNewMission();
            }
        }
    }

    // --- SENARYO OYNATICISI ---
   IEnumerator PlayLevelRoutine()
    {
        if (currentLevelData == null)
        {
            Debug.LogError("HATA: Level Data atanmamış!");
            yield break;
        }

        foreach (var evt in currentLevelData.timeline)
        {
            // --- 1. ZAMANLAMA ÇÖZÜMÜ ---
            if (evt.waitDuration > 0)
            {
                yield return new WaitForSeconds(evt.waitDuration);
            }
            
            yield return new WaitWhile(() => isInteractionLocked);


            // --- 2. OLAYLARI OYNAT ---
            switch (evt.eventType)
            {
                case LevelEventType.Wait:
               
                    break;

                case LevelEventType.SpawnMission:
                    SpawnNewMission(evt.mission); 
                    break;

                case LevelEventType.StartDialogue:
                    bool dialogueFinished = false;
                    DialogueManager.Instance.StartDialogue(evt.dialogue, () => { dialogueFinished = true; });
                    yield return new WaitUntil(() => dialogueFinished);
                    break;
                
                case LevelEventType.EndDay:
                    Debug.Log("GÜNÜN SENARYOSU BİTTİ!");
                    yield break; 

                case LevelEventType.TriggerBanter:
                    // --- BANTER (LAF ATMA) MANTIĞI ---
                    AgentData speaker = evt.banterAgent;
                    string text = evt.banterText;
                    Sprite icon = null;

                    // Eğer asıl konuşmacı MEŞGULSE ve yedek varsa -> Yedeğe geç
                    if (busyAgents.Contains(evt.banterAgent) && evt.altBanterAgent != null)
                    {
                        speaker = evt.altBanterAgent;
                        text = evt.altBanterText;
                    }

                    // Konuşacak biri varsa (veya yedek devreye girdiyse)
                    if (speaker != null)
                    {
                        icon = speaker.faceIcon;
                        NotificationManager.Instance.ShowMessage(text, icon);
                    }
                    break;
            }
        }
    }
    void UpdateMoneyUI(bool instant = false)
    {
        if (moneyText == null) return;

        if (instant)
        {
            displayedMoney = currentMoney;
            moneyText.text = "$ " + currentMoney;
        }
        else
        {
            DOTween.To(() => displayedMoney, x => displayedMoney = x, currentMoney, 1f)
                .OnUpdate(() => moneyText.text = "$ " + displayedMoney)
                .SetEase(Ease.OutExpo);
            
            if(currentMoney > displayedMoney) 
                moneyText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
        }
    }

    public void SpawnNewMission(MissionData specificMission = null)
    {
        // 1. Uygun Görev Havuzu Oluştur
        List<MissionData> availablePool = new List<MissionData>();

        if (specificMission != null)
        {
            // Eğer senaryodan özel görev geldiyse onu kullan (Listede olsa bile)
            availablePool.Add(specificMission);
        }
        else
        {
            // Rastgele moddaysa, haritada olmayan ve bitmemişleri seç
            foreach (var mission in allMissions)
            {
                if (!activeMissionsOnMap.Contains(mission) && !completedMissionsToday.Contains(mission))
                {
                    availablePool.Add(mission);
                }
            }
        }

        if (availablePool.Count == 0) return;

        // 2. Yer Bul
        Vector3 spawnPos = Vector3.zero;
        bool validPositionFound = false;
        int attempts = 0;

        while (!validPositionFound && attempts < 20)
        {
            float x = Random.Range(-350f, 350f); 
            float y = Random.Range(-200f, 200f);
            Vector3 potentialPos = new Vector3(x, y, 0);

            bool tooClose = false;
            foreach (Transform child in mapContainer)
            {
                if (Vector3.Distance(child.localPosition, potentialPos) < 150f)
                {
                    tooClose = true;
                    break;
                }
            }
            if (!tooClose)
            {
                spawnPos = potentialPos;
                validPositionFound = true;
            }
            attempts++;
        }

        if (validPositionFound)
        {
            GameObject obj = Instantiate(nodePrefab, mapContainer);
            obj.transform.localPosition = spawnPos;

            // Havuzdan (veya özelden) seç
            MissionData missionToAssign = availablePool[Random.Range(0, availablePool.Count)];

            obj.GetComponent<MapNode>().Setup(missionToAssign);
            activeMissionsOnMap.Add(missionToAssign);
        }
    }

    public void OpenDetailPanel(MapNode node)
    {
        if (isInteractionLocked) return;
        isInteractionLocked = true;
        currentSelectedNode = node;
        detailPanel.SetActive(true);
        detailPanel.transform.localScale = Vector3.zero;
        detailPanel.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
        
        missionTitle.text = node.missionData.missionTitle;
        missionBrief.text = node.missionData.missionBrief;
        
        foreach (Transform child in agentButtonContainer) Destroy(child.gameObject);
        foreach (var agent in allAgents)
        {
            GameObject btnObj = Instantiate(agentButtonPrefab, agentButtonContainer);
            Button btn = btnObj.GetComponent<Button>();
            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            txt.text = agent.agentName;

            bool isRestricted = false;
            if (node.missionData.specificAgent != null && node.missionData.specificAgent != agent)
            {
                isRestricted = true;
            }

            if (busyAgents.Contains(agent))
            {
                btn.interactable = false; 
                txt.text += " (Görevde)"; 
                txt.color = Color.red;    
            }
            else if (isRestricted)
            {
                btn.interactable = false;
                txt.text += " (Uygun Değil)";
                txt.color = Color.gray; 
            }
            else
            {
                btn.onClick.AddListener(() => 
                {
                    StartAgentTravelSequence(node, agent);
                    detailPanel.SetActive(false);
                    isInteractionLocked = false;
                });
            }
        }
    }

    void StartAgentTravelSequence(MapNode targetNode, AgentData agent)
    {
        targetNode.StopExpiration(); 
        targetNode.SetTraveling();   
        
        busyAgents.Add(agent);
        GameObject visualObj = Instantiate(agentVisualPrefab, mapContainer);
        visualObj.transform.localPosition = headquartersLocation.localPosition;
        
        MapAgentVisual visualScript = visualObj.GetComponent<MapAgentVisual>();
        visualScript.Setup(agent, false); 
        
        visualScript.MoveTo(targetNode.transform.localPosition, () => 
        {
            Destroy(visualObj); 
            if (targetNode != null)
            {
                targetNode.StartTimer(agent);
            }
        });
    }

    public void ResolveMission(MapNode node, AgentData agent)
    {
        if (isInteractionLocked) return;
        isInteractionLocked = true;

        if (activeMissionsOnMap.Contains(node.missionData)) activeMissionsOnMap.Remove(node.missionData);
        if (!completedMissionsToday.Contains(node.missionData)) completedMissionsToday.Add(node.missionData);

        Vector3 missionPosition = node.transform.localPosition;

        if (node.missionData.type == MissionType.Routine)
        {
            MissionResult result = MissionResolver.Instance.ResolveMission(agent, node.missionData);
            
            wheelController.StartSpin(result.successChance, result.isSuccess, () => 
            {
                Camera.main.transform.DOShakePosition(0.5f, 0.5f, 10, 90);
                if (result.isSuccess) currentMoney += rewardAmount;
                else currentMoney -= penaltyAmount;
                
                UpdateMoneyUI();
                wheelController.resultText.text = result.resultText;
                closeWheelButton.gameObject.SetActive(true);
                
                Destroy(node.gameObject);

                agentWaitingToReturn = agent;
                returnStartPosition = missionPosition;
            });
        }
        else if (node.missionData.type == MissionType.Crisis)
        {
            Destroy(node.gameObject);
            busyAgents.Remove(agent); 
            
            if (!completedMissionsToday.Contains(node.missionData))
            {
                completedMissionsToday.Add(node.missionData);
            }
            GameSession.SaveMapState(currentMoney, completedMissionsToday);
            
            CrisisManager.Instance.StartCrisis(node.missionData, agent);
        }
    }

    public void OnMissionExpired(MapNode node)
    {
        Debug.Log("GÖREV SÜRESİ DOLDU! KAÇIRDIN!");
        currentMoney -= penaltyAmount; 
        UpdateMoneyUI();

        if (activeMissionsOnMap.Contains(node.missionData)) activeMissionsOnMap.Remove(node.missionData);
        if (!completedMissionsToday.Contains(node.missionData)) completedMissionsToday.Add(node.missionData);

        Destroy(node.gameObject);
        CheckEndDayCondition();
    }

    void ReturnAgentToBase(AgentData agent, Vector3 startPos)
    {
        GameObject visualObj = Instantiate(agentVisualPrefab, mapContainer);
        visualObj.transform.localPosition = startPos;
        MapAgentVisual visualScript = visualObj.GetComponent<MapAgentVisual>();
        visualScript.Setup(agent, true);

        visualScript.MoveTo(headquartersLocation.localPosition, () => 
        {
            Destroy(visualObj);
            if (busyAgents.Contains(agent)) busyAgents.Remove(agent);
            Debug.Log(agent.agentName + " üsse döndü ve hazır.");
        });
    }

    void CheckEndDayCondition()
    {
        if (completedMissionsToday.Count >= allMissions.Length)
        {
            Debug.Log($"<color=green><b>GÜN BİTTİ!</b></color>");
        }
    }
}