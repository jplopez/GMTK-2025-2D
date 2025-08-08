using System;
using UnityEngine.InputSystem;

namespace Ameba.Input {

  /// <summary>
  /// <para>
  /// Attribute used to annotate methods in a <see cref="UnityEngine.MonoBehaviour"/> that should act as input handlers for a specific <see cref="InputActionRegistry"/>.
  /// </para>
  ///
  /// <para>
  /// To use this attribute, your MonoBehaviour must inherit from <see cref="InputHandlerBase"/>, which automatically registers annotated methods at runtime.
  /// </para>
  ///
  /// <para>
  /// Each method must match the signature <c>void MethodName(InputAction.CallbackContext context)</c>.
  /// You can specify one or more input phases (e.g., <c>Started</c>, <c>Performed</c>, <c>Canceled</c>) for which the method should be invoked.
  /// If no phases are specified, <c>Performed</c> is used by default.
  /// </para>
  ///
  /// <para>
  /// Example usage:
  /// </para>
  ///
  /// <example>
  /// <code>
  /// [InputHandler("Jump", InputActionPhase.Started, InputActionPhase.Performed)]
  /// private void HandleJump(InputAction.CallbackContext context) {
  ///     // Responds to both Started and Performed phases of the "Jump" action
  /// }
  ///
  /// [InputHandler("Attack")] // Defaults to Performed
  /// private void HandleAttack(InputAction.CallbackContext context) {
  ///     // Responds to Performed phase of the "Attack" action
  /// }
  /// </code>
  /// </example>
  /// </summary>
  [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
  public class InputHandlerAttribute : Attribute {
    public string ActionName { get; }
    public InputActionPhase[] Phases { get; }

    public InputHandlerAttribute(string action, params InputActionPhase[] phases) {
      ActionName = action;
      if (phases.Length > 0) {
        Phases = phases;
      } else {
        Phases = new[] { InputActionPhase.Performed };
      }
    }
  }
}