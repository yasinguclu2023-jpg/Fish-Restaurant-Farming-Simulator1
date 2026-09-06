using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class TagPozisyon
{
    public string tag;
    public Transform pozisyon;
}

public class NesneAlmaSistemi : MonoBehaviour
{
    [Header("Alma Ayarları")]
    [SerializeField] private List<TagPozisyon> tagPozisyonlari = new List<TagPozisyon>();

    [Header("Taşıma (Basılı Tutma)")]
    [Tooltip("SADECE bitki ve makineleri eline almak için BASILI TUTULACAK tuş. " +
             "Kasa, tepsi yığını, toplu kağıt vb. SAĞ TIK basılı tutularak alınır. " +
             "Sol tık taşıma yapmaz; sulama/hasat/anlık alma içindir.")]
    [SerializeField] private KeyCode tasimaTusu = KeyCode.X;

    [Header("Sistem")]
    [SerializeField] private bool sistemAktif = true;

    [Tooltip("Açık: Tab ile UI paneli açıkken hiçbir etkileşim çalışmaz " +
             "(sulama, hasat, alma, taşıma). UI'da tıklarken arkadaki nesneyi almayı engeller.")]
    [SerializeField] private bool uiAcikkenCalismasin = true;

    // Referanslar
    private RaycastSistemi raycastSistemi;
    private NesneYerlestirmeSistemi yerlestirmeSistemi;
    private EldeNesneKamera eldeNesneKamera;
    private BasiliTutmaYoneticisi tutmaYoneticisi;
    private KesmeSistemi kesmeSistemi;
    private BalikCevirmeSistemi cevirmeSistemi;
    private BitkiSulamaSistemi sulamaSistemi;

    // Basılı tutma durumu
    private GameObject bekleyenNesne;
    private Transform bekleyenPozisyon;
    private bool basiliTutmaAktif;
    private bool sagTikIleTutuluyor; // Tutma sağ tıkla mı başladı? (X ile karışmasın)

    void Start()
    {
        raycastSistemi = FindObjectOfType<RaycastSistemi>();
        yerlestirmeSistemi = FindObjectOfType<NesneYerlestirmeSistemi>();
        eldeNesneKamera = FindObjectOfType<EldeNesneKamera>();
        tutmaYoneticisi = BasiliTutmaYoneticisi.Instance;
        kesmeSistemi = FindObjectOfType<KesmeSistemi>();
        cevirmeSistemi = FindObjectOfType<BalikCevirmeSistemi>();
        sulamaSistemi = BitkiSulamaSistemi.Instance;

        if (raycastSistemi == null)
            Debug.LogError("RaycastSistemi bulunamadı!");
        if (yerlestirmeSistemi == null)
            Debug.LogError("NesneYerlestirmeSistemi bulunamadı!");
    }

    void Update()
    {
        if (!sistemAktif || raycastSistemi == null || yerlestirmeSistemi == null) return;

        // UI (Tab paneli) açıkken etkileşim yok: UI'a tıklarken arkadaki nesne alınmasın
        if (uiAcikkenCalismasin && UIYoneticisi.HerhangiBirUIAcikMi)
        {
            if (basiliTutmaAktif) IptalEt();
            return;
        }

        if (yerlestirmeSistemi.YerlesimModuAktif) return;
        if (cevirmeSistemi != null && cevirmeSistemi.CevirmeAktif) return;

        // Sulama sürerken input kilitli (kova animasyonu yarıda kesilmesin)
        if (sulamaSistemi != null && sulamaSistemi.SulamaAktif) return;

        if (basiliTutmaAktif)
        {
            BasiliTutmaDevam();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            SolTikIsle();
        }
        else if (Input.GetMouseButtonDown(1))
        {
            // Sağ tık: kasa, tepsi yığını, toplu kağıt ve bitki/makine dışındaki her şey
            TasimaIsle(true);
        }
        else if (Input.GetKeyDown(tasimaTusu))
        {
            // Taşıma tuşu (X): sadece bitki ve makineler
            TasimaIsle(false);
        }
    }

