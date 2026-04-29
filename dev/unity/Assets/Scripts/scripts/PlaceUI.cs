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

    // Previously this method fetched the route here in the menu scene and
    // discarded the response, then the AR scene fetched the same route a
    // second time via PathRequester. We now skip the menu-scene fetch and
    // let PathRequester (in the AR scene) own the single source of truth
    // for talking to /route.
    IEnumerator SendRouteAndOpen()
    {
        string from = NavigationData.startPoint;
        string to   = NavigationData.destination;

        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
        {
            Debug.LogError(
                "[PlacesUI] Cannot open AR scene — NavigationData.startPoint or " +
                $".destination is empty. startPoint='{from}', destination='{to}'.");

            // Restore the button so the user can retry.
            if (startButton != null) startButton.interactable = true;
            if (startButtonText != null) startButtonText.text = "Start Navigation";

            yield break;
        }

        appNavigation.OpenARCamera();
        yield break;
    }
}