using UnityEngine;

/// <summary>
/// Küvetten alınan nesneye eklenir.
/// Geri koyulduğunda kaç model açılacağını belirler.
/// 
/// Küvetle hiç temas etmemiş nesne → KuvetSistemi'ndeki koymaMiktari kullanılır
/// Küvetten alınmış nesne → bu component'teki miktar kullanılır
/// </summary>
public class KuvetDilimVerisi : MonoBehaviour
{
    [Tooltip("Bu nesne küvete geri koyulduğunda kaç model açılacak")]
    [SerializeField] private int miktar = 1;

    public int Miktar => miktar;

    public void MiktarAyarla(int yeniMiktar)
    {
        miktar = Mathf.Max(1, yeniMiktar);
    }
}