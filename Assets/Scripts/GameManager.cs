using System;
using System.Collections;
using FMODUnity;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private string backgroundMusic = "event:/Music_Events/BackingTrack";
    string clockSfx = "event:/SFX_Events/Clock";
    string buttonSfx = "event:/SFX_Events/UI_Buttons";

    [SerializeField] private GameObject playerPrefab;

    FMOD.Studio.EventInstance backgroundMusicInstance;
    FMOD.Studio.EventInstance clockInstance;
    FMOD.Studio.EventInstance buttonInstance;

    public static GameManager instance;

    private TextMeshProUGUI playerText, machineText, timeText, goldText, silverText, copperText;

    int playTest = 0;

    bool clockStarted = false;

    void UpdatePlayerScore(int score)
    {
        playerText.text = score.ToString();
    }

    void UpdateMachineScore(int score)
    {
        machineText.text = score.ToString();
    }

    void UpdateTime()
    {
        int minutes = GameData.TimeLeft / 60;
        int seconds = GameData.TimeLeft % 60;
        timeText.text = minutes.ToString("00") + "m " + seconds.ToString("00") + "s";
        
        if (!clockStarted && minutes == 0 && seconds <= 30)
        {
            clockInstance.setVolume(0);
            clockInstance.start();
            clockStarted = true;
            SetGameState(2);
        }

        if (clockStarted)
            clockInstance.setVolume((float)(30 - seconds) / 30f);
    }

    void UpdateGold(int gold)
    {
        goldText.text = "x" + gold + "(" +
                        (GameData.InvStorageQty[OreType.Gold] * GameData.PlayerData.inventory[OreType.Gold].Quantity) +
                        ")";
    }

    void UpdateSilver(int silver)
    {
        silverText.text = "x" + silver + "(" +
                          (GameData.InvStorageQty[OreType.Silver] *
                           GameData.PlayerData.inventory[OreType.Silver].Quantity) + ")";
    }

    void UpdateCopper(int copper)
    {
        copperText.text = "x" + copper + "(" +
                          (GameData.InvStorageQty[OreType.Copper] *
                           GameData.PlayerData.inventory[OreType.Copper].Quantity) + ")";
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    void Start()
    {
        FMODUnity.RuntimeManager.GetBus("bus:/").setVolume(0.7f);

        backgroundMusicInstance = RuntimeManager.CreateInstance(backgroundMusic);
        clockInstance = RuntimeManager.CreateInstance(clockSfx);
        buttonInstance = RuntimeManager.CreateInstance(buttonSfx);

        SetGameState(0);
        backgroundMusicInstance.setVolume(0.65f);
        backgroundMusicInstance.start();
    }

    //TODO: Change gamestate variable to change music instance

    public void ChangeDifficulty(int difficulty)
    {
        // Debug.Log("Difficulty changed to " + difficulty);
        buttonInstance.start();
        GameData.Difficulty = difficulty;
    }

    private void Start()
    {
        Transform menu = GameObject.FindWithTag("Canvas").transform.Find("Menu");
        menu.Find("StartGame").GetComponent<Button>().onClick.AddListener(StartTest2);
        menu.Find("QuitGame").GetComponent<Button>().onClick.AddListener(QuitGame);
    }

    void StartTest1()
    {
        buttonInstance.start();
        SetGameState(1);
        StartCoroutine(LoadGameSceneAsync("GameScene", RunGameStartFunctions));
    }

    void StartTest2()
    {
        // Gather which scene to load from data.
        bool fakeIsDDA = true;
        string gameScene = fakeIsDDA ? "GameSceneDDA" : "GameSceneRLAgent";
        StartCoroutine(LoadGameSceneAsync(gameScene));
    }

    void QuitGame()
    {
        buttonInstance.start();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }

    public void RunGameStartFunctions()
    {
        Debug.Log("Game started");
        
        GameData.PlayerData.Reset();
        GameData.MachineData.Reset();
        
        playTest++;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        Transform canvas = GameObject.FindWithTag("Canvas").transform;
        timeText = canvas.Find("Time").GetComponent<TextMeshProUGUI>();
        machineText = canvas.Find("EnemyScore").GetComponent<TextMeshProUGUI>();
        playerText = canvas.Find("Score").GetComponent<TextMeshProUGUI>();
        Transform inventory = canvas.Find("Inventory");
        Transform goldImage = inventory.Find("Gold").GetChild(0);
        Transform silverImage = inventory.Find("Silver").GetChild(0);
        Transform copperImage = inventory.Find("Copper").GetChild(0);
        goldText = goldImage.Find("qty").GetComponent<TextMeshProUGUI>();
        silverText = silverImage.Find("qty").GetComponent<TextMeshProUGUI>();
        copperText = copperImage.Find("qty").GetComponent<TextMeshProUGUI>();

        goldImage.Find("Panel").Find("inv_taken").GetComponent<TextMeshProUGUI>().text =
            GameData.InvStorageQty[OreType.Gold].ToString();
        silverImage.Find("Panel").Find("inv_taken").GetComponent<TextMeshProUGUI>().text =
            GameData.InvStorageQty[OreType.Silver].ToString();
        copperImage.Find("Panel").Find("inv_taken").GetComponent<TextMeshProUGUI>().text =
            GameData.InvStorageQty[OreType.Copper].ToString();

        // Set callbacks
        GameData.PlayerData.onScoreUpdated += UpdatePlayerScore;
        GameData.MachineData.onScoreUpdated += UpdateMachineScore;

        GameData.PlayerData.inventory[OreType.Gold].onQuantityUpdated += UpdateGold;
        GameData.PlayerData.inventory[OreType.Silver].onQuantityUpdated += UpdateSilver;
        GameData.PlayerData.inventory[OreType.Copper].onQuantityUpdated += UpdateCopper;

        UpdateGold(0);
        UpdateSilver(0);
        UpdateCopper(0);
        UpdatePlayerScore(0);
        UpdateMachineScore(0);

        GameObject[] deposits = GameObject.FindGameObjectsWithTag("Deposit");
        foreach (GameObject deposit in deposits)
        {
            if (deposit.name == "PlayerDeposit")
                deposit.GetComponent<DepositBuilding>().agentData = GameData.PlayerData;
            else
                deposit.GetComponent<DepositBuilding>().agentData = GameData.MachineData;
        }

        SpawnOres();
        StartCoroutine(CountDown());
        // StartCoroutine(DataCollector.Instance.LoopTimestampEvent());
        // DataCollector.Instance.gameActive = true;
    }

    void GameOver()
    {
        DataCollector.Instance.RecordEndOfGameEvent();
        string WinOrLose()
        {
            if (GameData.PlayerData.Score > GameData.MachineData.Score)
            {
                SetGameState(3);
                return "You Win!";
            }
            
            if (GameData.PlayerData.Score < GameData.MachineData.Score)
            {
                SetGameState(4);
                return "You Lose!";
            }

            SetGameState(3);
            return "It's a Draw!";
        }

        Debug.Log("Game over");
        Transform canvas = GameObject.FindWithTag("Canvas").transform;
        canvas.Find("EndText").GetComponent<TextMeshProUGUI>().text = WinOrLose();

        Transform continueButton = canvas.Find("Continue");
        continueButton.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (playTest == 1)
                StartTest2();
            else
                QuitGame();
        });
        continueButton.GetChild(0).GetComponent<TextMeshProUGUI>().text = playTest == 1 ? "CONTINUE" : "QUIT";
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    IEnumerator CountDown()
    {
        GameData.TimeLeft = GameData.InitialTime;

        UpdateTime();

        while (GameData.TimeLeft > 0)
        {
            yield return new WaitForSeconds(1);
            GameData.TimeLeft--;
            UpdateTime();
        }
        
        clockInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        DataCollector.Instance.gameActive = false;
        DataCollector.Instance.StopAllCoroutines();
        clockStarted = false;
        StartCoroutine(LoadGameSceneAsync("EndScene", GameOver));
    }

    void SpawnOres()
    {
        TerrainPopulator terrainPopulator = GameObject.FindGameObjectWithTag("Ground").GetComponent<TerrainPopulator>();
        
        terrainPopulator.SetOreSpawns(10, 5, 20, 75);
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

    void SetGameState(int state)
    {
        backgroundMusicInstance.setParameterByName("GameState", state);
    }
}