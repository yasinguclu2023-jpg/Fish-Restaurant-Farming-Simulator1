using UnityEngine;

/// <summary>
/// Sývý modeli kontrol sistemi.
/// Makinedeki sývý mesh'inin belirli bir eksende yükselip düþmesini yönetir.
/// 
/// ÖNEMLÝ: Bu script makinenin ALT OBJESÝNE eklenir.
/// Sývý objesi HER ZAMAN GÖRÜNÜR kalýr, sadece Scale/Position deðeri deðiþir.
/// Renderer asla kapatýlmaz.
/// 
/// Kurulum:
/// 1. Makinenin child'ý olan sývý mesh objesine bu scripti ekle
/// 2. Inspector'dan yükselme eksenini seç (X, Y veya Z)
/// 3. Scale mi Position mý deðiþecek seç
/// 4. Baþlangýç deðeri = sývýnýn sabit durduðu deðer (oyun açýldýðýnda böyle görünür)
/// 5. Maksimum deðer = makine çalýþýnca sývýnýn ulaþacaðý en yüksek deðer
/// 6. UretimMakinesiSistemi'nden bu scripti referans olarak ata
/// </summary>
public class SiviModeliSistemi : MonoBehaviour
{
    public enum SiviEkseni { X, Y, Z }
    public enum SiviModu { Scale, Position }

    [Header("Sývý Ayarlarý")]
    [Tooltip("Sývýnýn hangi eksende yükseleceði")]
    [SerializeField] private SiviEkseni yukselmeEkseni = SiviEkseni.Y;

    [Tooltip("Scale mý yoksa Position mý deðiþecek")]
    [SerializeField] private SiviModu siviModu = SiviModu.Scale;

    [Header("Yükselme Deðerleri")]
    [Tooltip("Baþlangýç deðeri (sývýnýn sabit durduðu deðer, oyun açýlýnca bu deðerde görünür)")]
    [SerializeField] private float baslangicDegeri = 0.1f;

    [Tooltip("Maksimum deðer (makine çalýþýnca sývýnýn ulaþacaðý en yüksek deðer)")]
    [SerializeField] private float maksimumDeger = 1f;

    [Header("Hýz Ayarlarý")]
    [Tooltip("Yükselme hýzý (birim/saniye)")]
    [SerializeField] private float yukselmeHizi = 0.5f;

    [Tooltip("Düþme hýzý (birim/saniye)")]
    [SerializeField] private float dusmeHizi = 0.3f;

    [Header("Düþme Ayarlarý")]
    [Tooltip("Düþme hedef deðeri (sývý düþtükten sonra kalacaðý deðer, genelde baslangicDegeri ile ayný)")]
    [SerializeField] private float dusmeBitisDegeri = 0.1f;

    [Tooltip("Düþme farklý eksende mi olacak?")]
    [SerializeField] private bool farkliEkseneDus = false;

    [Tooltip("Düþme ekseni (farkliEkseneDus = true ise kullanýlýr)")]
    [SerializeField] private SiviEkseni dusmeEkseni = SiviEkseni.Y;

    // Runtime
    private bool yukseliyor;
    private bool dusuyor;
    private float mevcutDeger;
    private float dusmeMevcutDeger;

    void Awake()
    {
        mevcutDeger = baslangicDegeri;

        // Baþlangýç deðerini uygula - sývý bu deðerde görünür durur
        DegerUygula(yukselmeEkseni, baslangicDegeri);

        // Renderer'a dokunma, objeye dokunma - her zaman görünür kalýr
    }

    void Update()
    {
        if (yukseliyor)
            YukselmeGuncelle();
        else if (dusuyor)
            DusmeGuncelle();
    }

    // ============================================================
    // YÜKSELME
    // ============================================================

    void YukselmeGuncelle()
    {
        mevcutDeger += yukselmeHizi * Time.deltaTime;

        if (mevcutDeger >= maksimumDeger)
        {
            mevcutDeger = maksimumDeger;
            // Maksimuma ulaþtý, DusmeBaslat çaðrýlana kadar burada kalýr
        }

        DegerUygula(yukselmeEkseni, mevcutDeger);
    }

    // ============================================================
    // DÜÞME
    // ============================================================

