using System.Collections;
using UnityEngine;
using Ameba;

namespace GMTK {
  public class GMTKInitialization : InitializationComponent {
    protected override IEnumerator InitializeAllScriptableObjects() {
      // Load service registry first
      yield return LoadResourceAsync<ServiceRegistry>("ServiceRegistry", registry => {
        Services.Initialize(registry);
      });

      // Wait one frame for services to settle
      yield return null;

      InitializationManager.MarkInitializationComplete();
    }
  }
}