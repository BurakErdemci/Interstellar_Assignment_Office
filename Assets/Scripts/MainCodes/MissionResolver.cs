using UnityEngine;

public class MissionResolver : MonoBehaviour
{
    public static MissionResolver Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    public MissionResult ResolveMission(AgentData agent, MissionData mission)
    {
        MissionResult result = new MissionResult();
        
        // --- 1. ÖZEL ETKİLEŞİM KONTROLÜ (Aynen Korundu) ---
        if (mission.specificAgent != null && mission.specificAgent == agent)
        {
            result.isSuccess = true; 
            result.successChance = 1.0f; // Çark %100 yeşil dönsün
            result.resultText = $"<color=yellow>* ÖZEL ETKİLEŞİM!</color>\n\n{mission.specialSuccessText}";

            if (mission.interactionType == InteractionType.TriggerMinigame)
            {
                result.resultText += "\n\n(SİSTEM HACKLENİYOR...)";
            }
            return result; 
        }
        
        // --- 2. YENİ SİSTEM: YETERLİLİK KONTROLÜ ---
        bool meetsAllRequirements = true;
        string missingStatName = "";

        // Görevin istediği her şartı tek tek kontrol et
        foreach (var req in mission.requirements)
        {
            int agentValue = agent.GetSkillValue(req.stat);
            
            // Eğer ajanın yeteneği, istenen seviyenin altındaysa
            if (agentValue < req.minLevel)
            {
                meetsAllRequirements = false;
                missingStatName = req.stat.ToString();
                break; // Bir tane bile eksikse yetersizdir
            }
        }

        // --- 3. SONUÇ HESAPLAMA ---
        if (meetsAllRequirements)
        {
            // Şartları sağlıyor! Şans faktörünü hesapla.
            // Temel şans %70 + Zorluk cezası
            float baseChance = 0.7f; 
            // Zorluk 100 ise %50 ceza, Zorluk 0 ise %0 ceza
            float difficultyPenalty = mission.difficultyLevel / 200f; 
            
            float finalChance = Mathf.Clamp(baseChance - difficultyPenalty, 0.4f, 0.95f);
            result.successChance = finalChance;

            // Zar At
            if (Random.value <= finalChance)
            {
                result.isSuccess = true;
                result.resultText = $"BAŞARILI! \n{mission.successText}";
                
                // Başarılı olduğu için XP ver
                agent.AddXP(20); 
            }
            else
            {
                result.isSuccess = false;
                result.resultText = $"ŞANSSIZLIK! \nHer şeye yetkin yetti ama şansın yaver gitmedi.\n{mission.failText}";
            }
        }
        else
        {
            // Şartları sağlamıyor!
            result.successChance = 0.05f; // Çok düşük bir mucize şansı (%5)
            
            // Burada direkt başarısız sayıyoruz (Çarkı kırmızı döndürmek için şansı düşük verdik)
            if (Random.value <= 0.05f) // Mucize oldu mu?
            {
                 result.isSuccess = true;
                 result.resultText = $"MUCİZE! \nYetersizdin ama nasılsa başardın!\n{mission.successText}";
            }
            else
            {
                result.isSuccess = false;
                string randomQuote = agent.funnyFailQuotes.Length > 0 
                    ? agent.funnyFailQuotes[Random.Range(0, agent.funnyFailQuotes.Length)] 
                    : "...";

                result.resultText = $"YETERSİZ! \n{agent.agentName}, {missingStatName} konusunda yetersiz kaldı.\n\n\"{randomQuote}\"";
            }
        }

        return result;
    }
}

public class MissionResult
{
    public bool isSuccess;
    public string resultText;
    public float successChance;
}