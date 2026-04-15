using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class PlacesUI : MonoBehaviour
{
    public GameObject buttonPrefab;
    public Transform content;
    public Button startButton;
    public TMP_InputField searchInput;

    public AppNavigation appNavigation;

    private GameObject selectedCard;
    private Room selectedRoom;
    private Room[] allRooms;

    private TMP_Text startButtonText;

    void Start()
    {
        Debug.Log("PlacesUI Started");

        startButton.interactable = false;

        // 👇 نخزن Text بتاع الزرار
        startButtonText = startButton.GetComponentInChildren<TMP_Text>();

        // 👇 search
        searchInput.onValueChanged.AddListener(OnSearchChanged);

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

            allRooms = response.rooms;

            Populate(allRooms);
        }
        else
        {
            Debug.LogError("API Error: " + request.error);
        }
    }

    public void Populate(Room[] rooms)
    {
        // 🧹 clear old
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

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

    void OnSearchChanged(string text)
    {
        if (allRooms == null) return;

        if (string.IsNullOrEmpty(text))
        {
            Populate(allRooms);
            return;
        }

        text = text.ToLower();

        List<Room> filtered = new List<Room>();

        foreach (Room room in allRooms)
        {
            if (room.name.ToLower().Contains(text) || room.zone.ToLower().Contains(text))
            {
                filtered.Add(room);
            }
        }

        Populate(filtered.ToArray());
    }

    void SelectCard(GameObject card, Room room)
    {
        if (selectedCard != null)
        {
            ResetCard(selectedCard);
        }

        selectedCard = card;
        selectedRoom = room;

        HighlightCard(card);

        startButton.interactable = true;

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

    startButton.interactable = false;

    NavigationData.destination = selectedRoom.zone;

    Debug.Log("✅ FROM: " + NavigationData.startPoint);
    Debug.Log("✅ TO: " + NavigationData.destination);

    // 👇 بس كده
    appNavigation.OpenARCamera();
}

    IEnumerator SendRouteAndOpen()
    {
        string url = "https://sweepingly-oxidative-dominga.ngrok-free.dev/route?from="
                     + NavigationData.startPoint + "&to=" + NavigationData.destination;

        Debug.Log("🌐 URL: " + url);

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.certificateHandler = new BypassCertificate();

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Route Ready");

            appNavigation.OpenARCamera();
        }
        else
        {
            Debug.LogError("❌ Route Error: " + request.error);

            // 🔄 رجع الزرار لو فشل
            startButton.interactable = true;

            if (startButtonText != null)
                startButtonText.text = "Start Navigation";
        }
    }
}