    void SolTikIsle()
    {
        if (BitkiHasatDene()) return;
        if (BitkiSulamaDene()) return;

        if (!raycastSistemi.ObjeyeBakiyorMu) return;

        GameObject hedef = raycastSistemi.BakilanObje;
        if (hedef == null) return;

        // ===== OLTA KONTROLÜ =====
        if (OltaKontrol(hedef)) return;
        // ==========================

        // ===== EKMEK SİSTEMİ KONTROLÜ =====
        if (EkmekSistemiKontrol(hedef)) return;
        // ===================================

        // ===== TOPLU SARMA KAĞIDI (sol tık = kağıt al) =====
        TopluSarmaKagidi topluKagit = hedef.GetComponent<TopluSarmaKagidi>();
        if (topluKagit == null)
            topluKagit = hedef.GetComponentInParent<TopluSarmaKagidi>();

        if (topluKagit != null && topluKagit.KagitVarMi)
        {
            GameObject kagit = topluKagit.KagitAl();
            if (kagit != null)
            {
                Transform pozisyon = PozisyonBul(kagit.tag);
                if (pozisyon == null)
                {
                    Debug.Log($"'{kagit.tag}' için el pozisyonu yok!");
                    Destroy(kagit);
                    return;
                }
                NesneAl(kagit, pozisyon);
                return;
            }
        }
        // ===================================================

        // ===== TEPSİ YIĞINI (sol tık = tepsi prefab al) =====
        TepsiYiginiSistemi tepsiYigini = hedef.GetComponent<TepsiYiginiSistemi>();
        if (tepsiYigini == null)
            tepsiYigini = hedef.GetComponentInParent<TepsiYiginiSistemi>();

        if (tepsiYigini != null && tepsiYigini.TepsiVarMi)
        {
            GameObject tepsi = tepsiYigini.TepsiAl();
            if (tepsi != null)
            {
                Transform pozisyon = PozisyonBul(tepsi.tag);
                if (pozisyon == null)
                {
                    Debug.Log($"'{tepsi.tag}' için el pozisyonu yok!");
                    Destroy(tepsi);
                    return;
                }
                NesneAl(tepsi, pozisyon);
                return;
            }
        }
        // =====================================================

        // ===== FİŞ BUTONU (sol tık = fiş kopyası al) =====
        FisButonu fisButonu = hedef.GetComponent<FisButonu>();
        if (fisButonu == null)
            fisButonu = hedef.GetComponentInParent<FisButonu>();

        if (fisButonu != null && fisButonu.FisVarMi)
        {
            GameObject fis = fisButonu.FisAl();
            if (fis != null)
            {
                Transform pozisyon = PozisyonBul(fis.tag);
                if (pozisyon == null)
                {
                    Debug.Log($"'{fis.tag}' için el pozisyonu yok!");
                    Destroy(fis);
                    return;
                }
                NesneAl(fis, pozisyon);
                return;
            }
        }
        // ==================================================

        // ===== ÜRETİM MAKİNESİ KONTROLÜ (tepsi alma) =====
        UretimMakinesiSistemi makine = hedef.GetComponent<UretimMakinesiSistemi>();
        if (makine == null)
            makine = hedef.GetComponentInParent<UretimMakinesiSistemi>();
        if (makine == null)
            makine = hedef.GetComponentInChildren<UretimMakinesiSistemi>();

        if (makine != null && makine.TepsiAlinabilirMi)
        {
            GameObject tepsi = makine.TepsiAl();
            if (tepsi != null)
            {
                Transform pozisyon = PozisyonBul(tepsi.tag);
                if (pozisyon == null)
                {
                    Debug.Log($"'{tepsi.tag}' için el pozisyonu yok!");
                    tepsi.transform.position = hedef.transform.position + Vector3.up * 0.5f;
                    return;
                }
                NesneAl(tepsi, pozisyon);
                return;
            }
        }
        // ===================================================

        // Kasa kontrolü
        KasaSistemi kasa = hedef.GetComponent<KasaSistemi>();
        if (kasa == null)
            kasa = hedef.GetComponentInParent<KasaSistemi>();

        if (kasa != null && kasa.UrunVarMi)
        {
            GameObject urun = kasa.UrunAl();
            if (urun != null)
            {
                Transform pozisyon = PozisyonBul(urun.tag);
                if (pozisyon == null)
                {
                    Debug.Log($"'{urun.tag}' için el pozisyonu yok!");
                    urun.transform.SetParent(kasa.transform);
                    return;
                }
                NesneAl(urun, pozisyon);
                return;
            }
        }

        // Küvet kontrolü
        KuvetSistemi kuvet = hedef.GetComponent<KuvetSistemi>();
        if (kuvet == null)
            kuvet = hedef.GetComponentInParent<KuvetSistemi>();

        if (kuvet != null && kuvet.UrunVarMi)
        {
            string cikisTag = kuvet.CikisTag;
            GameObject urun = kuvet.UrunAl();
            if (urun != null)
            {
                string tag = !string.IsNullOrEmpty(cikisTag) ? cikisTag : urun.tag;
                Transform pozisyon = PozisyonBul(tag);
                if (pozisyon == null)
                {
                    Debug.Log($"'{tag}' için el pozisyonu yok!");
                    Destroy(urun);
                    return;
                }
                NesneAl(urun, pozisyon);
                return;
            }
        }

        // Kesme tahtası kontrolü
        KesmeTahtasiSistemi tahta = hedef.GetComponent<KesmeTahtasiSistemi>();
        if (tahta == null)
            tahta = hedef.GetComponentInParent<KesmeTahtasiSistemi>();
        if (tahta == null)
            tahta = hedef.GetComponentInChildren<KesmeTahtasiSistemi>();

        if (tahta != null && tahta.NesneVarMi)
        {
            if (kesmeSistemi != null && kesmeSistemi.KesimDevamEdiyorMu)
            {
                Debug.Log("[NesneAlma] Kesim devam ediyor, nesne alınamaz!");
                return;
            }
            GameObject nesne = tahta.NesneAl();
            if (nesne != null)
            {
                Transform pozisyon = PozisyonBul(nesne.tag);
                if (pozisyon == null)
                {
                    Debug.Log($"'{nesne.tag}' için el pozisyonu yok!");
                    tahta.NesneYerlestir(nesne, nesne.transform.lossyScale, nesne.layer);
                    return;
                }
                NesneAl(nesne, pozisyon);
                return;
            }
        }

        // ===== PİŞİRME YÜZEYİ KONTROLÜ =====
        PisirilebilirNesne pisirilebilirHedef = hedef.GetComponent<PisirilebilirNesne>();
        if (pisirilebilirHedef == null)
            pisirilebilirHedef = hedef.GetComponentInParent<PisirilebilirNesne>();

        if (pisirilebilirHedef != null)
        {
            PisirmeSistemi[] tumPisirmeler = FindObjectsOfType<PisirmeSistemi>();
            for (int pi = 0; pi < tumPisirmeler.Length; pi++)
            {
                if (tumPisirmeler[pi].NesneBuYuzeydeMi(pisirilebilirHedef.gameObject))
                {
                    GameObject nesne = tumPisirmeler[pi].NesneAl(pisirilebilirHedef.gameObject);
                    if (nesne != null)
                    {
                        Transform pozisyon = PozisyonBul(nesne.tag);
                        if (pozisyon == null)
                        {
                            Debug.Log($"'{nesne.tag}' için el pozisyonu yok!");
                            tumPisirmeler[pi].NesneYerlestir(nesne, nesne.transform.lossyScale, nesne.layer, nesne.transform.position, nesne.transform.rotation);
                            return;
                        }
                        NesneAl(nesne, pozisyon);
                        return;
                    }
                }
            }
        }

        PisirmeSistemi pisirme = hedef.GetComponent<PisirmeSistemi>();
        if (pisirme == null)
            pisirme = hedef.GetComponentInParent<PisirmeSistemi>();
        if (pisirme == null)
            pisirme = hedef.GetComponentInChildren<PisirmeSistemi>();

        if (pisirme != null && pisirme.NesneVarMi)
        {
            GameObject nesneIzgara = pisirme.NesneAl();
            if (nesneIzgara != null)
            {
                Transform pozisyon = PozisyonBul(nesneIzgara.tag);
                if (pozisyon == null)
                {
                    Debug.Log($"'{nesneIzgara.tag}' için el pozisyonu yok!");
                    pisirme.NesneYerlestir(nesneIzgara, nesneIzgara.transform.lossyScale, nesneIzgara.layer, nesneIzgara.transform.position, nesneIzgara.transform.rotation);
                    return;
                }
                NesneAl(nesneIzgara, pozisyon);
                return;
            }
        }
        // =====================================

        // Taşıma gerektiren nesneler SOL TIK ile alınmaz:
        // bitki/makine → X basılı tut, diğerleri (kasa, tepsi yığını vb.) → SAĞ TIK basılı tut.
        // Bkz. TasimaIsle().
        if (TasinabilirBul(hedef) != null) return;

        // Normal nesne alma
        Transform pozisyonNormal = PozisyonBul(hedef.tag);
        if (pozisyonNormal == null)
        {
            Debug.Log($"'{hedef.tag}' için el pozisyonu yok!");
            return;
        }

        NesneAl(hedef, pozisyonNormal);
    }

