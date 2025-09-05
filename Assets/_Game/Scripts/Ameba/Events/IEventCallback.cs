using System;

namespace Ameba {
  public interface IEventCallback {
    void Invoke(object payload);
    Type PayloadType { get; }
  }
}