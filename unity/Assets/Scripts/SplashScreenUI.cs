// Assets/Scripts/SplashScreenUI.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the Splash Screen (Scene 0).
/// • Ensures AppManager exists (bootstraps it if needed)
/// • Populates the Popular Destinations list
/// • Animates the bottom sheet sliding up on start
/// </summary>
public class SplashScreenUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] RectTransform bottomSheet;
    [SerializeField] Transform destinationListParent;
    [SerializeField] GameObject destinationItemPrefab;
    [SerializeField] DestinationData destinationData;

    [Header("Dot Indicators")]
    [SerializeField] Image[] dots;
    [SerializeField] Color dotActiveColor  = Color.white;
    [SerializeField] Color dotInactiveColor = new Color(1,1,1,0.4f);

    [Header("Animation")]
    [SerializeField] float slideInDuration = 0.55f;
    [SerializeField] float slideStartY     = -320f;   // off-screen below

    // ── Unity Lifecycle ───────────────────────────────────────

    void Awake()
    {
        // Bootstrap AppManager if the splash is the entry scene
        if (AppManager.Instance == null)
        {
            var go = new GameObject("AppManager");
            go.AddComponent<AppManager>();
        }
    }

    void Start()
    {
        SetupDots(0);
        PopulateDestinations();
        StartCoroutine(AnimateBottomSheetIn());
    }

    // ── UI Population ─────────────────────────────────────────

    void PopulateDestinations()
    {
        if (destinationData == null) return;

        foreach (Transform child in destinationListParent)
            Destroy(child.gameObject);

        foreach (var dest in destinationData.destinations)
        {
            var item = Instantiate(destinationItemPrefab, destinationListParent);
            var ui   = item.GetComponent<DestinationItemUI>();
            if (ui != null)
                ui.Setup(dest, OnDestinationSelected);
        }
    }

    void SetupDots(int activeIndex)
    {
        for (int i = 0; i < dots.Length; i++)
        {
            if (dots[i] == null) continue;
            dots[i].color = (i == activeIndex) ? dotActiveColor : dotInactiveColor;
            var rt = dots[i].rectTransform;
            rt.sizeDelta = (i == activeIndex)
                ? new Vector2(24f, 8f)
                : new Vector2(8f,  8f);
        }
    }

    // ── Animation ─────────────────────────────────────────────

    IEnumerator AnimateBottomSheetIn()
    {
        if (bottomSheet == null) yield break;

        Vector2 start  = new Vector2(bottomSheet.anchoredPosition.x, slideStartY);
        Vector2 target = new Vector2(bottomSheet.anchoredPosition.x, 0f);
        float   t      = 0f;

        bottomSheet.anchoredPosition = start;

        while (t < 1f)
        {
            t += Time.deltaTime / slideInDuration;
            float ease = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f); // cubic ease-out
            bottomSheet.anchoredPosition = Vector2.Lerp(start, target, ease);
            yield return null;
        }

        bottomSheet.anchoredPosition = target;
    }

    // ── Callbacks ─────────────────────────────────────────────

    void OnDestinationSelected(Destination dest)
    {
        AppManager.Instance.GoToMain();
    }
}