    // ===== OLTA SİSTEMİ =====
    bool OltaKontrol(GameObject hedef)
    {
        // Önce stant kontrolü
        OltaStandi stant = hedef.GetComponent<OltaStandi>();
        if (stant == null)
            stant = hedef.GetComponentInParent<OltaStandi>();
        if (stant == null)
            stant = hedef.GetComponentInChildren<OltaStandi>();

        // Stant bulunamadıysa, belki oltanın kendisine tıklandı - yakındaki stantı bul
        if (stant == null)
        {
            OltaEntegrasyon olta = hedef.GetComponent<OltaEntegrasyon>();
            if (olta == null)
                olta = hedef.GetComponentInParent<OltaEntegrasyon>();
            if (olta == null)
                olta = hedef.GetComponentInChildren<OltaEntegrasyon>();

            if (olta != null)
            {
                // Sahnedeki tüm stantları kontrol et, bu olta hangi stantta?
                OltaStandi[] tumStantlar = FindObjectsOfType<OltaStandi>();
                for (int i = 0; i < tumStantlar.Length; i++)
                {
                    if (tumStantlar[i].mevcutOlta == olta.gameObject)
                    {
                        stant = tumStantlar[i];
                        break;
                    }
                }

                // Stant bulunamadıysa olta zaten elde değil ve stantta değil, pas geç
                if (stant == null) return false;
            }
        }

        if (stant == null) return false;

        if (stant.OltaVarMi)
        {
            GameObject oltaObj = stant.OltaAl();
            if (oltaObj == null) return true;

            Transform pozisyon = PozisyonBul(oltaObj.tag);
            if (pozisyon == null)
            {
                Debug.Log($"'{oltaObj.tag}' için el pozisyonu yok!");
                stant.OltaKoy(oltaObj);
                return true;
            }

            OltaElAl(oltaObj, pozisyon);
            return true;
        }

        return true;
    }

