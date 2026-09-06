using UnityEngine;

/// <summary>
/// Her ses tipi için bir ScriptableObject asset'i oluþtur.
/// Project > Right Click > Ses Sistemi > Ses Verisi
/// </summary>
[CreateAssetMenu(fileName = "YeniSes", menuName = "Ses Sistemi/Ses Verisi")]
public class SesVerisi : ScriptableObject
{
    [Header("Ses Klibi")]
    [Tooltip("Çalýnacak ses. Birden fazla atarsan rastgele seçilir.")]
    [SerializeField] private AudioClip[] sesKlipleri;

    [Header("Ses Ayarlarý")]
    [Range(0f, 1f)]
    [SerializeField] private float sesSeviyesi = 1f;

    [Tooltip("Her çalýþta pitch'i bu aralýkta rastgele deðiþtirir (doðallýk için)")]
    [SerializeField] private Vector2 pitchAraligi = new Vector2(0.95f, 1.05f);

    [Header("3D Ses Ayarlarý")]
    [Tooltip("0 = 2D ses, 1 = tam 3D ses")]
    [Range(0f, 1f)]
    [SerializeField] private float uzamsallik = 1f;

    [Tooltip("Sesin duyulmaya baþladýðý mesafe")]
    [SerializeField] private float minMesafe = 1f;

    [Tooltip("Sesin tamamen kaybolduðu mesafe")]
    [SerializeField] private float maxMesafe = 20f;

    // Public eriþimler
    public AudioClip RastgeleKlipAl()
    {
        if (sesKlipleri == null || sesKlipleri.Length == 0) return null;
        return sesKlipleri[Random.Range(0, sesKlipleri.Length)];
    }

    public float SesSeviyesi => sesSeviyesi;
    public float RastgelePitch => Random.Range(pitchAraligi.x, pitchAraligi.y);
    public float Uzamsallik => uzamsallik;
    public float MinMesafe => minMesafe;
    public float MaxMesafe => maxMesafe;
    public bool GecerliMi => sesKlipleri != null && sesKlipleri.Length > 0;
}