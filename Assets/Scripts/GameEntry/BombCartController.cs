using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombCartController : MonoBehaviour
{
    public Team team = Team.blue;
    public EntryFieldOfView entryFieldOfView;
    public CoasterfollowerAdv coasterfollowerAdv;
    public Transform target;
    public BombCartNetwork bombCartNetwork;
    [Tooltip("Metres per physics step with 0 / 1 / 2 / 3+ pushers: the slowest the cart ever goes (longer tracks go faster, see Push Share)")]
    public float[] speeds;
    [Tooltip("Share of the match time a whole uncontested push takes with 0 / 1 / 2 / 3+ pushers. The cart's speed is raised to " +
             "match on long tracks, so the blue team can reach the target in time on every map (one fixed speed made the 65 m " +
             "tracks take 77% of a match with one pusher)")]
    public float[] pushShare = { 0f, 0.40f, 0.30f, 0.24f };

    private Team enemyTeam;
    private bool deployed;
    // Start is called before the first frame update
    void Start()
    {
        enemyTeam = ConfigManager.GetEnemyTeam(team);
        coasterfollowerAdv.speedFactor = 0;
        FitSpeedsToTrack();
        // live tuning from PlayFab (Title Data "cart_payload_speed_scale", default 1), on top of the fitted speeds
        float scale = ConfigManager.cartPayloadSpeedScale;
        if (speeds != null && scale > 0f && !Mathf.Approximately(scale, 1f))
            for (int i = 1; i < speeds.Length; i++) speeds[i] *= scale;
    }

    /// <summary>Raises the speeds so that a whole push from the cart to the target, uncontested, takes pushShare of the
    /// match time (Title Data battle_map_times or the defaults, like the match timer), measured along the cart's own
    /// path. Never lowers them: short tracks keep the old pace.</summary>
    void FitSpeedsToTrack()
    {
        var arena = ArenaManager.Instance;
        if (arena == null || target == null || speeds == null || pushShare == null) return;
        int time = ConfigManager.BattleTimeOf(arena.map, arena.gameMode);
        float length = TrackLengthToTarget();
        if (time <= 0 || length <= 0f) return;
        for (int i = 1; i < speeds.Length && i < pushShare.Length; i++)
            if (pushShare[i] > 0f) speeds[i] = Mathf.Max(speeds[i], length / (pushShare[i] * time) * Time.fixedDeltaTime);
    }

    /// <summary>Length (m) of the cart's path from where the cart stands to the target, over the Bezier sample points
    /// the coaster follows.</summary>
    float TrackLengthToTarget()
    {
        var pts = new List<Vector3>();
        if (coasterfollowerAdv == null || coasterfollowerAdv.fragments == null) return 0f;
        foreach (var f in coasterfollowerAdv.fragments)
        {
            var bf = f ? f.GetComponent<BezierFragment>() : null;
            if (bf != null && bf.positions != null) pts.AddRange(bf.positions);
        }
        if (pts.Count < 2) return 0f;
        int Nearest(Vector3 p, int from)
        {
            int best = from; float bd = float.MaxValue;
            for (int i = from; i < pts.Count; i++) { float d = (pts[i] - p).sqrMagnitude; if (d < bd) { bd = d; best = i; } }
            return best;
        }
        int start = Nearest(transform.position, 0), end = Nearest(target.position, start);
        float length = 0f;
        for (int i = start; i < end; i++) length += Vector3.Distance(pts[i], pts[i + 1]);
        return length;
    }

    public void Update()
    {
        if (deployed)
        {
            coasterfollowerAdv.canMove = true;
            coasterfollowerAdv.speedFactor = speeds[3];
            return;
        }

        if(Vector3.Distance(transform.position, target.position) < 0.5f)
        {
            deployed = true;
            ArenaNetworkManager.instan.EndGame(0, 1);
            bombCartNetwork.SyncDeployBomb();
            return;
        }

        int teamDetect = entryFieldOfView.detectTeams[team].Count;
        if (teamDetect == 0 || entryFieldOfView.detectTeams[enemyTeam].Count > 0)
        {
            coasterfollowerAdv.canMove = false;
        }
        else if(teamDetect > 0)
        {
            teamDetect = teamDetect <= 3 ? teamDetect : 3;
            coasterfollowerAdv.canMove = true;
            coasterfollowerAdv.speedFactor = speeds[teamDetect];
        }
    }



    public void InitOnMaster()
    {
        entryFieldOfView.enabled = true;
        coasterfollowerAdv.enabled = true;
        this.enabled = true;
    }

    public void InitOnNonMaster()
    {
        entryFieldOfView.enabled = false;
        coasterfollowerAdv.enabled = false;
        this.enabled = false;
    }


}
