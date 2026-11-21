using UnityEngine;

[CreateAssetMenu(fileName = "AudioLibrary", menuName = "BusJam/Data/Audio Library")]
public class AudioLibrarySO : ScriptableObject
{
    [Header("SFX Clips")]
    public AudioClip click;
    public AudioClip honk;
    public AudioClip engine;
    public AudioClip footstep;
    public AudioClip crash;
    public AudioClip win;
    public AudioClip lose;
    public AudioClip coin;
    public AudioClip pop;
    public AudioClip booster;
    public AudioClip unlock;

    [Header("Background Music")]
    public AudioClip bgm;
}
