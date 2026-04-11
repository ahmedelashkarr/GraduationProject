using UnityEngine;
using System.Collections.Generic;

public class WifiScanner
{
    public static Dictionary<string, int> GetWifiScan()
    {
        Dictionary<string, int> wifiData = new Dictionary<string, int>();

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            AndroidJavaObject wifiManager = context.Call<AndroidJavaObject>("getSystemService", "wifi");

            // Start scan
            wifiManager.Call<bool>("startScan");


            AndroidJavaObject results = wifiManager.Call<AndroidJavaObject>("getScanResults");

            int size = results.Call<int>("size");

            for (int i = 0; i < size; i++)
            {
                AndroidJavaObject scanResult = results.Call<AndroidJavaObject>("get", i);

                string bssid = scanResult.Get<string>("BSSID");
                int level = scanResult.Get<int>("level");

                wifiData[bssid] = level;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("WiFi Scan Error: " + e.Message);
        }
#else
        // test data in editor
        wifiData["TEST_AP_1"] = -40;
        wifiData["TEST_AP_2"] = -65;
#endif

        return wifiData;
    }
}