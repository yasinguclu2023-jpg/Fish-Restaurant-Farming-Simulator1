using UnityEngine;
using UnityEngine.UI;
using FishingGameTool.Fishing;

/// <summary>
/// Olta atýþ gücü UI göstergesi.
/// Bu scripti CANVAS'a ekleyin, çerçeveyi referans olarak verin.
/// </summary>
public class OltaAtisBarUI : MonoBehaviour
{
    [Header("UI Referanslarý")]
    [Tooltip("Güç çerçevesi objesi (açýlýp kapanacak)")]
    [SerializeField] private GameObject cerceve;

    [Tooltip("Bar'ýn içindeki dolgu Image'ý (Fill)")]
    [SerializeField] private Image barDolgu;

    private FishingSystem fishingSystem;

    void Start()
    {
        if (cerceve != null)
            cerceve.SetActive(false);

        if (barDolgu != null)
            barDolgu.fillAmount = 0f;
    }

    void Update()
    {
        if (fishingSystem == null)
            fishingSystem = FindObjectOfType<FishingSystem>();

        if (fishingSystem == null || cerceve == null)
        {
            if (cerceve != null && cerceve.activeSelf)
                cerceve.SetActive(false);
            return;
        }

        float yuzde = 0f;

        if (fishingSystem._maxCastForce > 0f)
            yuzde = fishingSystem._currentCastForce / fishingSystem._maxCastForce;

        if (yuzde > 0f)
        {
            if (!cerceve.activeSelf)
                cerceve.SetActive(true);

            if (barDolgu != null)
                barDolgu.fillAmount = yuzde;
        }
        else
        {
            if (cerceve.activeSelf)
            {
                if (barDolgu != null)
                    barDolgu.fillAmount = 0f;

                cerceve.SetActive(false);
            }
        }
    }
}