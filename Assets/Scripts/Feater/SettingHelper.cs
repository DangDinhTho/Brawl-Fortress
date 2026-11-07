using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SettingHelper
{
    private static string GraphicKey = "GRAPHIC";
    private static string SoundKey = "SOUND";
    private static string LenguageKey = "LENGUAGE";
    private static string SensivityKey = "Sensivity";
    private static string FpsKey = "FPSTARGET";
    public static Action<float> OnSensitivityUpdate;
    public static Action<int> OnGraphicSettingUpdate;
    public static Action<int> OnFPSUpdate;

public static int GetGraphicValue()
    {
        if (PlayerPrefs.HasKey(GraphicKey))
        {
            return PlayerPrefs.GetInt(GraphicKey);
        }
        else
        {
            return 2;
        }
    }

    public static void SetFpsTarget(int value)
    {
        PlayerPrefs.SetInt(FpsKey, value);
        OnFPSUpdate?.Invoke(value);
    }
    
    public static int GetFPSValue()
    {
        if (PlayerPrefs.HasKey(FpsKey))
        {
            return PlayerPrefs.GetInt(FpsKey);
        }
        else
        {
            return 60;
        }
    }

    public static void SetGraphic(int value)
    {
        PlayerPrefs.SetInt(GraphicKey, value);
        OnGraphicSettingUpdate?.Invoke(value);
    }
    
    
    public static float GetSoundValue()
    {
        if (PlayerPrefs.HasKey(SoundKey))
        {
            return PlayerPrefs.GetFloat(SoundKey);
        }
        else
        {
            return 100;
        }
    }

    public static void SetSound(float value)
    {
        PlayerPrefs.SetFloat(SoundKey, value);
    }

    public static void SetSensivity(float value)
    {
        OnSensitivityUpdate?.Invoke(value);
        PlayerPrefs.SetFloat(SensivityKey, value);
    }

    public static float GetSensivityValue()
    {
        if (PlayerPrefs.HasKey(SensivityKey))
        {
            return PlayerPrefs.GetFloat(SensivityKey);
        }
        else
        {
            return 200;
        }
    }

  


    public static string GetLanguage()
    {
        if (PlayerPrefs.HasKey(LenguageKey))
        {
            return PlayerPrefs.GetString(LenguageKey);
        }
        else
        {
            string resLanguage = "English";
            //switch (Application.systemLanguage)
            //{
            //    case SystemLanguage.Russian:
            //        resLanguage = "Russian";
            //        break;
            //    case SystemLanguage.Vietnamese:
            //        resLanguage = "Vietnamese";
            //        break;
            //    case SystemLanguage.Thai:
            //        resLanguage = "Thai";
            //        break;
            //    case SystemLanguage.Spanish:
            //        resLanguage = "Spanish";
            //        break;
            //    default:
            //        resLanguage = "English";
            //        break;
            //}

            return resLanguage;
        }
    }
    
    public static void SetLanguage(string language)
    {
        PlayerPrefs.SetString(LenguageKey, language);
    }
    
    

}
