using UnityEngine;
using System.Collections.Generic;

public enum LevelEventType
{
    Wait,           
    SpawnMission,   
    StartDialogue,  
    EndDay,
    TriggerBanter 
}

[CreateAssetMenu(fileName = "NewDay", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    [System.Serializable]
    public struct Event
    {
        public string note; 
        public LevelEventType eventType;
        
        public float waitDuration;      
        public MissionData mission;     
        public DialogueData dialogue;   
        
        [Header("Banter (Laf Atma)")]
        public AgentData banterAgent;   // Asıl konuşmacı
        [TextArea] public string banterText; 

        // --- İŞTE EKSİK OLAN KISIMLAR ---
        [Header("Alternative Banter (Yedek)")]
        public AgentData altBanterAgent;   // Yedek konuşmacı (Eğer asıl olan meşgulse)
        [TextArea] public string altBanterText; 
        // -------------------------------
    }

    public List<Event> timeline; 
}