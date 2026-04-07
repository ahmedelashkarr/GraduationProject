//using UnityEngine;

//public class DestinationManager : MonoBehaviour
//{
//    // عملناه static عشان نقدر نقراه بسهولة من أي مشهد تاني
//    public static string selectedDestination = "";

//    // الدالة اللي هنربطها بكل كارت من الـ Cards
//    public void SelectDestination(string placeName)
//    {
//        selectedDestination = placeName;
//        Debug.Log("تم اختيار الوجهة: " + selectedDestination);

//        // مساحة فاضية تقدر تضيف فيها كود مستقبلاً لتغيير لون الكارت المتحدد
//    }
//}
using UnityEngine;
using UnityEngine.UI; // مهم عشان نقدر نوصل للـ Button

public class DestinationManager : MonoBehaviour
{
    public static string selectedDestination = "";

    void Start()
    {
        // الكود ده بيلف على كل الـ GameObjects الأبناء (الكروت) اللي تحت الأب ده
        foreach (Transform child in transform)
        {
            Button btn = child.GetComponent<Button>();

            // لو الابن ده عليه مكون Button
            if (btn != null)
            {
                // بناخد اسم المكان من اسم الـ GameObject نفسه
                string placeName = child.name;

                // بنربط الزرار بالدالة أوتوماتيك بالكود
                btn.onClick.AddListener(() => SelectDestination(placeName));
            }
        }
    }

    public void SelectDestination(string placeName)
    {
        selectedDestination = placeName;
        Debug.Log("تم اختيار الوجهة: " + selectedDestination);
    }
}