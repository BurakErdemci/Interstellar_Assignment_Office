using UnityEngine;
using TMPro;
using UnityEngine.UI;
//Bu kodu şu an kullanmıyoruz ileride oyunun genel kontrolünü buraya yazıcaz
public class GameManager : MonoBehaviour
{
    [Header("Data")]
    public AgentData[] availableAgents;
    public MissionData[] availableMissions;

    [Header("UI Connections")]
    public TextMeshProUGUI missionText;
    public TextMeshProUGUI resultText;
    public GameObject agentButtonPrefab;
    public Transform buttonContainer;
    public Button nextMissionButton; 
    private MissionData currentMission;
    public WheelController wheelController; 

    private void Start()
    {
     
        nextMissionButton.onClick.AddListener(LoadNewMission);
        
        LoadNewMission();
    }

    public void LoadNewMission()
    {
        wheelController.CloseWheel(); 

     
        nextMissionButton.gameObject.SetActive(false);
        // 1. Ekranı Temizle
        nextMissionButton.gameObject.SetActive(false);
        resultText.text = "AJAN SEÇİMİ BEKLENİYOR...";

        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }



        MissionData newMission;

        // Eğer listede 1'den fazla görev varsa, aynısını seçmemeye çalış
        if (availableMissions.Length > 1)
        {
            do
            {
                // Rastgele bir tane seç
                newMission = availableMissions[Random.Range(0, availableMissions.Length)];

                // Eğer az önceki görevle aynıysa döngü başa döner, tekrar seçer
            } while (newMission == currentMission);
        }
        else
        {
            // Listede tek görev varsa mecburen onu seç
            newMission = availableMissions[0];
        }

        currentMission = newMission;

        // ------------------------------------------

        missionText.text = $"GÖREV: {currentMission.missionTitle}\n\n" +
                           $"{currentMission.missionBrief}\n" +
                           $"Zorluk: {currentMission.difficultyLevel}";

        CreateAgentButtons();
    }

    void CreateAgentButtons()
    {
        foreach (var agent in availableAgents)
        {
            GameObject btn = Instantiate(agentButtonPrefab, buttonContainer);
            
            // Butonun üzerindeki yazıyı ayarla
            btn.GetComponentInChildren<TextMeshProUGUI>().text = agent.agentName;
            
            // Butona tıklanma özelliği ekle
            btn.GetComponent<Button>().onClick.AddListener(() => OnAgentSelected(agent));
        }
    }

    void OnAgentSelected(AgentData agent)
    {
        // 1. Hesaplama Yap (Sonuç ve Şans belirlendi)
        MissionResult result = MissionResolver.Instance.ResolveMission(agent, currentMission);

        // 2. Butonları kilitle
        foreach (Transform child in buttonContainer)
        {
            child.GetComponent<Button>().interactable = false;
        }

        // 3. DİREKT YAZDIRMA! Çarkı Döndür
        // Çark bittiğinde ne yapacağını (Callback) parantez içinde belirtiyoruz.
        wheelController.StartSpin(result.successChance, result.isSuccess, () => 
        {
            // --- ÇARK DURUNCA ÇALIŞACAK KOD ---
            resultText.text = result.resultText;
            nextMissionButton.gameObject.SetActive(true);
        
        
        });
    }
  
    
}