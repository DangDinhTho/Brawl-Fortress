using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UtilityHelper
{
    public static bool isMobile;

    public static string FormatTime(float sec)
    {
        int minutes = (int)sec / 60;
        int seconds = (int)sec - 60 * minutes;
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

}
