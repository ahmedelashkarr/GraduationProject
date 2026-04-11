using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;

public class PlacesUI : MonoBehaviour
{
    public GameObject buttonPrefab;
    public Transform content;
    public Button startButton; // 👈 اربطه من Inspector

    private GameObject selectedCard;
    private Room selectedRoom;

    void Start()
    {
        Debug.Log("PlacesUI Instance: " + this.GetInstanceID());

        startButton.interactable = false; // ❌ اقفل زرار البداية لحد ما يختار

        StartCoroutine(GetRoomsFromAPI());
    }

    IEnumerator GetRoomsFromAPI()
    {
        string url = "https://sweepingly-oxidative-dominga.ngrok-free.dev/rooms";

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
                Debug.Log("🟢 CLICKED: " + room.name);

                SelectCard(btn, room);
            });
        }
    }

    void SelectCard(GameObject card, Room room)
    {
        Debug.Log("🟡 ENTER SelectCard");

        if (selectedCard != null)
        {
            ResetCard(selectedCard);
        }

        selectedCard = card;
        selectedRoom = room;

        HighlightCard(card);

        startButton.interactable = true; // ✅ فعل زرار Start

        Debug.Log("✅ SelectedRoom: " + selectedRoom.name);
    }

    void HighlightCard(GameObject card)
    {
        Image img = card.GetComponentInChildren<Image>();
        if (img != null)
            img.color = new Color(0.2f, 0.6f, 1f);
    }

    void ResetCard(GameObject card)
    {
        Image img = card.GetComponentInChildren<Image>();
        if (img != null)
            img.color = Color.white;
    }

    public void StartNavigation()
    {
        Debug.Log("🔵 START BUTTON CLICKED");

        if (selectedRoom == null)
        {
            Debug.LogError("❌ No room selected!");
            return;
        }

        // ✅ نقل البيانات
        NavigationData.startPoint = "F1_LOBBY";
        NavigationData.destination = selectedRoom.zone;

        Debug.Log("✅ FROM: " + NavigationData.startPoint);
        Debug.Log("✅ TO: " + NavigationData.destination);

        // ✅ Call API
        StartCoroutine(SendRoute());
    }

    IEnumerator SendRoute()
    {
        string url = "https://sweepingly-oxidative-dominga.ngrok-free.dev/route?from="
                     + NavigationData.startPoint + "&to=" + NavigationData.destination;

        Debug.Log("🌐 URL: " + url);

        UnityWebRequest request = UnityWebRequest.Get(url);

        // ⚠️ حل مشكلة SSL
        request.certificateHandler = new BypassCertificate();

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Route Response: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("❌ Route Error: " + request.error);
        }
    }
}