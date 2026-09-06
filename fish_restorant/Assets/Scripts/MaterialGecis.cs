using UnityEngine;

public class MaterialGecis : MonoBehaviour
{
    [Header("Materyaller")]
    [SerializeField] private Material matA;
    [SerializeField] private Material matB;

    private RaycastSistemi raycastSistemi;
    private Renderer nesneRenderer;
    private bool matAMi = true;

    void Start()
    {
        raycastSistemi = FindObjectOfType<RaycastSistemi>();
        nesneRenderer = GetComponent<Renderer>();

        if (nesneRenderer == null)
            Debug.LogError($"[MaterialGecis] '{gameObject.name}' üzerinde Renderer bulunamadı!");

        if (matA != null && nesneRenderer != null)
            nesneRenderer.material = matA;
    }

    void Update()
    {
        if (raycastSistemi == null || nesneRenderer == null) return;
        if (raycastSistemi.BakilanObje != gameObject) return;
        if (!Input.GetMouseButtonDown(0)) return;

        matAMi = !matAMi;
        nesneRenderer.material = matAMi ? matA : matB;
    }
}
