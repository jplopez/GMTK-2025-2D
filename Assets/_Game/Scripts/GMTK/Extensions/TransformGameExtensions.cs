using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GMTK.Extensions {
  /// <summary>
  /// Game-specific Transform extensions for rotation and flipping operations.
  /// These methods provide pure transformation logic without game-specific event handling.
  /// </summary>
  public static class TransformGameExtensions {


    /// <summary>
    /// Find the first child Transform (excluding the parent itself) that has the specified tag.
    /// </summary>
    /// <param name="transform"></param>
    /// <param name="tag"></param>
    /// <returns></returns>
    public static Transform FindChildWithTag(this Transform transform, string tag) {
      return FindChildrenWithAnyTag(transform, new string[] { tag }).FirstOrDefault();
    }

    /// <summary>
    /// Finds all child Transform (excluding the parent itself) that have the specified tag.
    /// </summary>
    /// <param name="transform"></param>
    /// <param name="tag"></param>
    /// <returns></returns>
    public static List<Transform> FindChildsWithTag(this Transform transform, string tag) {
      var tags = new string[] { tag };
      return FindChildrenWithAnyTag(transform, tags);
    }

    /// <summary>
    /// Finds the first child Transform (excluding the parent itself) that has any of the specified tags.
    /// </summary>
    /// <param name="transform"></param>
    /// <param name="tags"></param>
    /// <returns></returns>
    public static Transform FindChildWithAnyTag(this Transform transform, string[] tags) {
      return FindChildrenWithAnyTag(transform, tags).FirstOrDefault();
    }

    /// <summary>
    /// Finds all child Transforms (excluding the parent itself) that have any of the specified tags.
    /// </summary>
    /// <param name="transform"></param>
    /// <param name="tags"></param>
    /// <returns></returns>
    public static List<Transform> FindChildrenWithAnyTag(this Transform transform, string[] tags) {
      // Get all Transform components in children (including the parent itself)
      Transform[] allChildrenTransforms = transform.GetComponentsInChildren<Transform>();

      // Filter for GameObjects with the target tag and exclude the parent itself
      List<Transform> foundChildren = allChildrenTransforms
          .Where(t => tags.Contains(t.tag) && t.gameObject != transform.gameObject).ToList();

      return foundChildren;
    }

    /// <summary>
    /// Rotates the transform clockwise by 90 degrees on the Z-axis.
    /// Accounts for existing flipped state to maintain consistent visual rotation.
    /// </summary>
    /// <param name="transform">The transform to rotate</param>
    /// <returns>The transform for method chaining</returns>
    public static Transform RotateClockwise(this Transform transform, float angle = 90f) {
      bool shouldInvert = transform.IsFlippedX() ^ transform.IsFlippedY(); // XOR - true if exactly one is flipped
      float rotationAmount = shouldInvert ? angle : -angle;
      transform.Rotate(Vector3.forward, rotationAmount);
      return transform;
    }

    /// <summary>
    /// Rotates the transform counter-clockwise by 90 degrees on the Z-axis.
    /// Accounts for existing flipped state to maintain consistent visual rotation.
    /// </summary>
    /// <param name="transform">The transform to rotate</param>
    /// <returns>The transform for method chaining</returns>
    public static Transform RotateCounterClockwise(this Transform transform, float angle = 90f) {
      bool shouldInvert = transform.IsFlippedX() ^ transform.IsFlippedY(); // XOR - true if exactly one is flipped
      float rotationAmount = shouldInvert ? -angle : angle;
      transform.Rotate(Vector3.forward, rotationAmount);
      return transform;
    }

    /// <summary>
    /// Flips the transform on the X-axis (up-down) by adding 180° to X rotation.
    /// This method works correctly regardless of current rotation state.
    /// </summary>
    /// <param name="transform">The transform to flip</param>
    /// <returns>The transform for method chaining</returns>
    public static Transform FlipX(this Transform transform) {
      Vector3 currentEuler = transform.eulerAngles;
      currentEuler.x = (currentEuler.x + 180f) % 360f;
      transform.eulerAngles = currentEuler;
      return transform;
    }

    /// <summary>
    /// Flips the transform on the Y-axis (left-right) by adding 180° to Y rotation.
    /// This method works correctly regardless of current rotation state.
    /// </summary>
    /// <param name="transform">The transform to flip</param>
    /// <returns>The transform for method chaining</returns>
    public static Transform FlipY(this Transform transform) {
      Vector3 currentEuler = transform.eulerAngles;
      currentEuler.y = (currentEuler.y + 180f) % 360f;
      transform.eulerAngles = currentEuler;
      return transform;
    }

    /// <summary>
    /// Checks if the transform is flipped on the X-axis (rotation around X is approximately 180°).
    /// </summary>
    /// <param name="transform">The transform to check</param>
    /// <returns>True if flipped on X-axis, false otherwise</returns>
    public static bool IsFlippedX(this Transform transform) {
      float xRotation = transform.eulerAngles.x;
      return Mathf.Approximately((xRotation % 360f + 360f) % 360f, 180f);
    }

    /// <summary>
    /// Checks if the transform is flipped on the Y-axis (rotation around Y is approximately 180°).
    /// </summary>
    /// <param name="transform">The transform to check</param>
    /// <returns>True if flipped on Y-axis, false otherwise</returns>
    public static bool IsFlippedY(this Transform transform) {
      float yRotation = transform.eulerAngles.y;
      return Mathf.Approximately((yRotation % 360f + 360f) % 360f, 180f);
    }

    /// <summary>
    /// Gets the current Z rotation in 90-degree increments (0, 90, 180, 270).
    /// Useful for grid-based games where elements snap to cardinal directions.
    /// </summary>
    /// <param name="transform">The transform to check</param>
    /// <returns>The rotation rounded to the nearest 90-degree increment</returns>
    public static int GetCardinalRotation(this Transform transform) {
      float zRotation = transform.eulerAngles.z;
      return Mathf.RoundToInt(zRotation / 90f) * 90 % 360;
    }

    /// <summary>
    /// Sets the Z rotation to a specific cardinal direction (0, 90, 180, 270).
    /// </summary>
    /// <param name="transform">The transform to modify</param>
    /// <param name="degrees">The rotation in degrees (will be clamped to 0, 90, 180, or 270)</param>
    /// <returns>The transform for method chaining</returns>
    public static Transform SetCardinalRotation(this Transform transform, int degrees) {
      int clampedDegrees = Mathf.RoundToInt(degrees / 90f) * 90 % 360;
      Vector3 currentEuler = transform.eulerAngles;
      currentEuler.z = clampedDegrees;
      transform.eulerAngles = currentEuler;
      return transform;
    }
  }
}