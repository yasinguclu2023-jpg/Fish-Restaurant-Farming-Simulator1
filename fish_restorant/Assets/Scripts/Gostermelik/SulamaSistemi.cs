using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SulamaSistemi : MonoBehaviour
{
    [Header("Tetikleme")]
    [Tooltip("Sulamay� ba�latan tu�")]
    public KeyCode tetikTusu = KeyCode.V;

    [Header("D�nme (Rotasyon) Ayarlar�")]
    [Tooltip("D�necek nesne. Bo� b�rak�l�rsa bu scriptin oldu�u nesne d�ner.")]
    public Transform donenNesne;
    [Tooltip("Hangi eksende d�ns�n. X i�in (1,0,0), Y i�in (0,1,0), Z i�in (0,0,1).")]
    public Vector3 donmeEkseni = new Vector3(1f, 0f, 0f);
    [Tooltip("Ka� DERECE d�ns�n (e�ilsin).")]
    public float donmeAcisi = 45f;
    [Tooltip("D�nme h�z�. D���K de�er = daha yava� ve yumu�ak.")]
    public float donmeHizi = 1f;
    [Tooltip("En u�ta (e�ik haldeyken) bekleme s�resi (saniye).")]
    public float tepedeBeklemeSuresi = 0.5f;

    [Header("Particle (Su) Ayarlar�")]
    [Tooltip("A��l�p kapanacak su particle'�.")]
    public ParticleSystem suParticle;
    [Tooltip("Tu�a bas�ld�ktan KA� saniye SONRA particle a��ls�n.")]
    public float particleAcilmaGecikmesi = 0.4f;
    [Tooltip("Particle a��ld�ktan sonra KA� saniye a��k kal�p kapans�n.")]
    public float particleAcikKalmaSuresi = 1.5f;

    [Header("Ses Ayarlar�")]
    [Tooltip("Sulama ba�lad���nda �alacak ses efekti.")]
    public AudioClip sesEfekti;
    [Range(0f, 1f)]
    public float sesSeviyesi = 1f;

    private AudioSource _audioSource;
    private Quaternion _baslangicRot;
    private bool _calisiyor = false;
    private Coroutine _sulamaCo;
    private Coroutine _particleCo;

    void Awake()
    {
        if (donenNesne == null)
            donenNesne = transform;

        _baslangicRot = donenNesne.localRotation;

        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(tetikTusu) && !_calisiyor)
            _sulamaCo = StartCoroutine(SulamaRutini());
    }

    void OnDisable()
    {
        // Nesne elden birakilirsa / kapatilirsa rutinler yarim kalmasin
        if (_sulamaCo != null) { StopCoroutine(_sulamaCo); _sulamaCo = null; }
        if (_particleCo != null) { StopCoroutine(_particleCo); _particleCo = null; }

        if (suParticle != null)
            suParticle.Stop();

        if (_calisiyor && donenNesne != null)
            donenNesne.localRotation = _baslangicRot;

        _calisiyor = false;
    }

    private IEnumerator SulamaRutini()
    {
        _calisiyor = true;

        // Referans rotasyonu TUSA BASILDIGI AN oku.
        // Nesne ele alindiginda localRotation sifirlandigi icin
        // Awake'te kaydedilen deger gecersiz kaliyor ve ani aci ziplamasi oluyordu.
        _baslangicRot = donenNesne.localRotation;

        if (sesEfekti != null)
            _audioSource.PlayOneShot(sesEfekti, sesSeviyesi);

        // Particle zamanlamas�n� ayr� ba�lat (d�nmeden ba��ms�z)
        if (_particleCo != null) StopCoroutine(_particleCo);
        _particleCo = StartCoroutine(ParticleRutini());

        // Hedef a��: ba�lang�� rotasyonu �zerine, se�ilen eksende belirtilen derece
        Quaternion hedefRot = _baslangicRot * Quaternion.AngleAxis(donmeAcisi, donmeEkseni.normalized);

        // 1) Yumu�ak�a e�il
        yield return StartCoroutine(YumusakDonus(_baslangicRot, hedefRot));

        // 2) U�ta bekle
        if (tepedeBeklemeSuresi > 0f)
            yield return new WaitForSeconds(tepedeBeklemeSuresi);

        // 3) Yumu�ak�a geri d�n
        yield return StartCoroutine(YumusakDonus(hedefRot, _baslangicRot));

        _calisiyor = false;
        _sulamaCo = null;
    }

    private IEnumerator YumusakDonus(Quaternion baslangic, Quaternion bitis)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * donmeHizi;
            float yumusak = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            donenNesne.localRotation = Quaternion.Slerp(baslangic, bitis, yumusak);
            yield return null;
        }
        donenNesne.localRotation = bitis;
    }

    private IEnumerator ParticleRutini()
    {
        if (suParticle == null) yield break;

        yield return new WaitForSeconds(particleAcilmaGecikmesi);
        suParticle.Play();

        yield return new WaitForSeconds(particleAcikKalmaSuresi);
        suParticle.Stop();
        _particleCo = null;
    }
}