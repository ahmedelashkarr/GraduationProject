// Assets/Scripts/AppManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent singleton that manages scene transitions and
/// carries selected destination across scenes.
/// </summary>
public class AppManager : MonoBehaviour
{
    public static AppManager Instance { get; private set; }

    // Scene build indices (match Build Settings order)
    public const int SCENE_SPLASH = 0;
    public const int SCENE_MAIN   = 1;
    public const int SCENE_AR     = 2;

    [HideInInspector] public Destination selectedDestination;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Navigation ────────────────────────────────────────────

    public void GoToMain()
    {
        SceneManager.LoadScene(SCENE_MAIN);
    }

    public void GoToAR(Destination destination)
    {
        selectedDestination = destination;
        SceneManager.LoadScene(SCENE_AR);
    }

    public void GoBack()
    {
        int current = SceneManager.GetActiveScene().buildIndex;
        if (current > 0)
            SceneManager.LoadScene(current - 1);
    }
}
