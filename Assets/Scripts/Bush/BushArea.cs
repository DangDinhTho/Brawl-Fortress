using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One connected bush (a group of grass tiles). A character standing inside is hidden from enemies,
/// except enemies standing in the same BushArea. Boxes are world-space (built by Tools/Bush/Build Bush Areas).
/// </summary>
public class BushArea : MonoBehaviour
{
    public static readonly List<BushArea> All = new List<BushArea>();

    [Tooltip("World-space footprint boxes of the tiles in this bush")]
    public List<Bounds> boxes = new List<Bounds>();
    private Bounds total;

    private void OnEnable()
    {
        RecalculateTotal();
        if (!All.Contains(this)) All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    public void RecalculateTotal()
    {
        if (boxes.Count == 0) { total = new Bounds(transform.position, Vector3.zero); return; }
        total = boxes[0];
        for (int i = 1; i < boxes.Count; i++) total.Encapsulate(boxes[i]);
    }

    public bool Contains(Vector3 point)
    {
        if (!total.Contains(point)) return false;
        for (int i = 0; i < boxes.Count; i++)
            if (boxes[i].Contains(point)) return true;
        return false;
    }

    /// <summary>Bush containing the point, or null.</summary>
    public static BushArea Find(Vector3 point)
    {
        for (int i = 0; i < All.Count; i++)
            if (All[i].Contains(point)) return All[i];
        return null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.35f);
        foreach (var b in boxes) Gizmos.DrawCube(b.center, b.size);
    }
#endif
}
