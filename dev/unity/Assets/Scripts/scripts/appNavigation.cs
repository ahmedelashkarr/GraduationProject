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
    
            // Validate inputs before transitioning to AR scene
            if (string.IsNullOrEmpty(from))
            {
                Debug.LogError("[SendRouteAndOpen] startPoint is empty — localization hasn't completed yet.");
                yield break;
        }
        if (string.IsNullOrEmpty(to))
        {
            Debug.LogError("[SendRouteAndOpen] destination is empty — please pick a destination.");
            yield break;
        }
    
        // Note: the actual /route request is made by PathRequester in the
        // AR scene (autoFetchOnStart). We don't fetch here — we just store
        // the values in NavigationData (already done) and switch scenes.
        Debug.Log($"[SendRouteAndOpen] Loading AR scene. from={from}, to={to}");
        SceneManager.LoadScene(2);
        yield break;
    }   
    public void BackToMainMenu()
    {
        SceneManager.LoadScene(1);
    }
}