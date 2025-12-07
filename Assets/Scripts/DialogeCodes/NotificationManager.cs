using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance;

    [Header("UI Components")]
    public RectTransform panelRect; // Panelin kendisi
    public Image portraitImage;     // Konuşanın resmi
    public TextMeshProUGUI messageText;

    [Header("Settings")]
    public float showDuration = 3f; // Ekranda kalma süresi
    public float slideDuration = 0.5f; // İnme/Çıkma hızı
    public Vector2 hiddenPos; // Ekran dışı konumu (Yüksek Y değeri)
    public Vector2 visiblePos; // Ekran içi konumu

    private Coroutine currentRoutine;

    private void Awake() 
    { 
        Instance = this; 
     
        visiblePos = panelRect.anchoredPosition;
        hiddenPos = visiblePos + new Vector2(0, 150); // 150 birim yukarı sakla
        
        panelRect.anchoredPosition = hiddenPos; // Başta gizle
    }

    public void ShowMessage(string message, Sprite icon)
    {
        // Eğer zaten bir mesaj varsa, rutini durdur ve hemen yenisini göster
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        
        currentRoutine = StartCoroutine(ShowRoutine(message, icon));
    }

    IEnumerator ShowRoutine(string message, Sprite icon)
    {
        // 1. Verileri Doldur
        messageText.text = message;
        
        if (icon != null)
        {
            portraitImage.sprite = icon;
            portraitImage.gameObject.SetActive(true);
        }
        else
        {
            portraitImage.gameObject.SetActive(false); // Resim yoksa kapat
        }

        // 2. Paneli İndir (Slide In)
        panelRect.DOAnchorPos(visiblePos, slideDuration).SetEase(Ease.OutBack);
        

        // 3. Bekle
        yield return new WaitForSeconds(showDuration);

        // 4. Paneli Kaldır (Slide Out)
        panelRect.DOAnchorPos(hiddenPos, slideDuration).SetEase(Ease.InBack);
    }
}