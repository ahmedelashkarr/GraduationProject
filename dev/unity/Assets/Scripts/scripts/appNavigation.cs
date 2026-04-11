using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Networking;

public class AppNavigation : MonoBehaviour
{
    void Start()
    {
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            StartCoroutine(SplashTimer());
        }
    }

    IEnumerator SplashTimer()
    {
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene(1);
    }

    public void OpenARCamera()
    {
        StartCoroutine(SendRouteAndOpen());
    }

    IEnumerator SendRouteAndOpen()
    {
        string from = NavigationData.startPoint;
        string to = NavigationData.destination;

        string url = "https://sweepingly-oxidative-dominga.ngrok-free.dev/route?from=" + from + "&to=" + to;

        Debug.Log("Route URL: " + url);
        Debug.Log("FROM: " + from);
        Debug.Log("TO: " + to);
        UnityWebRequest request = UnityWebRequest.Get(url);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Route Response: " + request.downloadHandler.text);

            SceneManager.LoadScene(2);
        }
        else
        {
            Debug.LogError("Route Error: " + request.error);
        }
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(1);
    }
}