    void DusmeGuncelle()
    {
        SiviEkseni aktifEksen = farkliEkseneDus ? dusmeEkseni : yukselmeEkseni;

        if (farkliEkseneDus)
        {
            dusmeMevcutDeger -= dusmeHizi * Time.deltaTime;
            if (dusmeMevcutDeger <= dusmeBitisDegeri)
            {
                dusmeMevcutDeger = dusmeBitisDegeri;
                DusmeBitti();
            }
            DegerUygula(aktifEksen, dusmeMevcutDeger);
        }
        else
        {
            mevcutDeger -= dusmeHizi * Time.deltaTime;
            if (mevcutDeger <= dusmeBitisDegeri)
            {
                mevcutDeger = dusmeBitisDegeri;
                DusmeBitti();
            }
            DegerUygula(aktifEksen, mevcutDeger);
        }
    }

    void DusmeBitti()
    {
        dusuyor = false;
        // Düþme bitti, sývý dusmeBitisDegeri'nde sabit kalýr
        // Renderer kapatýlmaz, obje kapatýlmaz - her zaman görünür
    }

    // ============================================================
    // DEÐER UYGULAMA
    // ============================================================

    void DegerUygula(SiviEkseni eksen, float deger)
    {
        if (siviModu == SiviModu.Scale)
        {
            Vector3 scale = transform.localScale;
            switch (eksen)
            {
                case SiviEkseni.X: scale.x = deger; break;
                case SiviEkseni.Y: scale.y = deger; break;
                case SiviEkseni.Z: scale.z = deger; break;
            }
            transform.localScale = scale;
        }
        else
        {
            Vector3 pos = transform.localPosition;
            switch (eksen)
            {
                case SiviEkseni.X: pos.x = deger; break;
                case SiviEkseni.Y: pos.y = deger; break;
                case SiviEkseni.Z: pos.z = deger; break;
            }
            transform.localPosition = pos;
        }
    }

    // ============================================================
    // PUBLIC KONTROL (UretimMakinesiSistemi çaðýrýr)
    // ============================================================

    /// <summary>Sývý yükselmeye baþlar</summary>
    public void YukselmeBaslat()
    {
        yukseliyor = true;
        dusuyor = false;
        mevcutDeger = baslangicDegeri;
        DegerUygula(yukselmeEkseni, mevcutDeger);
    }

    /// <summary>Sývý düþmeye baþlar (ürünler bittiðinde)</summary>
    public void DusmeBaslat()
    {
        yukseliyor = false;
        dusuyor = true;

        if (farkliEkseneDus)
            dusmeMevcutDeger = mevcutDeger;
    }

    /// <summary>Sývýyý baþlangýç deðerine anýnda geri al</summary>
    public void Sifirla()
    {
        yukseliyor = false;
        dusuyor = false;
        mevcutDeger = baslangicDegeri;
        DegerUygula(yukselmeEkseni, baslangicDegeri);
    }

    // ============================================================
    // PUBLIC PROPERTIES
    // ============================================================

    public bool Yukseliyor => yukseliyor;
    public bool Dusuyor => dusuyor;
    public float MevcutDeger => mevcutDeger;
    public float MevcutYuzde => Mathf.InverseLerp(baslangicDegeri, maksimumDeger, mevcutDeger);

    // ============================================================
    // EDITOR
    // ============================================================

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 pos = transform.position;

        Vector3 yukselmeYon = EksenYonu(yukselmeEkseni);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(pos, pos + yukselmeYon * maksimumDeger * 0.5f);
        UnityEditor.Handles.Label(pos + yukselmeYon * maksimumDeger * 0.5f, "Yukselme");

        if (farkliEkseneDus)
        {
            Vector3 dusmeYon = EksenYonu(dusmeEkseni);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(pos, pos + dusmeYon * 0.5f);
            UnityEditor.Handles.Label(pos + dusmeYon * 0.5f, "Dusme");
        }
    }

    Vector3 EksenYonu(SiviEkseni eksen)
    {
        switch (eksen)
        {
            case SiviEkseni.X: return transform.right;
            case SiviEkseni.Y: return transform.up;
            case SiviEkseni.Z: return transform.forward;
            default: return transform.up;
        }
    }
#endif
}