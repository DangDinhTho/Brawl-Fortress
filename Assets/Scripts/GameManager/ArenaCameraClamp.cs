using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

/// <summary>
/// Brawl-Stars-style match camera (Nov 2026): the camera follows its target (CameraController.freeLook, a
/// CinemachineVirtualCamera with a FramingTransposer) but stops at the arena's edges. After the Body stage it looks at
/// the view's footprint on the ground (near and far edge of the screen's centre line, the width at the screen centre)
/// and shifts the camera so that footprint stays inside the arena plus a margin: walking to the side or to an end, the
/// character moves towards the edge of the screen instead of dragging the camera off the map. When the view is wider
/// (or deeper) than the arena it is centred on it.
/// Added to the virtual camera by CameraController.InitCam; the arena comes from <see cref="ArenaBounds"/>.
/// </summary>
[AddComponentMenu("")]
public class ArenaCameraClamp : CinemachineExtension
{
    [Tooltip("How far past the arena's end at the bottom of the screen (the own base) the view may show (m)")]
    public float endMargin = 3f;
    [Tooltip("How far past the arena's end at the top of the screen (the enemy base) the view may show (m). Larger than " +
             "endMargin: the view reaches much farther ahead of the character than behind it, so with 2 m a character at " +
             "the enemy base ended up under the top HUD")]
    public float topMargin = 8f;
    [Tooltip("How far past the arena's sides the view may show at the screen's centre (m)")]
    public float sideMargin = 5f;

    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Body || !ArenaBounds.TryGet(out Rect arena)) return;
        Vector3 cam = state.RawPosition;
        Quaternion rot = state.RawOrientation;
        Vector3 fwd = rot * Vector3.forward;
        if (fwd.y > -0.05f || cam.y <= 0.1f) return;                     // not looking down at the ground
        Vector3 flatF = new Vector3(fwd.x, 0f, fwd.z);
        if (flatF.sqrMagnitude < 1e-6f) flatF = rot * Vector3.up;        // straight down: screen up is "forward"
        flatF.y = 0f; flatF.Normalize();
        Vector3 flatR = new Vector3(flatF.z, 0f, -flatF.x);

        float halfV = state.Lens.FieldOfView * 0.5f * Mathf.Deg2Rad;
        float aspect = state.Lens.Aspect > 0.01f ? state.Lens.Aspect : 16f / 9f;
        Vector3 right = rot * Vector3.right;
        Vector3 dNear = Quaternion.AngleAxis(state.Lens.FieldOfView * 0.5f, right) * fwd;   // bottom edge of the screen
        Vector3 dFar = Quaternion.AngleAxis(-state.Lens.FieldOfView * 0.5f, right) * fwd;   // top edge

        float tC = -cam.y / fwd.y;
        Vector3 centre = cam + fwd * tC;
        float halfW = tC * Mathf.Tan(halfV) * aspect;
        float nearS = dNear.y < -1e-4f ? Vector3.Dot(cam + dNear * (-cam.y / dNear.y), flatF) : Vector3.Dot(centre, flatF);
        bool farOnGround = dFar.y < -0.02f;
        float farS = farOnGround ? Vector3.Dot(cam + dFar * (-cam.y / dFar.y), flatF) : float.MaxValue;
        float centreR = Vector3.Dot(centre, flatR);

        // the arena's extent along the view's forward and right axes
        float sMin = float.MaxValue, sMax = float.MinValue, rMin = float.MaxValue, rMax = float.MinValue;
        for (int i = 0; i < 4; i++)
        {
            Vector3 c = new Vector3(i % 2 == 0 ? arena.xMin : arena.xMax, 0f, i < 2 ? arena.yMin : arena.yMax);
            float s = Vector3.Dot(c, flatF), r = Vector3.Dot(c, flatR);
            sMin = Mathf.Min(sMin, s); sMax = Mathf.Max(sMax, s); rMin = Mathf.Min(rMin, r); rMax = Mathf.Max(rMax, r);
        }
        sMin -= endMargin; sMax += topMargin; rMin -= sideMargin; rMax += sideMargin;

        float shiftS = 0f;
        if (farOnGround && farS - nearS >= sMax - sMin) shiftS = (sMin + sMax) * 0.5f - (nearS + farS) * 0.5f;
        else if (nearS < sMin) shiftS = sMin - nearS;
        else if (farOnGround && farS > sMax) shiftS = sMax - farS;

        float shiftR = 0f;
        if (2f * halfW >= rMax - rMin) shiftR = (rMin + rMax) * 0.5f - centreR;
        else if (centreR - halfW < rMin) shiftR = rMin - (centreR - halfW);
        else if (centreR + halfW > rMax) shiftR = rMax - (centreR + halfW);

        if (shiftS != 0f || shiftR != 0f) state.RawPosition = cam + flatF * shiftS + flatR * shiftR;
    }
}

