using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Collections;

public class PlacesUI : MonoBehaviour
{
    public GameObject buttonPrefab;
    public Transform content;

    void Start()
    {
        StartCoroutine(GetRoomsFromAPI());
    }

    IEnumerator GetRoomsFromAPI()
    {
        string url = "http://localhost:5000/rooms"; // 👈 حط اللينك الحقيقي

        UnityWebRequest request = UnityWebRequest.Get(url);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;

            Debug.Log("API Response: " + json);

            RoomResponse response = JsonUtility.FromJson<RoomResponse>(json);

            Populate(response.rooms);
        }
        else
        {
            Debug.LogError("API Error: " + request.error);
        }
    }

    public void Populate(Room[] rooms)
    {
        // // 🧹 مسح القديم
        // foreach (Transform child in content)
        // {
        //     Destroy(child.gameObject);
        // }

        // ➕ إضافة الجديد
        foreach (Room room in rooms)
        {
            GameObject btn = Instantiate(buttonPrefab, content);

            TMP_Text text = btn.GetComponentInChildren<TMP_Text>();
            text.text = room.name;

            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                Debug.Log("Clicked: " + room.name + " | Zone: " + room.zone);
            });
        }
    }
}