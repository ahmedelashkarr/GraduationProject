//using UnityEngine;

//public class DestinationManager : MonoBehaviour
//{
//    // المتغير ده static عشان يفضل شايل اسم المكان ونقدر نقراه من المشهد التاني
//    public static string selectedDestination = "";

//    // الدالة دي اللي هنربطها بكل كارت
//    public void SelectDestination(string placeName)
//    {
//        selectedDestination = placeName;
//        Debug.Log("room 7 selected: " + selectedDestination);
//    }
//}
using UnityEngine;
using UnityEngine.UI; // ضفنا دي عشان نتعامل مع الصور والألوان
using UnityEngine.EventSystems; // ضفنا دي عشان نعرف الزرار اللي اتداس عليه

public class DestinationManager : MonoBehaviour
{
    public static string selectedDestination = "";

    // متغير عشان نحفظ فيه الكارت اللي متلون حالياً، بحيث نرجعه لطبيعته لو اخترنا كارت غيره
    private Image lastSelectedCard;

    // دي الألوان، عملناها public عشان تقدر تغيرها براحتك من جوه اليونيتي
    public Color selectedColor = new Color(0.7f, 0.9f, 1f); // لون أزرق فاتح (تقدر تغيره)
    public Color normalColor = Color.white; // اللون الأبيض العادي

    public void SelectDestination(string placeName)
    {
        selectedDestination = placeName;
        Debug.Log("selected " + selectedDestination);

        // 1. نجيب الزرار اللي اليوزر لسه دايس عليه بالماوس دلوقتي حالاً
        GameObject clickedButton = EventSystem.current.currentSelectedGameObject;

        if (clickedButton != null)
        {
            // 2. نجيب مكون الصورة (Image) اللي على الزرار ده
            Image currentCardImage = clickedButton.GetComponent<Image>();

            // 3. لو في كارت كان متحدد قبل كده، نرجعه للونه الأبيض العادي
            if (lastSelectedCard != null)
            {
                lastSelectedCard.color = normalColor;
            }

            // 4. نلون الكارت الجديد اللي دوسنا عليه باللون المميز
            if (currentCardImage != null)
            {
                currentCardImage.color = selectedColor;

                // 5. نحفظ الكارت ده في المتغير عشان نبقى نرجعه للونه لو اخترنا حاجة تانية
                lastSelectedCard = currentCardImage;
            }
        }
    }
}