    void OltaElAl(GameObject nesne, Transform hedefPozisyon)
    {
        Vector3 orijinalScale = nesne.transform.lossyScale;
        int orijinalLayer = nesne.layer;

        if (nesne.TryGetComponent(out NesneSesVerisi sesVerisi))
            sesVerisi.AlmaSesiCal();

        if (nesne.TryGetComponent(out Rigidbody rb))
            rb.isKinematic = true;

        // Tüm collider'ları kapat (child'lardakiler dahil)
        Collider[] cols = nesne.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
            cols[i].enabled = false;

        nesne.transform.SetParent(hedefPozisyon, false);
        nesne.transform.localPosition = Vector3.zero;
        nesne.transform.localRotation = Quaternion.identity;
        nesne.transform.localScale = Vector3.one;

        // Olta modu ile yerleştirmeye başla - sağ tık ile İPTAL EDİLEMEZ, preview gösterilir
        yerlestirmeSistemi.OltaYerlestirmeyeBasla(nesne, orijinalScale, hedefPozisyon, orijinalLayer);
    }
    // ==========================

    /// <summary>
    /// Ekmek sistemi kontrolü:
    /// - EkmekMalzemesi marker'ı varsa → ekmek sisteminin parçası
    /// - Root'ta marker varsa → sarma yapılmamış → alınamaz
    /// - Root'ta marker yoksa → sarma yapılmış → root child'larıyla alınır
    /// </summary>
    bool EkmekSistemiKontrol(GameObject hedef)
    {
        EkmekMalzemesi marker = hedef.GetComponent<EkmekMalzemesi>();
        if (marker == null)
            marker = hedef.GetComponentInParent<EkmekMalzemesi>();

        if (marker == null) return false;

        EkmekMalzemeAlici alici = hedef.GetComponent<EkmekMalzemeAlici>();
        if (alici == null)
            alici = hedef.GetComponentInParent<EkmekMalzemeAlici>();

        if (alici == null)
        {
            Debug.Log("[NesneAlma] EkmekMalzemesi var ama alıcı bulunamadı, alınmıyor.");
            return true;
        }

        GameObject ekmekRoot = alici.gameObject;
        EkmekMalzemesi rootMarker = ekmekRoot.GetComponent<EkmekMalzemesi>();

        if (rootMarker != null)
        {
            Debug.Log("[NesneAlma] Ekmek henüz sarılmamış, alınamaz!");
            return true;
        }

        Transform pozisyon = PozisyonBul(ekmekRoot.tag);
        if (pozisyon == null)
        {
            Debug.Log($"'{ekmekRoot.tag}' için el pozisyonu yok!");
            return true;
        }

        NesneAl(ekmekRoot, pozisyon);
        return true;
    }

