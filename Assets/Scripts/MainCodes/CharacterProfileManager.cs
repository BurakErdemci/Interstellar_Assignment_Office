using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

public class CharacterProfileManager : MonoBehaviour
{
    public static CharacterProfileManager Instance;

    [Header("Data")]
    public AgentData[] allAgents; // Tüm ajanları buraya sürükle
    private int currentIndex = 0; // Şu an kime bakıyoruz?

    [Header("UI Components")]
    public GameObject profilePanel;
    public Image agentFaceImage;
    public TextMeshProUGUI agentNameText;
    public TextMeshProUGUI xpText;      
    public TextMeshProUGUI pointsText;  
    
    [Header("Navigation Buttons")]
    public Button nextButton;
    public Button prevButton;
    
    [Header("Stat Grid")]
    public Transform statsContainer; 
    public GameObject statRowPrefab; 

    private AgentData currentData;
    private List<StatRowUI> activeRows = new List<StatRowUI>();

    private void Awake() { Instance = this; }

    private void Start() 
    { 
        profilePanel.SetActive(false); 
        
        // Navigasyon butonlarını bağla
        if(nextButton) nextButton.onClick.AddListener(NextAgent);
        if(prevButton) prevButton.onClick.AddListener(PreviousAgent);
    }

    // --- BU FONKSİYONU "PERSONEL" BUTONUNA BAĞLA ---
    public void ToggleProfilePanel()
    {
        if (profilePanel.activeSelf)
        {
            CloseProfile();
        }
        else
        {
            // İlk açılışta mevcut indextekini aç (veya 0)
            OpenProfile(allAgents[currentIndex]);
        }
    }

    public void OpenProfile(AgentData agent)
    {
        currentData = agent;
        profilePanel.SetActive(true);
        
        // Animasyon (Sadece kapalıysa oynat, geçişlerde oynatma)
        if (profilePanel.transform.localScale.x < 0.1f)
        {
            profilePanel.transform.localScale = Vector3.zero;
            profilePanel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        }

        RefreshUI();
    }

    public void CloseProfile()
    {
        profilePanel.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack)
            .OnComplete(() => profilePanel.SetActive(false));
    }

    // --- İLERİ / GERİ GEZİNME ---
    public void NextAgent()
    {
        currentIndex++;
        if (currentIndex >= allAgents.Length) currentIndex = 0; // Başa dön
        OpenProfile(allAgents[currentIndex]);
    }

    public void PreviousAgent()
    {
        currentIndex--;
        if (currentIndex < 0) currentIndex = allAgents.Length - 1; // Sona dön
        OpenProfile(allAgents[currentIndex]);
    }

    public void UpdatePointsUI()
    {
        xpText.text = $"XP: {currentData.currentXP} / {currentData.xpToNextLevel}";
        pointsText.text = $"PUAN: {currentData.availableStatPoints}";

        foreach (var row in activeRows) row.UpdateVisuals();
    }

    void RefreshUI()
    {
        agentNameText.text = currentData.agentName;
        agentFaceImage.sprite = currentData.faceIcon;
        
        UpdatePointsUI();

        foreach (Transform child in statsContainer) Destroy(child.gameObject);
        activeRows.Clear();

        foreach (var skill in currentData.skills)
        {
            GameObject rowObj = Instantiate(statRowPrefab, statsContainer);
            StatRowUI rowScript = rowObj.GetComponent<StatRowUI>();
            rowScript.Setup(currentData, skill.type);
            activeRows.Add(rowScript);
        }
    }
}