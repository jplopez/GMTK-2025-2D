namespace Ameba {

  public interface IPoolable {
    string PrefabId { get; set; }
    int PrewarmCount { get; }
    void OnSpawn();
    void OnReturn();
  }
}