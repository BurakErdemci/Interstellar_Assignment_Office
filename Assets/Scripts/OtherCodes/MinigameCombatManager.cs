using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening; // DOTween kütüphanesini unutma!

public class MinigameCombatManager : MonoBehaviour
{
    public enum TurnState { PlayerTurn, EnemyTurn }

    [Header("Rhythm UI References")]
    public RectTransform trackContainer; // Notaların kayacağı şerit
    public RectTransform spawnPoint;     // Doğuş noktası (Sağ)
    public RectTransform targetZone;     // Hedef noktası (Sol)
    public GameObject keyPrefab;
    public TextMeshProUGUI feedbackText;

    [Header("Health UI")]
    public Slider playerHealthBar;
    public Slider enemyHealthBar;

    [Header("Phase Settings")]
    public float baseNoteSpeed = 400f;   
    public float speedIncreasePerTurn = 50f; 
    public int notesPerTurn = 5;         
    
    [Header("Hit Windows (Zorluk)")]
    public float perfectDistance = 30f;  // Biraz artırdım, daha kolay olsun
    public float goodDistance = 80f;

    [Header("Damage Settings")]
    public int damagePerPhase = 25; // Başarılı saldırı hasarı
    public int penaltyDamage = 20;  // Başarısız savunma hasarı

    // Canlar
    private int currentPlayerHP = 100;
    private int currentEnemyHP = 100;
    private TurnState currentTurn;
    
    // Değişkenler
    private float currentNoteSpeed;
    private int successfulHitsInTurn = 0;
    private int phaseCount = 0; 

    private class ActiveNote
    {
        public GameObject obj;
        public RectTransform rect;
        public KeyCode key;
    }
    private List<ActiveNote> activeNotes = new List<ActiveNote>();
    private List<KeyCode> possibleKeys = new List<KeyCode> { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D };
    
    private bool isGameActive = false;
    private int notesSpawnedInTurn = 0;
    private int notesProcessedInTurn = 0;

    private void Start()
    {
        // Başlangıç Ayarları
        playerHealthBar.maxValue = 100;
        enemyHealthBar.maxValue = 100;
        playerHealthBar.value = 100;
        enemyHealthBar.value = 100;

        currentNoteSpeed = baseNoteSpeed;
        phaseCount = 0;
        
        StartCoroutine(StartTurn(TurnState.PlayerTurn));
    }

    IEnumerator StartTurn(TurnState state)
    {
        currentTurn = state;
        isGameActive = false;
        
        // Temizlik
        foreach (var note in activeNotes) { if(note.obj) Destroy(note.obj); }
        activeNotes.Clear();
        
        notesSpawnedInTurn = 0;
        notesProcessedInTurn = 0;
        successfulHitsInTurn = 0;

        // Zorluk Artışı
        currentNoteSpeed = baseNoteSpeed + (phaseCount * speedIncreasePerTurn);
        
        // UI Bilgilendirme
        if (state == TurnState.PlayerTurn)
        {
            feedbackText.text = $"SALDIRI HAZIRLIĞI! (Hız: {phaseCount+1})";
            feedbackText.color = Color.cyan;
        }
        else
        {
            feedbackText.text = $"SAVUNMAYA GEÇ! (Hız: {phaseCount+1})";
            feedbackText.color = Color.red;
        }

        yield return new WaitForSeconds(2f);

        feedbackText.text = "";
        isGameActive = true;
        
        StartCoroutine(SpawnNotesRoutine());
    }

