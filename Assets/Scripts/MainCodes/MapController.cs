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

    // YENİ: Çark kapanana kadar bekleyecek ajanın bilgileri
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


    private void Awake() { Instance = this; }

    private void Start()
    {
        UpdateMoneyUI(true);
        detailPanel.SetActive(false);

        closeDetailButton.onClick.AddListener(() => 
        {
            // Önce küçült (0.3 saniyede), küçülme bitince (OnComplete) kapat.
            detailPanel.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack).OnComplete(() => 
            {
                detailPanel.SetActive(false);
                isInteractionLocked = false; 
            });
        });

        // --- ÇARKI KAPATMA BUTONU (GÜNCELLENDİ) ---
        closeWheelButton.onClick.AddListener(() => 
        {
            wheelController.CloseWheel();
            closeWheelButton.gameObject.SetActive(false);
            isInteractionLocked = false; 
            
            // YENİ: Eğer dönüş için bekleyen bir ajan varsa, ŞİMDİ yola çıksın
            if (agentWaitingToReturn != null)
            {
                ReturnAgentToBase(agentWaitingToReturn, returnStartPosition);
                // Veriyi temizle ki tekrar tekrar yollamasın
                agentWaitingToReturn = null;
            }

            CheckEndDayCondition();
        });

        StartCoroutine(MissionSpawnerRoutine());
    }
    
    IEnumerator MissionSpawnerRoutine()
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

    void UpdateMoneyUI(bool instant = false)
    {
        if (moneyText == null) return;

        if (instant)
        {
            // Oyun başında direkt yazsın
            displayedMoney = currentMoney;
            moneyText.text = "$ " + currentMoney;
        }
        else
        {
            // DOTween ile saydır
            // displayedMoney'den currentMoney'e, 1 saniyede say.
            // Her adımda (OnUpdate) texti güncelle.
            DOTween.To(() => displayedMoney, x => displayedMoney = x, currentMoney, 1f)
                .OnUpdate(() => moneyText.text = "$ " + displayedMoney)
                .SetEase(Ease.OutExpo); // Sona doğru yavaşlasın
            
            // Efekt: Para artıyorsa yeşil, azalıyorsa kırmızı parlasın
            if(currentMoney > displayedMoney) 
                moneyText.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
        }
    }

    public void SpawnNewMission(MissionData specificMission = null)
    {
        // ... (Bu kısım aynı, yer kaplamaması için kısalttım, eski kodun aynısı kalacak) ...
        // ... (Eğer istersen burayı da tekrar atarım ama değişmedi) ...
        
        // KISACA: Yer bulma ve spawn kodu aynen kalacak.
        List<MissionData> availablePool = new List<MissionData>();
        foreach (var mission in allMissions)
        {
            if (!activeMissionsOnMap.Contains(mission) && !completedMissionsToday.Contains(mission))
            {
                availablePool.Add(mission);
            }
        }
        if (availablePool.Count == 0) return;

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
            MissionData missionToAssign = availablePool[Random.Range(0, availablePool.Count)];
            obj.GetComponent<MapNode>().Setup(missionToAssign);
            activeMissionsOnMap.Add(missionToAssign);
        }
    }

    public void OpenDetailPanel(MapNode node)
    {
        // ... (Bu kısım da aynı kalacak) ...
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
            if (busyAgents.Contains(agent))
            {
                btn.interactable = false; 
                txt.text += " (Görevde)"; 
                txt.color = Color.red;    
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
        busyAgents.Add(agent);
        GameObject visualObj = Instantiate(agentVisualPrefab, mapContainer);
        visualObj.transform.localPosition = headquartersLocation.localPosition;
        MapAgentVisual visualScript = visualObj.GetComponent<MapAgentVisual>();
        visualScript.Setup(agent); 
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

        // Görev Pozisyonunu Kaydet
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

                // --- GÜNCELLENEN KISIM: HEMEN YOLLAMA, BEKLET ---
                // Eskiden: ReturnAgentToBase(...) diyorduk.
                // Şimdi:
                agentWaitingToReturn = agent;
                returnStartPosition = missionPosition;
                // Butona basınca (Start fonksiyonunda) yollayacağız.
            });
        }
        else if (node.missionData.type == MissionType.Crisis)
        {
            Destroy(node.gameObject);
            CrisisManager.Instance.StartCrisis(node.missionData);
            // Kriz görevlerinde ajanı ne zaman döndüreceğini CrisisManager'da yönetmen gerekir
            // veya burada manuel olarak busyAgents.Remove(agent) yapabilirsin.
            busyAgents.Remove(agent);
        }
    }

  
    public void OnMissionExpired(MapNode node)
    {
        Debug.Log("GÖREV SÜRESİ DOLDU! KAÇIRDIN!");

        // 1. Cezayı Kes
        currentMoney -= penaltyAmount; 
        UpdateMoneyUI();

        // 2. Aktif Listeden Sil (Artık haritada değil)
        if (activeMissionsOnMap.Contains(node.missionData))
        {
            activeMissionsOnMap.Remove(node.missionData);
        }

        // --- DÜZELTME BURADA ---
        // 3. "Tamamlananlar" Listesine Ekle (Kaçırılsa bile "Bitti" sayılır, tekrar gelmez)
        if (!completedMissionsToday.Contains(node.missionData))
        {
            completedMissionsToday.Add(node.missionData);
        }
        // -----------------------

        // 4. Görseli Yok Et
        Destroy(node.gameObject);
        
        // 5. Gün sonu kontrolü yap (Belki de bu son görevdi ve kaçırdın, gün bitmeli)
        CheckEndDayCondition();
    }
    void ReturnAgentToBase(AgentData agent, Vector3 startPos)
    {
        GameObject visualObj = Instantiate(agentVisualPrefab, mapContainer);
        visualObj.transform.localPosition = startPos;
        MapAgentVisual visualScript = visualObj.GetComponent<MapAgentVisual>();
        visualScript.Setup(agent);

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