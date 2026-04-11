using System.Collections.Generic;

public static class NavigationData
{
    // 📡 WiFi Scan
    public static Dictionary<string, int> lastScan = new Dictionary<string, int>();

    // 🎯 Destination
    public static string destination = "";

    // 🏁 Start Point
    public static string startPoint = "F1_LOBBY"; // تقدر تغيره
}