using UnityEngine;

/// <summary>Musteri cinsiyeti. (Su an kullanilmiyor; ileride cinsiyetli ses icin hazir durur.)</summary>
public enum Cinsiyet { Erkek, Kadin }

/// <summary>
/// Musteri tepki seslerinin merkezi bankasi (ScriptableObject).
/// Project > sag tik > Restoran > Musteri Ses Bankasi.
///
/// - Her alan bir SesVerisi'dir; SesVerisi icine BIRDEN FAZLA klip koyarak
///   cesitlilik saglayabilirsin (rastgele calar).
/// - Tum musteri prefablari AYNI banka asset'ini referans alir; ses eklemek/degistirmek
///   icin tek yeri (bu asset'i) duzenlemen yeter.
/// </summary>
[CreateAssetMenu(fileName = "MusteriSesBankasi", menuName = "Restoran/Musteri Ses Bankasi")]
public class MusteriSesBankasi : ScriptableObject
{
    [Header("Tepki Sesleri")]
    [Tooltip("Dogru servis olunca (mutlu).")]
    [SerializeField] private SesVerisi olumlu;
    [Tooltip("Yanlis / cig / yanik servis olunca (mutsuz).")]
    [SerializeField] private SesVerisi olumsuz;

    /// <summary>Olumlu (dogru servis) sesi. Atanmamissa null.</summary>
    public SesVerisi OlumluSes() => olumlu;

    /// <summary>Olumsuz (yanlis servis) sesi. Atanmamissa null.</summary>
    public SesVerisi OlumsuzSes() => olumsuz;
}
