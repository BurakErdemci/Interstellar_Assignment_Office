using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class CrisisManager : MonoBehaviour
{
    public static CrisisManager Instance;

    [Header("UI References")]
    public GameObject crisisPanel; 
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;

    [Header("Option A UI (Attack)")]
    public TextMeshProUGUI textOptionA_Title;
    public TextMeshProUGUI textOptionA_Desc;
    public Button btnOptionA;

    [Header("Option B UI (Defense)")]
    public TextMeshProUGUI textOptionB_Title;
    public TextMeshProUGUI textOptionB_Desc;
    public Button btnOptionB;

    private MissionData currentMission;
    private AgentData currentAgent;

    private void Awake() { Instance = this; }

    private void Start()
    {
        crisisPanel.SetActive(false); 
    }

   
    public void StartCrisis(MissionData mission, AgentData agent)
    {
        currentMission = mission;
        currentAgent = agent;
        
        crisisPanel.SetActive(true);
        CanvasGroup group = crisisPanel.GetComponent<CanvasGroup>();
        if (group == null) group = crisisPanel.AddComponent<CanvasGroup>();
        
        group.alpha = 0; 
        group.DOFade(1, 0.5f); 
        crisisPanel.transform.DOShakePosition(1f, 10f, 10, 90);

        // Metinleri Doldur
        titleText.text = "KRİTİK DURUM: " + mission.missionTitle;
        descriptionText.text = mission.missionBrief; 

        // Seçenek A
        textOptionA_Title.text = mission.optionATitle;
        textOptionA_Desc.text = mission.optionADesc;
        
        // Seçenek B
        textOptionB_Title.text = mission.optionBTitle;
        textOptionB_Desc.text = mission.optionBDesc;

        // Butonları temizle ve bağla
        btnOptionA.onClick.RemoveAllListeners();
        btnOptionA.onClick.AddListener(() => SelectOption(mission.optionAGame));

        btnOptionB.onClick.RemoveAllListeners();
        btnOptionB.onClick.AddListener(() => SelectOption(mission.optionBGame));
    }

    void SelectOption(MinigameType gameType)
    {
        // Seçim yapıldı, Dilemma ekranını kapat
        crisisPanel.SetActive(false);
        
      
        GameSession.SaveSessionForMinigame(currentMission, currentAgent);
        switch (gameType)
        {
            case MinigameType.AttackMode:
                Debug.Log("Saldırı Sahnesi Yükleniyor...");
                SceneManager.LoadScene("AttackGameScene"); 
                break;

            case MinigameType.DefenseMode:
                Debug.Log("Savunma Sahnesi Yükleniyor...");
                SceneManager.LoadScene("DefenseGameScene");
                break;
                
            case MinigameType.None:
                Debug.Log("Minigame yok, direkt devam et.");
                break;
        }
    }
}