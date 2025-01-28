using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    private TextMeshProUGUI playerText, machineText, timeText, goldText, silverText, copperText;

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

    void UpdateGold(int gold)
    {
        goldText.text = "x" + gold;
    }

    void UpdateSilver(int silver)
    {
        silverText.text = "x" + silver;
    }

    void UpdateCopper(int copper)
    {
        copperText.text = "x" + copper;
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
        Debug.Log("Difficulty changed to " + difficulty);
        GameData.Difficulty = difficulty;
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
        timeText = canvas.Find("Time").GetComponent<TextMeshProUGUI>();
        machineText = canvas.Find("EnemyScore").GetComponent<TextMeshProUGUI>();
        playerText = canvas.Find("Score").GetComponent<TextMeshProUGUI>();
        Transform inventory = canvas.Find("Inventory");
        goldText = inventory.Find("Gold").GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();
        silverText = inventory.Find("Silver").GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();
        copperText = inventory.Find("Copper").GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();
        
        // Set callbacks
        GameData.PlayerData.onScoreUpdated += UpdatePlayerScore;
        GameData.MachineData.onScoreUpdated += UpdateMachineScore;
        
        // TODO: Comment out if the old inventory ui is not in use.
        GameData.PlayerData.inventory["Gold"].onQuantityUpdated += UpdateGold;
        GameData.PlayerData.inventory["Silver"].onQuantityUpdated += UpdateSilver;
        GameData.PlayerData.inventory["Copper"].onQuantityUpdated += UpdateCopper;
        
        SpawnOres();
        UpdatePlayerScore(0);
        UpdateMachineScore(0);
        StartCoroutine(CountDown());
    }

    void GameOver()
    {
        Debug.Log("Game over");
    }

    IEnumerator CountDown()
    {
        int durationInSeconds = 60;
        UpdateTime(durationInSeconds);

        while (durationInSeconds > 0)
        {
            yield return new WaitForSeconds(1);
            UpdateTime(--durationInSeconds);
        }

        StartCoroutine(LoadGameSceneAsync("EndScene", GameOver));
    }
    
    void SpawnOres()
    {
        TerrainPopulator terrainPopulator = GameObject.FindGameObjectWithTag("Ground").GetComponent<TerrainPopulator>();
        
        // Easy
        if (GameData.Difficulty == 3)
            terrainPopulator.SetOreSpawns(40, 10,30,60);
        
        // Normal
        else if (GameData.Difficulty == 2)
            terrainPopulator.SetOreSpawns(30, 5, 25, 70);
        
        // Hard
        else
            terrainPopulator.SetOreSpawns(20, 5, 20, 75);
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
