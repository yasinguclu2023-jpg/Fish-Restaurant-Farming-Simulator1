using UnityEngine;
using FishingGameTool.Fishing;
using FishingGameTool.Fishing.Rod;

public class OltaEntegrasyon : MonoBehaviour
{
    [Header("Ayarlar")]
    public FishingSystem fishingSystem;

    private FishingRod fishingRod;
    private bool eldeOltaVar = false;

    void Awake()
    {
        fishingRod = GetComponent<FishingRod>();
        if (fishingRod == null)
            fishingRod = GetComponentInChildren<FishingRod>();
    }

    void Update()
    {
        bool suAnElde = false;
        Transform kontrol = transform;
        while (kontrol.parent != null)
        {
            kontrol = kontrol.parent;
            if (kontrol.GetComponent<FishingSystem>() != null)
            {
                suAnElde = true;
                break;
            }
        }

        if (suAnElde && !eldeOltaVar)
        {
            OltaAlindi();
        }
        else if (!suAnElde && eldeOltaVar)
        {
            OltaBirakildi();
        }
    }

    void OltaAlindi()
    {
        eldeOltaVar = true;

        if (fishingSystem == null)
            fishingSystem = FindObjectOfType<FishingSystem>();

        if (fishingSystem == null)
        {
            Debug.LogError("[OltaEntegrasyon] Sahnede FishingSystem bulunamad�!");
            return;
        }

        // �nce t�m state'leri temizle
        fishingSystem._currentCastForce = 0f;
        fishingSystem._fishingRod = fishingRod;
        fishingSystem.enabled = true;

        // Oltay� alan sol t�k'�n at�� olarak alg�lanmamas� i�in korumay� kur.
        // enabled zaten true ise OnEnable �al��mayaca��ndan bunu a��k�a �a��r�yoruz.
        fishingSystem.AtisKorumasiniKur();
    }

    void OltaBirakildi()
    {
        eldeOltaVar = false;

        if (fishingSystem == null) return;

        // �amand�ra varsa temizle
        if (fishingRod != null && fishingRod._fishingFloat != null)
        {
            Destroy(fishingRod._fishingFloat.gameObject);
            fishingRod._fishingFloat = null;
            fishingRod.FinishFishing();
        }

        // T�m state'leri s�f�rla
        fishingSystem._currentCastForce = 0f;
        fishingSystem.enabled = false;
        fishingSystem._fishingRod = null;
    }

    void OnDestroy()
    {
        if (fishingSystem != null && eldeOltaVar)
        {
            fishingSystem._currentCastForce = 0f;
            fishingSystem.enabled = false;
            fishingSystem._fishingRod = null;
        }
    }
}