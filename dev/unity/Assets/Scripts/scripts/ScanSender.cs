using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System.Collections.Generic;

public class ScanSender : MonoBehaviour
{
    public TMP_InputField inputField;
    public TMP_Text resultText;

    string url = "http://localhost:5000/locate"; 

    public void OnSendClicked()
    {
        StartCoroutine(SendScan());
    }

    IEnumerator SendScan()
{
    string json = @"{
        ""scan"": {
            ""20:3A:EB:AB:A2:13"": -40,
            ""30:99:35:A8:BA:20"": -60
        }
    }";

    Debug.Log("Sending JSON: " + json);

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

        ScanResponse response = JsonUtility.FromJson<ScanResponse>(responseJson);

        resultText.text =
            "Zone: " + response.zone +
            "\nFloor: " + response.floor +
            "\nConfidence: " + response.confidence;
    }
    else
    {
        Debug.LogError("Error: " + request.error);
        Debug.LogError("Server Response: " + request.downloadHandler.text);

        resultText.text = "Error: " + request.error;
    }
}
}