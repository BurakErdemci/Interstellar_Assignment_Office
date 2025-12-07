using UnityEngine;
using System.Collections.Generic; 

[CreateAssetMenu(fileName = "NewMission", menuName = "Game/Mission")]
public class MissionData : ScriptableObject
{
    [Header("Basic Info")]
    public string missionTitle;
    [TextArea] public string missionBrief;
    public MissionType type; // Rutin mi Kriz mi?    
    
    [System.Serializable]
    public struct Requirement
    {
        public StatType stat;      // Hangi yetenek lazım?
        [Range(1, 10)] public int minLevel; // Kaç seviye lazım? (1-10)
    }

    [Header("Routine Settings")]
    public List<Requirement> requirements; // Artık birden fazla şart ekleyebilirsin
    public int difficultyLevel; // 0-100 arası (Şans faktörünü etkiler)
    
    [TextArea] public string successText;
    [TextArea] public string failText;

    [Header("Crisis Settings (Story Only)")]
    // Seçenek A
    public string optionATitle; 
    [TextArea] public string optionADesc; 
    public MinigameType optionAGame; 

    // Seçenek B
    public string optionBTitle; 
    [TextArea] public string optionBDesc; 
    public MinigameType optionBGame; 

    [Header("--- SECRET INTERACTION ---")]
    public AgentData specificAgent; 
    public InteractionType interactionType; 
    [TextArea] public string specialSuccessText; 
    
    [Header("Context")]
    public MissionCategory category;
}

// --- ENUMLAR ---

public enum InteractionType
{
    TextOnly,       
    TriggerMinigame 
}

// Stat türleri (GÜNCELLENDİ)
public enum StatType
{
    Hack,
    Combat,    // Savaş
    Stealth,   // Gizlilik
    Intel,     // Zeka
    Speed,     // Hız
    Charisma   // Karizma
}

public enum MissionType
{
    Routine, 
    Crisis   
}

public enum MinigameType
{
    None,
    AttackMode, 
    DefenseMode 
}

public enum MissionCategory
{
    General,   // Genel işler (Kahve taşıma vs.)
    Hacking,   // Teknoloji işleri
    Combat,    // Vurdulu kırdılı işler
    Rescue,    // Kedi/Rehine kurtarma
    Stealth    // Gizlilik işleri
}