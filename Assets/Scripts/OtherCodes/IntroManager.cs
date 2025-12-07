using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using DG.Tweening; // DOTween kütüphanesi şart!

public class IntroManager : MonoBehaviour
{
    [Header("Data")]
    public CutsceneData introData;

    [Header("UI Elements")]
    public Image comicImage;       
    public TextMeshProUGUI subText; 
    public GameObject continueIcon;

    [Header("Audio Sources")]
    public AudioSource musicSource;      
    public AudioSource sfxSource;        
    public AudioSource typingSource;     

    [Header("Settings")]
    public float typeSpeed = 0.04f;
    public AudioClip typewriterClickSound; 

    private int currentPanelIndex = 0;
    private bool isTyping = false;
    private string currentFullText = "";

    private void Start()
    {
        if (continueIcon) continueIcon.SetActive(false);

     
        comicImage.color = new Color(1, 1, 1, 0); 

        if (introData != null)
        {
            if (introData.backgroundMusic != null)
            {
                musicSource.clip = introData.backgroundMusic;
                musicSource.loop = true; 
                musicSource.Play();
            }

            ShowPanel(0);
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
                // Hızlı geçiş
                StopAllCoroutines();
                // Devam eden DOTween efektlerini (Yazı için varsa) durdurma, sadece daktiloyu bitir
                subText.text = currentFullText;
                isTyping = false;
                if (continueIcon) continueIcon.SetActive(true);
            }
            else
            {
                NextPanel();
            }
        }
    }

void ShowPanel(int index)
    {
        // Eski panel ile yeni panelin resimlerini karşılaştırmak için önceki indexi al
        int prevIndex = currentPanelIndex;
        
        currentPanelIndex = index;
        var panel = introData.panels[index];

        // Her durumda eski yazıyı hemen sil
        subText.text = "";

        // --- KONTROL: Resim Aynı mı? ---
        bool isSameImage = false;
        
        // Eğer ilk kare değilsek (index > 0) ve resimler aynıysa
        if (index > 0 && panel.image == introData.panels[prevIndex].image)
        {
            isSameImage = true;
        }

        if (isSameImage)
        {
            // --- SENARYO 1: AYNI RESİM 
           

            if (panel.soundEffect != null)
            {
                sfxSource.Stop();
                sfxSource.PlayOneShot(panel.soundEffect);
            }

            SetupTextStyle(panel.speaker);

            // Yazıyı hemen başlat
            currentFullText = panel.text;
            StartCoroutine(TypeWriterEffect(currentFullText));
            
       
        }
        else
        {
            // --- SENARYO 2: FARKLI RESİM
         

            Sequence seq = DOTween.Sequence();

            // A. Karart
            seq.Append(comicImage.DOFade(0, 0.3f));

            // B. Değiştir
            seq.AppendCallback(() => 
            {
                comicImage.sprite = panel.image;
                comicImage.transform.localScale = Vector3.one; // Zoom'u sıfırla
                
                if (panel.soundEffect != null)
                {
                    sfxSource.Stop();
                    sfxSource.PlayOneShot(panel.soundEffect);
                }

                SetupTextStyle(panel.speaker);
            });

            // C. Aç ve Zoomla
            seq.Append(comicImage.DOFade(1, 1f));
            
          
            comicImage.transform.DOKill(); 
            comicImage.transform.DOScale(1.1f, 10f).SetEase(Ease.Linear);

            // D. Yazıyı Başlat
            seq.AppendCallback(() => 
            {
                currentFullText = panel.text;
                StartCoroutine(TypeWriterEffect(currentFullText));
            });
        }
    }
    void SetupTextStyle(CutsceneData.SpeakerType speaker)
    {
        switch (speaker)
        {
            case CutsceneData.SpeakerType.JackInternal:
                // Jack SARI
                subText.color = Color.yellow; 
                subText.fontStyle = FontStyles.Normal;
                break;

            case CutsceneData.SpeakerType.Tiny:
                // Tiny YEŞİL ve KALIN
                subText.color = Color.green; 
                subText.fontStyle = FontStyles.Bold;
                break;

            case CutsceneData.SpeakerType.Phone:
                // Telefon Beyaz 
                subText.color = Color.white; 
                subText.fontStyle = FontStyles.Italic;
                break;
        }
    }

    IEnumerator TypeWriterEffect(string text)
    {
        isTyping = true;
        subText.text = "";
        if (continueIcon) continueIcon.SetActive(false);

        foreach (char letter in text.ToCharArray())
        {
            subText.text += letter;

            if (typewriterClickSound != null && !char.IsWhiteSpace(letter))
            {
                typingSource.pitch = Random.Range(0.9f, 1.1f);
                typingSource.PlayOneShot(typewriterClickSound);
            }

            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
        if (continueIcon) continueIcon.SetActive(true);
    }

    void NextPanel()
    {
        if (currentPanelIndex < introData.panels.Length - 1)
        {
            ShowPanel(currentPanelIndex + 1);
        }
        else
        {
            // Sahne Geçişinde Müzik Yavaşça Kısılsın (Fade Out)
            musicSource.DOFade(0, 1f).OnComplete(() => 
            {
                Debug.Log("Intro Bitti. Sahne Yükleniyor...");
                SceneManager.LoadScene(introData.nextSceneName);
            });
        }
    }
}