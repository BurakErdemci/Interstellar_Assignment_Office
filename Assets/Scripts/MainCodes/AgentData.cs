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

    // YENİ STAT SİSTEMİ
    [System.Serializable]
    public class Skill
    {
        public StatType type;
        [Range(0, 10)] public int value; // 0-10 arası puan
    }

    // Artık statları buradan ekleyeceksin (Örn: Hack: 3, Speed: 5)
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
        if (currentXP >= xpToNextLevel)
        {
            currentXP -= xpToNextLevel;
            if (skills.Count > 0)
            {
                var randomSkill = skills[Random.Range(0, skills.Count)];
                if (randomSkill.value < 10) randomSkill.value++;
            }
        }
    }
    [Header("Diyaloglar (Barks)")]
    public string[] selectionQuotes; // Seçilince ne der?
    public string[] travelQuotes;    // Yürürken ne der?
    public string[] workingQuotes;   // İş yaparken ne der?

    // Rastgele cümle seçen yardımcı fonksiyon
    public string GetRandomQuote(string[] quoteList)
    {
        if (quoteList.Length == 0) return "...";
        return quoteList[Random.Range(0, quoteList.Length)];
    }
}