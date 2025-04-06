using System;
using System.Collections;
using Unity.Sentis;
using UnityEngine;

public class DDA_System : MonoBehaviour
{
    public static DDA_System Instance { get; private set; }

    private RLAgent rlAgent;
    ModelAsset easyModel, mediumModel, hardModel;
    
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        rlAgent = GameObject.FindWithTag("Agent").GetComponent<RLAgent>();
        
        easyModel = Resources.Load<ModelAsset>("Agent/V3/Easy/Easy");
        mediumModel = Resources.Load<ModelAsset>("Agent/V3/Medium/Medium");
        hardModel = Resources.Load<ModelAsset>("Agent/V3/Hard/Hard");
        
        // Start the DDA system
        StartCoroutine(StartDDA());
    }

    IEnumerator StartDDA()
    {
        // Makes sure dda only acts once enough time has passed
        yield return new WaitForSeconds(30f);
        while (true)
        {
            if(GameData.TimeLeft <= 0)
                break;
            else
                DDA();
            
            yield return new WaitForSeconds(15f);
        }
    }

    // Make this a static common function
    AgentCollectedData GetData(AgentData agentData)
    {
        string timeSpentMining = agentData.TimeSpentMining.ToString();
        string timeSpentWTravelling =
            Math.Round((float) (GameData.InitialTime - GameData.TimeLeft) - agentData.TimeSpentMining, 2).ToString();
        string dcore = agentData.Score.ToString();
        string inventoryUsed = agentData.TotalInventory.ToString();
        
        int totalScoreOfInventory = 0;
        
        foreach (var (key, value) in agentData.inventory)
        {
            totalScoreOfInventory += value.Score;
        }
        
        string scoreOfInventory = totalScoreOfInventory.ToString();
        
        AgentCollectedData collectedData = new AgentCollectedData(
            timeSpentMining,
            timeSpentWTravelling.ToString(),
            dcore,
            inventoryUsed,
            scoreOfInventory
        );

        return collectedData;
    }

    int MakeDecision(AgentCollectedData playerData, AgentCollectedData opponentData)
    {
        //TODO: Analyse every part of player and agent data, and calculate if difficulty is within acceptable limits.
        
        // Calculate performance metrics to see how well the player is doing compared to the opponent
        float scoreRatio = float.Parse(playerData.score) / Mathf.Max(float.Parse(opponentData.score), 1);
        float inventoryRatio = float.Parse(playerData.scoreOfInventory) / Mathf.Max(float.Parse(opponentData.scoreOfInventory), 1);
        float timeEfficiencyRatio = float.Parse(playerData.timeSpentMining) / Mathf.Max(float.Parse(opponentData.timeSpentMining), 1);

        // Define thresholds for difficulty adjustment
        // float upperScoreThreshold = 1.3f; // Player is outperforming the opponent by 30%
        // float lowerScoreThreshold = 0.7f; // Player is underperforming the opponent by 30%

        // float upperInventoryThreshold = 1.3f; // Player is outperforming the opponent by 30%
        // float lowerInventoryThreshold = 0.7f; // Player is underperforming the opponent by 30%
        
        // Calculate overall performance based on the ratios
        // Adjust weights as needed
        float overallPerformance = (scoreRatio * 0.5f) + (inventoryRatio * 0.3f) + (timeEfficiencyRatio * 0.2f);

        Debug.Log($"Score Ratio: {scoreRatio}, Inventory Ratio: {inventoryRatio}, Time Efficiency Ratio: {timeEfficiencyRatio}");
        Debug.Log($"Overall Performance: {overallPerformance}");

        if (overallPerformance > 1.25f)
            return GameData.Difficulty + 1; // Increase difficulty

        if (overallPerformance < 0.5f)
            return 0; // Set difficulty to minimum

        if (overallPerformance < 0.75f)
            return GameData.Difficulty - 1; // Decrease difficulty
        
        return GameData.Difficulty; // Keep difficulty the same
    }

    void AdjustDifficulty(int difficulty)
    {
        if (GameData.Difficulty == difficulty)
            return; // No change in difficulty
            
        difficulty = Mathf.Clamp(difficulty, 0, 2); // Ensure difficulty is within valid range

        GameData.Difficulty = difficulty;

        switch (difficulty)
        {
            case 0:
                rlAgent.ChangeModel(easyModel);
                break;
            
            case 1:
                rlAgent.ChangeModel(mediumModel);
                break;
            
            case 2:
                rlAgent.ChangeModel(hardModel);
                break;
            
            default:
                throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, null);
        }
    }
    
    void DDA()
    {
        AgentCollectedData playerData = GetData(GameData.PlayerData);
        AgentCollectedData opponentData = GetData(GameData.MachineData);
        
        int difficulty = MakeDecision(playerData, opponentData);
        
        AdjustDifficulty(difficulty);
    }
    

}