using UnityEngine;
using System.Collections.Generic;
using SpinMotion;

[System.Serializable]
public class AIChromosome
{
    public float speedControlWeight = 1f;
    public float steeringSensitivity = 1f;
    public float brakingAggression = 1f;
    public float cautiousnessFactor = 1f;
    public float lateralWanderAmount = 1f;
    public float accelerationWander = 1f;
    public void AddMutationNoise(float noiseAmount = 0.05f)
    {
        speedControlWeight += Random.Range(-noiseAmount, noiseAmount);
        steeringSensitivity += Random.Range(-noiseAmount, noiseAmount);
        brakingAggression += Random.Range(-noiseAmount, noiseAmount);
        cautiousnessFactor += Random.Range(-noiseAmount, noiseAmount);
        lateralWanderAmount += Random.Range(-noiseAmount, noiseAmount);
        accelerationWander += Random.Range(-noiseAmount, noiseAmount);
        ClampValues();
    }
    private void ClampValues()
    {
        speedControlWeight = Mathf.Clamp(speedControlWeight, 0.1f, 3f);
        steeringSensitivity = Mathf.Clamp(steeringSensitivity, 0.1f, 3f);
        brakingAggression = Mathf.Clamp(brakingAggression, 0.1f, 3f);
        cautiousnessFactor = Mathf.Clamp(cautiousnessFactor, 0.1f, 3f);
        lateralWanderAmount = Mathf.Clamp(lateralWanderAmount, 0.1f, 3f);
        accelerationWander = Mathf.Clamp(accelerationWander, 0.1f, 3f);
    }
}
public class AIAdaptiveSystem : MonoBehaviour
{
    private CrossCorrelationSystem correlationSystem;
    private CarAIControl aiCarControl;
    private RealTimeRacePositions racePositions;
    private AIChromosome winningChromosome;
    private AIChromosome losingChromosome;
    private AIChromosome currentChromosome;

    private float mutationNoise = 0.05f;
    private float updateInterval = 2.0f;

    private int playerIndex = 0;
    private int aiIndex = 1;

    private float nextUpdateTime = 0f;
    private List<double> playerScoreHistory = new List<double>();
    private List<double> aiScoreHistory = new List<double>();
    void Start()
    {
        FindAllComponents();
        CreateChromosomes();
        ApplyCurrentChromosome();
    }

    void FindAllComponents()
    {
        correlationSystem = FindObjectOfType<CrossCorrelationSystem>();
        if (correlationSystem == null)
            Debug.LogWarning("CrossCorrelationSystem not found!");
        racePositions = FindObjectOfType<RealTimeRacePositions>();
        if (racePositions == null)
            Debug.LogWarning("RealTimeRacePositions not found!");
        aiCarControl = GetComponent<CarAIControl>();

        if (aiCarControl == null)
            Debug.LogError("CarAIControl not found!");
    }

    void CreateChromosomes()
    {
        winningChromosome = new AIChromosome();
        winningChromosome.speedControlWeight = 1.3f;
        winningChromosome.steeringSensitivity = 1.2f;
        winningChromosome.brakingAggression = 1.4f;
        winningChromosome.cautiousnessFactor = 0.7f;
        winningChromosome.lateralWanderAmount = 0.6f;
        winningChromosome.accelerationWander = 0.5f;

        losingChromosome = new AIChromosome();
        losingChromosome.speedControlWeight = 0.7f;
        losingChromosome.steeringSensitivity = 0.9f;
        losingChromosome.brakingAggression = 0.6f;
        losingChromosome.cautiousnessFactor = 1.3f;
        losingChromosome.lateralWanderAmount = 1.4f;
        losingChromosome.accelerationWander = 1.5f;
        currentChromosome = winningChromosome;
    }
    void Update()
    {
        if (Time.time < nextUpdateTime) return;
        UpdateRaceData();
        EvaluateRaceSegment();
        SelectAppropriateChromosome();
        ApplyCurrentChromosome();
        nextUpdateTime = Time.time + updateInterval;
    }

    void UpdateRaceData()
    {
        if (racePositions != null &&
            racePositions.RacePositionTotalScores.Count > Mathf.Max(playerIndex, aiIndex))
        {
            playerScoreHistory.Add(racePositions.RacePositionTotalScores[playerIndex]);
            aiScoreHistory.Add(racePositions.RacePositionTotalScores[aiIndex]);
            if (playerScoreHistory.Count > 50)
            {
                playerScoreHistory.RemoveAt(0);
                aiScoreHistory.RemoveAt(0);
            }
        }
    }

