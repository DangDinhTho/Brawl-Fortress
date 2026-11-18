using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Engineer blueprint (the ghost that follows the engineer before building / when moving a building).
/// Top-down game: everything lives on one floor plane, so the blueprint always sits on the floor the engineer stands on.
/// It is valid when its centre and its 4 corners (drawnPoints) are on walkable floor (NavMesh) at that height, and
/// nothing solid (walls, props, other buildings, characters) stands inside its footprint. The floor itself is never an
/// obstacle (the old trigger test touched the ground and rejected flat teleporters all the time).
/// </summary>
public class Blueprint : MonoBehaviour
{
    public BuildableType buildableType;
    [Tooltip("Old ground mask (unused since the flat-floor check)")] public LayerMask LayerMask;
    public Transform[] drawnPoints;
    public float distanceRay = 3;
    public float doDoc;
    public Animator animator;
    [HideInInspector]
    public bool reject, touch;
    public int expRequired;

    public AudioClip goOnUpVoice;

    [Header("Flat floor placement")]
    [Tooltip("Solid things that block a building (the floor is not in it)")]
    public LayerMask obstacleMask;
    [Tooltip("Max distance from a footprint point to walkable floor (m)")] public float floorTolerance = 0.45f;
    [Tooltip("Height of the space the building needs (m)")] public float clearance = 1.2f;

    Transform owner;
    static readonly Collider[] hits = new Collider[16];

    void Awake()
    {
        owner = transform.root;
        if (obstacleMask == 0) obstacleMask = LayerMask.GetMask("Default", "Water", "Wall", "Cart", "VitualWall", "Player", "Bot");
    }

    void Update()
    {
        // stay on the engineer's floor (no climbing onto ledges or dropping into holes)
        Vector3 p = transform.position;
        float floorY = owner != null ? owner.position.y : p.y;
        if (Mathf.Abs(p.y - floorY) > 0.001f) transform.position = new Vector3(p.x, floorY, p.z);

        bool ok = OnFloor(floorY) && !Blocked(floorY);
        if (reject == ok)
        {
            reject = !ok;
            if (animator != null) animator.SetBool("Reject", reject);
        }
    }

    bool OnWalkable(Vector3 point, float floorY)
    {
        point.y = floorY;
        if (!NavMesh.SamplePosition(point, out var hit, floorTolerance + 0.5f, NavMesh.AllAreas)) return false;
        Vector3 d = hit.position - point;
        return Mathf.Abs(d.y) < 0.35f && new Vector2(d.x, d.z).magnitude < floorTolerance;
    }

    bool OnFloor(float floorY)
    {
        if (!OnWalkable(transform.position, floorY)) return false;
        if (drawnPoints != null)
            foreach (var c in drawnPoints)
                if (c != null && !OnWalkable(c.position, floorY)) return false;
        return true;
    }

    /// <summary>Box over the footprint (from the corner points), from just above the floor up to 'clearance'.</summary>
    bool Blocked(float floorY)
    {
        Vector2 min = new Vector2(-0.5f, -0.5f), max = new Vector2(0.5f, 0.5f);
        if (drawnPoints != null && drawnPoints.Length > 0)
        {
            min = new Vector2(float.MaxValue, float.MaxValue); max = new Vector2(float.MinValue, float.MinValue);
            foreach (var c in drawnPoints)
            {
                if (c == null) continue;
                Vector3 l = transform.InverseTransformPoint(c.position);
                min = Vector2.Min(min, new Vector2(l.x, l.z)); max = Vector2.Max(max, new Vector2(l.x, l.z));
            }
        }
        var ls = transform.lossyScale;
        Vector3 half = new Vector3(Mathf.Max(0.1f, (max.x - min.x) * 0.5f * Mathf.Abs(ls.x) - 0.05f), clearance * 0.5f, Mathf.Max(0.1f, (max.y - min.y) * 0.5f * Mathf.Abs(ls.z) - 0.05f));
        Vector3 centerLocal = new Vector3((min.x + max.x) * 0.5f, 0f, (min.y + max.y) * 0.5f);
        Vector3 center = transform.TransformPoint(centerLocal);
        center.y = floorY + 0.03f + half.y; // from just above the floor: flat teleporter pads block too
        Quaternion rot = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        int n = Physics.OverlapBoxNonAlloc(center, half, hits, rot, obstacleMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            var col = hits[i];
            if (col == null) continue;
            if (owner != null && col.transform.IsChildOf(owner)) continue;   // the engineer himself
            if (col.transform.IsChildOf(transform)) continue;               // the blueprint
            return true;
        }
        return false;
    }
}
