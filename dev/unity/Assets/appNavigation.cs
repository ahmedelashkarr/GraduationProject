//using UnityEngine;
//using UnityEngine.SceneManagement;
//using System.Collections;

//public class AppNavigation : MonoBehaviour
//{
//    void Start()
//    {
//        // لو إحنا في مشهد الـ Splash (رقم 0)، استنى 3 ثواني وانقل
//        if (SceneManager.GetActiveScene().buildIndex == 0)
//        {
//            StartCoroutine(SplashTimer());
//        }
//    }

//    IEnumerator SplashTimer()
//    {
//        yield return new WaitForSeconds(3f);
//        SceneManager.LoadScene(1); // افتح المنيو (رقم 1)
//    }

//    // دالة لزرار الـ Start اللي في المنيو
//    public void OpenARCamera()
//    {
//        SceneManager.LoadScene(2); // افتح الكاميرا (رقم 2)
//    }
//}
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AppNavigation : MonoBehaviour
{
    void Start()
    {
        // لو إحنا في مشهد الـ Splash (رقم 0)، استنى 3 ثواني وانقل
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            StartCoroutine(SplashTimer());
        }
    }

    IEnumerator SplashTimer()
    {
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene(1); // افتح المنيو (رقم 1)
    }

    // دالة لزرار الـ Start اللي في المنيو
    public void OpenARCamera()
    {
        SceneManager.LoadScene(2); // افتح الكاميرا (رقم 2)
    }

    // الدالة الجديدة لزرار الرجوع للصفحة الرئيسية
    public void BackToMainMenu()
    {
        SceneManager.LoadScene(1); // هيرجع للمنيو (المشهد رقم 1)
    }
}