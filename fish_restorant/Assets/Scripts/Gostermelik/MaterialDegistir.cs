using UnityEngine;

public class MaterialDegistir : MonoBehaviour
{
    [SerializeField] private Renderer hedefRenderer;    // Modelin Renderer'ý
    [SerializeField] private Material[] materialler;    // Sýrayla geçiþ yapýlacak materialler (3 tane ekle)
    [SerializeField] private int materialIndex = 0;     // Modelde hangi material slotu deðiþecek (0, 1, 2...)
    [SerializeField] private AudioSource sesKaynagi;    // Opsiyonel: tuþa basýnca çalacak ses
    [SerializeField] private AudioClip degisimSesi;     // Opsiyonel: ses efekti

    private int aktifMaterial = 0;  // Þu an kaçýncý material gösteriliyor

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            SonrakiMateriale_Gec();
        }
    }

    private void SonrakiMateriale_Gec()
    {
        // Güvenlik kontrolleri
        if (materialler == null || materialler.Length == 0)
            return;

        Material[] modelMaterialleri = hedefRenderer.materials;

        if (materialIndex < 0 || materialIndex >= modelMaterialleri.Length)
            return;

        // Sýradaki material'e geç (sona gelince baþa döner)
        aktifMaterial = (aktifMaterial + 1) % materialler.Length;

        // Sadece seçili slotu deðiþtir, diðerlerine dokunma
        modelMaterialleri[materialIndex] = materialler[aktifMaterial];
        hedefRenderer.materials = modelMaterialleri;

        // Ses efekti (atanmýþsa çal)
        if (sesKaynagi != null && degisimSesi != null)
            sesKaynagi.PlayOneShot(degisimSesi);
    }
}