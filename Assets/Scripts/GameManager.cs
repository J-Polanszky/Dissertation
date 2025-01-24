using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    private TextMeshProUGUI playerText, machineText, timeText;

    void UpdatePlayerScore(int score)
    {
        playerText.text = score.ToString();
    }

    void UpdateMachineScore(int score)
    {
        machineText.text = score.ToString();
    }

    void UpdateTime(int timeInSeconds)
    {
        int minutes = timeInSeconds / 60;
        int seconds = timeInSeconds % 60;
        timeText.text = minutes.ToString("00") + "m " + seconds.ToString("00") + "s";
    }
    
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
        StartCoroutine(LoadGameSceneAsync("GameScene", RunGameStartFunctions));
    }
    
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }

    void RunGameStartFunctions()
    {
        Debug.Log("Game started");
        Transform canvas = GameObject.Find("Canvas").transform;
        Debug.Log("Canvas found" + canvas.childCount);
        timeText = canvas.Find("Time").GetComponent<TextMeshProUGUI>();
        machineText = canvas.Find("EnemyScore").GetComponent<TextMeshProUGUI>();
        playerText = canvas.Find("Score").GetComponent<TextMeshProUGUI>();
        
        Debug.Log("Player text found" + playerText.text);
        Debug.Log("Machine text found" + machineText.text);
        Debug.Log("Time text found" + timeText.text);
        
        // Set callbacks
        GameData.PlayerData.onScoreUpdated += UpdatePlayerScore;
        GameData.MachineData.onScoreUpdated += UpdateMachineScore;
        
        SpawnOres();
        UpdatePlayerScore(0);
        UpdateMachineScore(0);
        StartCoroutine(CountDown());
    }

    IEnumerator CountDown()
    {
        int durationInSeconds = 600;
        UpdateTime(durationInSeconds);

        while (durationInSeconds > 0)
        {
            yield return new WaitForSeconds(1);
            UpdateTime(--durationInSeconds);
        }
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
