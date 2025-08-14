using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GMTK {
  /// <summary>
  /// Represents the occupants of one cell in a LevelGrid
  /// Uses <c>CellLayeringMode</c> to determine how to order the elements for rendering
  /// </summary>
  public class OccupancyCell {

    private readonly int maxOccupants;
    private readonly CellLayeringOrder mode;
    private readonly LinkedList<GridSnappable> occupants = new();
    public int Count => occupants.Count;

    public OccupancyCell(int maxOccupants=1, CellLayeringOrder mode=CellLayeringOrder.LastToFirst) {
      this.maxOccupants = maxOccupants;
      this.mode = mode;
    }

    public CellLayeringOrder LayerOrder => mode;

    public bool HasReachedMaxOccupancy => occupants.Count >= maxOccupants;
    public bool HasAnyOccupant => occupants.Count > 0;
    public bool IsEmpty => occupants.Count == 0;

    public void Add(GridSnappable snappable) {
      if (HasReachedMaxOccupancy) return;

      switch (mode) {
        case CellLayeringOrder.FirstToLast:
          occupants.AddLast(snappable);
          break;
        case CellLayeringOrder.LastToFirst:
          occupants.AddLast(snappable);
          break;
      }
    }

    public bool Contains(GridSnappable snappable) => occupants.Contains(snappable);
    public void Remove(GridSnappable snappable) => occupants.Remove(snappable);

    public IEnumerable<GridSnappable> GetOccupants() => occupants;

    public GridSnappable PeekTop() => occupants.First?.Value;

    public override string ToString() {
      StringBuilder sb = new();
      sb.Append($"maxOccupants:{maxOccupants} ");
      sb.Append($"LayerOrder:{mode} ");
      sb.Append($"Occupants({Count}): [");
      sb.AppendJoin<GridSnappable>(',', occupants.ToArray());
      sb.AppendLine(" ]");
      return sb.ToString();
    }
  }

}