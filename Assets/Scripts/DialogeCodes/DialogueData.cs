using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewConversation", menuName = "Story/Dialogue")]
public class DialogueData : ScriptableObject
{
    [System.Serializable]
    public struct Line
    {
        [Header("Karakter Görseli")]
        public string characterName;   // Konuşan Kişi (Örn: Tiny)
        public Sprite characterSprite; // Yüz ifadesi (Mutlu, Kızgın vb.)
        public bool isLeftPosition;    // Tikliyse SOLDA, tiksizse SAĞDA durur
        
        [Header("Metin")]
        [TextArea(3, 10)] public string text; // Söylenecek söz
        
        [Header("Ses Efekti (Text Juice)")]
        public AudioClip voiceClip;    // Karakterin konuşma sesi (Bip, Tık, Pıt)
        [Range(0.5f, 2.5f)] public float voicePitch; // Ses tonu (0.8 = Kalın/Jack, 1.6 = İnce/Tiny)
    }

    public List<Line> conversationLines; // Konuşmanın tamamı
}