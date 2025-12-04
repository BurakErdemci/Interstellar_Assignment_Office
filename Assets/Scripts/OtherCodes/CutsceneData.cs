using UnityEngine;

[CreateAssetMenu(fileName = "NewIntro", menuName = "Story/Cutscene")]
public class CutsceneData : ScriptableObject
{
    [Header("Global Audio")]
    public AudioClip backgroundMusic;

    // Konuşmacı türleri
    public enum SpeakerType { JackInternal, Tiny, Phone }

    [System.Serializable]
    public struct Panel
    {
        public Sprite image;
        [TextArea(3, 10)] public string text;
        public AudioClip soundEffect;
        
        // Bu karede kim konuşuyor?
        public SpeakerType speaker; 
    }

    public Panel[] panels;
    public string nextSceneName;
}