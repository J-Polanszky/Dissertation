using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class ApiResponse
{
    public bool isDDA;
}

public class DataCollector : MonoBehaviour
{
    public static DataCollector Instance { get; private set; }

    // private const string API_URL = "http://localhost:8000";
    private const string API_URL = "https://jp-dissertation.up.railway.app";

    private string playtestName;

    public bool gameActive = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public string PlaytestName
    {
        get => playtestName;
        set => playtestName = value;
    }

    public void RecordEndOfGameEvent()
    {
        string difficulty = GameData.Difficulty switch
        {
            0 => "Easy",
            1 => "Medium",
            2 => "Hard",
            _ => "Unknown"
        };
        string playerScore = GameData.PlayerData.Score.ToString();
        string opponentScore = GameData.MachineData.Score.ToString();

        EndOfGame endOfGame = new EndOfGame(
            difficulty,
            playerScore,
            opponentScore
        );

        string json = JsonUtility.ToJson(endOfGame);

        Task send = SendToApi(
            $"{API_URL}/{PlaytestName}/{AuthenticationService.Instance.PlayerInfo.Username}/end_of_game", json);
    }

    public IEnumerator LoopTimestampEvent()
    {
        // Ensures the program waits for the game to start before starting the loop
        yield return new WaitForSeconds(30);
        while (gameActive)
        {
            RecordTimestampEvent();
            yield return new WaitForSeconds(30);
        }
    }

    public void RecordTimestampEvent()
    {
        string timestamp = (GameData.InitialTime - GameData.TimeLeft).ToString();
        string difficulty = GameData.Difficulty switch
        {
            0 => "Easy",
            1 => "Medium",
            2 => "Hard",
            _ => "Unknown"
        };
        string timePassed = (GameData.InitialTime - GameData.TimeLeft).ToString();

        // Player data

        string playerTimeSpentMining = GameData.PlayerData.TimeSpentMining.ToString();
        string playerTimeSpentWTravelling =
            Math.Round((float)(GameData.InitialTime - GameData.TimeLeft) - GameData.PlayerData.TimeSpentMining, 2)
                .ToString();
        string playerScore = GameData.PlayerData.Score.ToString();
        string playerInventoryUsed = GameData.PlayerData.TotalInventory.ToString();

        int totalScoreOfInventory = 0;

        foreach (var (key, value) in GameData.PlayerData.inventory)
        {
            totalScoreOfInventory += value.Score;
        }

        string playerScoreOfInventory = totalScoreOfInventory.ToString();

        Debug.Log(
            $"Player Data: Mining: {playerTimeSpentMining}, Travelling: {playerTimeSpentWTravelling}, Score: {playerScore}, Inv: {playerInventoryUsed}, InvScore: {playerScoreOfInventory}");

        AgentCollectedData playerCollectedData = new AgentCollectedData(
            playerTimeSpentMining,
            playerTimeSpentWTravelling.ToString(),
            playerScore,
            playerInventoryUsed,
            playerScoreOfInventory
        );

        // Opponent data

        string opponentTimeSpentMining = GameData.MachineData.TimeSpentMining.ToString();
        string opponentTimeSpentWTravelling =
            Math.Round((float)(GameData.InitialTime - GameData.TimeLeft) - GameData.MachineData.TimeSpentMining, 2)
                .ToString();
        string opponentScore = GameData.MachineData.Score.ToString();
        string opponentInventoryUsed = GameData.MachineData.TotalInventory.ToString();

        totalScoreOfInventory = 0;

        foreach (var (key, value) in GameData.MachineData.inventory)
        {
            totalScoreOfInventory += value.Score;
        }

        string opponentScoreOfInventory = totalScoreOfInventory.ToString();

        Debug.Log(
            $"Opponent Data: Mining: {opponentTimeSpentMining}, Travelling: {opponentTimeSpentWTravelling}, Score: {opponentScore}, Inv: {opponentInventoryUsed}, InvScore: {opponentScoreOfInventory}");

        AgentCollectedData opponentCollectedData = new AgentCollectedData(
            opponentTimeSpentMining,
            opponentTimeSpentWTravelling,
            opponentScore,
            opponentInventoryUsed,
            opponentScoreOfInventory
        );

        TimeStampEvent timestampEvent = new TimeStampEvent(
            difficulty,
            timePassed,
            playerCollectedData,
            opponentCollectedData
        );

        string json = JsonUtility.ToJson(timestampEvent);
        Task send = SendToApi(
            $"{API_URL}/{PlaytestName}/{AuthenticationService.Instance.PlayerInfo.Username}/{timestamp}", json);
    }

    public void RecordDDAEvent(int difficulty, int newDifficulty)
    {
        string previousDifficulty = difficulty switch
        {
            0 => "Easy",
            1 => "Medium",
            2 => "Hard",
            _ => "Unknown"
        };

        string newDifficult = newDifficulty switch
        {
            0 => "Easy",
            1 => "Medium",
            2 => "Hard",
            _ => "Unknown"
        };

        string timestamp = (GameData.InitialTime - GameData.TimeLeft).ToString();

        DDAChange ddaEvent = new DDAChange(
            previousDifficulty,
            newDifficult,
            timestamp
        );


        Debug.Log($"DDA Event: Previous: {previousDifficulty}, New: {newDifficult}");

        string json = JsonUtility.ToJson(ddaEvent);
        Task send = SendToApi($"{API_URL}/{PlaytestName}/{AuthenticationService.Instance.PlayerInfo.Username}/dda",
            json);
    }

    private async Task SendToApi(string url, string json)
    {
        Debug.Log($"Sending data to API: {url}");
        Debug.Log($"Data: {json}");

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error sending data to API: {request.error}");
            }
            else
            {
                Debug.Log("Data sent to API successfully!");
            }
        }
    }

    public async Task<bool> GetUserData(string playerID)
    {
        using (UnityWebRequest request = UnityWebRequest.Get($"{API_URL}/users/{playerID}/isdda"))
        {
            request.uploadHandler = new UploadHandlerRaw(new byte[0]);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error retrieving data from API: {request.error}");
            }
            else
            {
                // Return should be a json like this {"isDDA": true}
                string json = request.downloadHandler.text;
                Debug.Log($"Data retrieved from API successfully: {json}");

                // Parse the JSON response
                var response = JsonUtility.FromJson<ApiResponse>(json);
                if (response != null)
                {
                    Debug.Log($"isDDA: {response.isDDA}");
                    return response.isDDA;
                }

                Debug.LogError("Invalid JSON format");
            }
        }

        Debug.LogError("Failed to retrieve data from API");
        // Wait 5 seconds before retrying
        await Task.Delay(5000);
        // Retry the request
        return await GetUserData(playerID);
    }
}