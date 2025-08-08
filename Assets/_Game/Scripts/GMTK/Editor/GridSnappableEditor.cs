using GMTK;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GridSnappable))]
[CanEditMultipleObjects]
public class GridSnappableEditor : Editor {
  private SnappableTemplate template;

  public override void OnInspectorGUI() {
    DrawDefaultInspector();

    EditorGUILayout.Space();
    EditorGUILayout.LabelField("Template Initializer", EditorStyles.boldLabel);

    template = (SnappableTemplate)EditorGUILayout.ObjectField("Template", template, typeof(SnappableTemplate), false);

    if (template != null && GUILayout.Button("Apply Template")) {
      ApplyTemplate((GridSnappable)target, template);
    }
  }

  private void ApplyTemplate(GridSnappable snappable, SnappableTemplate template) {
    var go = snappable.gameObject;

    // Sprite
    ApplySprite(template, go);

    // Collider
    ApplyCollider(template, go);

    // Rigidbody
    ApplyRigidBody(snappable, template, go);

    // Optional: Resize or reposition based on grid size
    go.transform.localScale = new Vector3(template.SizeInGridUnits.x, template.SizeInGridUnits.y, 1);

    // Store reference
    snappable.AppliedTemplate = template;

    EditorUtility.SetDirty(go);
    Debug.Log($"Applied template: {template.name}", go);
  }

  private static void ApplyCollider(SnappableTemplate template, GameObject go) {
    if (!go.TryGetComponent<Collider2D>(out _)) {
      var col = go.AddComponent<PolygonCollider2D>();
      col.sharedMaterial = SnappableMaterialStrategy.GetMaterial(template.Friction, template.Bounciness);
    }
  }

  private static void ApplyRigidBody(GridSnappable snappable, SnappableTemplate template, GameObject go) {
    if (!go.TryGetComponent<Rigidbody2D>(out _)) {
      go.AddComponent<Rigidbody2D>();
    }
    if (template.ForceStaticRigidBody) {
      snappable.StaticBody = true;
      var rb = go.GetComponent<Rigidbody2D>();
      rb.bodyType = RigidbodyType2D.Static;
      rb.mass = 0f;
      rb.angularDamping = 0f;
      rb.gravityScale = 0f;
    }
    else {
      snappable.StaticBody = false;
      var rb = go.GetComponent<Rigidbody2D>();
      rb.bodyType = RigidbodyType2D.Dynamic;
      rb.mass = template.Mass;
      rb.angularDamping = template.AngularDamping;
      rb.gravityScale = template.Gravity ? 1f : 0f;
    }
  }

  private static void ApplySprite(SnappableTemplate template, GameObject go) {
    // Sprite is optional, so we check if it's null
    if (template.Sprite == null) return;

    if (go.TryGetComponent<SpriteRenderer>(out var existingSr)) {

      // If there's already a sprite, ask user if they want to replace it
      if (existingSr.sprite != null && existingSr.sprite != template.Sprite) {
        
        if (!EditorUtility.DisplayDialog("Replace Sprite?", $"The GameObject already has a Sprite assigned ({existingSr.sprite.name}). Do you want to replace it with the template's sprite ({template.Sprite.name})?", "Yes", "No")) {
          Debug.Log("Template application cancelled by user.", go);
          return;
        } // else continue to replace
        else {
          existingSr.sprite = template.Sprite;
        }

      }
    } // If there's no existing SpriteRenderer we create one 
    else {
      var sr = go.AddComponent<SpriteRenderer>();
      sr.sprite = template.Sprite;
      sr.sortingLayerName = "Interactives";
    }
  }
}