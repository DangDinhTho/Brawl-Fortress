using UnityEngine;

/// <summary>Tints the control-point disc with the colour of the team that owns it (KingOfTheHillManager.teamOwnerColor).</summary>
public class KothPointVisual : MonoBehaviour
{
    public Renderer[] renderers;
    private MaterialPropertyBlock block;
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int ColorDimId = Shader.PropertyToID("_ColorDim");

    private void Start()
    {
        block = new MaterialPropertyBlock();
        var mgr = KingOfTheHillManager.instan;
        if (mgr == null) return;
        mgr.OnTeamOwnerChanged += Apply;
        Apply(mgr.teamOwner);
    }

    private void OnDestroy()
    {
        if (KingOfTheHillManager.instan != null) KingOfTheHillManager.instan.OnTeamOwnerChanged -= Apply;
    }

    private void Apply(Team team)
    {
        var mgr = KingOfTheHillManager.instan;
        int i = (int)team;
        Color c = mgr != null && mgr.teamOwnerColor != null && i >= 0 && i < mgr.teamOwnerColor.Length ? mgr.teamOwnerColor[i] : Color.white;
        foreach (var r in renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(block);
            block.SetColor(ColorId, c);
            block.SetColor(ColorDimId, c * 0.8f);
            r.SetPropertyBlock(block);
        }
    }
}
