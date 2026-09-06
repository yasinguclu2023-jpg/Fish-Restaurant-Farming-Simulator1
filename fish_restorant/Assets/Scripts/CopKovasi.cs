using UnityEngine;

/// <summary>
/// Cop kovasi isareti. Elde obje tutarken bu objeye bakip sol tiklayinca:
/// - Normal obje                                  -> komple silinir.
/// - ICopEtkilesimi olan (tepsi/kuvet/balik kasasi) -> icini bosaltir, kap elde kalir.
///
/// Asil mantik NesneYerlestirmeSistemi.CopKovasinaBirakmaDene() icindedir.
///
/// KURULUM: Cop kovasi objesine bu scripti + bir Collider ekle
/// (oyuncunun bakis mesafesi / maxMesafe icinde olmali).
/// </summary>
public class CopKovasi : MonoBehaviour
{
    [Header("Ses (opsiyonel)")]
    [Tooltip("Cope atinca calan ses.")]
    [SerializeField] private SesVerisi copSesi;

    /// <summary>Cop sesini verilen konumda calar (atanmamissa sessiz).</summary>
    public void SesCal(Vector3 pozisyon)
    {
        if (copSesi != null && SesYoneticisi.Instance != null)
            SesYoneticisi.Instance.SesCal(copSesi, pozisyon);
    }
}
