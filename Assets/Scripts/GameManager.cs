using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }else
            Destroy(gameObject);
        
    }
    
    public void ChangeDifficulty(int difficulty)
    {
        Debug.Log("Difficulty changed to " + (difficulty + 1));
        GameData.Difficulty = 3 - difficulty;
    }

    public void StartGame()
    {
        StartCoroutine(LoadGameSceneAsync("GameScene", SpawnOres));
    }
    
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }

    void SpawnOres()
    {
        TerrainPopulator terrainPopulator = GameObject.FindGameObjectWithTag("Ground").GetComponent<TerrainPopulator>();
        
        // Easy
        if (GameData.Difficulty == 3)
            terrainPopulator.SetOreSpawns(40, 33,33,34);
        
        // Normal
        else if (GameData.Difficulty == 2)
            terrainPopulator.SetOreSpawns(30, 30, 35, 35);
        
        // Hard
        else
            terrainPopulator.SetOreSpawns(20, 20, 30, 40);
    }
    
    IEnumerator LoadGameSceneAsync(string sceneName, System.Action callback = null)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        asyncLoad.allowSceneActivation = false;

        // Wait until the scene is fully loaded
        while (!asyncLoad.isDone)
        {
            // The scene has loaded, but lighting is still being calculated
            if (asyncLoad.progress >= 0.9f)
            {
                // Activate the scene
                asyncLoad.allowSceneActivation = true;
            }


            yield return null;
        }

        if (callback != null)
            callback();
    }
}
