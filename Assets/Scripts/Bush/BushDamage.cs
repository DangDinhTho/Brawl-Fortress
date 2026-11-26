using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

/// <summary>
/// Brawl-Stars-style destructible bushes (Nov 2026). Explosions (grenades, sticky bombs, rockets, sentry rockets: every
/// ExplosionDamage, and rockets where they blow up) and fire (the Pyro's flame) wear the grass clumps they reach down
/// (BushArea.Damage, ClumpHp each). A clump at 0 is torn apart (BushArea.Shatter: leaves burst, a few cut stalks stay: BushStubble) and
/// no longer hides anyone (BushArea.Contains skips it: BushStealth, bots' FieldOfView and the grass fade follow).
/// Any hit on a bush (a blast too weak to tear it, a bullet flying through, someone hiding in it being shot) shakes a few
/// leaves out (BrawlFx.Leaves, local only).
/// Sync: the projectiles fly on every client, but only the client that owns the shot (DamageObject.isOwner: the
/// shooter, the master client for bots) counts the damage; it tears the clumps down at once and sends which ones to the
/// others (Photon event BushEventCode: [bush index, clump index, ...], bush index = sibling index under the map's
/// "Bushes" root). A player joining later does not get the bushes already torn down.
/// </summary>
public static class BushDamage
{
    public const byte BushEventCode = 41;
    /// <summary>Health of one grass clump (~4 clumps per 1.5 m tile).</summary>
    public static float ClumpHp = 80f;
    /// <summary>Share of the flame's damage dealt to clumps in it every FireTick seconds.</summary>
    public static float FireFactor = 0.6f, FireTick = 0.2f;
    /// <summary>Most a blast reaches into the grass (m): some ExplosionDamage prefabs keep the default radius 20.</summary>
    public static float MaxBlastRadius = 4f;
    /// <summary>A clump is hit when the blast reaches its middle within this (m): half a clump.</summary>
    const float ClumpReach = 0.45f;

    static readonly List<int> torn = new List<int>();

    /// <summary>A blast at pos. Every client shakes leaves out of the clumps it reaches; the owner of the shot also
    /// deals the damage and tears down (and broadcasts) the clumps it brings to 0.</summary>
    public static void Explosion(Vector3 pos, float radius, float damage, bool owner)
    {
        radius = Mathf.Min(radius, MaxBlastRadius);
        if (radius <= 0f) return;
        torn.Clear();
        var all = BushArea.All;
        for (int b = 0; b < all.Count; b++)
        {
            var bush = all[b];
            if (bush == null || !InReach(bush, pos, radius + ClumpReach + 1f)) continue;
            bool shaken = false;
            for (int i = 0; i < bush.boxes.Count; i++)
            {
                if (bush.IsGone(i)) continue;
                Vector3 c = bush.ClumpCenter(i);
                float dx = c.x - pos.x, dz = c.z - pos.z;
                if (dx * dx + dz * dz > (radius + ClumpReach) * (radius + ClumpReach) || Mathf.Abs(c.y - pos.y) > radius + 2f) continue;
                if (owner && bush.Damage(i, damage)) { torn.Add(Index(bush)); torn.Add(i); }
                else if (!shaken) { shaken = true; BrawlFx.Leaves(c + Vector3.up * 0.8f, LeafColor(bush.ClumpRenderer(i)), 6, 0.6f); }
            }
        }
        if (owner) Commit();
    }

    /// <summary>Fire over the box 'area' (the flame's trigger) for one FireTick: owner only.</summary>
    public static void Fire(Bounds area, float damage)
    {
        torn.Clear();
        var all = BushArea.All;
        for (int b = 0; b < all.Count; b++)
        {
            var bush = all[b];
            if (bush == null) continue;
            for (int i = 0; i < bush.boxes.Count; i++)
            {
                if (bush.IsGone(i) || !area.Intersects(bush.boxes[i])) continue;
                if (bush.Damage(i, damage * FireFactor)) { torn.Add(Index(bush)); torn.Add(i); }
                else if (Random.value < 0.25f) BrawlFx.Leaves(bush.ClumpCenter(i) + Vector3.up * 0.9f, LeafColor(bush.ClumpRenderer(i)), 2, 0.4f);
            }
        }
        Commit();
    }

