// Assets/Scripts/ARNavigationUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the AR Navigation Screen HUD (Scene 2).
/// • Reads selected destination from AppManager
/// • Wires up Stop / Recenter / Map buttons
/// • Updates destination card
/// • Manages distance label (updates as user moves)
/// </summary>
public class ARNavigationUI : MonoBehaviour
{
    [Header("Top Bar")]
    [SerializeField] Button          backButton;
    [SerializeField] TMP_InputField  searchInput;
    [SerializeField] Button          micButton;

    [Header("Control Buttons")]
    [SerializeField] Button stopButton;
    [SerializeField] Button recenterButton;
    [SerializeField] Button mapButton;

    [Header("Destination Card")]
    [SerializeField] Image           destIconBg;
    [SerializeField] Image           destIconImage;
    [SerializeField] TextMeshProUGUI destNameText;
    [SerializeField] TextMeshProUGUI destMetaText;
    [SerializeField] Button          destCardButton;

    [Header("Arrow Controller")]
    [SerializeField] ARArrowController arrowController;

    [Header("Distance Update")]
    [SerializeField] Transform cameraTransform;   // AR Main Camera
    [SerializeField] float     updateInterval = 0.5f; // Seconds between distance updates

    Destination _destination;
    float       _nextUpdateTime;

    // ── Unity Lifecycle ───────────────────────────────────────

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main?.transform;

        _destination = AppManager.Instance?.selectedDestination;

        SetupButtons();
        RefreshDestinationCard();
    }

    void Update()
    {
        if (Time.time >= _nextUpdateTime)
        {
            _nextUpdateTime = Time.time + updateInterval;
            UpdateDistanceLabel();
        }
    }

    // ── Setup ─────────────────────────────────────────────────

    void SetupButtons()
    {
        backButton?.onClick.AddListener(OnBack);
        stopButton?.onClick.AddListener(OnStop);
        recenterButton?.onClick.AddListener(OnRecenter);
        mapButton?.onClick.AddListener(OnMapTapped);
        destCardButton?.onClick.AddListener(OnDestCardTapped);
        micButton?.onClick.AddListener(OnMicTapped);
    }

    void RefreshDestinationCard()
    {
        if (_destination == null) return;

        if (destNameText) destNameText.text = _destination.name;
        if (destMetaText) destMetaText.text = _destination.metaInfo;

        if (destIconBg && _destination.iconBackground != default)
            destIconBg.color = _destination.iconBackground;

        if (destIconImage && _destination.icon != null)
            destIconImage.sprite = _destination.icon;
    }

    void UpdateDistanceLabel()
    {
        if (_destination == null || cameraTransform == null) return;
        if (_destination.worldPosition == Vector3.zero) return;

        float distMetres = Vector3.Distance(
            new Vector3(cameraTransform.position.x, 0, cameraTransform.position.z),
            new Vector3(_destination.worldPosition.x, 0, _destination.worldPosition.z));

        int seconds = Mathf.Max(1, Mathf.RoundToInt(distMetres / 1.4f)); // ~1.4 m/s walking

        if (destMetaText)
            destMetaText.text = $"{distMetres:F0} meters · {FormatTime(seconds)}";
    }

    string FormatTime(int seconds)
    {
        if (seconds < 60) return $"{seconds} sec";
        return $"{seconds / 60} min {seconds % 60} sec";
    }

    // ── Button Callbacks ──────────────────────────────────────

    void OnBack()
    {
        arrowController?.StopNavigation();
        AppManager.Instance.GoBack();
    }

    void OnStop()
    {
        arrowController?.StopNavigation();
        AppManager.Instance.GoBack();
    }

    void OnRecenter()
    {
        arrowController?.Recenter();
    }

    void OnMapTapped()
    {
        // TODO: Overlay the floor map UI (instantiate MainScene map panel as overlay)
        Debug.Log("Map button tapped — show mini-map overlay.");
    }

    void OnDestCardTapped()
    {
        Debug.Log($"Destination card tapped: {_destination?.name}");
    }

    void OnMicTapped()
    {
        Debug.Log("Mic tapped — voice search.");
    }
}
