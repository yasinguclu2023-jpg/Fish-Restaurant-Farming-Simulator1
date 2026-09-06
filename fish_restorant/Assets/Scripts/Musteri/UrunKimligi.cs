using UnityEngine;

/// <summary>
/// Bir yemek/urun objesinin hangi Malzeme oldugunu tutar.
/// Masaya servis edilince siparisle eslestirmek icin kullanilir (referansla, yazim hatasiz).
///
/// KURULUM: Her yemek prefab'ina (balik, domates, marul, kola...) bu scripti ekle ve
/// "malzeme" alanina o yemegin Malzeme asset'ini surukle.
/// </summary>
public class UrunKimligi : MonoBehaviour
{
    [Tooltip("Bu yemek objesinin karsiligi olan Malzeme asset'i.")]
    [SerializeField] private Malzeme malzeme;

    public Malzeme Malzeme => malzeme;
}