    /// <summary>
    /// TAŞIMA: büyük nesneleri eline alma.
    /// - SAĞ TIK  → kasa, toplu kağıt, tepsi yığını ve bitki/makine olmayan her nesne
    /// - TAŞIMA TUŞU (X) → sadece bitki ve makineler
    /// Hangi nesnenin hangi tuşa ait olduğu BasiliTutmaGerekli.TasimaTusuIleAlinir ile belirlenir.
    /// </summary>
    void TasimaIsle(bool sagTikIle)
    {
        if (!raycastSistemi.ObjeyeBakiyorMu) return;

        GameObject hedef = raycastSistemi.BakilanObje;
        if (hedef == null) return;

        GameObject tasinacak = TasinacakNesneBul(hedef);
        if (tasinacak == null) return;

        // Basılan tuş bu nesnenin tuşu değilse hiçbir şey yapma
        bool tasimaTusuGerekli = TasimaTusuGerekliMi(tasinacak);
        if (tasimaTusuGerekli == sagTikIle) return;

        TasimayaBasla(tasinacak, sagTikIle);
    }

    /// <summary>
    /// Bakılan objeden yola çıkarak taşınacak kök nesneyi bulur.
    /// (Kasa / toplu kağıt / tepsi yığını özel durumları, sonra genel BasiliTutmaGerekli)
    /// </summary>
    GameObject TasinacakNesneBul(GameObject hedef)
    {
        // Kasa (üstündeki ürüne değil, kasanın kendisine bakılıyor olabilir)
        KasaSistemi kasa = hedef.GetComponent<KasaSistemi>();
        if (kasa == null)
            kasa = hedef.GetComponentInParent<KasaSistemi>();

        if (kasa != null) return kasa.gameObject;

        // Toplu sarma kağıdı (objenin tamamını al)
        TopluSarmaKagidi topluKagit = hedef.GetComponent<TopluSarmaKagidi>();
        if (topluKagit == null)
            topluKagit = hedef.GetComponentInParent<TopluSarmaKagidi>();

        if (topluKagit != null) return topluKagit.gameObject;

        // Tepsi yığını (tüm yığını al)
        TepsiYiginiSistemi tepsiYigini = hedef.GetComponent<TepsiYiginiSistemi>();
        if (tepsiYigini == null)
            tepsiYigini = hedef.GetComponentInParent<TepsiYiginiSistemi>();

        if (tepsiYigini != null) return tepsiYigini.gameObject;

        // Genel: BasiliTutmaGerekli olan her nesne (bitki, makine vb.)
        BasiliTutmaGerekli btScript = TasinabilirBul(hedef);
        return btScript != null ? btScript.gameObject : null;
    }

