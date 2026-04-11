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

    // 🔥 غير الـ IP ده حسب جهازك
    string url = "https://sweepingly-oxidative-dominga.ngrok-free.dev/locate";

    void Start()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // طلب permission
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
            yield return new WaitForSeconds(2f);
        }

        // يبدأ الإرسال التلقائي
        StartCoroutine(AutoSendLoop());
    }

    IEnumerator AutoSendLoop()
    {
        while (true)
        {
            yield return StartCoroutine(SendScan());
            yield return new WaitForSeconds(3f);
        }
    }

    IEnumerator SendScan()
    {
        Dictionary<string, int> scanData = WifiScanner.GetWifiScan();
        NavigationData.lastScan = scanData;
        foreach (var kvp in scanData)
        {
            Debug.Log("AP: " + kvp.Key + " RSSI: " + kvp.Value);
        }   

        // بناء JSON
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
Debug.Log("=========================");

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseJson = request.downloadHandler.text;
            Debug.Log("Response: " + responseJson);

            if (resultText != null)
                resultText.text = responseJson;
        }
        else
        {
            Debug.LogError("Error: " + request.error);

            if (resultText != null)
                resultText.text = "Error: " + request.error;
        }
    }
}