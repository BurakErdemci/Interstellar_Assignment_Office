using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewAgent", menuName = "Game/Agent")]
public class AgentData : ScriptableObject
{
    public string agentName;       
    [TextArea] public string description; 
    public Sprite faceIcon;
    
    [Header("Progression")]
    public int currentXP = 0;
    public int xpToNextLevel = 100;
    public int availableStatPoints = 0;
    [Header("Diyaloglar (Barks)")]
    public string[] selectionQuotes; // Seçilince ne der?
    public string[] travelQuotes;    // Yürürken ne der?
    public string[] workingQuotes;   // İş yaparken ne der?
    public string[] returnQuotes;   

    // STAT SİSTEMİ
    [System.Serializable]
    public class Skill
    {
        public StatType type;
        [Range(0, 10)] public int value; // 0-10 arası puan
    }
    [System.Serializable]
    public struct ContextQuote
    {
        public MissionCategory category; // Hangi tür?
        [TextArea] public string[] quotes; // O türe özel laflar
    }
    
    [Header("Akıllı Replik Sistemi")]
    public string[] generalWorkingQuotes; // Eğer özel tür bulamazsa bunu söyler (Yedek)
    public List<ContextQuote> specificQuotes; // Özel durumlar listesi


    // Artık statları buradan eklenecek (Örn: Hack: 3, Speed: 5)
    public List<Skill> skills = new List<Skill>();
    
    // Karakter Zayıflıkları
    public string weakness; 
    
    // Karakterin başarısızlık replikleri
    public string[] funnyFailQuotes; 

    // --- YARDIMCI FONKSİYONLAR ---

    // Ajanın spesifik bir yetenek puanını bulur
    public int GetSkillValue(StatType typeToCheck)
    {
        foreach (var skill in skills)
        {
            if (skill.type == typeToCheck)
                return skill.value;
        }
        return 0; // Yeteneği yoksa 0
    }

    // XP Kazanma Sistemi
    public void AddXP(int amount)
    {
        currentXP += amount;
        
        // Eğer seviye atlama sınırını geçtiyse
        while (currentXP >= xpToNextLevel)
        {
            currentXP -= xpToNextLevel;
            availableStatPoints++; // 1 Puan kazan

            xpToNextLevel += 50; 
        }
    }
    public void UpgradeStat(StatType type)
    {
        if (availableStatPoints > 0)
        {
            foreach (var skill in skills)
            {
                if (skill.type == type && skill.value < 10) // Max 10
                {
                    skill.value++;
                    availableStatPoints--;
                    return;
                }
            }
        }
    }
    public string GetWorkingQuote(MissionCategory missionType)
    {
        // 1. Önce özel listeyi tara
        foreach (var item in specificQuotes)
        {
            if (item.category == missionType && item.quotes.Length > 0)
            {
                // O kategoriye uygun lafları bulduk, içinden rastgele seç
                return item.quotes[Random.Range(0, item.quotes.Length)];
            }
        }

        // 2. Eğer özel laf yoksa, genel havuzdan seç (Fallback)
        if (generalWorkingQuotes.Length > 0)
        {
            return generalWorkingQuotes[Random.Range(0, generalWorkingQuotes.Length)];
        }

        return "...";
    }

    // Rastgele cümle seçen yardımcı fonksiyon
    public string GetRandomQuote(string[] quoteList)
    {
        if (quoteList.Length == 0) return "...";
        return quoteList[Random.Range(0, quoteList.Length)];
    }
}