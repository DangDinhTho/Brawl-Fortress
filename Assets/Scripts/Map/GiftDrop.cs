using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// Xmas maps: a Christmas gift at a fixed spot, bobbing over a ring on the ground. The first player or bot to touch it
/// opens it: a hurt character (below hurtBelow of its max health) is healed healFraction of its max health, anyone else
/// gets a speed boost for speedSeconds (SpeedBuff). Then the ring fills up again and the gift is back respawn seconds
/// later. Spots are mirrored (and one in the middle), fair for both teams.
/// The master client decides who took it (two players touching it at once on two phones must not both get it) and
/// sends MapMechanics.GiftEvent {id, view id, kind, clock time it is back} to everyone; the character's owner applies
/// the heal (HealthBase.Buff syncs it), every client shows the boost's speed lines. Only while the match is running.
/// Placed by the map builder (BrawlMapBuilder.PlaceGifts) with a GiftBox model as its 'box' child.
/// </summary>
public class GiftDrop : MonoBehaviour
{
    public int id;
    public Transform box;
    public float respawn = 25f;
    public float pickRadius = 0.95f;
    [Range(0f, 1f)] public float healFraction = 0.4f;
    [Range(0f, 1f)] public float hurtBelow = 0.75f;
    public float speedSeconds = 6f;
    public Color ringColor = new Color(1f, 0.86f, 0.32f, 1f);

    static readonly List<GiftDrop> all = new List<GiftDrop>();
    static readonly int FillId = Shader.PropertyToID("_Fill"), AlphaId = Shader.PropertyToID("_Alpha"), PulseId = Shader.PropertyToID("_Pulse");

    double readyAt;     // clock time the gift is back (0: there from the start)
    float nextScan, boxY, nextSparkle;
    MeshRenderer ring;
    MaterialPropertyBlock mpb;
    bool wasThere = true;

    void Awake()
    {
        mpb = new MaterialPropertyBlock();
        if (box) boxY = box.localPosition.y;
        ring = MechMesh.Child(transform, "GiftRing", MechMesh.Ring(0.78f, 0.98f, ringColor), MechMesh.Decal, 0.03f);
        MechMesh.Child(transform, "GiftGlow", MechMesh.SoftDisc(0.85f, new Color(1f, 0.9f, 0.55f, 0.35f)), MechMesh.Decal, 0.025f);
    }

    void OnEnable() { all.Add(this); MapMechanics.On(MapMechanics.GiftEvent, OnGift); }
    void OnDisable() { all.Remove(this); }

    void Update()
    {
        double now = MapMechanics.Clock;
        bool there = now >= readyAt;
        if (box)
        {
            if (box.gameObject.activeSelf != there) box.gameObject.SetActive(there);
            if (there)
            {
                float t = Time.time + id * 1.3f;
                box.localPosition = new Vector3(0f, boxY + 0.25f + 0.12f * Mathf.Sin(t * 2.4f), 0f);
                box.localRotation = Quaternion.Euler(0f, t * 55f, 0f);
                if (!wasThere) box.localScale = Vector3.one * 0.2f;
                box.localScale = Vector3.Lerp(box.localScale, Vector3.one, Time.deltaTime * 8f);
            }
        }
        wasThere = there;
        // gold sparkles around a gift that is there: it must not read as one of the decorative parcels of the map
        if (there && box && Time.time >= nextSparkle)
        {
            nextSparkle = Time.time + 0.35f;
            BrawlFx.Burst(box.position + Random.insideUnitSphere * 0.35f, new Color(1f, 0.92f, 0.45f, 1f), new Color(1f, 1f, 0.85f, 1f), 1, 1.2f);
        }
        if (ring)
        {
            ring.GetPropertyBlock(mpb);
            mpb.SetFloat(FillId, there ? 1f : Mathf.Clamp01(1f - (float)((readyAt - now) / respawn)));
            mpb.SetFloat(AlphaId, there ? 1f : 0.7f);
            mpb.SetFloat(PulseId, there ? 4f : 0f);
            ring.SetPropertyBlock(mpb);
        }

        if (!there || !MapMechanics.Authority || !MapMechanics.Running || Time.time < nextScan) return;
        nextScan = Time.time + 0.1f;
        TF2Player best = null; float bestD = pickRadius * pickRadius;
        Vector3 c = transform.position;
        foreach (var p in MapMechanics.Characters())
        {
            if (!MapMechanics.Alive(p) || p.photonView == null) continue;
            Vector3 d = p.transform.position - c; float up = d.y; d.y = 0f;
            if (up < -1f || up > 1.8f || d.sqrMagnitude > bestD) continue;
            bestD = d.sqrMagnitude; best = p;
        }
        if (best == null) return;
        var hm = best.healthManager;
        int kind = hm.currentHealth < hm.maxHealth * hurtBelow ? 0 : 1;   // 0 heal, 1 speed
        MapMechanics.Send(MapMechanics.GiftEvent, new object[] { id, best.photonView.ViewID, kind, now + respawn });
    }

    static void OnGift(object[] d)
    {
        if (d == null || d.Length < 4) return;
        int gid = (int)d[0], view = (int)d[1], kind = (int)d[2];
        double back = (double)d[3];
        GiftDrop g = null;
        for (int i = 0; i < all.Count; i++) if (all[i].id == gid) { g = all[i]; break; }
        if (g == null) return;
        g.readyAt = back;
        Vector3 at = g.box ? g.box.position : g.transform.position + Vector3.up * 0.6f;
        BrawlFx.Burst(at, new Color(1f, 0.86f, 0.3f, 1f), new Color(0.95f, 0.25f, 0.22f, 1f), 16, 5f);
        var pv = PhotonView.Find(view);
        var p = pv != null ? pv.GetComponent<TF2Player>() : null;
        if (p == null) return;
        if (kind == 1) SpeedBuff.Give(p, g.speedSeconds);
        else BrawlFx.Heal(p.transform.position);
        if (kind == 0 && pv.IsMine && MapMechanics.Alive(p)) p.healthManager.Buff(g.healFraction);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, pickRadius);
    }
#endif
}
