using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class CinematicAction : MonoBehaviour
{
    public static CinematicAction Instance;

    [Header("Scene Objects")]
    public RectTransform playerObj;   
    public Image playerImage;         
    public RectTransform enemyObj;    
    public Image enemyImage;          
    public GameObject hitEffectPrefab;

    [Header("UI Layers")]
    public GameObject playerCutIn;    // Senin Gözler
    public GameObject enemyAttackCutIn; // Hırsızın KIZGIN Gözleri (Saldırırken)
    public GameObject enemySurpriseCutIn; // Hırsızın ŞAŞKIN Gözleri (Dayak yerken)
    public CanvasGroup uiLayer;       

    [Header("Player Sprites")]
    public Sprite playerIdle;
    public Sprite playerAttack;
    public Sprite playerHit;

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

    // --- KRİTİK RUTİNİ ---
    IEnumerator PlayerCriticalRoutine(System.Action onComplete)
    {
        uiLayer.DOFade(0, 0.5f); 

        // A. SENİN GÖZLER
        playerCutIn.SetActive(true);
        playerCutIn.transform.localScale = new Vector3(1, 0, 1);
        playerCutIn.transform.DOScaleY(1, 0.2f).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(1.5f); 
        playerCutIn.transform.DOScaleY(0, 0.2f).OnComplete(() => playerCutIn.SetActive(false));
        
        // B. HIRSIZIN ŞAŞKIN GÖZLERİ (YENİ!)
        enemySurpriseCutIn.SetActive(true);
        enemySurpriseCutIn.transform.localScale = new Vector3(1, 0, 1);
        enemySurpriseCutIn.transform.DOScaleY(1, 0.2f).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(1.0f);
        enemySurpriseCutIn.transform.DOScaleY(0, 0.2f).OnComplete(() => enemySurpriseCutIn.SetActive(false));

        // C. VURUŞ
        yield return StartCoroutine(PerformHitLogic());

        uiLayer.DOFade(1, 1f);
        onComplete?.Invoke();
    }

    // --- BASİT RUTİN (HIZLI) ---
    IEnumerator SimpleAttackRoutine(System.Action onComplete)
    {
        // Cut-in yok, bekleme yok, direkt dalıyoruz
        yield return StartCoroutine(PerformHitLogic());
        onComplete?.Invoke();
    }

    // Ortak Vuruş Mantığı (Kod tekrarını önlemek için)
    IEnumerator PerformHitLogic()
    {
        playerImage.sprite = playerAttack; 
        enemyImage.sprite = enemyHit;      

        playerObj.anchoredPosition = enemyObj.anchoredPosition + new Vector2(-150, 0);
        
        if(hitEffectPrefab)
        {
            hitEffectPrefab.SetActive(true);
            hitEffectPrefab.transform.localScale = Vector3.zero;
            hitEffectPrefab.transform.DOScale(1.5f, 0.2f);
        }

        Camera.main.transform.DOShakePosition(0.5f, 10, 20); // Daha kısa sarsıntı
        enemyImage.DOColor(Color.red, 0.2f).SetLoops(2, LoopType.Yoyo);

        yield return new WaitForSeconds(0.6f); // Kısa bekleme

        // Reset
        if(hitEffectPrefab) hitEffectPrefab.SetActive(false);
        playerImage.sprite = playerIdle; 
        enemyImage.sprite = enemyIdle;
        playerImage.color = Color.white;
        enemyImage.color = Color.white;
        playerObj.anchoredPosition = playerStartPos;
    }

    // --- DÜŞMAN SALDIRISI ---
    public void PlayEnemyAttack(System.Action onComplete)
    {
        StartCoroutine(EnemyAttackRoutine(onComplete));
    }

    IEnumerator EnemyAttackRoutine(System.Action onComplete)
    {
        // Düşmanda sadece tek tip saldırı olsun şimdilik
        uiLayer.DOFade(0, 0.2f);

        // KIZGIN GÖZLER (Attack Eyes)
        enemyAttackCutIn.SetActive(true);
        enemyAttackCutIn.transform.localScale = new Vector3(1, 0, 1);
        enemyAttackCutIn.transform.DOScaleY(1, 0.2f).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(1f);
        enemyAttackCutIn.transform.DOScaleY(0, 0.2f).OnComplete(() => enemyAttackCutIn.SetActive(false));

        enemyImage.sprite = enemyAttack;
        playerImage.sprite = playerHit; 

        enemyObj.anchoredPosition = playerObj.anchoredPosition + new Vector2(150, 0);
        Camera.main.transform.DOShakePosition(0.5f, 20, 20); 
        playerImage.DOColor(Color.red, 0.2f).SetLoops(4, LoopType.Yoyo); 

        yield return new WaitForSeconds(1.0f);

        playerImage.color = Color.white;
        enemyImage.color = Color.white;
        enemyImage.sprite = enemyIdle;
        playerImage.sprite = playerIdle;
        enemyObj.anchoredPosition = enemyStartPos;

        uiLayer.DOFade(1, 0.5f);
        onComplete?.Invoke();
    }
}