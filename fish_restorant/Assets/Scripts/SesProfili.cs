using UnityEngine;

[CreateAssetMenu(fileName = "YeniSesProfili", menuName = "Olustur/Ses Profili")]
public class SesProfili : ScriptableObject
{
    [Header("Nesne Sesleri")]
    public AudioClip almaSesi;
    public AudioClip birakmaSesi;
    public AudioClip hasatSesi;
    public AudioClip satisSesi;
    public AudioClip kullanimSesi;
    public AudioClip kirilmaSesi;

    [Header("Ses Ayarlari")]
    [Range(0f, 1f)]
    public float sesSeviyesi = 1f;

    [Range(0.8f, 1.2f)]
    public float pitchMin = 0.95f;

    [Range(0.8f, 1.2f)]
    public float pitchMax = 1.05f;

    public float RastgelePitch => Random.Range(pitchMin, pitchMax);
}