/// <summary>
/// Cop kovasina atilan (elde tutulan) konteynerlerin ozel davranisi.
///
/// - CopeAtildi() TRUE donerse: obje BOSALTILDI, elde kalir (kap korunur).
/// - CopeAtildi() FALSE donerse: cop kovasi objeyi KOMPLE siler.
/// - Bu arayuzu HIC tasimayan objeler dogrudan silinir (varsayilan davranis).
/// </summary>
public interface ICopEtkilesimi
{
    bool CopeAtildi();
}
