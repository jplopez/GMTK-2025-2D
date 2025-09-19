using UnityEngine;

namespace Ameba {

  /// <summary>
  /// Interface for objects that can detect hover over IHoverable objects.
  /// Provides methods to manage hover detection and state.
  /// </summary>
  public interface IHover<T> where T : IHoverable {
    /// <summary>
    /// Whether hover detection is currently enabled.
    /// </summary>
    bool CanHover { get; set; }

    /// <summary>
    /// Whether hover is currently active over an element.
    /// </summary>
    bool IsHovering { get; }

    /// <summary>
    /// The currently hovered element, if any.
    /// </summary>
    T HoveredElement { get; }

    /// <summary>
    /// Check for hoverable elements at the specified world position.
    /// </summary>
    /// <param name="worldPosition">World position to check</param>
    /// <param name="element">The hoverable element found, if any</param>
    /// <returns>True if a hoverable element was found</returns>
    bool TryGetHoverableAt(Vector3 worldPosition, out T element);

    /// <summary>
    /// Check for hoverable elements at the specified screen position.
    /// </summary>
    /// <param name="screenPosition">Screen position to check</param>
    /// <param name="element">The hoverable element found, if any</param>
    /// <returns>True if a hoverable element was found</returns>
    bool TryGetHoverableAt(Vector2 screenPosition, out T element);

    /// <summary>
    /// Start hovering over the specified element.
    /// </summary>
    /// <param name="element">The element to start hovering</param>
    void StartHover(T element);

    /// <summary>
    /// Stop hovering over the current element.
    /// </summary>
    void StopHover();

    /// <summary>
    /// Update hover detection at the current pointer position.
    /// </summary>
    void UpdateHover();
  }
}