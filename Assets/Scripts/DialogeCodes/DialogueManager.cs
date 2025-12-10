using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening; // DOTween'i unutma!

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI Components")]
    public GameObject dialoguePanel; // Ana kutu
    public Image leftPortrait;       // Soldaki karakter resmi
    public Image rightPortrait;      // Sağdaki karakter resmi
    public TextMeshProUGUI nameText; // İsim
    public TextMeshProUGUI bodyText; // Konuşma metni
    public GameObject continueIcon;  // "Devam et" oku

    [Header("Audio")]
    public AudioSource audioSource;  // Konuşma sesi için hoparlör

    [Header("Settings")]
    public float typeSpeed = 0.03f; // Yazı hızı

    private Queue<DialogueData.Line> linesQueue = new Queue<DialogueData.Line>();
    private bool isTyping = false;
    private string currentFullText = "";
    
    // Diyalog bitince çalışacak fonksiyon (Geri çağırma)
    private System.Action onDialogueEnd; 

    private void Awake() 
    { 
        Instance = this; 
        if(dialoguePanel) dialoguePanel.SetActive(false);
    }

    public void StartDialogue(DialogueData data, System.Action onEndCallback = null)
    {
        onDialogueEnd = onEndCallback;
        
        // Paneli Aç (Animasyonlu)
        dialoguePanel.SetActive(true);
        dialoguePanel.transform.localScale = Vector3.zero;
        dialoguePanel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

        // Kuyruğu doldur
        linesQueue.Clear();
        foreach (var line in data.conversationLines)
        {
            linesQueue.Enqueue(line);
        }

        DisplayNextLine();
    }

    public void DisplayNextLine()
    {
        // Kuyruk bittiyse diyaloğu bitir
        if (linesQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        var line = linesQueue.Dequeue();
        
        // UI Güncelle
        nameText.text = line.characterName;
        currentFullText = line.text;

        // Resim Ayarı (Sol mu Sağ mı?)
        if (line.isLeftPosition)
        {
            leftPortrait.gameObject.SetActive(true);
            rightPortrait.gameObject.SetActive(false);
            
            if(line.characterSprite) leftPortrait.sprite = line.characterSprite;
            
            // Konuşan resmi hafifçe zıplat
            leftPortrait.transform.DOKill();
            leftPortrait.transform.localScale = Vector3.one;
            leftPortrait.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
            
            // Eğer siyah-beyaz (karartma) efekti yapacaksan rengi burada açarsın:
            leftPortrait.color = Color.white;
        }
        else
        {
            leftPortrait.gameObject.SetActive(false);
            rightPortrait.gameObject.SetActive(true);
            
            if(line.characterSprite) rightPortrait.sprite = line.characterSprite;
            
            rightPortrait.transform.DOKill();
            rightPortrait.transform.localScale = Vector3.one;
            rightPortrait.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
            
            rightPortrait.color = Color.white;
        }

        StopAllCoroutines();
        // Ses verilerini de gönderiyoruz
        StartCoroutine(TypeSentence(currentFullText, line.voiceClip, line.voicePitch));
    }

    IEnumerator TypeSentence(string sentence, AudioClip voiceClip, float pitch)
    {
        isTyping = true;
        bodyText.text = "";
        if(continueIcon) continueIcon.SetActive(false);

        int charCount = 0;

        foreach (char letter in sentence.ToCharArray())
        {
            bodyText.text += letter;

            // --- SES ÇALMA (Beep-Boop Efekti) ---
            // 1. Boşluk değilse
            // 2. Ses dosyası varsa
            // 3. Her 2 harfte bir çal (Kafa ütülemesin diye)
            if (!char.IsWhiteSpace(letter) && voiceClip != null && charCount % 2 == 0)
            {
                audioSource.pitch = pitch + Random.Range(-0.1f, 0.1f); // Hafif ton farkı kat
                audioSource.PlayOneShot(voiceClip);
            }
            charCount++;

            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
        if(continueIcon) continueIcon.SetActive(true);
    }

    void EndDialogue()
    {
        // Paneli kapat
        dialoguePanel.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack).OnComplete(() => 
        {
            dialoguePanel.SetActive(false);
            onDialogueEnd?.Invoke(); // Bitiş işlemini (MapManager'daki kodun devamını) çalıştır
        });
    }

    void Update()
    {
        // Tıklama Kontrolü (Panel açıksa)
        if (dialoguePanel.activeInHierarchy && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
        {
            if (isTyping)
            {
                // Hızlı tamamla
                StopAllCoroutines();
                bodyText.text = currentFullText;
                isTyping = false;
                if(continueIcon) continueIcon.SetActive(true);
            }
            else
            {
                // Sonraki satır
                DisplayNextLine();
            }
        }
    }
}