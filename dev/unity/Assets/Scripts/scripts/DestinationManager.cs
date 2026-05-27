using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DestinationManager : MonoBehaviour
{
    public static string selectedDestination = "";

    private Image lastSelectedCard;

    public Color selectedColor = new Color(0.7f, 0.9f, 1f);
    public Color normalColor = Color.white;

    public void SelectDestination(string placeName)
    {
        selectedDestination = placeName;

        // ✅ هنا الربط المهم
        NavigationData.destination = placeName;

        Debug.Log("selected " + selectedDestination);

        GameObject clickedButton = EventSystem.current.currentSelectedGameObject;

        if (clickedButton != null)
        {
            Image currentCardImage = clickedButton.GetComponent<Image>();

            if (lastSelectedCard != null)
            {
                lastSelectedCard.color = normalColor;
            }

            if (currentCardImage != null)
            {
                currentCardImage.color = selectedColor;
                lastSelectedCard = currentCardImage;
            }
        }
    }
}