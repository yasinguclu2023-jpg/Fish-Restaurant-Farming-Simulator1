using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// GECICI TEST SCRIPTI - NavMesh bake dogru mu diye kontrol icin.
///
/// KURULUM:
/// 1. Sahneye bir Capsule ekle (3D Object > Capsule) -> NavMeshAgent ekle -> bu scripti ekle.
/// 2. Sahneye bir hedef obje koy (orn. bos GameObject veya kucuk bir Cube).
/// 3. O hedefi bu scriptteki "hedef" alanina surukle.
/// 4. Capsule'u mavi NavMesh alanina koy -> Play'e bas.
/// 5. Capsule hedefe dogru yurumeli. Play'deyken hedefi Scene goruntusunde
///    surukleyip baska yere tasiyinca capsule pesinden gitmeli.
///
/// Engellerden (masa/sandalye) dolasarak gidiyorsa NavMesh kurulumu tamam.
/// Test bitince bu scripti ve test objelerini silebilirsin.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class NavTest : MonoBehaviour
{
    [Tooltip("Capsule'un takip edecegi hedef. Sahnedeki bir objeyi buraya surukle.")]
    [SerializeField] private Transform hedef;

    [Tooltip("Hedef her bu kadar saniyede bir guncellenir (surekli degil, performans icin).")]
    [SerializeField] private float guncellemeAraligi = 0.2f;

    private NavMeshAgent agent;
    private float sonrakiGuncelleme;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (hedef == null) return;
        if (Time.time < sonrakiGuncelleme) return;

        sonrakiGuncelleme = Time.time + guncellemeAraligi;
        agent.SetDestination(hedef.position);
    }
}
