using UnityEngine;
using System.Collections.Generic;

public static class GameSession
{
    // --- MİNİGAME VERİLERİ (GEÇİCİ) ---
    public static MissionData activeMission;
    public static AgentData activeAgent;
    public static bool minigameWin;
    public static bool returningFromMinigame = false;

    // --- KALICI OYUN VERİLERİ (YENİ) ---
    // Bu değişkenler sahne değişse bile silinmez (Static olduğu için)
    public static int savedMoney = 1000; 
    public static List<MissionData> savedCompletedMissions = new List<MissionData>(); 
    public static bool isGameStarted = false; // Oyunun ilk açılışı mı?

    // Minigame'e giderken verileri kaydet
    public static void SaveSessionForMinigame(MissionData mission, AgentData agent)
    {
        activeMission = mission;
        activeAgent = agent;
        returningFromMinigame = true;
    }

    // Harita durumunu (Para ve Listeyi) kaydet
    public static void SaveMapState(int money, List<MissionData> completedList)
    {
        savedMoney = money;
        // Listeyi kopyalayarak kaydediyoruz ki referans hatası olmasın
        savedCompletedMissions = new List<MissionData>(completedList); 
        isGameStarted = true;
    }

    public static void ClearMinigameData()
    {
        activeMission = null;
        activeAgent = null;
        returningFromMinigame = false;
        minigameWin = false;
    }
}