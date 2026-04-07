using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class RealRSSIManager : MonoBehaviour
{
    [Header("API Settings")]
    // رابط الباك إند الفعلي
    public string backendURL = "https://your-api.com/api/location";
    // الوقت بين كل قراءة والتانية (بالثواني)
    public float updateInterval = 2.0f;

    private string deviceId;

    void Start()
    {
        // سحب ID مميز للموبايل عشان الباك إند يقدر يفرق بين المستخدمين
        deviceId = SystemInfo.deviceUniqueIdentifier;

        // تشغيل عملية الإرسال المتكرر
        StartCoroutine(RoutineSendRSSI());
    }

    private IEnumerator RoutineSendRSSI()
    {
        // حلقة لا نهائية شغالة طول ما التطبيق مفتوح
        while (true)
        {
            int currentRssi = GetAndroidWifiRSSI();

            // إرسال البيانات
            yield return StartCoroutine(PostRSSIData(deviceId, currentRssi));

            // الانتظار قبل القراءة اللي بعدها
            yield return new WaitForSeconds(updateInterval);
        }
    }

    // دالة مخصصة لقراءة إشارة الواي فاي من نظام أندرويد مباشرة
    private int GetAndroidWifiRSSI()
    {
        // الكود ده هيشتغل بس لو عملت Build للموبايل
#if UNITY_ANDROID && !UNITY_EDITOR
        try {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject wifiManager = activity.Call<AndroidJavaObject>("getSystemService", "wifi");
                AndroidJavaObject connectionInfo = wifiManager.Call<AndroidJavaObject>("getConnectionInfo");
                return connectionInfo.Call<int>("getRssi");
            }
        } catch (System.Exception e) {
            Debug.LogError("Error reading RSSI: " + e.Message);
            return 0;
        }
#else
        // لو مشغل اللعبة على الكمبيوتر هيرجع قيمة وهمية للتجربة
        return -55;
#endif
    }

    private IEnumerator PostRSSIData(string devId, int rssi)
    {
        string jsonBody = $"{{\"device_id\":\"{devId}\", \"rssi\":{rssi}}}";

        using (UnityWebRequest request = new UnityWebRequest(backendURL, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Backend Response: " + request.downloadHandler.text);
                // هنا المفروض الباك إند بيرد عليك بـ X و Y علشان تحرك الـ AR
            }
            else
            {
                Debug.LogError("API Error: " + request.error);
            }
        }
    }
}