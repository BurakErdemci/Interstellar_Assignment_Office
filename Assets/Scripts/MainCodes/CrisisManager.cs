using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class CrisisManager : MonoBehaviour
{
    public static CrisisManager Instance;

    [Header("UI References")]
    public GameObject crisisPanel; // Tüm paneli açıp kapatmak için
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

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        crisisPanel.SetActive(false); // Başlangıçta gizle
    }

    // MapManager buradan çağıracak
    public void StartCrisis(MissionData mission)
    {
        currentMission = mission;
        crisisPanel.SetActive(true);
        CanvasGroup group = crisisPanel.GetComponent<CanvasGroup>();
        if (group == null) group = crisisPanel.AddComponent<CanvasGroup>();
        group.alpha = 0; // Görünmez başla
        group.DOFade(1, 0.5f); // Yarım saniyede görünür ol
        crisisPanel.transform.DOShakePosition(1f, 10f, 10, 90);

        // Metinleri Doldur
        titleText.text = "KRİTİK DURUM: " + mission.missionTitle;
        descriptionText.text = mission.missionBrief; // "Komşudan silah sesleri geliyor..."

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

        // Hangi oyun seçildiyse onu başlat
        switch (gameType)
        {
            case MinigameType.AttackMode:
                Debug.Log("SALDIRI MİNİGAME BAŞLATILIYOR (Tuşlara Bas!)");
                // StartAttackGame(); -> Yarın burayı yazacağız
                break;

            case MinigameType.DefenseMode:
                Debug.Log("SAVUNMA MİNİGAME BAŞLATILIYOR (Sessiz Ol!)");
                // StartDefenseGame(); -> Yarın burayı yazacağız
                break;
        }
    }
}