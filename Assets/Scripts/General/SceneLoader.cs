using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static void Load(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public static void LoadNext()
    {
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextIndex);
        }
    }

    public static void Reload()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
