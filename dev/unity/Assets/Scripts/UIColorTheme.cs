// Assets/Scripts/UIColorTheme.cs
using UnityEngine;

/// <summary>
/// Centralized color palette matching the Smart Indoor Navigation design.
/// Reference this from any UI script instead of hardcoding colors.
/// </summary>
public static class UIColorTheme
{
    // ── Brand Blues ───────────────────────────────────────────
    public static readonly Color BluePrimary  = HexToColor("#1565C0");
    public static readonly Color BlueLight    = HexToColor("#1976D2");
    public static readonly Color BlueBright   = HexToColor("#2196F3");
    public static readonly Color BlueSoft     = HexToColor("#BBDEFB");

    // ── AR Glow ───────────────────────────────────────────────
    public static readonly Color CyanGlow     = HexToColor("#00E5FF");

    // ── Destination Icon Backgrounds ─────────────────────────
    public static readonly Color IconBgLab    = HexToColor("#DBEAFE"); // light blue
    public static readonly Color IconBgRoom   = HexToColor("#FFF3CD"); // light yellow
    public static readonly Color IconBgCaf    = HexToColor("#D4EDDA"); // light green
    public static readonly Color IconBgExit   = HexToColor("#D1ECF1"); // light teal

    // ── UI Neutrals ───────────────────────────────────────────
    public static readonly Color White        = Color.white;
    public static readonly Color Gray100      = HexToColor("#F4F6F9");
    public static readonly Color Gray300      = HexToColor("#E0E0E0");
    public static readonly Color Gray500      = HexToColor("#9E9E9E");
    public static readonly Color TextPrimary  = HexToColor("#212121");
    public static readonly Color TextMuted    = HexToColor("#757575");

    // ── Semantic ─────────────────────────────────────────────
    public static readonly Color Red          = HexToColor("#F44336");
    public static readonly Color Green        = HexToColor("#2E7D32");

    // ── Helper ───────────────────────────────────────────────
    public static Color HexToColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color c))
            return c;
        Debug.LogWarning($"[UIColorTheme] Could not parse hex: {hex}");
        return Color.magenta;
    }

    /// <summary>Returns the correct icon background color for a destination name.</summary>
    public static Color GetDestinationIconColor(string destinationName) =>
        destinationName switch
        {
            "Computer Lab" => IconBgLab,
            "Room 305"     => IconBgRoom,
            "Cafeteria"    => IconBgCaf,
            "Exit"         => IconBgExit,
            _              => BlueSoft,
        };
}