/// <summary>
/// The playable rectangle of the loaded map (x, z), measured once per match. Ends: the inner faces of the end
/// VitualWalls (the invisible walls behind the spawns) when the map has them. Otherwise, and for the sides: from the
/// middle of the spawn points, rays at 1 m height along each direction, one every metre across the arena; the wall is
/// the farthest first hit (up to MaxReach): obstacles stop most rays early, the border is where the rays that get
/// through a lane end. Never narrower than the spawn points plus a margin. Works on generated maps (border cliffs, end
/// VitualWalls) and on the hand-made ones (off-centre, some without end walls).
/// </summary>
public static class ArenaBounds
{
    static Rect rect;
    static bool ready;
    static int owner;   // ArenaManager instance the bounds belong to (a new match measures again)
    static readonly List<float> dists = new List<float>();

    public static void Reset() { ready = false; }

    public static bool TryGet(out Rect r)
    {
        r = rect;
        var arena = ArenaManager.Instance;
        if (arena == null) return false;
        if (ready && owner == arena.GetInstanceID()) { r = rect; return true; }
        var tp = GameObject.Find("TeamPos");
        if (tp == null) return false;   // the map is not loaded yet
        Vector3 c = Vector3.zero; int n = 0;
        float sxMin = float.MaxValue, sxMax = float.MinValue, szMin = float.MaxValue, szMax = float.MinValue;
        foreach (var t in tp.GetComponentsInChildren<Transform>())
        {
            if (t.childCount != 0) continue;
            Vector3 p = t.position; c += p; n++;
            sxMin = Mathf.Min(sxMin, p.x); sxMax = Mathf.Max(sxMax, p.x); szMin = Mathf.Min(szMin, p.z); szMax = Mathf.Max(szMax, p.z);
        }
        if (n == 0) return false;
        c /= n;
        int mask = ~LayerMask.GetMask("Player", "Bot", "Healthable", "CheckPickup", "Ignore Raycast", "UI", "TransparentFX", "Water");
        float zPos = Scan(c, Vector3.forward, Vector3.right, -14f, 14f, mask, 22f);
        float zNeg = Scan(c, Vector3.back, Vector3.right, -14f, 14f, mask, 22f);
        EndWalls(c, ref zNeg, ref zPos);
        zPos = Mathf.Max(zPos, szMax - c.z + 2.5f); zNeg = Mathf.Max(zNeg, c.z - szMin + 2.5f);
        float xPos = Scan(c, Vector3.right, Vector3.forward, -zNeg + 1f, zPos - 1f, mask, 16f);
        float xNeg = Scan(c, Vector3.left, Vector3.forward, -zNeg + 1f, zPos - 1f, mask, 16f);
        xPos = Mathf.Max(xPos, sxMax - c.x + 4f); xNeg = Mathf.Max(xNeg, c.x - sxMin + 4f);
        rect = Rect.MinMaxRect(c.x - xNeg, c.z - zNeg, c.x + xPos, c.z + zPos);
        ready = true; owner = arena.GetInstanceID();
        r = rect;
        return true;
    }

    /// <summary>Farther than this a first hit is something beyond the border seen through a gap: ignored.</summary>
    const float MaxReach = 32f;

    static float Scan(Vector3 c, Vector3 dir, Vector3 across, float from, float to, int mask, float fallback)
    {
        dists.Clear();
        for (float a = from; a <= to; a += 1f)
        {
            Vector3 o = new Vector3(c.x, 1f, c.z) + across * a;
            if (Physics.Raycast(o, dir, out var hit, MaxReach, mask, QueryTriggerInteraction.Ignore)) dists.Add(hit.distance);
        }
        if (dists.Count == 0) return fallback;
        float best = 0f;
        for (int i = 0; i < dists.Count; i++) best = Mathf.Max(best, dists[i]);
        return best;
    }

    /// <summary>The end VitualWalls (long along x, in front of / behind the spawns): their inner faces are the ends.</summary>
    static void EndWalls(Vector3 c, ref float zNeg, ref float zPos)
    {
        var vw = GameObject.Find("VitualWall");
        if (vw == null) return;
        float pos = float.MaxValue, neg = float.MaxValue;
        foreach (var col in vw.GetComponentsInChildren<Collider>(true))
        {
            Bounds b = col.bounds;
            if (b.size.x < b.size.z * 2f || b.min.x > c.x || b.max.x < c.x) continue;   // only walls across the arena
            if (b.center.z > c.z) pos = Mathf.Min(pos, b.min.z - c.z);
            else neg = Mathf.Min(neg, c.z - b.max.z);
        }
        if (pos < float.MaxValue && pos > 3f) zPos = pos;
        if (neg < float.MaxValue && neg > 3f) zNeg = neg;
    }
}
