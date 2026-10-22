using UnityEngine;

//drawing helpers for the three temporary debug HUDs (acrobat, fortune teller, knight), so the bar and the ult meter r only written once

public static class DebugHud
{
    public const float BarHeight = 18f;
    //a label style 
    public static GUIStyle RichText()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.richText = true;
        return style;
    }

    //a filled bar with text on it
    public static void DrawBar(float x, float y, float width, float fill, Color color, string label)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(x, y, width, BarHeight), Texture2D.whiteTexture);                      
        GUI.color = color;
        GUI.DrawTexture(new Rect(x, y, width * Mathf.Clamp01(fill), BarHeight), Texture2D.whiteTexture); 
        GUI.color = Color.white;
        GUI.Label(new Rect(x + 5f, y, width, BarHeight), label);
    }
    //the ult meter which shows the charge, "READY", or the active ultimate with its timer
    //activeName/activeRemaining = what to show while the ult is running, passing null or equal to 0 when not running
    public static void DrawUltBar(float x, float y, float width, PlayerStats stats, string activeName, float activeRemaining, string readyKey)
    {
        float charge = 0f;
        if (stats != null && stats.Ultimate != null) charge = stats.Ultimate.ChargePercent;

        string label;
        Color color;
        float fill = charge;

        if (activeName != null)
        {
            label = activeName + " " + activeRemaining.ToString("F1") + "s";
            color = Color.magenta;
            fill = 1f;
        }
        else if (stats == null || stats.Ultimate == null)
        {
            label = "No ultimate assigned";
            color = new Color(0.6f, 0.3f, 0.9f);
        }
        else if (charge >= 1f)
        {
            label = "ULT READY (" + readyKey + ")";
            color = Color.yellow;
        }
        else
        {
            label = "Ult " + charge.ToString("P0");
            color = new Color(0.6f, 0.3f, 0.9f);
        }

        DrawBar(x, y, width, fill, color, label);
    }
}
