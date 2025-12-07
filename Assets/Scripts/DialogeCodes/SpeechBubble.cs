using UnityEngine;
using TMPro;
using DG.Tweening; // DOTween şart!
using System.Collections;

public class SpeechBubble : MonoBehaviour
{
    [Header("UI")]
    public GameObject bubbleObject; // Balonun görseli (Image)
    public TextMeshProUGUI textMesh; // İçindeki yazı

    private void Awake()
    {
        // Oyun başlarken balon gizli olsun
        if(bubbleObject) bubbleObject.SetActive(false);
    }

    public void Speak(string message, float duration = 2.5f)
    {
        if (bubbleObject == null) return;

        // Varsa eski işlemi durdur
        StopAllCoroutines();

        // Yazıyı ata ve aç
        textMesh.text = message;
        bubbleObject.SetActive(true);

        // POP! Animasyonu (Küçükten büyüsün)
        bubbleObject.transform.localScale = Vector3.zero;
        bubbleObject.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

        // Kapatmak için sayacı başlat
        StartCoroutine(HideRoutine(duration));
    }

    IEnumerator HideRoutine(float time)
    {
        yield return new WaitForSeconds(time);

        // Kapanış animasyonu
        bubbleObject.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack)
            .OnComplete(() => bubbleObject.SetActive(false));
    }
}