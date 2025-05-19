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
        // Root node (implicitly a selector node that returns the first success)
        return EvaluateDifficultyBehaviorTree(playerData, opponentData);
    }

    int EvaluateDifficultyBehaviorTree(AgentCollectedData playerData, AgentCollectedData opponentData)
    {
        // Parse the data once
        float playerScore = float.Parse(playerData.score);
        float opponentScore = float.Parse(opponentData.score);
        float playerInventoryScore = float.Parse(playerData.scoreOfInventory);
        float opponentInventoryScore = float.Parse(opponentData.scoreOfInventory);
        float playerTimeSpentMining = float.Parse(playerData.timeSpentMining);
        float opponentTimeSpentMining = float.Parse(opponentData.timeSpentMining);
        
        // Calculate metrics
        float scoreRatio = playerScore / opponentScore;
        float inventoryRatio = playerInventoryScore / opponentInventoryScore;
        float timeEfficiencyRatio = playerTimeSpentMining / opponentTimeSpentMining;
        
        // Calculate composite performance
        float overallPerformance = (scoreRatio * 0.5f) + (inventoryRatio * 0.3f) + (timeEfficiencyRatio * 0.2f);
        
        Debug.Log($"Score Ratio: {scoreRatio}, Inventory Ratio: {inventoryRatio}, Time Efficiency Ratio: {timeEfficiencyRatio}");
        Debug.Log($"Overall Performance: {overallPerformance}");
        
        // Behavior Tree Structure:
        
        // First sequence: Check if player is struggling severely (sequence must pass all conditions)
        if (CheckPlayerStrugglingBadly(scoreRatio, inventoryRatio, overallPerformance))
        {
            return ExecuteSetMinimumDifficulty();
        }
        
        // Second sequence: Check if player is struggling
        if (CheckPlayerStrugglingSlightly(scoreRatio, inventoryRatio, overallPerformance))
        {
            return ExecuteDecreaseDifficulty();
        }
        
        // Third sequence: Check if player is doing very well
        if (CheckPlayerDominating(scoreRatio, inventoryRatio, overallPerformance))
        {
            return ExecuteSetMaximumDifficulty();
        }
        
        // Fourth sequence: Check if player is doing well
        if (CheckPlayerWinning(scoreRatio, inventoryRatio, overallPerformance))
        {
            return ExecuteIncreaseDifficulty();
        }
        
        // Fallback action: maintain current difficulty
        return ExecuteMaintainDifficulty();
    }

    // Condition nodes
    bool CheckPlayerStrugglingBadly(float scoreRatio, float inventoryRatio, float overallPerformance)
    {
        return overallPerformance < 0.5f || (scoreRatio < 0.4f && inventoryRatio < 0.4f);
    }

    bool CheckPlayerStrugglingSlightly(float scoreRatio, float inventoryRatio, float overallPerformance)
    {
        return overallPerformance < 0.75f || (scoreRatio < 0.7f && inventoryRatio < 0.7f);
    }
    
    bool CheckPlayerDominating(float scoreRatio, float inventoryRatio, float overallPerformance)
    {
        return overallPerformance > 1.5f || (scoreRatio > 1.5f && inventoryRatio > 1.5f);
    }
    
    bool CheckPlayerWinning(float scoreRatio, float inventoryRatio, float overallPerformance)
    {
        return overallPerformance > 1.25f || (scoreRatio > 1.3f && inventoryRatio > 1.3f);
    }

    // Action nodes
    int ExecuteSetMinimumDifficulty()
    {
        Debug.Log("Behavior Tree Action: Setting minimum difficulty");
        DataCollector.Instance.RecordDDAEvent(GameData.Difficulty, 0);
        return 0; // Easy difficulty
    }
    
    int ExecuteSetMaximumDifficulty()
    {
        Debug.Log("Behavior Tree Action: Setting maximum difficulty");
        DataCollector.Instance.RecordDDAEvent(GameData.Difficulty, 2);
        return 2; // Hard difficulty
    }

    int ExecuteIncreaseDifficulty()
    {
        Debug.Log("Behavior Tree Action: Increasing difficulty");
        DataCollector.Instance.RecordDDAEvent(GameData.Difficulty, Math.Min(GameData.Difficulty + 1, 2));
        return GameData.Difficulty + 1;
    }

    int ExecuteDecreaseDifficulty()
    {
        Debug.Log("Behavior Tree Action: Decreasing difficulty");
        DataCollector.Instance.RecordDDAEvent(GameData.Difficulty, Math.Max(GameData.Difficulty - 1, 0));
        return GameData.Difficulty - 1;
    }

    int ExecuteMaintainDifficulty()
    {
        Debug.Log("Behavior Tree Action: Maintaining current difficulty");
        return GameData.Difficulty;
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