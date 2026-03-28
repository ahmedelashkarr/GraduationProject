// Assets/Scripts/MainScreenUI.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the Main Screen (Scene 1).
/// • Shows floor map (via RenderTexture from MapCamera)
/// • Populates both destination sections
/// • "Start AR Navigation" button transitions to AR scene
/// </summary>
public class MainScreenUI : MonoBehaviour
{
    [Header("Top Bar")]
    [SerializeField] TMP_InputField searchInput;
    [SerializeField] Button         micButton;
    [SerializeField] Button         backButton;

    [Header("Floor Bar")]
    [SerializeField] TextMeshProUGUI floorTitle;
    [SerializeField] Button          floorBackButton;
    [SerializeField] Button          menuButton;
    [SerializeField] Button          lockButton;

    [Header("Map")]
    [SerializeField] RawImage  mapDisplay;        // Shows RenderTexture from MapCamera
    [SerializeField] Image     youBadge;
    [SerializeField] Transform youMarkerOnMap;    // RectTransform positioned over map

    [Header("Destination Lists")]
    [SerializeField] Transform         topListParent;
    [SerializeField] Transform         bottomListParent;
    [SerializeField] GameObject        destinationItemPrefab;
    [SerializeField] DestinationData   destinationData;

    [Header("CTA")]
    [SerializeField] Button startARButton;
    [SerializeField] TextMeshProUGUI startARButtonText;

    [Header("Map Route")]
    [SerializeField] FloorMapRenderer floorMapRenderer;

    // Currently selected destination (highlighted in map)
    Destination _selected;

    // ── Unity Lifecycle ───────────────────────────────────────

    void Start()
    {
        SetupButtons();
        PopulateLists();

        // Pre-select first destination
        if (destinationData != null && destinationData.destinations.Count > 0)
            SelectDestination(destinationData.destinations[0]);
    }

    // ── Setup ─────────────────────────────────────────────────

    void SetupButtons()
    {
        backButton?.onClick.AddListener(() => AppManager.Instance.GoBack());
        floorBackButton?.onClick.AddListener(() => AppManager.Instance.GoBack());
        startARButton?.onClick.AddListener(OnStartAR);
        micButton?.onClick.AddListener(OnMicTapped);
    }

    void PopulateLists()
    {
        if (destinationData == null) return;

        // Top list: first item only
        PopulateList(topListParent, destinationData.destinations, 0, 1);

        // Bottom list: all items
        PopulateList(bottomListParent, destinationData.destinations, 0, destinationData.destinations.Count);
    }

    void PopulateList(Transform parent, System.Collections.Generic.List<Destination> list,
                      int startIdx, int count)
    {
        if (parent == null) return;

        foreach (Transform child in parent)
            Destroy(child.gameObject);

        for (int i = startIdx; i < Mathf.Min(startIdx + count, list.Count); i++)
        {
            var item = Instantiate(destinationItemPrefab, parent);
            var ui   = item.GetComponent<DestinationItemUI>();
            if (ui != null)
                ui.Setup(list[i], SelectDestination);
        }
    }

    // ── Destination Selection ─────────────────────────────────

    void SelectDestination(Destination dest)
    {
        _selected = dest;

        if (floorMapRenderer != null)
            floorMapRenderer.DrawRouteTo(dest);

        // Update start button
        if (startARButtonText)
            startARButtonText.text = $"Start AR Navigation → {dest.name}";
    }

    // ── Callbacks ─────────────────────────────────────────────

    void OnStartAR()
    {
        if (_selected == null && destinationData?.destinations.Count > 0)
            _selected = destinationData.destinations[0];

        AppManager.Instance.GoToAR(_selected);
    }

    void OnMicTapped()
    {
        // Voice search — integrate platform mic here
        Debug.Log("Mic tapped — voice search not yet implemented.");
    }
}
