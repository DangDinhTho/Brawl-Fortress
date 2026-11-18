using UnityEditor;

public static class SafeAreaSimMenu
{
    const string Menu = "Tools/UI/Simulate Notch (Editor)";

    [MenuItem(Menu)]
    static void Toggle()
    {
        bool on = !EditorPrefs.GetBool(SafeAreaInset.SimulatePref, false);
        EditorPrefs.SetBool(SafeAreaInset.SimulatePref, on);
        SafeAreaInset.SimulateNotch = on;
    }

    [MenuItem(Menu, true)]
    static bool Check()
    {
        UnityEditor.Menu.SetChecked(Menu, EditorPrefs.GetBool(SafeAreaInset.SimulatePref, false));
        return true;
    }
}
