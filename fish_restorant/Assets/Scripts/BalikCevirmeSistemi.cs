using UnityEngine;
using System.Collections;

/// <summary>
/// E tuşuyla ızgaradaki balığı çevirir.
/// Havaya kalkar → 180° döner → geri iner. EaseInOutQuad animasyon.
/// Direkt balığa veya ızgaraya bakarak çalışır.
/// 
/// Kurulum:
/// 1. Oyuncuya ekle
/// 2. RaycastSistemi sahnede olmalı
/// </summary>
public class BalikCevirmeSistemi : MonoBehaviour
{
    [Header("Çevirme Ayarları")]
    [Tooltip("Çevirme tuşu")]
    [SerializeField] private KeyCode cevirmeTusu = KeyCode.E;

    [Tooltip("Y ekseninde yükselme miktarı")]
    [SerializeField] private float yukselmeYuksekligi = 0.135f;

    [Tooltip("Y ekseninde iniş miktarı (yükselme noktasından aşağı)")]
    [SerializeField] private float inisMesafesi = 0.135f;

    [Tooltip("Yükselme süresi (saniye)")]
    [SerializeField] private float yukselmeSuresi = 0.3f;

    [Tooltip("Dönme süresi (saniye)")]
    [SerializeField] private float donmeSuresi = 0.4f;

    [Tooltip("İniş süresi (saniye)")]
    [SerializeField] private float inisSuresi = 0.3f;

    [Header("Dönme Ekseni")]
    [Tooltip("X ekseni = öne/arkaya, Z ekseni = yana")]
    [SerializeField] private DonmeEkseni donmeEkseni = DonmeEkseni.X;

    private RaycastSistemi raycastSistemi;
    private bool cevirmeAktif = false;

    public enum DonmeEkseni { X, Z }

    void Start()
    {
        raycastSistemi = FindObjectOfType<RaycastSistemi>();

        if (raycastSistemi == null)
            Debug.LogError("[BalikCevirme] RaycastSistemi bulunamadı!");
    }

    void Update()
    {
        if (cevirmeAktif) return;
        if (raycastSistemi == null) return;

        if (Input.GetKeyDown(cevirmeTusu))
        {
            CevirmeDene();
        }
    }

    void CevirmeDene()
    {
        if (!raycastSistemi.ObjeyeBakiyorMu) return;

        GameObject hedef = raycastSistemi.BakilanObje;
        if (hedef == null) return;

        // Önce direkt bakılan objede PisirilebilirNesne ara (balığa bakıyor olabilir)
        PisirilebilirNesne pisirilebilir = hedef.GetComponent<PisirilebilirNesne>();
        if (pisirilebilir == null)
            pisirilebilir = hedef.GetComponentInParent<PisirilebilirNesne>();
        if (pisirilebilir == null)
            pisirilebilir = hedef.GetComponentInChildren<PisirilebilirNesne>();

        // PisirmeSistemi'ni bul (balığın üzerinde durduğu ızgara)
        PisirmeSistemi pisirme = null;

        if (pisirilebilir != null)
        {
            // Balığa bakıyor - ızgarayı bul
            pisirme = FindPisirmeForNesne(pisirilebilir.gameObject);
        }
        else
        {
            // Izgaraya bakıyor olabilir - ızgaradaki ilk balığı bul
            pisirme = hedef.GetComponent<PisirmeSistemi>();
            if (pisirme == null)
                pisirme = hedef.GetComponentInParent<PisirmeSistemi>();
            if (pisirme == null)
                pisirme = hedef.GetComponentInChildren<PisirmeSistemi>();

            // Izgarada balık yoksa çık
            if (pisirme == null || !pisirme.NesneVarMi) return;

            // Izgaraya bakarak çevirme desteklenmiyor - balığa direkt bakması lazım
            return;
        }

        if (pisirme == null)
        {
            Debug.Log("[BalikCevirme] Bu balık bir pişirme yüzeyinde değil!");
            return;
        }

        StartCoroutine(CevirmeAnimasyonu(pisirme, pisirilebilir));
    }

