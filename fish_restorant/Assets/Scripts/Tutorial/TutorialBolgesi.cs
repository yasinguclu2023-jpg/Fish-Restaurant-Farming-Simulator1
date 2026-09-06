using UnityEngine;

// ===== TUTORIAL (kaldirilabilir) =====

/// <summary>
/// "Bahceye git", "iskeleye cik", "olta standina git" gibi adimlar icin GIDILECEK ALAN.
///
/// Oyuncu bu alanin icine girdiginde "Girildi" true olur ve OYLE KALIR
/// (oyuncu cikinca sart bozulmaz).
///
/// KURULUM:
/// 1. Bos bir GameObject olustur, gidilmesini istedigin yere koy.
/// 2. Box Collider ekle -> "Is Trigger" TIKINI AC -> alani oyuncunun gececegi
///    genislikte ayarla (Scene view'da yesil kutu olarak gorunur).
/// 3. Bu scripti ekle.
/// 4. Oyuncunun tag'i "Player" degilse "Oyuncu Tag" alanina dogru tag'i yaz.
/// 5. TutorialKosulu'nda tip = BolgeyeGirildi, Hedef = bu obje.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TutorialBolgesi : MonoBehaviour
{
    [Header("Ayarlar")]
    [Tooltip("Bu tag'e sahip obje girince sart saglanir. Oyuncunun tag'i.")]
    [SerializeField] private string oyuncuTag = "Player";

    [Tooltip("Alan bir kez tetiklenince Scene'de gorunmez olsun mu? (sadece gizmo)")]
    [SerializeField] private bool girincePasiflestir = true;

    [Header("Teshis")]
    [SerializeField] private bool teshisLogu = false;

    private bool girildi;

    /// <summary>Oyuncu bu alana girdi mi? (bir kez girince true kalir)</summary>
    public bool Girildi => girildi;

    void Reset()
    {
        // Kolayligi icin: script eklenince collider otomatik trigger yapilir.
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        Degerlendir(other);
    }

    // Oyuncu adim acildiginda ZATEN alanin icindeyse Enter tetiklenmez;
    // Stay bu durumu yakalar (sart takilip kalmaz).
    void OnTriggerStay(Collider other)
    {
        Degerlendir(other);
    }

    void Degerlendir(Collider other)
    {
        if (girildi) return;
        if (!other.CompareTag(oyuncuTag)) return;

        girildi = true;

        if (teshisLogu)
            Debug.Log($"[TutorialBolgesi] '{gameObject.name}' alanina girildi.", this);

        if (girincePasiflestir)
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }

    /// <summary>Akis yeniden baslarken cagrilir (yeni gun / tutorial resetlenirse).</summary>
    public void Sifirla()
    {
        girildi = false;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = girildi ? new Color(0.3f, 0.3f, 0.3f, 0.25f) : new Color(0.2f, 1f, 0.3f, 0.25f);

        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
    }
#endif
}
// ===== TUTORIAL SONU =====
