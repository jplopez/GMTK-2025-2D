
using System;
namespace Ameba.Runtime {

  [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
  public class RuntimeVariableAttribute : Attribute {
    public string VariableId { get; }
    public bool TwoWay { get; }

    public RuntimeVariableAttribute(string variableId, bool twoWay) {
      VariableId = variableId;
      TwoWay = twoWay;
    }
  }
}