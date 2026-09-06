using UnityEngine;
using System.Collections;

public class PisirilebilirNesne : MonoBehaviour
{
    [Header("Model Referansları")]
    [Tooltip("Balığın alt kısmının modeli (ilk pişen)")]
    [SerializeField] private Renderer altModel;

    [Tooltip("Balığın üst kısmının modeli (çevirdikten sonra pişen)")]
    [SerializeField] private Renderer ustModel;

    [Header("Pişme Efektleri")]
    [Tooltip("Efektlerin aktif olacağı yüzey tag'leri (ör: Izgara). Boş bırakırsan her yerde açılır.")]
    [SerializeField] private string[] efektAktifTagler;

    // Otomatik bulunan particle'lar
    private ParticleSystem[] cachedParticles;

    // Pişme durumları
    private float altPismeIlerleme = 0f;
    private float ustPismeIlerleme = 0f;
    private bool altPisti = false;
    private bool ustPisti = false;
    private bool cevirildi = false;

    void Awake()
    {
        // Child'lardaki tüm particle'ları bul ve cache'le
        cachedParticles = GetComponentsInChildren<ParticleSystem>(true);

        // Hepsini durdur ve Play On Awake kapat
        for (int i = 0; i < cachedParticles.Length; i++)
        {
            var main = cachedParticles[i].main;
            main.playOnAwake = false;
            cachedParticles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    // === Aktif Yüz ===
    public Renderer AktifYuzRenderer => cevirildi ? ustModel : altModel;
    public float AktifYuzIlerleme => cevirildi ? ustPismeIlerleme : altPismeIlerleme;
    public bool AktifYuzPisti => cevirildi ? ustPisti : altPisti;

    // === Alt Yüz ===
    public bool AltPisti => altPisti;
    public float AltPismeIlerleme => altPismeIlerleme;
    public Renderer AltModel => altModel;
    public void AltPismeDurumunuAyarla(bool pisti) { altPisti = pisti; }
    public void AltPismeIlerlemeyiAyarla(float ilerleme) { altPismeIlerleme = Mathf.Clamp(ilerleme, 0f, 3f); }

    // === Üst Yüz ===
    public bool UstPisti => ustPisti;
    public float UstPismeIlerleme => ustPismeIlerleme;
    public Renderer UstModel => ustModel;
    public void UstPismeDurumunuAyarla(bool pisti) { ustPisti = pisti; }
    public void UstPismeIlerlemeyiAyarla(float ilerleme) { ustPismeIlerleme = Mathf.Clamp(ilerleme, 0f, 3f); }

    // === Çevirme ===
    public bool Cevirildi => cevirildi;
    public void CevrildiOlarakIsaretle() { cevirildi = true; }
    public void TekrarCevrildiOlarakIsaretle() { cevirildi = false; }

    public void FizikselYuzTespitEt()
    {
        float dot = Vector3.Dot(transform.up, Vector3.up);
        cevirildi = dot < 0f;
    }

    // === Genel ===
    public bool TamamenPisti => altPisti && ustPisti;
    public bool ModellerGecerli => altModel != null && ustModel != null;

    // === Efektler ===

    public void EfektleriAc(string yuzeyTag)
    {
        if (cachedParticles == null || cachedParticles.Length == 0) return;

        // Tag kontrolü
        if (efektAktifTagler != null && efektAktifTagler.Length > 0)
        {
            bool tagUygun = false;
            for (int i = 0; i < efektAktifTagler.Length; i++)
            {
                if (efektAktifTagler[i] == yuzeyTag)
                {
                    tagUygun = true;
                    break;
                }
            }
            if (!tagUygun) return;
        }

        StartCoroutine(EfektleriGecikmeliAc());
    }

    private IEnumerator EfektleriGecikmeliAc()
    {
        yield return null;

        for (int i = 0; i < cachedParticles.Length; i++)
        {
            if (cachedParticles[i] == null) continue;
            cachedParticles[i].gameObject.SetActive(true);
            cachedParticles[i].Clear();
            cachedParticles[i].Play(true);
        }
    }

    public void EfektleriKapat()
    {
        // Coroutine'leri durdur (gecikmeli açma iptal)
        StopAllCoroutines();

        if (cachedParticles == null) return;
        for (int i = 0; i < cachedParticles.Length; i++)
        {
            if (cachedParticles[i] == null) continue;
            cachedParticles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            cachedParticles[i].gameObject.SetActive(false);
        }
    }
}