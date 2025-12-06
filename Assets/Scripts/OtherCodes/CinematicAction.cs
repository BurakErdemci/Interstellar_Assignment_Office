using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class CinematicAction : MonoBehaviour
{
    public static CinematicAction Instance;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip cutInSound;
    public AudioClip heavyHitSound;

    [Header("Scene Objects")]
    public RectTransform playerObj;   
    public Image playerImage;         
    public RectTransform enemyObj;    
    public Image enemyImage;          
    public GameObject hitEffectPrefab;

    [Header("UI Layers")]
    public GameObject playerCutIn;    
    public GameObject enemyAttackCutIn; 
    public GameObject enemySurpriseCutIn; 
    public CanvasGroup uiLayer;       

    [Header("Player Sprites")]
    public Sprite playerIdle;   // Normal duruş
    public Sprite playerAttack; // Vuruş resmi
    public Sprite playerHit;    // Hasar yeme resmi (Varsa)

    [Header("Enemy Sprites")]
    public Sprite enemyIdle;
    public Sprite enemyAttack;
    public Sprite enemyHit;

    private Vector3 playerStartPos;
    private Vector3 enemyStartPos;

    private void Awake() { Instance = this; }

    private void Start()
    {
        playerStartPos = playerObj.anchoredPosition;
        enemyStartPos = enemyObj.anchoredPosition;
        
        // Başlangıç resimlerini ayarla
        playerImage.sprite = playerIdle;
        enemyImage.sprite = enemyIdle;

        if(playerCutIn) playerCutIn.SetActive(false);
        if(enemyAttackCutIn) enemyAttackCutIn.SetActive(false);
        if(enemySurpriseCutIn) enemySurpriseCutIn.SetActive(false);
        if(hitEffectPrefab) hitEffectPrefab.SetActive(false);
    }

    // --- 1. SİNEMATİK SALDIRI (KRİTİK) ---
    public void PlayPlayerCriticalAttack(System.Action onComplete)
    {
        StartCoroutine(PlayerCriticalRoutine(onComplete));
    }

    // --- 2. BASİT SALDIRI (HIZLI) ---
    public void PlayPlayerSimpleAttack(System.Action onComplete)
    {
        StartCoroutine(SimpleAttackRoutine(onComplete));
    }

    IEnumerator PlayerCriticalRoutine(System.Action onComplete)
    {
        uiLayer.DOFade(0, 0.2f); 

        if(audioSource && cutInSound) audioSource.PlayOneShot(cutInSound);

        // A. SENİN GÖZLER
        if(playerCutIn)
        {
            playerCutIn.SetActive(true);
            playerCutIn.transform.localScale = new Vector3(1, 0, 1);
            playerCutIn.transform.DOScaleY(1, 0.2f).SetEase(Ease.OutBack);
            yield return new WaitForSeconds(1.5f); 
            playerCutIn.transform.DOScaleY(0, 0.2f).OnComplete(() => playerCutIn.SetActive(false));
        }
        
        // B. HIRSIZIN ŞAŞKIN GÖZLERİ
        if(enemySurpriseCutIn)
        {
            enemySurpriseCutIn.SetActive(true);
            enemySurpriseCutIn.transform.localScale = new Vector3(1, 0, 1);
            enemySurpriseCutIn.transform.DOScaleY(1, 0.2f).SetEase(Ease.OutBack);
            yield return new WaitForSeconds(1.0f);
            enemySurpriseCutIn.transform.DOScaleY(0, 0.2f).OnComplete(() => enemySurpriseCutIn.SetActive(false));
        }

        yield return StartCoroutine(PerformHitLogic());

        uiLayer.DOFade(1, 1f);
        onComplete?.Invoke();
    }

    IEnumerator SimpleAttackRoutine(System.Action onComplete)
    {
        yield return StartCoroutine(PerformHitLogic());
        onComplete?.Invoke();
    }

    // --- VURUŞ MANTIĞI (SPRITE SWAP + DASH) ---
    IEnumerator PerformHitLogic()
    {
        // 1. GERİLME
        playerObj.DOAnchorPos(playerStartPos + new Vector3(-50, -20, 0), 0.2f);
        yield return new WaitForSeconds(0.2f);

        // 2. SALDIRI POZUNA GEÇ & FIRLA
        playerImage.sprite = playerAttack; // <--- RESİM DEĞİŞTİ
        
        Vector3 targetPos = enemyObj.anchoredPosition + new Vector2(-150, 0);
        playerObj.DOAnchorPos(targetPos, 0.1f).SetEase(Ease.InExpo); 
        
        yield return new WaitForSeconds(0.1f);

        // 3. VURUŞ ANI
        enemyImage.sprite = enemyHit;      
        
        if(audioSource && heavyHitSound) audioSource.PlayOneShot(heavyHitSound);

        if(hitEffectPrefab)
        {
            hitEffectPrefab.SetActive(true);
            hitEffectPrefab.transform.localScale = Vector3.zero;
            hitEffectPrefab.transform.DOScale(1.5f, 0.1f);
        }

        Camera.main.transform.DOShakePosition(0.5f, 15, 20); 
        enemyImage.DOColor(Color.red, 0.2f).SetLoops(2, LoopType.Yoyo);

        yield return new WaitForSeconds(0.8f); 

        // 4. GERİ DÖNÜŞ
        if(hitEffectPrefab) hitEffectPrefab.SetActive(false);
        
        playerObj.DOJumpAnchorPos(playerStartPos, 50f, 1, 0.4f);

        // Resimleri Normale Döndür
        playerImage.sprite = playerIdle; // <--- IDLE'A DÖN
        enemyImage.sprite = enemyIdle;
        enemyImage.color = Color.white;
        
        yield return new WaitForSeconds(0.5f);
    }

    // --- DÜŞMAN SALDIRISI ---
    public void PlayEnemyAttack(System.Action onComplete)
    {
        StartCoroutine(EnemyAttackRoutine(onComplete));
    }

    IEnumerator EnemyAttackRoutine(System.Action onComplete)
    {
        uiLayer.DOFade(0, 0.2f);

        if(audioSource && cutInSound) audioSource.PlayOneShot(cutInSound);

        // 1. KIZGIN GÖZLER
        if (enemyAttackCutIn != null)
        {
            enemyAttackCutIn.SetActive(true);
            enemyAttackCutIn.transform.localScale = new Vector3(1, 0, 1);
            enemyAttackCutIn.transform.DOScaleY(1, 0.2f).SetEase(Ease.OutBack);
            yield return new WaitForSeconds(1.5f); 
            enemyAttackCutIn.transform.DOScaleY(0, 0.2f).OnComplete(() => enemyAttackCutIn.SetActive(false));
        }

        // 2. HAZIRLIK
        enemyObj.DOAnchorPos(enemyStartPos + new Vector3(50, -10, 0), 0.2f);
        yield return new WaitForSeconds(0.2f);

        // 3. SALDIRI
        enemyImage.sprite = enemyAttack; 
        
        Vector2 targetPos = playerObj.anchoredPosition + new Vector2(150, 0);
        enemyObj.DOAnchorPos(targetPos, 0.1f).SetEase(Ease.InExpo);
        
        yield return new WaitForSeconds(0.1f);

        // 4. VURUŞ ANI (Hasar yiyorsun)
        if(audioSource && heavyHitSound) audioSource.PlayOneShot(heavyHitSound);

        // Eğer hasar yeme resmin varsa kullan, yoksa sadece kızar
        if (playerHit != null) playerImage.sprite = playerHit; 
        playerImage.color = Color.red; 
        
        Camera.main.transform.DOShakePosition(0.4f, 20, 20);
        playerObj.DOAnchorPos(playerStartPos + new Vector3(-50, 0, 0), 0.1f).SetLoops(2, LoopType.Yoyo);

        yield return new WaitForSeconds(0.8f); 

        // 5. GERİ DÖNÜŞ
        enemyImage.sprite = enemyIdle;
        enemyObj.DOJumpAnchorPos(enemyStartPos, 50f, 1, 0.4f);

        playerImage.sprite = playerIdle; // <--- IDLE'A DÖN
        playerImage.color = Color.white;
        playerObj.anchoredPosition = playerStartPos;

        yield return new WaitForSeconds(0.5f);
        
        uiLayer.DOFade(1, 0.5f);
        onComplete?.Invoke();
    }
}