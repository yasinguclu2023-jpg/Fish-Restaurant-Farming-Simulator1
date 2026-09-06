using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Steam sayfası / fragman için göstermelik animasyon sekansı.
/// Sıralı adımları (Animator trigger, nesne göster/gizle, ses) zamanlamalı oynatır.
/// Bağımsız çalışır; mevcut oynanış sistemlerine dokunmaz.
/// Oynat() ile başlatılır (GostermelikTetikleyici veya UnityEvent üzerinden).
/// </summary>
public class GostermelikSekans : MonoBehaviour
{
    public enum AdimTipi
    {
        AnimatorTetikle,   // Animator'a trigger gönder
        AnimatorBoolAyarla,// Animator bool parametresi set et
        NesneGoster,       // GameObject.SetActive(true)
        NesneGizle,        // GameObject.SetActive(false)
        SesCal             // SesYoneticisi üzerinden ses çal
    }

    [System.Serializable]
    public class Adim
    {
        [Tooltip("Inspector'da okunabilirlik için açıklama (çalışmayı etkilemez)")]
        public string aciklama;

        [Tooltip("Bu adım çalışmadan ÖNCE beklenecek süre (saniye)")]
        public float gecikme = 0f;

        public AdimTipi tip = AdimTipi.AnimatorTetikle;

        [Header("Animator (Tetikle / Bool için)")]
        public Animator animator;
        [Tooltip("Animator parametre adı (trigger veya bool)")]
        public string parametreAdi;
        [Tooltip("AnimatorBoolAyarla için verilecek değer")]
        public bool boolDeger = true;

        [Header("Nesne (Göster / Gizle için)")]
        public GameObject hedefObje;

        [Header("Ses (SesCal için)")]
        public SesVerisi ses;
    }

    [Header("Sekans Adımları (sırayla çalışır)")]
    [SerializeField] private List<Adim> adimlar = new List<Adim>();

    [Header("Ayarlar")]
    [Tooltip("Açıkken sekans yalnızca bir kez oynatılabilir")]
    [SerializeField] private bool tekSefer = false;
    [Tooltip("Oynarken tekrar Oynat() çağrılırsa yok say")]
    [SerializeField] private bool oynarkenTekrariEngelle = true;

    [Header("Olaylar")]
    public UnityEvent onSekansBasladi;
    public UnityEvent onSekansBitti;

    private bool oynuyor;
    private bool oynatildi;

    public bool Oynuyor => oynuyor;
    public bool Oynatildi => oynatildi;

    /// <summary>
    /// Sekansı baştan başlatır.
    /// </summary>
    public void Oynat()
    {
        if (tekSefer && oynatildi) return;
        if (oynuyor && oynarkenTekrariEngelle) return;

        StopAllCoroutines();
        StartCoroutine(SekansCalistir());
    }

    /// <summary>
    /// Çalışan sekansı durdurur (adımlar yarıda kalır).
    /// </summary>
    public void Durdur()
    {
        StopAllCoroutines();
        oynuyor = false;
    }

    /// <summary>
    /// tekSefer kilidini sıfırlar (tekrar oynatılabilir hale getirir).
    /// </summary>
    public void Sifirla()
    {
        oynatildi = false;
    }

    private IEnumerator SekansCalistir()
    {
        oynuyor = true;
        oynatildi = true;
        onSekansBasladi?.Invoke();

        for (int i = 0; i < adimlar.Count; i++)
        {
            Adim adim = adimlar[i];

            if (adim.gecikme > 0f)
                yield return new WaitForSeconds(adim.gecikme);

            AdimUygula(adim);
        }

        oynuyor = false;
        onSekansBitti?.Invoke();
    }

    private void AdimUygula(Adim adim)
    {
        switch (adim.tip)
        {
            case AdimTipi.AnimatorTetikle:
                if (adim.animator != null && !string.IsNullOrEmpty(adim.parametreAdi))
                    adim.animator.SetTrigger(adim.parametreAdi);
                break;

            case AdimTipi.AnimatorBoolAyarla:
                if (adim.animator != null && !string.IsNullOrEmpty(adim.parametreAdi))
                    adim.animator.SetBool(adim.parametreAdi, adim.boolDeger);
                break;

            case AdimTipi.NesneGoster:
                if (adim.hedefObje != null)
                    adim.hedefObje.SetActive(true);
                break;

            case AdimTipi.NesneGizle:
                if (adim.hedefObje != null)
                    adim.hedefObje.SetActive(false);
                break;

            case AdimTipi.SesCal:
                if (adim.ses != null && SesYoneticisi.Instance != null)
                    SesYoneticisi.Instance.SesCal(adim.ses, transform.position);
                break;
        }
    }
}
