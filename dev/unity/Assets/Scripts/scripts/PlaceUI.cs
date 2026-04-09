//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using System.Collections.Generic;
//using UnityEngine.Networking;
//using System.Collections;

//public class PlacesUI : MonoBehaviour
//{
//    public GameObject buttonPrefab;
//    public Transform content;

//    void Start()
//    {
//        StartCoroutine(GetRoomsFromAPI());
//    }

//    IEnumerator GetRoomsFromAPI()
//    {
//        string url = "https://sweepingly-oxidative-dominga.ngrok-free.dev/rooms"; // 👈 حط اللينك الحقيقي

//        UnityWebRequest request = UnityWebRequest.Get(url);

//        yield return request.SendWebRequest();

//        if (request.result == UnityWebRequest.Result.Success)
//        {
//            string json = request.downloadHandler.text;

//            Debug.Log("API Response: " + json);

//            RoomResponse response = JsonUtility.FromJson<RoomResponse>(json);

//            Populate(response.rooms);
//        }
//        else
//        {
//            Debug.LogError("API Error: " + request.error);
//        }
//    }

//    public void Populate(Room[] rooms)
//    {
//        // // 🧹 مسح القديم
//        // foreach (Transform child in content)
//        // {
//        //     Destroy(child.gameObject);
//        // }

//        // ➕ إضافة الجديد
//        foreach (Room room in rooms)
//        {
//            GameObject btn = Instantiate(buttonPrefab, content);

//            TMP_Text text = btn.GetComponentInChildren<TMP_Text>();
//            text.text = room.name;

//            btn.GetComponent<Button>().onClick.AddListener(() =>
//            {
//                Debug.Log("Clicked: " + room.name + " | Zone: " + room.zone);
//            });
//        }
//    }
//}
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

    // ✅ الإضافات الجديدة
    private GameObject selectedCard;
    private Room selectedRoom;

    void Start()
    {
        StartCoroutine(GetRoomsFromAPI());
    }

    IEnumerator GetRoomsFromAPI()
    {
        string url = "https://sweepingly-oxidative-dominga.ngrok-free.dev/rooms"; // 👈 حط اللينك الحقيقي

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
        foreach (Room room in rooms)
        {
            GameObject btn = Instantiate(buttonPrefab, content);

            TMP_Text text = btn.GetComponentInChildren<TMP_Text>();
            text.text = room.name;

            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                Debug.Log("Clicked: " + room.name + " | Zone: " + room.zone);

                SelectCard(btn, room);
            });
        }
    }

    // ✅ دالة الاختيار
    void SelectCard(GameObject card, Room room)
    {
        if (selectedCard != null)
        {
            ResetCard(selectedCard);
        }

        selectedCard = card;
        selectedRoom = room;

        HighlightCard(card);

        Debug.Log("Selected Destination: " + room.name);
    }

    // ✅ تغيير الشكل عند الاختيار (تم التعديل هنا 👇)
    void HighlightCard(GameObject card)
    {
        Image img = card.GetComponentInChildren<Image>(); // 👈 التعديل المهم
        if (img != null)
            img.color = new Color(0.2f, 0.6f, 1f);
    }

    // ✅ رجوع الشكل الطبيعي (تم التعديل هنا 👇)
    void ResetCard(GameObject card)
    {
        Image img = card.GetComponentInChildren<Image>(); // 👈 التعديل المهم
        if (img != null)
            img.color = Color.white;
    }

    // ✅ زرار Start Navigation
    public void StartNavigation()
    {
        if (selectedRoom != null)
        {
            Debug.Log("Navigating to: " + selectedRoom.name + " | Zone: " + selectedRoom.zone);

            // 👇 اربط هنا مع نظام الـ AR
            // NavigationManager.Instance.SetDestination(selectedRoom);
        }
        else
        {
            Debug.LogWarning("Please select a destination first!");
        }
    }
}