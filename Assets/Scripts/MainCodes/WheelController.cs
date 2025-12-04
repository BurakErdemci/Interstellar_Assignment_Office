using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WheelController : MonoBehaviour
{
    [Header("Referanslar")]
    public GameObject wheelPanel;      
    public Transform rotator;          
    public Image greenSlice;           
    public TextMeshProUGUI resultText; 

    [Header("Audio Settings")]
    public AudioSource mechanicalSource; // "Tırrr" dişli sesi (Pitch değişir)
    public AudioSource tensionSource;    // GERİLİM SESİ
    public AudioSource fxSource;         // Win/Fail efekti (Ding/Bzzzt)
    
    [Header("Audio Clips")]
    public AudioClip mechanicalClip;     // Dişli/Motor sesi
    public AudioClip tensionClip;        // gerilim sesi
    public AudioClip winClip;            // Kazanma sesi
    public AudioClip failClip;           // Kaybetme sesi

    [Header("Ayarlar")]
    public float spinDuration = 4f;    
    public AnimationCurve spinCurve;
    [Range(0, 360)] public float rotationOffset = 0f; 

    private void Start()
    {
        wheelPanel.SetActive(false);
    }

    public void StartSpin(float successChance01, bool finalResultIsSuccess, System.Action onSpinEnd)
    {
        wheelPanel.SetActive(true);
        resultText.text = ""; 
        rotator.localRotation = Quaternion.identity;

        // Görsel Ayarı
        float visualChance = Mathf.Clamp(successChance01, 0.05f, 0.95f);
        greenSlice.fillAmount = visualChance;

        // Hedef Açı
        float targetZ = CalculateTargetRotation(visualChance, finalResultIsSuccess);

        // --- 1. MEKANİK SESİ BAŞLAT  ---
        if (mechanicalClip != null && mechanicalSource != null)
        {
            mechanicalSource.clip = mechanicalClip;
            mechanicalSource.loop = true; 
            mechanicalSource.pitch = 1f;  
            mechanicalSource.Play();
        }

        // --- 2. GERİLİM SESİNİ BAŞLAT  ---
        if (tensionClip != null && tensionSource != null)
        {
            tensionSource.clip = tensionClip;
            tensionSource.loop = false; // 6 saniye zaten yeterli, looplamasın
            tensionSource.volume = 1f;  // Ses seviyesi
            tensionSource.Play();
        }

        StartCoroutine(SpinRoutine(targetZ, onSpinEnd, finalResultIsSuccess));
    }

    float CalculateTargetRotation(float chance, bool isSuccess)
    {
        float boundaryAngle = chance * 360f; 
        float pickedAngle;

        if (isSuccess)
            pickedAngle = Random.Range(10f, boundaryAngle - 10f); 
        else
            pickedAngle = Random.Range(boundaryAngle + 10f, 350f);

        return (360f * 5f) + pickedAngle + rotationOffset; 
    }

    IEnumerator SpinRoutine(float targetAngle, System.Action onSpinEnd, bool isSuccess)
    {
        float timer = 0f;
        float startAngle = 0f; 

        while (timer < spinDuration)
        {
            timer += Time.deltaTime;
            float percentage = timer / spinDuration;
            float curveValues = spinCurve.Evaluate(percentage);
            
            float currentZ = Mathf.Lerp(startAngle, targetAngle, curveValues);
            rotator.localRotation = Quaternion.Euler(0, 0, currentZ);

            // --- SES EFEKTİ: SADECE MEKANİK SES YAVAŞLASIN ---
            if (mechanicalSource != null)
            {
                // Mekanik ses kalınlaşarak yavaşlar
                mechanicalSource.pitch = Mathf.Lerp(1.0f, 0.2f, percentage);
            }

            yield return null;
        }

        rotator.localRotation = Quaternion.Euler(0, 0, targetAngle);

        // --- DÖNME BİTTİ: BÜTÜN DÖNME SESLERİNİ SUSTUR ---
        if (mechanicalSource != null) mechanicalSource.Stop();
        if (tensionSource != null) tensionSource.Stop(); // Gerilim de bitsin

        // --- SONUÇ SESİNİ ÇAL ---
        if (fxSource != null)
        {
            if (isSuccess && winClip != null)
                fxSource.PlayOneShot(winClip);
            else if (!isSuccess && failClip != null)
                fxSource.PlayOneShot(failClip);
        }

        onSpinEnd?.Invoke();
    }

    public void CloseWheel()
    {
        // Garanti olsun diye kapatırken de sesleri susturalım
        if (mechanicalSource != null) mechanicalSource.Stop();
        if (tensionSource != null) tensionSource.Stop();
        
        wheelPanel.SetActive(false);
    }
}