    /// <summary>A few leaves where something hits the grass at p (cosmetic, local).</summary>
    public static void Rustle(Vector3 p, int count = 4)
    {
        var bush = BushArea.Find(p);
        if (bush == null) return;
        int best = -1; float bd = float.MaxValue;
        for (int i = 0; i < bush.boxes.Count; i++)
        {
            if (bush.IsGone(i)) continue;
            Vector3 c = bush.ClumpCenter(i); float d = (c.x - p.x) * (c.x - p.x) + (c.z - p.z) * (c.z - p.z);
            if (d < bd) { bd = d; best = i; }
        }
        if (best >= 0) BrawlFx.Leaves(new Vector3(p.x, Mathf.Max(p.y, bush.ClumpCenter(best).y + 0.6f), p.z), LeafColor(bush.ClumpRenderer(best)), count, 0.5f);
    }

    static bool InReach(BushArea bush, Vector3 p, float r)
    {
        var bs = bush.boxes;
        if (bs.Count == 0) return false;
        Bounds t = bs[0]; for (int i = 1; i < bs.Count; i++) t.Encapsulate(bs[i]);
        t.Expand(new Vector3(r * 2f, 0f, r * 2f));
        return t.Contains(new Vector3(p.x, t.center.y, p.z));
    }

    // ------------------------------------------------------------------ network
    static int Index(BushArea bush) => bush.transform.GetSiblingIndex();

    static BushArea ByIndex(int index)
    {
        var all = BushArea.All;
        for (int b = 0; b < all.Count; b++)
            if (all[b] != null && all[b].transform.GetSiblingIndex() == index) return all[b];
        return null;
    }

    static void Commit()
    {
        if (torn.Count == 0) return;
        for (int k = 0; k + 1 < torn.Count; k += 2) { var bush = ByIndex(torn[k]); if (bush != null) bush.Shatter(torn[k + 1]); }
        if (PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode && PhotonNetwork.InRoom)
            PhotonNetwork.RaiseEvent(BushEventCode, torn.ToArray(), new RaiseEventOptions { Receivers = ReceiverGroup.Others }, SendOptions.SendReliable);
        torn.Clear();
    }

    class Listener : IOnEventCallback
    {
        public void OnEvent(EventData e)
        {
            if (e.Code != BushEventCode || !(e.CustomData is int[] data)) return;
            for (int k = 0; k + 1 < data.Length; k += 2) { var bush = ByIndex(data[k]); if (bush != null) bush.Shatter(data[k + 1]); }
        }
    }

    static Listener listener;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Register()
    {
        if (listener != null) return;
        listener = new Listener();
        PhotonNetwork.AddCallbackTarget(listener);
    }

    // ------------------------------------------------------------------ leaf colour
    static readonly Dictionary<long, Color> leafCache = new Dictionary<long, Color>();
    public static Color DefaultLeaf = new Color(0.45f, 0.78f, 0.3f);

    /// <summary>Colour of a clump's leaves: its mesh's baked leaf colour (BushLeafColors, Tools/Bush/Bake Leaf Colours)
    /// times the material's tint (night / desert materials); DefaultLeaf times the tint for an unknown mesh.</summary>
    public static Color LeafColor(Renderer r)
    {
        if (r == null) return DefaultLeaf;
        var mf = r.GetComponent<MeshFilter>();
        var mat = r.sharedMaterial;
        if (mf == null || mf.sharedMesh == null || mat == null) return DefaultLeaf;
        string name = BushLeafColors.GrassName(r);   // by the clump's name: a statically batched clump's mesh is the scene's combined mesh
        long key = ((long)name.GetHashCode() << 32) ^ (uint)mat.GetInstanceID();
        if (leafCache.TryGetValue(key, out var c)) return c;
        if (!BushLeafColors.TryGet(name, out c) && (r.isPartOfStaticBatch || !BushLeafColors.TryGet(mf.sharedMesh.name, out c))) c = DefaultLeaf;
        Color tint = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
        c = new Color(Mathf.Clamp01(c.r * tint.r), Mathf.Clamp01(c.g * tint.g), Mathf.Clamp01(c.b * tint.b), 1f);
        leafCache[key] = c;
        return c;
    }
}
