using UnityEngine;
using FishingGameTool.Fishing;

/// <summary>
/// Olta at�� yay� (wind-up) mekani�i.
/// - Sol t�k basl� tutup g�� biriktirirken oltay� geriye yaslar.
/// - Tu� b�rak�l�nca olta ba�lang�� konumuna d�ner, ard�ndan misina at�l�r.
///
/// Bu script'i olta objesine (FishingRod ile ayn� obje) ekleyin.
/// "Geri Rotasyon" alan�na -20'yi istedi�iniz eksene koyun (�rn: (-20,0,0) veya (0,0,-20)).
/// </summary>
public class OltaAtisRotasyonu : MonoBehaviour
{
    [Header("Referanslar")]
    [Tooltip("D�nd�r�lecek obje. Bo� b�rak�rsan bu script'in oldu�u obje kullan�l�r.")]
    [SerializeField] private Transform donecekObje;

    [Tooltip("Bo� b�rak�rsan sahnedeki FishingSystem otomatik bulunur.")]
    [SerializeField] private FishingSystem fishingSystem;

    [Header("Rotasyon Ayarlar�")]
    [Tooltip("G�� TAM dolunca uygulanacak ek rotasyon (Euler derece). " +
             "-20'yi istedi�in eksene koy: �rn (-20,0,0), (0,0,-20)...")]
    [SerializeField] private Vector3 geriRotasyon = new Vector3(-20f, 0f, 0f);

    [Tooltip("Rotasyonun yumu�akl��� (Lerp h�z�). B�y�k = daha h�zl� tepki.")]
    [SerializeField] private float yumusaklik = 10f;

    // Uygulanan yay miktar� (0 = ba�lang��, 1 = tam geri)
    private float mevcutYuzde;
    // Ge�en frame'de uygulad���m�z offset (temiz rest rotasyonunu bulmak i�in)
    private Quaternion oncekiOffset = Quaternion.identity;

    void Awake()
    {
        if (donecekObje == null)
            donecekObje = transform;
    }

    void OnDisable()
    {
        // Script/obje kapan�rsa uygulad���m�z ek rotasyonu geri al ki poz bozulmas�n
        donecekObje.localRotation = donecekObje.localRotation * Quaternion.Inverse(oncekiOffset);
        oncekiOffset = Quaternion.identity;
        mevcutYuzde = 0f;
    }

    // LateUpdate: FishingSystem g�c� ve FishingRod animasyonu i�lendikten SONRA
    // rotasyonu ekliyoruz ki �st�ne temiz binsin.
    void LateUpdate()
    {
        if (fishingSystem == null)
            fishingSystem = FindObjectOfType<FishingSystem>();

        float hedefYuzde = 0f;

        // Sadece olta elde + sistem aktifken g�ce g�re geriye yaslan
        if (fishingSystem != null && fishingSystem.enabled &&
            fishingSystem._fishingRod != null && fishingSystem._maxCastForce > 0f)
        {
            hedefYuzde = fishingSystem._currentCastForce / fishingSystem._maxCastForce;
        }

        // Tu� b�rak�l�nca _currentCastForce 0 olur -> hedefYuzde 0 -> yumu�ak�a ba�a d�ner
        mevcutYuzde = Mathf.Lerp(mevcutYuzde, hedefYuzde, yumusaklik * Time.deltaTime);
        if (mevcutYuzde < 0.001f) mevcutYuzde = 0f;

        // �nceki offset'i geri al -> temiz rest rotasyonu; sonra yeni offset'i uygula.
        // Bu "additive" y�ntem, ellerin/animasyonun rotasyonuyla �ak��maz.
        Quaternion temizRot = donecekObje.localRotation * Quaternion.Inverse(oncekiOffset);
        Quaternion yeniOffset = Quaternion.Euler(geriRotasyon * mevcutYuzde);
        donecekObje.localRotation = temizRot * yeniOffset;
        oncekiOffset = yeniOffset;
    }
}
