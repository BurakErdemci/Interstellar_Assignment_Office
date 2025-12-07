using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class MinigameCombatManager : MonoBehaviour
{
    public enum TurnState { PlayerTurn, EnemyTurn }
    public enum NoteType { Normal, Wavy, Turbo, Ghost, Bomb } // Nota Davranışları

    [Header("Rhythm UI References")]
    public RectTransform trackContainer;
    public RectTransform spawnPoint;
    public RectTransform targetZone;
    public GameObject keyPrefab;
    public TextMeshProUGUI feedbackText;
    public CanvasGroup uiCanvasGroup; // Tüm UI'ı şeffaflaştırmak için (

    [Header("Audio Settings (YENİ)")]
    public AudioSource musicSource;    // Arka plan müziği
    public AudioSource sfxSource;      // Efektler
    public AudioClip bgMusic;          // Müzik dosyası
    public AudioClip hitSound;         // Vuruş sesi
    public AudioClip missSound;        // Kaçırma/Hasar sesi
    public AudioClip bombSound;        // Bomba patlama sesi

    [Header("Juice")]
    public GameObject explosionPrefab;
    public float hitStopDuration = 0.1f;

    [Header("Health UI")]
    public Slider playerHealthBar;
    public Slider enemyHealthBar;

    [Header("Settings")]
    public float baseNoteSpeed = 400f;
    public float speedIncreasePerTurn = 50f;
    public int notesPerTurn = 5;
    public float perfectDistance = 30f;
    public float goodDistance = 80f;

    [Header("Damage")]
    public int damagePerPhase = 25;
    public int penaltyDamage = 20;

    // Durumlar
    private int currentPlayerHP = 100;
    private int currentEnemyHP = 100;
    private TurnState currentTurn;
    
    private float currentNoteSpeed;
    private int successfulHitsInTurn = 0;
    private int phaseCount = 0; 

    private class ActiveNote
    {
        public GameObject obj;
        public RectTransform rect;
        public KeyCode key;
        public NoteType type;      // Notanın tipi ne?
        public CanvasGroup cg;     // Görünmezlik (Ghost) için
        public Image img;          // Renk değişimi (Bomb) için
        public Vector2 startPos;   // Dalgalı hareket için başlangıç Y'si
        public float timeAlive;    // Ne kadar süredir hayatta?
    }

    private List<ActiveNote> activeNotes = new List<ActiveNote>();
    private List<KeyCode> possibleKeys = new List<KeyCode> { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D };
    
    private bool isGameActive = false;
    private int notesSpawnedInTurn = 0;
    private int notesProcessedInTurn = 0;

    private void Start()
    {
        // Müzik Başlat
        if (musicSource != null && bgMusic != null)
        {
            musicSource.clip = bgMusic;
            musicSource.loop = true;
            musicSource.Play();
        }

        playerHealthBar.maxValue = 100;
        enemyHealthBar.maxValue = 100;
        UpdateHealthUI();

        currentNoteSpeed = baseNoteSpeed;
        phaseCount = 0;
        
        StartCoroutine(StartTurn(TurnState.PlayerTurn));
    }

    IEnumerator StartTurn(TurnState state)
    {
        currentTurn = state;
        isGameActive = false;
        
        foreach (var note in activeNotes) { if(note.obj) Destroy(note.obj); }
        activeNotes.Clear();
        
        notesSpawnedInTurn = 0;
        notesProcessedInTurn = 0;
        successfulHitsInTurn = 0;
        currentNoteSpeed = baseNoteSpeed + (phaseCount * speedIncreasePerTurn);
        
        string turnName = state == TurnState.PlayerTurn ? "SALDIRI" : "SAVUNMA";
        feedbackText.text = $"{turnName}! (Zorluk: {phaseCount+1})";
        feedbackText.color = state == TurnState.PlayerTurn ? Color.cyan : Color.red;

        yield return new WaitForSeconds(2f);
        feedbackText.text = "";
        isGameActive = true;
        StartCoroutine(SpawnNotesRoutine());
    }

    IEnumerator SpawnNotesRoutine()
    {
        float dynamicInterval = 1.5f - (phaseCount * 0.1f);
        if (dynamicInterval < 0.6f) dynamicInterval = 0.6f;

        while (notesSpawnedInTurn < notesPerTurn && isGameActive)
        {
            SpawnNote();
            notesSpawnedInTurn++;
            yield return new WaitForSeconds(dynamicInterval);
        }
    }

    void SpawnNote()
    {
        GameObject newKey = Instantiate(keyPrefab, trackContainer);
        newKey.transform.position = spawnPoint.position;
        newKey.transform.localScale = Vector3.one; 
        
        // --- TİP BELİRLEME (ZORLUK EĞRİSİ) ---
        NoteType selectedType = NoteType.Normal;
        
        // Tur ilerledikçe yeni belalar açılır
        int chance = Random.Range(0, 100);
        if (phaseCount >= 1 && chance < 30) selectedType = NoteType.Wavy;  // 2. Turda Dalgalı gelebilir
        if (phaseCount >= 2 && chance < 50) selectedType = NoteType.Turbo; // 3. Turda Hızlanan gelebilir
        if (phaseCount >= 3 && chance < 70) selectedType = NoteType.Ghost; // 4. Turda Görünmez gelebilir
        if (phaseCount >= 2 && chance > 90) selectedType = NoteType.Bomb;  // Arada bir Bomba

        KeyCode randomKey = possibleKeys[Random.Range(0, possibleKeys.Count)];
        
        // Obje Referanslarını Al
        var tmpText = newKey.GetComponentInChildren<TextMeshProUGUI>();
        var rectTrans = newKey.GetComponent<RectTransform>();
        var canvasGroup = newKey.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = newKey.AddComponent<CanvasGroup>();
        var imageComp = newKey.GetComponent<Image>();

        // Bomba Ayarı
        if (selectedType == NoteType.Bomb)
        {
            tmpText.text = "!";
            imageComp.color = Color.red; // Kırmızı yap
            randomKey = KeyCode.None;    // Tuşu yok, basılmamalı
        }
        else
        {
            if (randomKey == KeyCode.Space)
            {
                tmpText.text = "SPACE";
                rectTrans.sizeDelta = new Vector2(200, 80); 
            }
            else
            {
                tmpText.text = randomKey.ToString();
                rectTrans.sizeDelta = new Vector2(80, 80);
            }
        }

        ActiveNote noteData = new ActiveNote
        {
            obj = newKey,
            rect = rectTrans,
            key = randomKey,
            type = selectedType,
            cg = canvasGroup,
            img = imageComp,
            startPos = rectTrans.anchoredPosition,
            timeAlive = 0f
        };
        
        activeNotes.Add(noteData);
    }

    void Update()
    {
        if (!isGameActive) return;

        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            ActiveNote note = activeNotes[i];
            note.timeAlive += Time.deltaTime;

            float moveAmount = currentNoteSpeed * Time.deltaTime;

            // --- DAVRANIŞLAR (BEHAVIORS) ---
            switch (note.type)
            {
                case NoteType.Normal:
                    // Dümdüz git
                    note.rect.anchoredPosition -= new Vector2(moveAmount, 0);
                    break;

                case NoteType.Wavy:
                    // Yılan gibi git (Sinüs Dalgası)
                    note.rect.anchoredPosition -= new Vector2(moveAmount, 0);
                    float waveY = Mathf.Sin(note.timeAlive * 10f) * 50f; // 50 birim yukarı aşağı
                    note.rect.anchoredPosition = new Vector2(note.rect.anchoredPosition.x, note.startPos.y + waveY);
                    break;

                case NoteType.Turbo:
                    // Hedefe yaklaştıkça hızlan (Roket)
                    float dist = Mathf.Abs(note.rect.anchoredPosition.x - targetZone.anchoredPosition.x);
                    float speedMultiplier = (dist < 300f) ? 2.5f : 1f; // 300 birim kala 2.5 kat hızlan
                    note.rect.anchoredPosition -= new Vector2(moveAmount * speedMultiplier, 0);
                    break;

                case NoteType.Ghost:
                    // Yolda görünmez ol (Fade Out)
                    note.rect.anchoredPosition -= new Vector2(moveAmount, 0);
                    // X: 500'de başla, 0'da tamamen görünmez ol
                    float alpha = Mathf.Clamp01(Mathf.Abs(note.rect.anchoredPosition.x - targetZone.anchoredPosition.x) / 400f);
                    note.cg.alpha = alpha;
                    break;

                case NoteType.Bomb:
                    // Bomba da düz gider ama kırmızıdır
                    note.rect.anchoredPosition -= new Vector2(moveAmount, 0);
                    // Yanıp sönme efekti
                    float blink = Mathf.PingPong(Time.time * 10, 1);
                    note.img.color = Color.Lerp(Color.red, Color.yellow, blink);
                    break;
            }

            // EKRANDAN ÇIKTI MI?
            if (note.rect.anchoredPosition.x < targetZone.anchoredPosition.x - goodDistance)
            {
                // Eğer Bombaysa ve çıkarsa -> İYİ BİR ŞEY (Patlamadı)
                if (note.type == NoteType.Bomb)
                {
                    RemoveNoteSafely(note);
                }
                else
                {
                    MissNote(note);
                    activeNotes.RemoveAt(i);
                    CheckTurnEnd();
                }
            }
        }

        // INPUT KONTROLÜ
        if (Input.anyKeyDown && activeNotes.Count > 0)
        {
            ActiveNote targetNote = activeNotes[0];

            // BOMBA KONTROLÜ
            if (targetNote.type == NoteType.Bomb)
            {
                // Herhangi bir tuşa basarsan ve en öndeki bombaysa -> GÜM!
                 if (Mathf.Abs(targetNote.rect.anchoredPosition.x - targetZone.anchoredPosition.x) <= goodDistance)
                 {
                     TriggerBomb(targetNote);
                 }
                 return;
            }

            // NORMAL NOTA KONTROLÜ
            if (Input.GetKeyDown(targetNote.key))
            {
                float distance = Mathf.Abs(targetNote.rect.anchoredPosition.x - targetZone.anchoredPosition.x);

                if (distance <= goodDistance)
                {
                    bool isCrit = distance <= perfectDistance;
                    HitNote(targetNote, isCrit); 
                }
                else
                {
                    MissNote(targetNote);
                    activeNotes.RemoveAt(0);
                    CheckTurnEnd();
                }
            }
            else if (!Input.GetMouseButtonDown(0))
            {
                // Yanlış tuş
                MissNote(targetNote);
                activeNotes.RemoveAt(0);
                CheckTurnEnd();
            }
        }
    }

    void TriggerBomb(ActiveNote note)
    {
        if (sfxSource) sfxSource.PlayOneShot(bombSound);
        
        // Patlama efekti
        if (explosionPrefab)
        {
            GameObject vfx = Instantiate(explosionPrefab, trackContainer);
            vfx.transform.position = note.obj.transform.position;
            vfx.transform.localScale = Vector3.one * 2f;
            // Kırmızı patlasın
            var main = vfx.GetComponent<ParticleSystem>().main;
            main.startColor = Color.red;
            Destroy(vfx, 1.5f);
        }

        feedbackText.text = "BOMBA!";
        feedbackText.color = Color.red;
        Camera.main.transform.DOShakePosition(0.5f, 30, 50);
        
        Destroy(note.obj);
        activeNotes.Remove(note);
        notesProcessedInTurn++;

        // Direkt Hasar ve Ceza
        DealDamageToPlayer(penaltyDamage * 2, false); // Bomba çok acıtır
        CheckTurnEnd();
    }

    void HitNote(ActiveNote note, bool isCritical)
    {
        if (sfxSource) sfxSource.PlayOneShot(hitSound);

        if (explosionPrefab != null)
        {
            GameObject vfx = Instantiate(explosionPrefab, trackContainer); 
            vfx.transform.position = note.obj.transform.position;
            Vector3 pos = note.obj.transform.position;
            pos.z -= 2f; 
            vfx.transform.position = pos;
            vfx.transform.localScale = Vector3.one * 2f; 
            Destroy(vfx, 1.5f); 
        }

        Destroy(note.obj);
        activeNotes.Remove(note);
        notesProcessedInTurn++;
        successfulHitsInTurn++; 

        feedbackText.transform.DOKill(true);
        feedbackText.transform.localScale = Vector3.one;
        
        if (isCritical)
        {
            feedbackText.text = "MÜKEMMEL!";
            feedbackText.color = Color.yellow;
            feedbackText.transform.DOPunchScale(Vector3.one * 0.5f, 0.2f, 10, 1);
            StartCoroutine(HitStopRoutine());
        }
        else
        {
            feedbackText.text = "İYİ!";
            feedbackText.color = Color.green;
            feedbackText.transform.DOPunchScale(Vector3.one * 0.2f, 0.1f);
        }

        CheckTurnEnd();
    }

    void MissNote(ActiveNote note)
    {
        if (sfxSource) sfxSource.PlayOneShot(missSound);

        Destroy(note.obj);
        notesProcessedInTurn++;
        
        feedbackText.text = "KAÇTI!";
        feedbackText.color = Color.red;
        feedbackText.transform.DOShakePosition(0.5f, 10); 

        CheckTurnEnd();
    }

    void RemoveNoteSafely(ActiveNote note)
    {
        // Bombayı pas geçince (vurmayınca) başarılı sayılırız ama skor almayız
        // Sadece listeden silip devam ediyoruz
        Destroy(note.obj);
        activeNotes.Remove(note);
        notesProcessedInTurn++;
        CheckTurnEnd();
    }


    
    IEnumerator HitStopRoutine()
    {
        Time.timeScale = 0f; 
        yield return new WaitForSecondsRealtime(hitStopDuration); 
        Time.timeScale = 1f; 
    }

    void CheckTurnEnd()
    {
        if (notesProcessedInTurn >= notesPerTurn && activeNotes.Count == 0)
        {
            isGameActive = false; 
            StartCoroutine(ResolveTurnRoutine());
        }
    }

    IEnumerator ResolveTurnRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        if (currentTurn == TurnState.PlayerTurn)
        {
            if (successfulHitsInTurn >= 3)
            {
                feedbackText.text = "KOMBO TAMAMLANDI!";
                feedbackText.color = Color.yellow;
                CinematicAction.Instance.PlayPlayerCriticalAttack(() => { DealDamageToEnemy(damagePerPhase, true); });
            }
            else
            {
                feedbackText.text = "SALDIRI BAŞARISIZ...";
                feedbackText.color = Color.gray;
                yield return new WaitForSeconds(1.5f);
                NextTurn();
            }
        }
        else
        {
            if (successfulHitsInTurn >= 3)
            {
                feedbackText.text = "BLOKLADIN!";
                feedbackText.color = Color.green;
                yield return new WaitForSeconds(1.5f);
                NextTurn();
            }
            else
            {
                feedbackText.text = "HASAR ALDIN!";
                feedbackText.color = Color.red;
                CinematicAction.Instance.PlayEnemyAttack(() => { DealDamageToPlayer(penaltyDamage, true); });
            }
        }
    }

    void DealDamageToEnemy(int amount, bool changeTurn)
    {
        currentEnemyHP -= amount;
        UpdateHealthUI();
        if (currentEnemyHP <= 0) EndGame(true);
        else if (changeTurn) NextTurn();
    }

    void DealDamageToPlayer(int amount, bool changeTurn)
    {
        currentPlayerHP -= amount;
        UpdateHealthUI();
        Camera.main.transform.DOShakePosition(0.5f, 15, 20);
        if (currentPlayerHP <= 0) EndGame(false);
        else if (changeTurn) NextTurn();
    }

    void NextTurn()
    {
        phaseCount++; 
        TurnState nextState = (currentTurn == TurnState.PlayerTurn) ? TurnState.EnemyTurn : TurnState.PlayerTurn;
        StartCoroutine(StartTurn(nextState));
    }

    void EndGame(bool playerWon)
    {
        isGameActive = false;
        StopAllCoroutines();
        feedbackText.text = playerWon ? "KAZANDIN!" : "KAYBETTİN...";
        feedbackText.color = playerWon ? Color.green : Color.red;
        GameSession.minigameWin = playerWon;
        GameSession.returningFromMinigame = true;
        StartCoroutine(ReturnToMapRoutine());
    }

    void UpdateHealthUI()
    {
        if (playerHealthBar) playerHealthBar.value = currentPlayerHP;
        if (enemyHealthBar) enemyHealthBar.value = currentEnemyHP;
    }
    IEnumerator ReturnToMapRoutine()
    {
        // 1. Sonucu okuması için 2 saniye bekle
        yield return new WaitForSeconds(2f);

        // 2. Ana sahneyi yükle
      
        UnityEngine.SceneManagement.SceneManager.LoadScene("MissionScene");
    }
}