    /// <summary>
    /// Sahnedeki tüm PisirmeSistemi'lerinde bu nesneyi ara
    /// </summary>
    PisirmeSistemi FindPisirmeForNesne(GameObject nesne)
    {
        PisirmeSistemi[] tumPisirmeler = FindObjectsOfType<PisirmeSistemi>();
        for (int i = 0; i < tumPisirmeler.Length; i++)
        {
            if (tumPisirmeler[i].NesneBuYuzeydeMi(nesne))
                return tumPisirmeler[i];
        }
        return null;
    }

    IEnumerator CevirmeAnimasyonu(PisirmeSistemi pisirme, PisirilebilirNesne pisirilebilir)
    {
        cevirmeAktif = true;

        GameObject nesne = pisirilebilir.gameObject;

        // Pişirmeyi duraklat
        pisirme.PismeDuraklat(nesne);

        Vector3 baslangicPoz = nesne.transform.position;

        // Dönme ekseni (world space)
        Vector3 donmeVektoru = donmeEkseni == DonmeEkseni.X
            ? nesne.transform.right
            : nesne.transform.forward;

        // === AŞAMA 1: Yükselme ===
        Vector3 hedefPoz = baslangicPoz + Vector3.up * yukselmeYuksekligi;

        float gecenSure = 0f;
        while (gecenSure < yukselmeSuresi)
        {
            gecenSure += Time.deltaTime;
            float t = EaseInOutQuad(Mathf.Clamp01(gecenSure / yukselmeSuresi));
            nesne.transform.position = Vector3.Lerp(baslangicPoz, hedefPoz, t);
            yield return null;
        }
        nesne.transform.position = hedefPoz;

        // === AŞAMA 2: 180° Dönme ===
        Quaternion donmeBaslangic = nesne.transform.rotation;
        Quaternion donmeHedef = Quaternion.AngleAxis(180f, donmeVektoru) * donmeBaslangic;

        // Particle'ların world rotasyonlarını kaydet (sabit kalmaları için)
        ParticleSystem[] particles = nesne.GetComponentsInChildren<ParticleSystem>(true);
        Quaternion[] particleWorldRot = new Quaternion[particles.Length];
        for (int i = 0; i < particles.Length; i++)
        {
            particleWorldRot[i] = particles[i].transform.rotation;
        }

        gecenSure = 0f;
        while (gecenSure < donmeSuresi)
        {
            gecenSure += Time.deltaTime;
            float t = EaseInOutQuad(Mathf.Clamp01(gecenSure / donmeSuresi));
            nesne.transform.rotation = Quaternion.Slerp(donmeBaslangic, donmeHedef, t);

            // Particle'ları world space'te sabit tut
            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i] != null)
                    particles[i].transform.rotation = particleWorldRot[i];
            }

            yield return null;
        }
        nesne.transform.rotation = donmeHedef;

        // Son kez sabit rotasyonu uygula
        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i] != null)
                particles[i].transform.rotation = particleWorldRot[i];
        }

        // === AŞAMA 3: İniş ===
        Vector3 inisPoz = hedefPoz - Vector3.up * inisMesafesi;

        gecenSure = 0f;
        while (gecenSure < inisSuresi)
        {
            gecenSure += Time.deltaTime;
            float t = EaseInOutQuad(Mathf.Clamp01(gecenSure / inisSuresi));
            nesne.transform.position = Vector3.Lerp(hedefPoz, inisPoz, t);
            yield return null;
        }
        nesne.transform.position = inisPoz;

        // === Yüz değiştir ===
        if (pisirilebilir.Cevirildi)
            pisirilebilir.TekrarCevrildiOlarakIsaretle();
        else
            pisirilebilir.CevrildiOlarakIsaretle();

        // Yeni yüzün pişirmesini başlat
        pisirme.CevirmeSonrasiPismeDevam(nesne);

        cevirmeAktif = false;
    }

    float EaseInOutQuad(float t)
    {
        return t < 0.5f
            ? 2f * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
    }

    public bool CevirmeAktif => cevirmeAktif;
}