    /// <summary>
    /// Nesne taşıma tuşu (X) ile mi alınır? Marker yoksa varsayılan SAĞ TIK'tır.
    /// </summary>
    bool TasimaTusuGerekliMi(GameObject nesneObj)
    {
        BasiliTutmaGerekli btScript = nesneObj.GetComponent<BasiliTutmaGerekli>();
        return btScript != null && btScript.TasimaTusuIleAlinir;
    }

    /// <summary>
    /// Hedefte BasiliTutmaGerekli arar. Hedefte yoksa parent'larda arar ama
    /// SADECE parentTanCalissin = true ise kabul eder:
    ///  - Bitki (true): child mesh'e bakılsa bile bulunur
    ///  - Makine (false): üstündeki tepsi/kasaya bakılınca tetiklenmez
    /// </summary>
    BasiliTutmaGerekli TasinabilirBul(GameObject hedef)
    {
        BasiliTutmaGerekli btScript = hedef.GetComponent<BasiliTutmaGerekli>();

        if (btScript == null)
        {
            BasiliTutmaGerekli parentBT = hedef.GetComponentInParent<BasiliTutmaGerekli>();
            if (parentBT != null && parentBT.ParentTanCalissin)
                btScript = parentBT;
        }

        return (btScript != null && btScript.Alinabilir) ? btScript : null;
    }

    /// <summary>
    /// Taşıma ile nesne alma (basılı tutma destekli).
    /// sagTikIle: tutmanın hangi tuşla sürdürüleceğini belirler.
    /// </summary>
    void TasimayaBasla(GameObject nesneObj, bool sagTikIle)
    {
        Transform pozisyon = PozisyonBul(nesneObj.tag);
        if (pozisyon == null)
        {
            Debug.Log($"'{nesneObj.tag}' için el pozisyonu yok!");
            return;
        }

        BasiliTutmaGerekli btScript = nesneObj.GetComponent<BasiliTutmaGerekli>();

        if (btScript != null && btScript.Alinabilir && tutmaYoneticisi != null)
        {
            bekleyenNesne = nesneObj;
            bekleyenPozisyon = pozisyon;
            basiliTutmaAktif = true;
            sagTikIleTutuluyor = sagTikIle;
            tutmaYoneticisi.Baslat(btScript.Sure, BasiliTutmaYoneticisi.TutmaKaynagi.NesneAlma);
        }
        else
        {
            NesneAl(nesneObj, pozisyon);
        }
    }

    void BasiliTutmaDevam()
    {
        bool tusTutulu = sagTikIleTutuluyor ? Input.GetMouseButton(1) : Input.GetKey(tasimaTusu);

        if (!tusTutulu)
        {
            IptalEt();
            return;
        }

        if (!raycastSistemi.ObjeyeBakiyorMu)
        {
            IptalEt();
            return;
        }

        GameObject bakilan = raycastSistemi.BakilanObje;
        bool ayniNesne = bakilan == bekleyenNesne ||
                         bakilan.transform.IsChildOf(bekleyenNesne.transform) ||
                         bekleyenNesne.transform.IsChildOf(bakilan.transform);

        if (!ayniNesne)
        {
            IptalEt();
            return;
        }

        if (tutmaYoneticisi.Guncelle())
        {
            NesneAl(bekleyenNesne, bekleyenPozisyon);
            Temizle();
        }
    }

    void IptalEt()
    {
        tutmaYoneticisi?.Iptal();
        Temizle();
    }

