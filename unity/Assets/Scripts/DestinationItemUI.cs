// Assets/Scripts/DestinationItemUI.cs
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Reusable UI component for a single destination row.
/// Attach to the DestinationItem prefab.
/// </summary>
[RequireComponent(typeof(Button))]
public class DestinationItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] Image       iconBackground;
    [SerializeField] Image       iconImage;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI metaText;

    Destination            _destination;
    Action<Destination>    _onSelected;

    // ── Public API ────────────────────────────────────────────

    public void Setup(Destination dest, Action<Destination> onSelected)
    {
        _destination = dest;
        _onSelected  = onSelected;

        if (nameText) nameText.text = dest.name;
        if (metaText) metaText.text = dest.metaInfo;

        if (iconBackground)
            iconBackground.color = dest.iconBackground != default
                ? dest.iconBackground
                : new Color(0.84f, 0.92f, 0.99f); // default light blue

        if (iconImage && dest.icon != null)
            iconImage.sprite = dest.icon;

        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    // ── Callbacks ─────────────────────────────────────────────

    void OnClick()
    {
        _onSelected?.Invoke(_destination);
    }
}
