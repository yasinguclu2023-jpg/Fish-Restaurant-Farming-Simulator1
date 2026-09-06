using UnityEngine;

public class NesneSesProfili : MonoBehaviour
{
    [SerializeField] private SesProfili profil;

    public SesProfili Profil => profil;

    public void ProfilAyarla(SesProfili yeniProfil)
    {
        profil = yeniProfil;
    }
}