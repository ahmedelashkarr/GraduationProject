using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using UnityEngine.Android;

public class ScanSender : MonoBehaviour
{
    public TMP_Text resultText;

    string url = "https://sweepingly-oxidative-dominga.ngrok-free.dev/locate";

    private bool isSending = false;

    void Start()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
            yield return new WaitForSeconds(2f);
        }

        // 👇 بدل loop القديم
        InvokeRepeating(nameof(StartScan), 0f, 3f);
    }

    void StartScan()
    {
        // ❌ امنع overlap
        if (isSending) return;

        StartCoroutine(SendScan());
    }

    IEnumerator SendScan()
    {
        isSending = true;

        Dictionary<string, int> scanData = WifiScanner.GetWifiScan();
        NavigationData.lastScan = scanData;

        foreach (var kvp in scanData)
        {
            Debug.Log("AP: " + kvp.Key + " RSSI: " + kvp.Value);
        }

        // JSON
        StringBuilder jsonBuilder = new StringBuilder();
        jsonBuilder.Append("{\"scan\":{");

        foreach (var kvp in scanData)
        {
            jsonBuilder.Append("\"" + kvp.Key + "\":" + kvp.Value + ",");
        }

        if (scanData.Count > 0)
            jsonBuilder.Length--;

        jsonBuilder.Append("}}");

        string json = jsonBuilder.ToString();

        Debug.Log("======= JSON SENT =======");
        Debug.Log(json);

        UnityWebRequest request = new UnityWebRequest(url, "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // ⚠️ SSL FIX (لو ngrok)
        request.certificateHandler = new BypassCertificate();

        // 👇 مهم: مش هنوقف الفريم
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
        string responseJson = request.downloadHandler.text;
        Debug.Log("📍 Response: " + responseJson);
        
        // ✅ فك JSON
        ZoneResponse res = JsonUtility.FromJson<ZoneResponse>(responseJson);
        
        // ✅ حفظ start point (مرة واحدة بس)
        if (string.IsNullOrEmpty(NavigationData.startPoint) || NavigationData.startPoint == "F1_LOBBY")
        {
            NavigationData.startPoint = res.zone;
            Debug.Log("🏁 Start Point Saved: " + NavigationData.startPoint);
        }

// UI
if (resultText != null)
    resultText.text = res.zone;
        }
        else
        {
            Debug.LogError("❌ Error: " + request.error);
        }

        isSending = false;
    }
}