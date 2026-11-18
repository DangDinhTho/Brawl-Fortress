/// <summary>
/// Switches the promo-video tools (Tools > Promo, editor only) flip while recording gameplay.
/// Both stay false in builds: nothing sets them outside the editor.
///  - Active: the game behaves as if ads were removed (no interstitial, banner or MREC on the footage).
///  - BotsPassive: bots and sentries pick no targets (FieldOfView), so a shot can be staged without anyone dying.
/// </summary>
public static class PromoHooks
{
    public static bool Active;
    public static bool BotsPassive;
}
