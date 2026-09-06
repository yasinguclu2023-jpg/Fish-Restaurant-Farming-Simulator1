using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Steam fragmanı için göstermelik "masada yemek yeme" sekansı.
/// Bu script'i küpe (trigger bölgesine) ekle.
///
/// Akış:
/// 1) Mevcut yerleştirme sistemiyle, "tetikleyenTag" tag'li bir nesne küpün içine bırakılır.
/// 2) Küp bunu algılar → karakterin Animator'una "yeme" trigger'ı gönderilir.
/// 3) Bırakılan nesnenin altındaki "silinecekTag" tag'li child(lar) silinir.
/// 4) Karakterin elindeki (başta kapalı) obje açılır (SetActive true).
///
/// Bağımsız çalışır; yerleştirme sistemine dokunmaz, sadece trigger ile dinler.
///
/// KÜP KURULUMU: Box Collider (Is Trigger ✓) + Rigidbody (Is Kinematic ✓, Use Gravity ✗).
/// </summary>
[RequireComponent(typeof(Collider))]
public class MasaYemeSekansi : MonoBehaviour
{
    [Header("1) Tetikleme")]
    [Tooltip("Küpe bu tag'li bir nesne girince sekans başlar")]
    [SerializeField] private string tetikleyenTag = "Yemek";

    [Header("2) Karakter Animasyonu")]
    [Tooltip("Masada oturan karakterin Animator'ı")]
    [SerializeField] private Animator karakterAnimator;
    [Tooltip("Animator'daki yeme bool parametresinin adı (true iken yeme animasyonu loop oynar)")]
    [SerializeField] private string yemeBoolParametresi = "Yiyor";
    [Tooltip("Yeme animasyonu kaç saniye loop olsun? Süre sonunda idle'a döner")]
    [SerializeField] private float yemeSuresi = 3f;

    [Header("3) Bırakılan Nesnede Silinecek Parça")]
    [Tooltip("Küpe konan nesnenin altında bu tag'e sahip child(lar) silinir")]
    [SerializeField] private string silinecekTag = "Tabak_Yemegi";
    [Tooltip("Trigger'dan kaç saniye sonra silinsin (yeme animasyonuyla senkron için)")]
    [SerializeField] private float silmeGecikmesi = 1.0f;

    [Header("4) Karakterin Elindeki Obje")]
    [Tooltip("Başta kapalı (SetActive false) olan, sekans sonunda açılacak obje")]
    [SerializeField] private GameObject elindekiObje;
    [Tooltip("Trigger'dan kaç saniye sonra elindeki açılsın")]
    [SerializeField] private float elAcmaGecikmesi = 1.5f;

    [Header("Ses (opsiyonel)")]
    [SerializeField] private SesVerisi yemeSesi;

    [Header("Ayarlar")]
    [Tooltip("Açıkken sekans yalnızca bir kez çalışır")]
    [SerializeField] private bool tekSefer = true;

    private bool calisti;

    void Reset()
    {
        // Kolaylık: collider'ı otomatik trigger yap
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (tekSefer && calisti) return;
        if (!other.CompareTag(tetikleyenTag)) return;

        calisti = true;
        StartCoroutine(SekansCalistir(other.transform.root));
    }

    private IEnumerator SekansCalistir(Transform birakilanKok)
    {
        // 2) Yeme animasyonunu başlat (bool true → loop oynar)
        if (karakterAnimator != null && !string.IsNullOrEmpty(yemeBoolParametresi))
            karakterAnimator.SetBool(yemeBoolParametresi, true);

        // Ses (varsa)
        if (yemeSesi != null && SesYoneticisi.Instance != null)
            SesYoneticisi.Instance.SesCal(yemeSesi, transform.position);

        // Tüm gecikmeler trigger anına göre (mutlak zaman) ölçülür.
        float gecenSure = 0f;

        // 3) Bırakılan nesnenin altındaki tag'li parçaları sil
        if (silmeGecikmesi > gecenSure)
        {
            yield return new WaitForSeconds(silmeGecikmesi - gecenSure);
            gecenSure = silmeGecikmesi;
        }
        TaglıCocuklariSil(birakilanKok, silinecekTag);

        // 4) Karakterin elindekini aç
        if (elAcmaGecikmesi > gecenSure)
        {
            yield return new WaitForSeconds(elAcmaGecikmesi - gecenSure);
            gecenSure = elAcmaGecikmesi;
        }
        if (elindekiObje != null)
            elindekiObje.SetActive(true);

        // 5) Yeme süresi dolunca idle'a dön (bool false)
        if (yemeSuresi > gecenSure)
        {
            yield return new WaitForSeconds(yemeSuresi - gecenSure);
            gecenSure = yemeSuresi;
        }
        if (karakterAnimator != null && !string.IsNullOrEmpty(yemeBoolParametresi))
            karakterAnimator.SetBool(yemeBoolParametresi, false);
    }

    private void TaglıCocuklariSil(Transform kok, string tag)
    {
        if (kok == null || string.IsNullOrEmpty(tag)) return;

        // Tüm alt objeleri tara, tag eşleşenleri topla, sonra sil
        // (silerken listede gezmemek için önce topluyoruz)
        var silinecekler = new List<GameObject>();
        Transform[] hepsi = kok.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < hepsi.Length; i++)
        {
            if (hepsi[i] != null && hepsi[i].CompareTag(tag))
                silinecekler.Add(hepsi[i].gameObject);
        }

        for (int i = 0; i < silinecekler.Count; i++)
            Destroy(silinecekler[i]);
    }

    /// <summary>
    /// tekSefer kilidini sıfırlar (tekrar test için).
    /// </summary>
    public void Sifirla() => calisti = false;
}
