using System;
using UnityEngine;

/// <summary>
/// Gun dongusunun merkezi yoneticisi (Singleton).
///
/// - Saat bitisSaati'ne ulasinca HICBIR SEY degismez; sadece gun sonu objesi basilabilir olur.
/// - Oyuncu gun sonu objesine basinca GunuBitirVeBaslat() cagrilir: butun sistemler
///   sifirlanir, gun sayaci artar ve yeni gun (dukkan KAPALI) baslar.
///
/// Sifirlama modulerdir: her sistem kendi Sifirla()/GunuSifirla() metoduyla temizlenir.
/// Ekstra sistemler icin ayrica GunYenidenBaslatildi event'i tetiklenir (UI, kayit vb.).
///
/// KURULUM:
/// 1. Sahnede bos bir GameObject olustur -> bu scripti ekle.
/// 2. "zamanSistemi" ve "musteriUretici" alanlarini surukle-birak ata.
/// 3. "tabelaSistemi" opsiyonel: atarsan yeni gunde tabela gorseli de kapali konuma doner.
/// 4. SiraYonetimi / SiparisYonetimi / MasaYonetimi sahnede varsa OTOMATIK bulunur.
/// </summary>
public class GunYoneticisi : MonoBehaviour
{
    public static GunYoneticisi Instance { get; private set; }

    [Header("Zorunlu Referanslar")]
    [Tooltip("Sahnedeki ZamanSistemi.")]
    [SerializeField] private ZamanSistemi zamanSistemi;

    [Tooltip("Sahnedeki MusteriUretici (gun sonunda musterileri temizler).")]
    [SerializeField] private MusteriUretici musteriUretici;

    [Header("Opsiyonel")]
    [Tooltip("Atanirsa yeni gunde tabela gorsel olarak KAPALI konuma alinir.")]
    [SerializeField] private TabelaSistemi tabelaSistemi;

    [Header("Durum (sadece izlemek icin)")]
    [Tooltip("Kacinci gundeyiz (1'den baslar).")]
    [SerializeField] private int gun = 1;

    /// <summary>Kacinci gunde oldugumuz (1'den baslar).</summary>
    public int Gun => gun;

    /// <summary>Gun bitti mi? (gun sonu objesi bunu okuyup basilabilir mi diye karar verir)</summary>
    public bool GunSonuMu => zamanSistemi != null && zamanSistemi.GunBittiMi();

    /// <summary>Her yeni gun baslarken tetiklenir. UI/kayit gibi ek sistemler abone olabilir.</summary>
    public event Action GunYenidenBaslatildi;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(this); return; }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Gun sonu objesine BASILINCA cagrilir (yalnizca GunSonuMu true iken).
    /// Butun sistemleri sifirlar ve yeni gunu baslatir.
    /// </summary>
    public void GunuBitirVeBaslat()
    {
        // ILERIDE: burada kayit (save) yapilacak.
        // TODO: SaveSistemi.Kaydet();

        // 1) Sahnedeki musterileri temizle (masa/sira kayitlari sonra sifirlanacak).
        if (musteriUretici != null) musteriUretici.GunuSifirla();

        // 2) Sira / siparis / masa kayitlarini temizle (moduler: her biri kendini sifirlar).
        if (SiraYonetimi.Instance != null) SiraYonetimi.Instance.Sifirla();
        if (SiparisYonetimi.Instance != null) SiparisYonetimi.Instance.Sifirla();
        if (MasaYonetimi.Instance != null) MasaYonetimi.Instance.Sifirla();

        // 3) Tabelayi (varsa) gorsel + mantik olarak kapali konuma al.
        if (tabelaSistemi != null) tabelaSistemi.KapaliyaAl();

        // 4) Saati baslangica dondur, dukkani kapali birak, gun-bitti bayragini sifirla.
        if (zamanSistemi != null) zamanSistemi.GunuSifirla();

        // 5) Gun sayacini artir ve dinleyicileri haberdar et.
        gun++;
        GunYenidenBaslatildi?.Invoke();
    }
}
