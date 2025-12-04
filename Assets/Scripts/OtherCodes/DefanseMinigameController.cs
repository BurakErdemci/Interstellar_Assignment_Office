using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class DefenseGameManager : MonoBehaviour
{
    [Header("UI References")]
    public Slider noiseSlider;
    public Slider progressSlider;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI timerText;
    public Image dangerOverlay; // YENİ: Kırmızı ekran paneli

    [Header("Audio Settings (YENİ)")]
    public AudioSource heartbeatSource; // Kalp atışı
    public AudioSource ambientSource;   // Kapı arkası sesleri
    public AudioSource fxSource;        // Win/Fail efektleri
    public AudioClip winClip;
    public AudioClip failClip;
    public AudioSource backgroundMusicSource; // YENİ: Gerilim Müziği
    public AudioSource cryingSource;          // YENİ: Ağlama Sesi

    private bool hasRealizedDrama = false;
    [Header("Crying Settings")]
    public float cryDelay = 1.5f; // Kaç saniye sonra ağlasın?
    private float currentPressDuration = 0f; // Ne kadar süredir basıyoruz?
    
    [Header("Character Visuals")]
    public Image characterImage;       
    public RectTransform characterRect; 
    public Sprite idleSprite;          
    public Sprite listeningSprite;     

    [Header("Animation Settings")]
    public float leanDistance = 50f;   
    public float leanAngle = -10f;     
    
    private Vector2 charOriginalPos;       
    private Vector3 sliderOriginalPos;     

    [Header("Game Settings")]
    public float timeLimit = 20f;
    public float noiseIncreaseSpeed = 35f;
    public float noiseDecreaseSpeed = 20f;
    public float progressSpeed = 15f;

    private float currentNoise = 0f;
    private float currentProgress = 0f;
    private bool isGameOver = false;

    void Start()
    {
        if (characterRect != null) charOriginalPos = characterRect.anchoredPosition;
        if (noiseSlider != null) sliderOriginalPos = noiseSlider.transform.localPosition;

        characterImage.sprite = idleSprite; 
        noiseSlider.maxValue = 100;
        progressSlider.maxValue = 100;
        feedbackText.text = "";

        // Sesleri Başlat (Sessizce)
        if (heartbeatSource != null)
        {
            heartbeatSource.loop = true;
            heartbeatSource.volume = 0; // Başta sessiz
            heartbeatSource.Play();
        }
        if (ambientSource != null)
        {
            ambientSource.loop = true;
            ambientSource.volume = 0;
            ambientSource.Play();
        }
        if (backgroundMusicSource != null)
        {
            backgroundMusicSource.loop = true;
            backgroundMusicSource.volume = 0.5f; // Çok bağırmasın
            backgroundMusicSource.Play();
        }

        // Ağlamayı Hazırla (Ama sesini kapat)
        if (cryingSource != null)
        {
            cryingSource.loop = true;
            cryingSource.volume = 0; // Başta duyulmasın
            cryingSource.Play();     // Çalsın ama sesi 0 olsun
        }
    }

    void Update()
    {
        if (isGameOver) return;

        timeLimit -= Time.deltaTime;
        if(timerText) timerText.text = "Süre: " + Mathf.Ceil(timeLimit);

        if (timeLimit <= 0)
        {
            EndGame(false, "SÜRE DOLDU! BAŞARAMADIN.");
            return;
        }

        // --- GİRİŞ KONTROLÜ ---
        bool isPressing = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0);

        if (isPressing)
        {
            currentNoise += noiseIncreaseSpeed * Time.deltaTime;
            currentProgress += progressSpeed * Time.deltaTime;

            if (characterImage.sprite != listeningSprite)
            {
                characterImage.sprite = listeningSprite;
                characterRect.DOAnchorPos(charOriginalPos + new Vector2(leanDistance, 0), 0.3f);
                characterRect.DORotate(new Vector3(0, 0, leanAngle), 0.3f);
            }
            
            // TITREME (JUICE)
            if (currentNoise > 70)
            {
                float shakeAmount = (currentNoise - 70) * 0.2f; 
                float x = Random.Range(-shakeAmount, shakeAmount);
                float y = Random.Range(-shakeAmount, shakeAmount);
                noiseSlider.transform.localPosition = sliderOriginalPos + new Vector3(x, y, 0);
            }
            else
            {
                noiseSlider.transform.localPosition = sliderOriginalPos;
            }
        }
        else
        {
            currentNoise -= noiseDecreaseSpeed * Time.deltaTime;

            if (characterImage.sprite != idleSprite)
            {
                characterImage.sprite = idleSprite;
                characterRect.DOAnchorPos(charOriginalPos, 0.3f);
                characterRect.DORotate(Vector3.zero, 0.3f);
                noiseSlider.transform.localPosition = sliderOriginalPos;
            }
        }

        currentNoise = Mathf.Clamp(currentNoise, 0, 100);
        currentProgress = Mathf.Clamp(currentProgress, 0, 100);

        noiseSlider.value = currentNoise;
        progressSlider.value = currentProgress;

        // --- CİLA: SES VE GÖRSEL EFEKTLERİN GÜNCELLENMESİ ---
        UpdateJuice();

        if (currentNoise >= 100) EndGame(false, "YAKALANDIN! ÇOK SES ÇIKARDIN!");
        else if (currentProgress >= 100) EndGame(true, "GÖREV BAŞARILI! BİLGİLERİ ALDIN.");
    }

    // --- YENİ FONKSİYON: EFEKTLERİ YÖNETME ---
    