    IEnumerator SpawnNotesRoutine()
    {
        // Dinamik aralık (Hızlandıkça notalar sıklaşsın)
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
        
        KeyCode randomKey = possibleKeys[Random.Range(0, possibleKeys.Count)];
        
        // Space Tuşu Özel Ayarı
        if (randomKey == KeyCode.Space)
        {
            newKey.GetComponentInChildren<TextMeshProUGUI>().text = "SPACE";
            newKey.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 80); 
        }
        else
        {
            newKey.GetComponentInChildren<TextMeshProUGUI>().text = randomKey.ToString();
            newKey.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 80);
        }

        ActiveNote noteData = new ActiveNote
        {
            obj = newKey,
            rect = newKey.GetComponent<RectTransform>(),
            key = randomKey
        };
        
        activeNotes.Add(noteData);
    }

    void Update()
    {
        if (!isGameActive) return;

        // --- 1. NOTALARI HAREKET ETTİR VE KAÇIRMA KONTROLÜ ---
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            ActiveNote note = activeNotes[i];
            
            // Sola doğru kaydır
            note.rect.anchoredPosition -= new Vector2(currentNoteSpeed * Time.deltaTime, 0);

            // EKRANDAN ÇIKTI MI? (KAÇIRMA)
            if (note.rect.anchoredPosition.x < targetZone.anchoredPosition.x - goodDistance)
            {
                MissNote(note);          // İşlemi yap
                activeNotes.RemoveAt(i); // Listeden sil
                CheckTurnEnd();          // Sonra kontrol et
            }
        }

        // --- 2. INPUT KONTROLÜ (Hareket döngüsünden BAĞIMSIZ olmalı) ---
        if (Input.anyKeyDown && activeNotes.Count > 0)
        {
            // En öndeki (Target'a en yakın) notayı al
            ActiveNote targetNote = activeNotes[0];

            // Oyuncunun bastığı tuş, notanın tuşu mu?
            if (Input.GetKeyDown(targetNote.key))
            {
                // Mesafeyi hesapla
                float distance = Mathf.Abs(targetNote.rect.anchoredPosition.x - targetZone.anchoredPosition.x);

                if (distance <= goodDistance)
                {
                    // Menzil içindeyse VURDU
                    // (Critical kontrolü için perfectDistance kullanılabilir)
                    bool isCrit = distance <= perfectDistance;
                    HitNote(targetNote, isCrit); 
                }
                else
                {
                    // Erken bastı ama çok uzakta (Cezalandırabilirsin veya yok sayabilirsin)
                    Debug.Log("Çok erken bastın!");
                }
            }
            // Yanlış tuşa basarsa (Opsiyonel ceza eklenebilir)
        }
    }

    void HitNote(ActiveNote note, bool isCritical)
    {
        Destroy(note.obj);
        activeNotes.Remove(note);
        notesProcessedInTurn++;
        successfulHitsInTurn++; 

        if (isCritical)
        {
            feedbackText.text = "MÜKEMMEL!";
            feedbackText.color = Color.yellow;
            feedbackText.transform.DOPunchScale(Vector3.one * 0.3f, 0.1f);
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
        Destroy(note.obj);
        notesProcessedInTurn++;
        
        feedbackText.text = "KAÇTI!";
        feedbackText.color = Color.red;

       
    }

    // --- TUR SONU HESAPLAMA ---
    void CheckTurnEnd()
    {
        if (notesProcessedInTurn >= notesPerTurn && activeNotes.Count == 0)
        {
            isGameActive = false; // Oyunu dondur
            StartCoroutine(ResolveTurnRoutine());
        }
    }

    IEnumerator ResolveTurnRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        // SALDIRI SIRASI
        if (currentTurn == TurnState.PlayerTurn)
        {
            // 5 notanın en az 3'ünü vurdun mu?
            if (successfulHitsInTurn >= 3)
            {
                feedbackText.text = "KOMBO TAMAMLANDI!";
                feedbackText.color = Color.yellow;

                // Sinematik Vuruş
                CinematicAction.Instance.PlayPlayerCriticalAttack(() => 
                {
                    DealDamageToEnemy(damagePerPhase);
                });
            }
            else
            {
                feedbackText.text = "SALDIRI BAŞARISIZ...";
                feedbackText.color = Color.gray;
                yield return new WaitForSeconds(1.5f);
                NextTurn();
            }
        }
        // SAVUNMA SIRASI
        else
        {
            // En az 3 tanesini blokladın mı?
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
                
                // Düşman Sinematiği
                CinematicAction.Instance.PlayEnemyAttack(() => 
                {
                    DealDamageToPlayer(penaltyDamage);
                });
            }
        }
    }

    void DealDamageToEnemy(int amount)
    {
        currentEnemyHP -= amount;
        UpdateHealthUI();
        
        if (currentEnemyHP <= 0) 
        {
            EndGame(true);
        }
        else 
        {
            NextTurn(); // Hata buradaydı, düzeltildi
        }
    }

    void DealDamageToPlayer(int amount)
    {
        currentPlayerHP -= amount;
        UpdateHealthUI();
        
        Camera.main.transform.DOShakePosition(0.5f, 15, 20);
        
        if (currentPlayerHP <= 0) 
        {
            EndGame(false);
        }
        else 
        {
            NextTurn(); // Hata buradaydı, düzeltildi
        }
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
        Debug.Log("Oyun Bitti: " + (playerWon ? "Zafer" : "Yenilgi"));
    }

    void UpdateHealthUI()
    {
        if (playerHealthBar) playerHealthBar.value = currentPlayerHP;
        if (enemyHealthBar) enemyHealthBar.value = currentEnemyHP;
    }
}