    void Temizle()
    {
        basiliTutmaAktif = false;
        sagTikIleTutuluyor = false;
        bekleyenNesne = null;
        bekleyenPozisyon = null;
    }

    /// <summary>
    /// Bakılan bitki susuzsa sol tıka BASILDIĞI an sulamayı başlatır.
    /// Devamı (basılı tutma / iptal) BitkiSulamaSistemi'nin kendi Update'inde işlenir.
    /// Para yetmezse / bitki susuz değilse false döner ve sol tık akışı devam eder.
    /// </summary>
    bool BitkiSulamaDene()
    {
        if (sulamaSistemi == null)
        {
            sulamaSistemi = BitkiSulamaSistemi.Instance;
            if (sulamaSistemi == null) return false;
        }

        BitkiBuyumeSistemi bitki = raycastSistemi.BakilanBitki;
        if (bitki == null || !bitki.SulanabilirMi) return false;

        return sulamaSistemi.SulamayaBasla(bitki);
    }

    bool BitkiHasatDene()
    {
        BitkiBuyumeSistemi bitki = raycastSistemi.BakilanBitki;
        if (bitki == null || !bitki.HasatHazirMi) return false;

        Transform elPoz = null;
        foreach (var tp in tagPozisyonlari)
        {
            if (tp.tag.ToLower().Contains("hasat") || tp.tag.ToLower().Contains("bitki"))
            {
                elPoz = tp.pozisyon;
                break;
            }
        }
        if (elPoz == null && tagPozisyonlari.Count > 0)
            elPoz = tagPozisyonlari[0].pozisyon;

        return bitki.HasatEt(elPoz);
    }

    void NesneAl(GameObject nesne, Transform hedefPozisyon)
    {
        Vector3 orijinalScale = nesne.transform.lossyScale;
        int orijinalLayer = nesne.layer;

        PisirilebilirNesne pisirilebilir = nesne.GetComponent<PisirilebilirNesne>();
        if (pisirilebilir == null) pisirilebilir = nesne.GetComponentInChildren<PisirilebilirNesne>();
        if (pisirilebilir == null) pisirilebilir = nesne.GetComponentInParent<PisirilebilirNesne>();
        if (pisirilebilir != null) pisirilebilir.EfektleriKapat();

        PisirmeSistemi[] tumPisirmeler = FindObjectsOfType<PisirmeSistemi>();
        for (int i = 0; i < tumPisirmeler.Length; i++)
        {
            if (tumPisirmeler[i].NesneBuYuzeydeMi(nesne))
            {
                tumPisirmeler[i].NesneAl(nesne);
                break;
            }
        }

        if (nesne.TryGetComponent(out NesneSesVerisi sesVerisi))
            sesVerisi.AlmaSesiCal();

        if (nesne.TryGetComponent(out Rigidbody rb))
            rb.isKinematic = true;

        if (nesne.TryGetComponent(out Collider col))
            col.enabled = false;

        nesne.transform.SetParent(hedefPozisyon, false);
        nesne.transform.localPosition = Vector3.zero;
        nesne.transform.localRotation = Quaternion.identity;

        Vector3 parentScale = hedefPozisyon.lossyScale;
        nesne.transform.localScale = new Vector3(
            orijinalScale.x / parentScale.x,
            orijinalScale.y / parentScale.y,
            orijinalScale.z / parentScale.z
        );

        if (eldeNesneKamera != null)
            eldeNesneKamera.NesneLayerAyarla(nesne);

        yerlestirmeSistemi.YerlestirmeyeBasla(nesne, orijinalScale, hedefPozisyon, orijinalLayer);
    }

    Transform PozisyonBul(string tag)
    {
        foreach (var tp in tagPozisyonlari)
            if (tp.tag == tag) return tp.pozisyon;
        return null;
    }

    public void SistemAcKapat(bool aktif) => sistemAktif = aktif;
    public Transform ElPozisyonuAl(string tag) => PozisyonBul(tag);
    public bool SistemAktif => sistemAktif;
}