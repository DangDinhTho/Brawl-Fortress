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

    [Tooltip("Grass / bush renderers of this bush (baked). Faded around allies by BushGrassFade.")]
    public List<Renderer> renderers = new List<Renderer>();
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
            if (!IsGone(i) && boxes[i].Contains(point)) return true;
        return false;
    }

    // ------------------------------------------------------------------ destructible clumps (Nov 2026, BushDamage)
    // Each box is one grass clump. Fire / explosions wear its health down; at 0 it is torn apart (leaves fly), its
    // renderer is hidden, a short stubble of the same grass stays and the box no longer hides anyone.
    [System.NonSerialized] float[] hp;
    [System.NonSerialized] bool[] gone;
    [System.NonSerialized] Renderer[] clumpRenderer;
    [System.NonSerialized] int goneCount;

    public bool IsGone(int i) => gone != null && i >= 0 && i < gone.Length && gone[i];
    public bool AllGone => gone != null && goneCount >= boxes.Count;

    /// <summary>Ground point at the middle of clump i (the boxes start 0.5 m below the ground).</summary>
    public Vector3 ClumpCenter(int i) { var b = boxes[i]; return new Vector3(b.center.x, b.min.y + 0.5f, b.center.z); }

    void EnsureClumps()
    {
        if (hp != null && hp.Length == boxes.Count) return;
        hp = new float[boxes.Count]; gone = new bool[boxes.Count]; clumpRenderer = new Renderer[boxes.Count]; goneCount = 0;
        for (int i = 0; i < boxes.Count; i++)
        {
            hp[i] = BushDamage.ClumpHp;
            // the renderer of each box (same order when baked; matched by position to be safe)
            float best = float.MaxValue; Renderer pick = null;
            for (int k = 0; k < renderers.Count; k++)
            {
                var r = renderers[k]; if (r == null) continue;
                Vector3 d = r.bounds.center - boxes[i].center; d.y = 0f;
                if (d.sqrMagnitude < best) { best = d.sqrMagnitude; pick = r; }
            }
            clumpRenderer[i] = pick;
        }
    }

    public Renderer ClumpRenderer(int i) { EnsureClumps(); return clumpRenderer[i]; }

    /// <summary>Takes damage on clump i; true when that brought it down (it is not torn apart yet: see Shatter).</summary>
    public bool Damage(int i, float damage)
    {
        EnsureClumps();
        if (gone[i]) return false;
        hp[i] -= damage;
        return hp[i] <= 0f;
    }

    /// <summary>Tears clump i apart: leaves burst out, the clump is hidden, a short stubble of it stays.</summary>
    public void Shatter(int i)
    {
        EnsureClumps();
        if (i < 0 || i >= gone.Length || gone[i]) return;
        gone[i] = true; goneCount++; hp[i] = 0f;
        var r = clumpRenderer[i];
        Color leaf = BushDamage.LeafColor(r);
        BrawlFx.Leaves(ClumpCenter(i) + Vector3.up * 0.7f, leaf, 16, 1f);
        if (r == null) return;
        // another clump of this bush may share the renderer: hide it only when no clump that uses it is left
        bool shared = false;
        for (int k = 0; k < clumpRenderer.Length; k++) if (k != i && !gone[k] && clumpRenderer[k] == r) shared = true;
        if (shared) return;
        BushGrassFade.Forget(r);
        BushStubble.Create(r, transform);   // a few stalks cut off low (not the clump squashed: that read as a cabbage)
        r.enabled = false;
    }

    /// <summary>Contains test with every box grown by 'margin' on X/Z (used to leave a bush a bit later than entering).</summary>
    public bool Contains(Vector3 point, float margin)
    {
        if (margin <= 0f) return Contains(point);
        var grow = new Vector3(margin * 2f, 0f, margin * 2f);
        var t = total; t.Expand(grow);
        if (!t.Contains(point)) return false;
        for (int i = 0; i < boxes.Count; i++)
        {
            if (IsGone(i)) continue;
            var b = boxes[i]; b.Expand(grow);
            if (b.Contains(point)) return true;
        }
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
