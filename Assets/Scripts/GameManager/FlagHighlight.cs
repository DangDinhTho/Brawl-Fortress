using UnityEngine;

// Makes a CTF flag (BriefCase) easy to spot while it lies at home or on the ground:
// the case bobs and spins, a team-coloured light beam and a pulsing ring mark its spot.
// Beam/ring are hidden while someone carries the flag (so a carrier hiding in a bush is not revealed).
// Lives on its own root object (not under the flag) so it never follows the carrier's bones.
public class FlagHighlight : MonoBehaviour
{
    public FlagController flag;
    [Tooltip("Model child of the flag that bobs/spins (the 'briefcase' LOD object).")]
    public Transform visual;
    public Transform beam;
    public Transform ring;
    public Renderer[] glowRenderers;
    public Color glowColor = Color.white;

    public float bobHeight = 0.15f;
    public float bobSpeed = 2.4f;
    public float spinSpeed = 70f;
    public float beamHeight = 4f;
    public float ringPulse = 0.15f;
    public float pulseSpeed = 3f;

    Vector3 visualLocalPos;
    Quaternion visualLocalRot;
    Vector3 ringScale;
    float groundY;
    bool hasState;
    FlagState lastState;
    MaterialPropertyBlock mpb;
    static readonly int TintId = Shader.PropertyToID("_TintColor");

    void Awake()
    {
        if (visual)
        {
            visualLocalPos = visual.localPosition;
            visualLocalRot = visual.localRotation;
        }
        if (ring) ringScale = ring.localScale;
        mpb = new MaterialPropertyBlock();
    }

    void LateUpdate()
    {
        if (!flag) return;
        bool carried = flag.state == FlagState.moving;

        if (!hasState || flag.state != lastState)
        {
            hasState = true;
            lastState = flag.state;
            ResetVisual();
            if (beam) beam.gameObject.SetActive(!carried);
            if (ring) ring.gameObject.SetActive(!carried);
            if (!carried) groundY = FindGroundY(flag.transform.position);
        }
        if (carried) return;

        float t = Time.time;
        Vector3 p = flag.transform.position;

        if (visual)
        {
            ResetVisual();
            visual.RotateAround(p, Vector3.up, (t * spinSpeed) % 360f);
            visual.position += Vector3.up * (bobHeight * (0.5f + 0.5f * Mathf.Sin(t * bobSpeed)));
        }

        Vector3 ground = new Vector3(p.x, groundY, p.z);
        if (ring)
        {
            ring.position = ground + Vector3.up * 0.04f;
            ring.rotation = Quaternion.Euler(90f, 0f, 0f);
            ring.localScale = ringScale * (1f + ringPulse * Mathf.Sin(t * pulseSpeed));
        }
        if (beam)
        {
            beam.position = ground + Vector3.up * (beamHeight * 0.5f);
            Camera cam = Camera.main;
            if (cam)
            {
                Vector3 d = cam.transform.position - beam.position;
                d.y = 0;
                if (d.sqrMagnitude > 0.001f) beam.rotation = Quaternion.LookRotation(-d);
            }
        }

        if (glowRenderers != null && glowRenderers.Length > 0)
        {
            Color c = glowColor;
            c.a *= 0.75f + 0.25f * Mathf.Sin(t * pulseSpeed);
            mpb.SetColor(TintId, c);
            for (int i = 0; i < glowRenderers.Length; i++)
                if (glowRenderers[i]) glowRenderers[i].SetPropertyBlock(mpb);
        }
    }

    void ResetVisual()
    {
        if (!visual) return;
        visual.localPosition = visualLocalPos;
        visual.localRotation = visualLocalRot;
    }

    float FindGroundY(Vector3 p)
    {
        RaycastHit hit;
        if (Physics.Raycast(p + Vector3.up * 1f, Vector3.down, out hit, 6f, flag.groundMask, QueryTriggerInteraction.Ignore))
            return hit.point.y;
        return p.y;
    }
}
