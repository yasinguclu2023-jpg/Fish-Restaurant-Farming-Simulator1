using UnityEngine;
using System;

/// <summary>
/// Bas�l� tutma i�lemlerini y�neten singleton.
/// UI bu eventleri dinler.
/// </summary>
public class BasiliTutmaYoneticisi : MonoBehaviour
{
    public static BasiliTutmaYoneticisi Instance { get; private set; }

    /// <summary>
    /// Basılı tutmayı hangi sistem başlattı. Radial UI'lar bununla filtrelenir (karışmaz).
    /// </summary>
    public enum TutmaKaynagi { NesneAlma, Kesme }

    // Events - kaynak taşır, böylece her RadialIlerlemeUI sadece kendi kaynağını dinler
    public event Action<TutmaKaynagi> OnBasladi;
    public event Action<TutmaKaynagi, float> OnIlerleme; // (kaynak, 0-1)
    public event Action<TutmaKaynagi> OnTamamlandi;
    public event Action<TutmaKaynagi> OnIptal;

    // Durum
    private bool aktif;
    private float baslangicZamani;
    private float hedefSure;
    private TutmaKaynagi aktifKaynak;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void Baslat(float sure, TutmaKaynagi kaynak = TutmaKaynagi.NesneAlma)
    {
        aktif = true;
        hedefSure = sure;
        baslangicZamani = Time.time;
        aktifKaynak = kaynak;
        OnBasladi?.Invoke(kaynak);
    }

    /// <summary>
    /// Her frame �a��r. true d�nerse tamamland�.
    /// </summary>
    public bool Guncelle()
    {
        if (!aktif) return false;

        float ilerleme = Mathf.Clamp01((Time.time - baslangicZamani) / hedefSure);
        OnIlerleme?.Invoke(aktifKaynak, ilerleme);

        if (ilerleme >= 1f)
        {
            aktif = false;
            OnTamamlandi?.Invoke(aktifKaynak);
            return true;
        }
        return false;
    }

    public void Iptal()
    {
        if (!aktif) return;
        aktif = false;
        OnIptal?.Invoke(aktifKaynak);
    }

    public bool Aktif => aktif;
    public TutmaKaynagi AktifKaynak => aktifKaynak;
}