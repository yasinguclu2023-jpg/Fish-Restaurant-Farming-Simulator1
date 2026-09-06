using UnityEngine;

/// <summary>
/// Karakterin üstüne eklenir.
/// J = düz ilerlemeye baþla, K = dur, L = karakteri sil.
/// </summary>
public class TusIleYuruyus : MonoBehaviour
{
    [Tooltip("Yürüme hýzý (birim / saniye)")]
    [SerializeField] private float yurumeHizi = 2f;

    private bool yuruyor = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
            yuruyor = true;

        if (Input.GetKeyDown(KeyCode.K))
            yuruyor = false;

        if (Input.GetKeyDown(KeyCode.L))
            Destroy(gameObject);

        if (yuruyor)
            transform.position += transform.forward * yurumeHizi * Time.deltaTime;
    }
}