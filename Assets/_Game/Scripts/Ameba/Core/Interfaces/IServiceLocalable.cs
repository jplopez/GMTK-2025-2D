
namespace Ameba {
  /// <summary>
  /// Interface for services that can be localized by a ServiceLocator.
  /// The interfaces defines a common life cycle to initialize and reset and identification property.
  /// </summary>
  public interface IServiceLocalable {

    public string ServiceName { get; }

    public bool IsInitialized { get; }

    public void InitializeService();

    public void ResetService();

  }
}