void UpdateJuice()
    {
        bool isPressing = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0);

        // Sayaç
        if (isPressing)
        {
            currentPressDuration += Time.deltaTime;
        }
        else
        {
            currentPressDuration = 0f;
        }

        // 1. KALP ATIŞI (Aynı kalsın)
        if (heartbeatSource != null)
        {
            float targetVolume = (currentNoise > 20) ? (currentNoise / 100f) : 0f;
            heartbeatSource.volume = Mathf.Lerp(heartbeatSource.volume, targetVolume, Time.deltaTime * 5f);
            heartbeatSource.pitch = 1f + (currentNoise / 100f);
        }

        // 2. KAPIDAKİ SESLER (Aynı kalsın)
        if (ambientSource != null)
        {
            float targetVol = isPressing ? 0.8f : 0f;
            ambientSource.volume = Mathf.Lerp(ambientSource.volume, targetVol, Time.deltaTime * 3f);
        }

        // --- 3. AĞLAMA SESİ (GÜNCELLENDİ: "Öğrenen" Sistem) ---
        if (cryingSource != null)
        {
            float targetCryVol = 0f;

            if (isPressing)
            {
                // SENARYO A: Zaten daha önce olayı anladıysa -> DİREKT AĞLA
                // SENARYO B: İlk kez dinliyorsa -> SÜRE DOLUNCA AĞLA
                if (hasRealizedDrama || currentPressDuration > cryDelay)
                {
                    targetCryVol = 0.6f;
                    
                    // Süre dolduysa "Artık olayı anladı" diye işaretle
                    if (!hasRealizedDrama) hasRealizedDrama = true; 
                }
            }

            cryingSource.volume = Mathf.Lerp(cryingSource.volume, targetCryVol, Time.deltaTime * 3f);
        }
        // -------------------------------------------------------

        // 4. KIRMIZI EKRAN (Aynı kalsın)
        if (dangerOverlay != null)
        {
            if (currentNoise > 60)
            {
                float alpha = (Mathf.Sin(Time.time * 10f) + 1f) / 2f; 
                float intensity = (currentNoise - 60) / 40f; 
                Color c = dangerOverlay.color;
                c.a = alpha * intensity * 0.5f; 
                dangerOverlay.color = c;
            }
            else
            {
                Color c = dangerOverlay.color;
                c.a = 0;
                dangerOverlay.color = c;
            }
        }
    }
    void EndGame(bool success, string message)
    {
        isGameOver = true;
        feedbackText.text = message;
        feedbackText.color = success ? Color.green : Color.red;
        
        // Sesleri Sustur
        if (heartbeatSource) heartbeatSource.Stop();
        if (ambientSource) ambientSource.Stop();
        if (backgroundMusicSource) backgroundMusicSource.Stop();
        if (cryingSource) cryingSource.Stop();


        // Sonuç Sesi
        if (fxSource)
        {
            AudioClip clip = success ? winClip : failClip;
            if(clip) fxSource.PlayOneShot(clip);
        }

        noiseSlider.transform.localPosition = sliderOriginalPos;
        if(dangerOverlay) dangerOverlay.color = new Color(1,0,0,0); // Kırmızılığı sil

        Debug.Log("Oyun Bitti: " + message);
        // Invoke("ReturnToMap", 2f); // İleride eklenecek
    }
}