    void EvaluateRaceSegment()
    {
        if (playerScoreHistory.Count < 2) return;

        double lastPlayerScore = playerScoreHistory[playerScoreHistory.Count - 1];
        double prevPlayerScore = playerScoreHistory[playerScoreHistory.Count - 2];

        double lastAiScore = aiScoreHistory[aiScoreHistory.Count - 1];
        double prevAiScore = aiScoreHistory[aiScoreHistory.Count - 2];
        double playerGain = lastPlayerScore - prevPlayerScore;
        double aiGain = lastAiScore - prevAiScore;

        if (aiGain > playerGain)
        {
            UpdateWinningChromosome();
        }
        else
        {
            UpdateLosingChromosome();
        }
    }

    void UpdateWinningChromosome()
    {
        float correlation = correlationSystem != null ?
            correlationSystem.GetOverallCorrelation() : 0.5f;
        if (correlation < 0.3f)
        {
            winningChromosome.speedControlWeight *= 0.95f;
            winningChromosome.brakingAggression *= 0.9f;
            winningChromosome.cautiousnessFactor *= 1.1f;
            winningChromosome.lateralWanderAmount *= 1.05f;
        }
        else
        {
            winningChromosome.speedControlWeight *= 1.05f;
            winningChromosome.steeringSensitivity *= 1.03f;
        }
        winningChromosome.AddMutationNoise(mutationNoise);
    }

    void UpdateLosingChromosome()
    {
        float correlation = correlationSystem != null ?
            correlationSystem.GetOverallCorrelation() : 0.5f;
        if (correlation > 0.7f)
        {
            losingChromosome.speedControlWeight *= 1.05f;
            losingChromosome.brakingAggression *= 1.03f;
        }
        else
        {
            losingChromosome.speedControlWeight *= 0.9f;
            losingChromosome.brakingAggression *= 0.85f;
            losingChromosome.cautiousnessFactor *= 1.15f;
            losingChromosome.lateralWanderAmount *= 1.1f;
        }

        losingChromosome.AddMutationNoise(mutationNoise);
    }

    void SelectAppropriateChromosome()
    {
        int playerPos = GetRacePosition(playerIndex);
        int aiPos = GetRacePosition(aiIndex);
        float correlation = correlationSystem != null ?
            correlationSystem.GetOverallCorrelation() : 0.5f;

        if (playerPos > aiPos)
            currentChromosome = losingChromosome;
        else if (playerPos < aiPos)
            currentChromosome = winningChromosome;
        else
            currentChromosome = (correlation < 0.3f) ? losingChromosome : winningChromosome;

    }

    int GetRacePosition(int carIndex)
    {
        if (racePositions == null || carIndex >= racePositions.CarCheckpointTrackers.Count)
            return 1;

        return racePositions.GetPlayerRacePosition(carIndex);
    }

    void ApplyCurrentChromosome()
    {

        if (aiCarControl == null) return;
        System.Type aiType = typeof(CarAIControl);

        try
        {
            var steerField = aiType.GetField("m_SteerSensitivity",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (steerField != null)
                steerField.SetValue(aiCarControl, 0.05f * currentChromosome.steeringSensitivity);
            var accelField = aiType.GetField("m_AccelSensitivity",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (accelField != null)
                accelField.SetValue(aiCarControl, 0.04f * currentChromosome.speedControlWeight);
            var brakeField = aiType.GetField("m_BrakeSensitivity",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (brakeField != null)
                brakeField.SetValue(aiCarControl, 1f * currentChromosome.brakingAggression);
            var cautiousField = aiType.GetField("m_CautiousSpeedFactor",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (cautiousField != null)
                cautiousField.SetValue(aiCarControl, 0.05f * currentChromosome.cautiousnessFactor);
            var wanderField = aiType.GetField("m_LateralWanderDistance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (wanderField != null)
                wanderField.SetValue(aiCarControl, 3f * currentChromosome.lateralWanderAmount);
            var accelWanderField = aiType.GetField("m_AccelWanderAmount",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (accelWanderField != null)
                accelWanderField.SetValue(aiCarControl, 0.1f * currentChromosome.accelerationWander);

        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Couldn't apply the chromosome to AI: {e.Message}");
        }
    }

    public string GetActiveChromosomeType()
    {
        return currentChromosome == winningChromosome ? "WINNING" : "LOSING";
    }

    public void ResetSystem()
    {
        playerScoreHistory.Clear();
        aiScoreHistory.Clear();
        CreateChromosomes();
    }
}