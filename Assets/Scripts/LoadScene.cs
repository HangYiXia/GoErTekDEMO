using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{
    [SerializeField]
    public string targetSceneName = "";

    [SerializeField]
    public string targetSceneMode = "";

    public static string sceneMode = "";
    
    public void Load()
    {
        if (targetSceneName != "")
        {
            sceneMode = targetSceneMode;
            SceneManager.LoadScene(targetSceneName);
        }
    }

    public void Exit()
    {
        Application.Quit();
    }
}
