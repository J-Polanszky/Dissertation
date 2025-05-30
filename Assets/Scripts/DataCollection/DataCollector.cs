using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        PerformanceLog log = PerformanceLogger.Instance.EndLogging();

        string difficulty = GameData.Difficulty switch
        {
            0 => "Easy",
            1 => "Medium",
            2 => "Hard",
            _ => "Unknown"
        };
        string playerScore = GameData.PlayerData.Score.ToString();
        string opponentScore = GameData.MachineData.Score.ToString();
        
        AgentCollectedData playerCollectedData = GatherData(GameData.PlayerData);
        AgentCollectedData opponentCollectedData = GatherData(GameData.MachineData);

        EndOfGame endOfGame = new EndOfGame(
            difficulty,
            playerScore,
            opponentScore,
            playerCollectedData,
            opponentCollectedData,
            log
        );

        string json = JsonUtility.ToJson(endOfGame);

        Task save = SaveToDisk($"{PlaytestName}", "end_of_game", json);
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

    AgentCollectedData GatherData(AgentData agentData)
    {
        // Player data

        string timeSpentMining = agentData.TimeSpentMining.ToString();
        string timeSpentWTravelling =
            Math.Round((float)(GameData.InitialTime - GameData.TimeLeft) - agentData.TimeSpentMining, 2)
                .ToString();
        string score = agentData.Score.ToString();
        string inventoryUsed = agentData.TotalInventory.ToString();

        int totalScoreOfInventory = 0;

        foreach (var (key, value) in agentData.inventory)
        {
            totalScoreOfInventory += value.Score;
        }

        string scoreOfInventory = totalScoreOfInventory.ToString();

        Debug.Log(
            $"Data: Mining: {timeSpentMining}, Travelling: {timeSpentWTravelling}, Score: {score}, Inv: {inventoryUsed}, InvScore: {scoreOfInventory}");

        AgentCollectedData collectedData = new AgentCollectedData(
            timeSpentMining,
            timeSpentWTravelling,
            score,
            inventoryUsed,
            scoreOfInventory
        );

        return collectedData;
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

        AgentCollectedData playerCollectedData = GatherData(GameData.PlayerData);
        
        AgentCollectedData opponentCollectedData = GatherData(GameData.MachineData);

        TimeStampEvent timestampEvent = new TimeStampEvent(
            difficulty,
            timePassed,
            playerCollectedData,
            opponentCollectedData
        );

        string json = JsonUtility.ToJson(timestampEvent);
        Task save = SaveToDisk($"{PlaytestName}", timestamp, json);
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
            newDifficult
        );


        Debug.Log($"DDA Event: Previous: {previousDifficulty}, New: {newDifficult}");

        string json = JsonUtility.ToJson(ddaEvent);
        Task save = SaveToDisk($"{PlaytestName}/dda", timestamp, json);
        Task send = SendToApi(
            $"{API_URL}/{PlaytestName}/{AuthenticationService.Instance.PlayerInfo.Username}/dda/{timestamp}",
            json);
    }

    private async Task SaveToDisk(string path, string filename, string json)
    {
        // Save a copy to persistent storage
        try
        {
            // Construct the full directory path in persistent storage
            string directoryPath = Path.Combine(Application.persistentDataPath, path);

            // Ensure the directory exists
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Save the JSON data to a file
            string filePath = Path.Combine(directoryPath, filename + ".json");
            File.WriteAllText(filePath, json);

            Debug.Log($"Data saved to persistent storage: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save data to persistent storage: {e.Message}");
        }
    }

    private async Task SendToApi(string url, string json)
    {
        Debug.Log($"Sending data to API: {url}");
        Debug.Log($"Data: {json}");

        // Send data to the API
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
            request.uploadHandler = new UploadHandlerRaw(Array.Empty<byte>());
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