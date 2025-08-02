using UnityEngine;

namespace GMTK {
  public class SnappableFactory : MonoBehaviour {
    public GridManager GridManager;

    public GameObject CreateSnappable(SnappableTemplate template, Vector2Int gridCoord) {
      GameObject go = new("GridSnappable");
      go.transform.position = GridManager.SnapToGrid(GridManager.Origin + new Vector2(gridCoord.x * GridManager.CellSize, gridCoord.y * GridManager.CellSize));

      var sr = go.AddComponent<SpriteRenderer>();
      sr.sprite = template.Sprite;

      var rb = go.AddComponent<Rigidbody2D>();
      var col = go.AddComponent<PolygonCollider2D>();
      col.sharedMaterial = SnappableMaterialStrategy.GetMaterial(template.Friction, template.Bounciness);

      var snappable = go.AddComponent<GridSnappable>();
      GridManager.RegisterElement(snappable);

      return go;
    }
  }

}