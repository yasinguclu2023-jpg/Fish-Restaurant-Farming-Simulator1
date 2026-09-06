using UnityEngine;

public class ObjeDegistir : MonoBehaviour
{
    [SerializeField] private GameObject[] objeler;      // Sýrayla geçiþ yapýlacak objeler (3 tane ekle)
    [SerializeField] private AudioSource sesKaynagi;    // Opsiyonel: tuþa basýnca çalacak ses
    [SerializeField] private AudioClip degisimSesi;     // Opsiyonel: ses efekti

    private int aktifIndex = 0;  // Þu an açýk olan obje

    void Start()
    {
        // Baþlangýçta sadece ilk obje açýk, diðerleri kapalý
        for (int i = 0; i < objeler.Length; i++)
        {
            objeler[i].SetActive(i == 0);
        }
        aktifIndex = 0;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            SonrakiObjeye_Gec();
        }
    }

    private void SonrakiObjeye_Gec()
    {
        if (objeler == null || objeler.Length == 0)
            return;

        // Þu anki objeyi kapat
        objeler[aktifIndex].SetActive(false);

        // Sýradaki objeye geç (sona gelince baþa döner)
        aktifIndex = (aktifIndex + 1) % objeler.Length;

        // Yeni objeyi aç
        objeler[aktifIndex].SetActive(true);

        // Ses efekti (atanmýþsa çal)
        if (sesKaynagi != null && degisimSesi != null)
            sesKaynagi.PlayOneShot(degisimSesi);
    }
}